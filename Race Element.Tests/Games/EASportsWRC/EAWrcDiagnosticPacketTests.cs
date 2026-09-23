using RaceElement.Data.Games.EASportsWRC.Diagnostics;
using System.Buffers.Binary;

namespace RaceElement.Tests.Games.EASportsWRC;

public class EAWrcDiagnosticPacketTests
{
    [Fact]
    public void PacketSizeMatchesReviewedLayout()
    {
        Assert.Equal(140, EAWrcDiagnosticPacket.Size);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(139)]
    [InlineData(141)]
    public void RejectsWrongPacketLength(int length)
    {
        bool parsed = EAWrcDiagnosticPacketParser.TryParse(new byte[length], out EAWrcDiagnosticPacket? packet, out EAWrcPacketParseError error);

        Assert.False(parsed);
        Assert.Null(packet);
        Assert.Equal(EAWrcPacketParseError.WrongLength, error);
    }

    [Fact]
    public void RejectsWrongFourCc()
    {
        byte[] bytes = CreatePacketBytes();
        "SESU"u8.CopyTo(bytes);

        bool parsed = EAWrcDiagnosticPacketParser.TryParse(bytes, out EAWrcDiagnosticPacket? packet, out EAWrcPacketParseError error);

        Assert.False(parsed);
        Assert.Null(packet);
        Assert.Equal(EAWrcPacketParseError.WrongFourCc, error);
    }

    [Fact]
    public void ParsesEveryReviewedFieldAtItsExactOffset()
    {
        byte[] bytes = CreatePacketBytes();
        WriteUInt64(bytes, 4, 0x0102030405060708UL);
        WriteSingle(bytes, 12, 1.25f);
        WriteSingle(bytes, 16, 2.5f);
        WriteUInt64(bytes, 20, 0x1112131415161718UL);
        WriteSingle(bytes, 28, 3.25f);
        WriteSingle(bytes, 32, 4.25f);
        WriteSingle(bytes, 36, 5.25f);
        WriteSingle(bytes, 40, 6.25f);
        WriteSingle(bytes, 44, 7.25f);
        WriteSingle(bytes, 48, 8.25f);
        WriteSingle(bytes, 52, 9.25f);
        WriteSingle(bytes, 56, 10.25f);
        WriteSingle(bytes, 60, 11.25f);
        WriteSingle(bytes, 64, 12.25f);
        WriteSingle(bytes, 68, 13.25f);
        WriteSingle(bytes, 72, 14.25f);
        WriteSingle(bytes, 76, 15.25f);
        WriteSingle(bytes, 80, 16.25f);
        WriteSingle(bytes, 84, 17.25f);
        WriteSingle(bytes, 88, 18.25f);
        WriteSingle(bytes, 92, 19.25f);
        WriteSingle(bytes, 96, 20.25f);
        WriteSingle(bytes, 100, 21.25f);
        WriteSingle(bytes, 104, 22.25f);
        WriteSingle(bytes, 108, 0.1f);
        WriteSingle(bytes, 112, 0.2f);
        WriteSingle(bytes, 116, 0.3f);
        WriteSingle(bytes, 120, -0.4f);
        WriteSingle(bytes, 124, 0.5f);
        WriteSingle(bytes, 128, 23.25f);
        WriteDouble(bytes, 132, 123456.75);

        Assert.True(EAWrcDiagnosticPacketParser.TryParse(bytes, out EAWrcDiagnosticPacket? packet, out EAWrcPacketParseError error));
        Assert.Equal(EAWrcPacketParseError.None, error);
        Assert.NotNull(packet);
        Assert.Equal(0x0102030405060708UL, packet.PacketUid);
        Assert.Equal(1.25f, packet.GameTotalTime);
        Assert.Equal(2.5f, packet.GameDeltaTime);
        Assert.Equal(0x1112131415161718UL, packet.GameFrameCount);
        Assert.Equal(3.25f, packet.VehicleSpeed);
        Assert.Equal(4.25f, packet.VehicleTransmissionSpeed);
        Assert.Equal(5.25f, packet.VehicleVelocityX);
        Assert.Equal(6.25f, packet.VehicleVelocityY);
        Assert.Equal(7.25f, packet.VehicleVelocityZ);
        Assert.Equal(8.25f, packet.VehicleForwardDirectionX);
        Assert.Equal(9.25f, packet.VehicleForwardDirectionY);
        Assert.Equal(10.25f, packet.VehicleForwardDirectionZ);
        Assert.Equal(11.25f, packet.VehicleHubPositionBackLeft);
        Assert.Equal(12.25f, packet.VehicleHubPositionBackRight);
        Assert.Equal(13.25f, packet.VehicleHubPositionFrontLeft);
        Assert.Equal(14.25f, packet.VehicleHubPositionFrontRight);
        Assert.Equal(15.25f, packet.VehicleHubVelocityBackLeft);
        Assert.Equal(16.25f, packet.VehicleHubVelocityBackRight);
        Assert.Equal(17.25f, packet.VehicleHubVelocityFrontLeft);
        Assert.Equal(18.25f, packet.VehicleHubVelocityFrontRight);
        Assert.Equal(19.25f, packet.VehicleContactPatchForwardSpeedBackLeft);
        Assert.Equal(20.25f, packet.VehicleContactPatchForwardSpeedBackRight);
        Assert.Equal(21.25f, packet.VehicleContactPatchForwardSpeedFrontLeft);
        Assert.Equal(22.25f, packet.VehicleContactPatchForwardSpeedFrontRight);
        Assert.Equal(0.1f, packet.VehicleThrottle);
        Assert.Equal(0.2f, packet.VehicleBrake);
        Assert.Equal(0.3f, packet.VehicleClutch);
        Assert.Equal(-0.4f, packet.VehicleSteering);
        Assert.Equal(0.5f, packet.VehicleHandbrake);
        Assert.Equal(23.25f, packet.StageCurrentTime);
        Assert.Equal(123456.75, packet.StageCurrentDistance);
    }

