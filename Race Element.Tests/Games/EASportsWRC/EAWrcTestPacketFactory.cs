using RaceElement.Data.Games.EASportsWRC.Diagnostics;

namespace RaceElement.Tests.Games.EASportsWRC;

internal static class EAWrcTestPacketFactory
{
    internal static EAWrcDiagnosticPacket Create(
        ulong packetUid = 1,
        ulong frameCount = 1,
        float gameTotalTime = 1,
        float velocityX = 0,
        float velocityY = 0,
        float velocityZ = 10,
        float forwardX = 0,
        float forwardY = 0,
        float forwardZ = 1,
        float cpBackLeft = 10,
        float cpBackRight = 10,
        float cpFrontLeft = 10,
        float cpFrontRight = 10) => new()
    {
        PacketUid = packetUid,
        GameTotalTime = gameTotalTime,
        GameDeltaTime = 1f / 60f,
        GameFrameCount = frameCount,
        VehicleSpeed = 10,
        VehicleTransmissionSpeed = 10,
        VehicleVelocityX = velocityX,
        VehicleVelocityY = velocityY,
        VehicleVelocityZ = velocityZ,
        VehicleForwardDirectionX = forwardX,
        VehicleForwardDirectionY = forwardY,
        VehicleForwardDirectionZ = forwardZ,
        VehicleHubPositionBackLeft = 0,
        VehicleHubPositionBackRight = 0,
        VehicleHubPositionFrontLeft = 0,
        VehicleHubPositionFrontRight = 0,
        VehicleHubVelocityBackLeft = 0,
        VehicleHubVelocityBackRight = 0,
        VehicleHubVelocityFrontLeft = 0,
        VehicleHubVelocityFrontRight = 0,
        VehicleContactPatchForwardSpeedBackLeft = cpBackLeft,
        VehicleContactPatchForwardSpeedBackRight = cpBackRight,
        VehicleContactPatchForwardSpeedFrontLeft = cpFrontLeft,
        VehicleContactPatchForwardSpeedFrontRight = cpFrontRight,
        VehicleThrottle = 0,
        VehicleBrake = 0,
        VehicleClutch = 0,
        VehicleSteering = 0,
        VehicleHandbrake = 0,
        StageCurrentTime = 0,
        StageCurrentDistance = 0,
    };
}
