using RaceElement.Data.Games.EASportsWRC.Diagnostics;

namespace RaceElement.Data.Games.EASportsWRC;

public sealed record EAWrcProductionSlipResult(
    double LongitudinalVelocity,
    float SignedSlipFrontLeft,
    float SignedSlipFrontRight,
    float SignedSlipRearLeft,
    float SignedSlipRearRight,
    bool IsValid)
{
    public float[] AbsoluteRaceElementOrder() =>
    [
        Math.Abs(SignedSlipFrontLeft),
        Math.Abs(SignedSlipFrontRight),
        Math.Abs(SignedSlipRearLeft),
        Math.Abs(SignedSlipRearRight),
    ];
}

public static class EAWrcProductionMath
{
    public const double MinimumSlipSpeedMetresPerSecond = 1.0;
    public const double Epsilon = 0.0001;

    public static EAWrcProductionSlipResult Calculate(EAWrcDiagnosticPacket packet)
    {
        double? longitudinalVelocityValue = EAWrcTelemetryMath.CalculateLongitudinalVelocity(packet);
        if (!longitudinalVelocityValue.HasValue)
            return Invalid();

        double longitudinalVelocity = longitudinalVelocityValue.Value;
        double absoluteLongitudinalVelocity = Math.Abs(longitudinalVelocity);
        if (absoluteLongitudinalVelocity < MinimumSlipSpeedMetresPerSecond)
            return Invalid(longitudinalVelocity);

        double denominator = Math.Max(absoluteLongitudinalVelocity, Epsilon);
        float? frontLeft = CalculateWheel(absoluteLongitudinalVelocity, packet.VehicleContactPatchForwardSpeedFrontLeft, denominator);
        float? frontRight = CalculateWheel(absoluteLongitudinalVelocity, packet.VehicleContactPatchForwardSpeedFrontRight, denominator);
        float? rearLeft = CalculateWheel(absoluteLongitudinalVelocity, packet.VehicleContactPatchForwardSpeedBackLeft, denominator);
        float? rearRight = CalculateWheel(absoluteLongitudinalVelocity, packet.VehicleContactPatchForwardSpeedBackRight, denominator);

        if (!frontLeft.HasValue || !frontRight.HasValue || !rearLeft.HasValue || !rearRight.HasValue)
            return Invalid(longitudinalVelocity);

        return new(
            longitudinalVelocity,
            frontLeft.Value,
            frontRight.Value,
            rearLeft.Value,
            rearRight.Value,
            true);
    }

    private static float? CalculateWheel(double absoluteLongitudinalVelocity, float contactPatchForwardSpeed, double denominator)
    {
        if (!float.IsFinite(contactPatchForwardSpeed))
            return null;

        return (float)((absoluteLongitudinalVelocity - Math.Abs(contactPatchForwardSpeed)) / denominator);
    }

    private static EAWrcProductionSlipResult Invalid(double longitudinalVelocity = 0) =>
        new(longitudinalVelocity, 0, 0, 0, 0, false);
}
