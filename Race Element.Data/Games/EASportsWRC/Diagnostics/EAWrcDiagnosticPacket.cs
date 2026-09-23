using System.Buffers.Binary;

namespace RaceElement.Data.Games.EASportsWRC.Diagnostics;

public enum EAWrcPacketParseError
{
    None,
    WrongLength,
    WrongFourCc,
}

public sealed class EAWrcDiagnosticPacket
{
    public const int Size = 140;

    public required ulong PacketUid { get; init; }
    public required float GameTotalTime { get; init; }
    public required float GameDeltaTime { get; init; }
    public required ulong GameFrameCount { get; init; }
    public required float VehicleSpeed { get; init; }
    public required float VehicleTransmissionSpeed { get; init; }
    public required float VehicleVelocityX { get; init; }
    public required float VehicleVelocityY { get; init; }
    public required float VehicleVelocityZ { get; init; }
    public required float VehicleForwardDirectionX { get; init; }
    public required float VehicleForwardDirectionY { get; init; }
    public required float VehicleForwardDirectionZ { get; init; }
    public required float VehicleHubPositionBackLeft { get; init; }
    public required float VehicleHubPositionBackRight { get; init; }
    public required float VehicleHubPositionFrontLeft { get; init; }
    public required float VehicleHubPositionFrontRight { get; init; }
    public required float VehicleHubVelocityBackLeft { get; init; }
    public required float VehicleHubVelocityBackRight { get; init; }
    public required float VehicleHubVelocityFrontLeft { get; init; }
    public required float VehicleHubVelocityFrontRight { get; init; }
    public required float VehicleContactPatchForwardSpeedBackLeft { get; init; }
    public required float VehicleContactPatchForwardSpeedBackRight { get; init; }
    public required float VehicleContactPatchForwardSpeedFrontLeft { get; init; }
    public required float VehicleContactPatchForwardSpeedFrontRight { get; init; }
    public required float VehicleThrottle { get; init; }
    public required float VehicleBrake { get; init; }
    public required float VehicleClutch { get; init; }
    public required float VehicleSteering { get; init; }
    public required float VehicleHandbrake { get; init; }
    public required float StageCurrentTime { get; init; }
    public required double StageCurrentDistance { get; init; }

    public float[] ContactPatchForwardSpeedsRaceElementOrder() =>
    [
        VehicleContactPatchForwardSpeedFrontLeft,
        VehicleContactPatchForwardSpeedFrontRight,
        VehicleContactPatchForwardSpeedBackLeft,
        VehicleContactPatchForwardSpeedBackRight,
    ];
}

public static class EAWrcDiagnosticPacketParser
{
    private static readonly byte[] SessionUpdateFourCc = "sesu"u8.ToArray();

    public static bool TryParse(ReadOnlySpan<byte> bytes, out EAWrcDiagnosticPacket? packet, out EAWrcPacketParseError error)
    {
        packet = null;

        if (bytes.Length != EAWrcDiagnosticPacket.Size)
        {
            error = EAWrcPacketParseError.WrongLength;
            return false;
        }

        if (!bytes[..4].SequenceEqual(SessionUpdateFourCc))
        {
            error = EAWrcPacketParseError.WrongFourCc;
            return false;
        }

        packet = new EAWrcDiagnosticPacket
        {
            PacketUid = ReadUInt64(bytes, 4),
            GameTotalTime = ReadSingle(bytes, 12),
            GameDeltaTime = ReadSingle(bytes, 16),
            GameFrameCount = ReadUInt64(bytes, 20),
            VehicleSpeed = ReadSingle(bytes, 28),
            VehicleTransmissionSpeed = ReadSingle(bytes, 32),
            VehicleVelocityX = ReadSingle(bytes, 36),
            VehicleVelocityY = ReadSingle(bytes, 40),
            VehicleVelocityZ = ReadSingle(bytes, 44),
            VehicleForwardDirectionX = ReadSingle(bytes, 48),
            VehicleForwardDirectionY = ReadSingle(bytes, 52),
            VehicleForwardDirectionZ = ReadSingle(bytes, 56),
            VehicleHubPositionBackLeft = ReadSingle(bytes, 60),
            VehicleHubPositionBackRight = ReadSingle(bytes, 64),
            VehicleHubPositionFrontLeft = ReadSingle(bytes, 68),
            VehicleHubPositionFrontRight = ReadSingle(bytes, 72),
            VehicleHubVelocityBackLeft = ReadSingle(bytes, 76),
            VehicleHubVelocityBackRight = ReadSingle(bytes, 80),
            VehicleHubVelocityFrontLeft = ReadSingle(bytes, 84),
            VehicleHubVelocityFrontRight = ReadSingle(bytes, 88),
            VehicleContactPatchForwardSpeedBackLeft = ReadSingle(bytes, 92),
            VehicleContactPatchForwardSpeedBackRight = ReadSingle(bytes, 96),
            VehicleContactPatchForwardSpeedFrontLeft = ReadSingle(bytes, 100),
            VehicleContactPatchForwardSpeedFrontRight = ReadSingle(bytes, 104),
            VehicleThrottle = ReadSingle(bytes, 108),
            VehicleBrake = ReadSingle(bytes, 112),
            VehicleClutch = ReadSingle(bytes, 116),
            VehicleSteering = ReadSingle(bytes, 120),
            VehicleHandbrake = ReadSingle(bytes, 124),
            StageCurrentTime = ReadSingle(bytes, 128),
            StageCurrentDistance = ReadDouble(bytes, 132),
        };
        error = EAWrcPacketParseError.None;
        return true;
    }

    private static ulong ReadUInt64(ReadOnlySpan<byte> bytes, int offset) =>
        BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(offset, sizeof(ulong)));

    private static float ReadSingle(ReadOnlySpan<byte> bytes, int offset) =>
        BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(offset, sizeof(float))));

    private static double ReadDouble(ReadOnlySpan<byte> bytes, int offset) =>
        BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(bytes.Slice(offset, sizeof(double))));
}
