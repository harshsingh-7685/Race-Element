using RaceElement.Data.Common.SimulatorData;
using RaceElement.Data.Common.SimulatorData.LocalCar;
using RaceElement.Data.Games.EASportsWRC.Diagnostics;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace RaceElement.Data.Games.EASportsWRC;

public sealed class EAWrcTelemetryFreshness
{
    public static readonly TimeSpan StaleAfter = TimeSpan.FromMilliseconds(500);
    private long _lastReceiveTimestamp = long.MinValue;

    public void RecordPacket(long receiveTimestamp) =>
        Volatile.Write(ref _lastReceiveTimestamp, receiveTimestamp);

    public bool IsFresh(long currentTimestamp)
    {
        long lastReceiveTimestamp = Volatile.Read(ref _lastReceiveTimestamp);
        return lastReceiveTimestamp != long.MinValue &&
               Stopwatch.GetElapsedTime(lastReceiveTimestamp, currentTimestamp) <= StaleAfter;
    }

    public void Clear() => Volatile.Write(ref _lastReceiveTimestamp, long.MinValue);
}

internal sealed class EAWrcDataProvider : AbstractSimDataProvider
{
    private const int DefaultPort = 20877;
    private readonly Lock _packetLock = new();
    private CancellationTokenSource? _cancellation;
    private UdpClient? _udpClient;
    private Task? _receiverTask;
    private EAWrcDiagnosticPacket? _latestPacket;
    private readonly EAWrcTelemetryFreshness _freshness = new();

    internal override void Start()
    {
        if (_receiverTask is not null)
            return;

        int port = DefaultPort;
        GamePortSettings settings = new();
        if (settings.Get().GamePorts.TryGetValue(Game.EASportsWRC, out int configuredPort) && configuredPort is > 0 and <= 65535)
            port = configuredPort;

        try
        {
            _cancellation = new CancellationTokenSource();
            _udpClient = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));
            _receiverTask = Task.Run(() => ReceiveAsync(_cancellation.Token));
            Debug.WriteLine($"EA SPORTS WRC telemetry listening on 127.0.0.1:{port}");
        }
        catch (Exception exception) when (exception is SocketException or IOException)
        {
            Debug.WriteLine($"EA SPORTS WRC telemetry failed to bind: {exception.Message}");
            _udpClient?.Dispose();
            _udpClient = null;
            _cancellation?.Dispose();
            _cancellation = null;
        }
    }

    internal override int PollingRate() => 60;

    internal override void Stop()
    {
        _cancellation?.Cancel();
        _udpClient?.Dispose();

        try
        {
            _receiverTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException exception)
        {
            Exception? unexpected = exception.Flatten().InnerExceptions.FirstOrDefault(
                inner => inner is not OperationCanceledException and not ObjectDisposedException);
            if (unexpected is not null)
                Debug.WriteLine($"EA SPORTS WRC telemetry shutdown error: {unexpected.Message}");
        }

        _receiverTask = null;
        _udpClient = null;
        _cancellation?.Dispose();
        _cancellation = null;
        lock (_packetLock)
        {
            _latestPacket = null;
            _freshness.Clear();
        }
    }

    public override void Update(ref LocalCarData localCar, ref SessionData sessionData, ref GameData gameData)
    {
        EAWrcDiagnosticPacket? packet;
        lock (_packetLock)
            packet = _freshness.IsFresh(Stopwatch.GetTimestamp()) ? _latestPacket : null;

        gameData.Name = Game.EASportsWRC.ToShortName();
        if (packet is null)
        {
            NeutralizeStaleTelemetry(ref localCar, ref gameData);
            return;
        }

        EAWrcProductionSlipResult slip = EAWrcProductionMath.Calculate(packet);

        localCar.Inputs.Throttle = packet.VehicleThrottle;
        localCar.Inputs.Brake = packet.VehicleBrake;
        localCar.Inputs.HandBrake = packet.VehicleHandbrake;
        localCar.Inputs.Steering = packet.VehicleSteering;
        localCar.Physics.Velocity = packet.VehicleSpeed * 3.6f;
        localCar.Tyres.SlipRatio = slip.AbsoluteRaceElementOrder();
        localCar.Engine.IsRunning = true;

        gameData.IsGamePaused = false;
        gameData.IsRunning = true;
    }

    public override List<string> GetCarClasses() => ["Rally"];

    public override bool HasTelemetry() => _freshness.IsFresh(Stopwatch.GetTimestamp());

    private async Task ReceiveAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                UdpReceiveResult received = await _udpClient!.ReceiveAsync(cancellationToken);
                if (!EAWrcDiagnosticPacketParser.TryParse(received.Buffer, out EAWrcDiagnosticPacket? packet, out _))
                    continue;

                lock (_packetLock)
                {
                    _latestPacket = packet;
                    _freshness.RecordPacket(Stopwatch.GetTimestamp());
                }
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
            Debug.WriteLine($"EA SPORTS WRC telemetry receive error: {exception.Message}");
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"EA SPORTS WRC telemetry stopped unexpectedly: {exception.Message}");
        }
        finally
        {
            _freshness.Clear();
        }
    }

    private static void NeutralizeStaleTelemetry(ref LocalCarData localCar, ref GameData gameData)
    {
        localCar.Inputs.Throttle = 0;
        localCar.Inputs.Brake = 0;
        localCar.Inputs.HandBrake = 0;
        localCar.Inputs.Steering = 0;
        localCar.Tyres.SlipRatio = [0, 0, 0, 0];
        localCar.Engine.IsRunning = false;
        gameData.IsGamePaused = true;
        gameData.IsRunning = false;
    }
}