    [Fact]
    public void ParsesIntegersAndFloatingPointAsLittleEndian()
    {
        byte[] bytes = CreatePacketBytes();
        bytes[4] = 0x08;
        bytes[5] = 0x07;
        bytes[6] = 0x06;
        bytes[7] = 0x05;
        bytes[8] = 0x04;
        bytes[9] = 0x03;
        bytes[10] = 0x02;
        bytes[11] = 0x01;
        bytes[28] = 0x00;
        bytes[29] = 0x00;
        bytes[30] = 0x20;
        bytes[31] = 0x41;

        Assert.True(EAWrcDiagnosticPacketParser.TryParse(bytes, out EAWrcDiagnosticPacket? packet, out _));
        Assert.Equal(0x0102030405060708UL, packet!.PacketUid);
        Assert.Equal(10f, packet.VehicleSpeed);
    }

    [Fact]
    public void MapsEaBackWheelsToRaceElementRearWheels()
    {
        byte[] bytes = CreatePacketBytes();
        WriteSingle(bytes, 92, 30);
        WriteSingle(bytes, 96, 40);
        WriteSingle(bytes, 100, 10);
        WriteSingle(bytes, 104, 20);
        Assert.True(EAWrcDiagnosticPacketParser.TryParse(bytes, out EAWrcDiagnosticPacket? packet, out _));

        Assert.Equal([10f, 20f, 30f, 40f], packet!.ContactPatchForwardSpeedsRaceElementOrder());
    }

    private static byte[] CreatePacketBytes()
    {
        byte[] bytes = new byte[EAWrcDiagnosticPacket.Size];
        "sesu"u8.CopyTo(bytes);
        return bytes;
    }

    private static void WriteUInt64(Span<byte> bytes, int offset, ulong value) =>
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.Slice(offset, sizeof(ulong)), value);

    private static void WriteSingle(Span<byte> bytes, int offset, float value) =>
        BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(offset, sizeof(float)), BitConverter.SingleToInt32Bits(value));

    private static void WriteDouble(Span<byte> bytes, int offset, double value) =>
        BinaryPrimitives.WriteInt64LittleEndian(bytes.Slice(offset, sizeof(double)), BitConverter.DoubleToInt64Bits(value));
}
