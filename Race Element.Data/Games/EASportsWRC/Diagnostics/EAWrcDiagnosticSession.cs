using RaceElement.Core.Settings;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RaceElement.Data.Games.EASportsWRC.Diagnostics;

public enum EAWrcSequenceClassification
{
    First,
    InOrder,
    Gap,
    Duplicate,
    Backward,
    SuspectedReset,
    UInt64Wrap,
}

public sealed record EAWrcSequenceObservation(
    EAWrcSequenceClassification Classification,
    ulong? PreviousPacketUid,
    ulong? ForwardUidDelta,
    ulong? MissingPacketCount);

public sealed class EAWrcPacketSequenceTracker
{
    private const ulong HalfRange = 1UL << 63;
    private EAWrcDiagnosticPacket? _previous;

    public EAWrcSequenceObservation Observe(EAWrcDiagnosticPacket packet)
    {
        EAWrcDiagnosticPacket? previous = _previous;
        _previous = packet;

        if (previous is null)
            return new(EAWrcSequenceClassification.First, null, null, null);

        if (packet.PacketUid == previous.PacketUid)
            return new(EAWrcSequenceClassification.Duplicate, previous.PacketUid, 0, 0);

        if (packet.PacketUid < previous.PacketUid &&
            packet.GameFrameCount < previous.GameFrameCount &&
            packet.GameTotalTime < previous.GameTotalTime)
        {
            return new(EAWrcSequenceClassification.SuspectedReset, previous.PacketUid, null, null);
        }

        ulong forwardDelta = unchecked(packet.PacketUid - previous.PacketUid);
        if (forwardDelta < HalfRange)
        {
            ulong missing = forwardDelta > 1 ? forwardDelta - 1 : 0;
            EAWrcSequenceClassification classification = packet.PacketUid < previous.PacketUid
                ? EAWrcSequenceClassification.UInt64Wrap
                : forwardDelta == 1
                    ? EAWrcSequenceClassification.InOrder
                    : EAWrcSequenceClassification.Gap;

            return new(classification, previous.PacketUid, forwardDelta, missing);
        }

        return new(EAWrcSequenceClassification.Backward, previous.PacketUid, null, null);
    }
}

public sealed class EAWrcDiagnosticSession : IDisposable
{
    public const int Port = 20877;

    private const int RowsPerFlush = 60;
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private readonly Action<string> _log;
    private readonly EAWrcPacketSequenceTracker _sequenceTracker = new();
    private readonly Stopwatch _receiveClock = new();
    private readonly CancellationTokenSource _cancellation = new();
    private UdpClient? _udpClient;
    private Task? _receiveTask;
    private StreamWriter? _writer;
    private string? _csvPath;
    private bool _csvFailed;
    private int _rowsSinceFlush;
    private long _validPackets;
    private long _wrongLengthPackets;
    private long _wrongFourCcPackets;
    private long _socketErrors;
    private long _ioErrors;
    private bool _disposed;

    public EAWrcDiagnosticSession(Action<string> log)
    {
        _log = log;
    }

    public bool Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_receiveTask is not null)
            return true;

        try
        {
            _udpClient = new UdpClient(new IPEndPoint(IPAddress.Loopback, Port));
            _receiveClock.Start();
            _receiveTask = Task.Run(() => ReceiveLoopAsync(_cancellation.Token));
            _log($"EA WRC diagnostics: listening on 127.0.0.1:{Port}. CSV will be created on the first valid sesu packet.");
            return true;
        }
        catch (Exception exception) when (exception is SocketException or IOException)
        {
            _socketErrors++;
            _log($"EA WRC diagnostics: unable to bind 127.0.0.1:{Port}: {exception.Message}");
            _udpClient?.Dispose();
            _udpClient = null;
            return false;
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                UdpReceiveResult received = await _udpClient!.ReceiveAsync(cancellationToken);
                DateTimeOffset localTimestamp = DateTimeOffset.Now;
                double receiveElapsedMilliseconds = _receiveClock.Elapsed.TotalMilliseconds;

                if (!EAWrcDiagnosticPacketParser.TryParse(received.Buffer, out EAWrcDiagnosticPacket? packet, out EAWrcPacketParseError error))
                {
                    if (error == EAWrcPacketParseError.WrongLength)
                        _wrongLengthPackets++;
                    else if (error == EAWrcPacketParseError.WrongFourCc)
                        _wrongFourCcPackets++;
                    continue;
                }

                _validPackets++;
                EAWrcSequenceObservation sequence = _sequenceTracker.Observe(packet!);
                EAWrcDiagnosticMathResult math = EAWrcDiagnosticMath.Calculate(packet!);
                WriteRow(packet!, sequence, math, localTimestamp, receiveElapsedMilliseconds);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (SocketException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _socketErrors++;
            _log($"EA WRC diagnostics: UDP receive stopped: {exception.Message}");
        }
        catch (Exception exception)
        {
            _socketErrors++;
            _log($"EA WRC diagnostics: receiver stopped after an unexpected error: {exception.Message}");
        }
        finally
        {
            FlushAndCloseWriter();
        }
    }

    private void WriteRow(
        EAWrcDiagnosticPacket packet,
        EAWrcSequenceObservation sequence,
        EAWrcDiagnosticMathResult math,
        DateTimeOffset localTimestamp,
        double receiveElapsedMilliseconds)
    {
        if (_csvFailed)
            return;

        try
        {
            EnsureWriter();
            string[] values =
            [
                localTimestamp.ToString("O", Invariant),
                Format(receiveElapsedMilliseconds),
                "sesu",
                packet.PacketUid.ToString(Invariant),
                packet.GameTotalTime.ToString("R", Invariant),
                packet.GameDeltaTime.ToString("R", Invariant),
                packet.GameFrameCount.ToString(Invariant),
                sequence.Classification.ToString(),
                Format(sequence.PreviousPacketUid),
                Format(sequence.ForwardUidDelta),
                Format(sequence.MissingPacketCount),
                packet.VehicleSpeed.ToString("R", Invariant),
                packet.VehicleTransmissionSpeed.ToString("R", Invariant),
                packet.VehicleVelocityX.ToString("R", Invariant),
                packet.VehicleVelocityY.ToString("R", Invariant),
                packet.VehicleVelocityZ.ToString("R", Invariant),
                packet.VehicleForwardDirectionX.ToString("R", Invariant),
                packet.VehicleForwardDirectionY.ToString("R", Invariant),
                packet.VehicleForwardDirectionZ.ToString("R", Invariant),
                packet.VehicleHubPositionBackLeft.ToString("R", Invariant),
                packet.VehicleHubPositionBackRight.ToString("R", Invariant),
                packet.VehicleHubPositionFrontLeft.ToString("R", Invariant),
                packet.VehicleHubPositionFrontRight.ToString("R", Invariant),
                packet.VehicleHubVelocityBackLeft.ToString("R", Invariant),
                packet.VehicleHubVelocityBackRight.ToString("R", Invariant),
                packet.VehicleHubVelocityFrontLeft.ToString("R", Invariant),
                packet.VehicleHubVelocityFrontRight.ToString("R", Invariant),
                packet.VehicleContactPatchForwardSpeedBackLeft.ToString("R", Invariant),
                packet.VehicleContactPatchForwardSpeedBackRight.ToString("R", Invariant),
                packet.VehicleContactPatchForwardSpeedFrontLeft.ToString("R", Invariant),
                packet.VehicleContactPatchForwardSpeedFrontRight.ToString("R", Invariant),
                packet.VehicleThrottle.ToString("R", Invariant),
                packet.VehicleBrake.ToString("R", Invariant),
                packet.VehicleClutch.ToString("R", Invariant),
                packet.VehicleSteering.ToString("R", Invariant),
                packet.VehicleHandbrake.ToString("R", Invariant),
                packet.StageCurrentTime.ToString("R", Invariant),
                packet.StageCurrentDistance.ToString("R", Invariant),
                Format(math.LongitudinalVelocity),
                Format(math.SpeedDifferenceFrontLeft),
                Format(math.SpeedDifferenceFrontRight),
                Format(math.SpeedDifferenceRearLeft),
                Format(math.SpeedDifferenceRearRight),
                Format(math.ProvisionalSignedSlipFrontLeft),
                Format(math.ProvisionalSignedSlipFrontRight),
                Format(math.ProvisionalSignedSlipRearLeft),
                Format(math.ProvisionalSignedSlipRearRight),
                math.SlipValid.ToString(Invariant),
                math.LowSpeed.ToString(Invariant),
            ];

            _writer!.WriteLine(string.Join(',', values));
            _rowsSinceFlush++;
            if (_rowsSinceFlush >= RowsPerFlush)
            {
                _writer.Flush();
                _rowsSinceFlush = 0;
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _ioErrors++;
            _csvFailed = true;
            _log($"EA WRC diagnostics: CSV logging disabled after an I/O error: {exception.Message}");
            FlushAndCloseWriter();
        }
    }

    private void EnsureWriter()
    {
        if (_writer is not null)
            return;

        string directory = Path.Combine(FileUtil.RaceElementDataPath, "EA WRC Diagnostics");
        Directory.CreateDirectory(directory);
        string timestamp = DateTimeOffset.Now.ToUniversalTime().ToString("yyyyMMdd'T'HHmmssfff'Z'", Invariant);
        _csvPath = Path.Combine(directory, $"ea-wrc-telemetry-{timestamp}.csv");
        _writer = new StreamWriter(new FileStream(_csvPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read), new UTF8Encoding(false));
        _writer.WriteLine(CsvHeader);
        _writer.Flush();
        _log($"EA WRC diagnostics: writing CSV to {_csvPath}");
    }

    private void FlushAndCloseWriter()
    {
        if (_writer is null)
            return;

        StreamWriter writer = _writer;
        _writer = null;

        try
        {
            writer.Flush();
        }
        catch (IOException)
        {
            _ioErrors++;
        }

        try
        {
            writer.Dispose();
        }
        catch (IOException)
        {
            _ioErrors++;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _cancellation.Cancel();
        _udpClient?.Dispose();
        try
        {
            bool stopped = _receiveTask?.Wait(TimeSpan.FromSeconds(2)) ?? true;
            if (!stopped)
                _log("EA WRC diagnostic receiver did not stop within two seconds.");
        }
        catch (AggregateException exception)
        {
            Exception unexpected = exception.Flatten().InnerExceptions.FirstOrDefault(
                inner => inner is not OperationCanceledException and not ObjectDisposedException)!;
            if (unexpected is not null)
                _log($"EA WRC diagnostics: receiver shutdown error: {unexpected.Message}");
        }

        _receiveClock.Stop();
        _cancellation.Dispose();
        _log($"EA WRC diagnostics stopped. Valid={_validPackets}, wrong-size={_wrongLengthPackets}, wrong-FourCC={_wrongFourCcPackets}, socket-errors={_socketErrors}, I/O-errors={_ioErrors}, CSV={_csvPath ?? "not created"}.");
    }

    private static string Format(double? value) => value?.ToString("R", Invariant) ?? string.Empty;
    private static string Format(ulong? value) => value?.ToString(Invariant) ?? string.Empty;

    private const string CsvHeader =
        "local_timestamp,receive_elapsed_ms,packet_4cc,packet_uid,game_total_time_s,game_delta_time_s,game_frame_count," +
        "sequence_classification,previous_packet_uid,uid_forward_delta,uid_missing_count," +
        "vehicle_speed_mps,vehicle_transmission_speed_mps,vehicle_velocity_x_mps,vehicle_velocity_y_mps,vehicle_velocity_z_mps," +
        "vehicle_forward_direction_x,vehicle_forward_direction_y,vehicle_forward_direction_z," +
        "vehicle_hub_position_bl_m,vehicle_hub_position_br_m,vehicle_hub_position_fl_m,vehicle_hub_position_fr_m," +
        "vehicle_hub_velocity_bl_mps,vehicle_hub_velocity_br_mps,vehicle_hub_velocity_fl_mps,vehicle_hub_velocity_fr_mps," +
        "vehicle_cp_forward_speed_bl_mps,vehicle_cp_forward_speed_br_mps,vehicle_cp_forward_speed_fl_mps,vehicle_cp_forward_speed_fr_mps," +
        "vehicle_throttle,vehicle_brake,vehicle_clutch,vehicle_steering,vehicle_handbrake,stage_current_time_s,stage_current_distance_m," +
        "calculated_longitudinal_velocity_mps,speed_difference_fl_mps,speed_difference_fr_mps,speed_difference_rl_mps,speed_difference_rr_mps," +
        "provisional_signed_slip_fl,provisional_signed_slip_fr,provisional_signed_slip_rl,provisional_signed_slip_rr,slip_valid,low_speed";
}
