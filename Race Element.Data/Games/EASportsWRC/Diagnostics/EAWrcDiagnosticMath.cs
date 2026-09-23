namespace RaceElement.Data.Games.EASportsWRC.Diagnostics;

public sealed record EAWrcDiagnosticMathResult(
    double? LongitudinalVelocity,
    double? SpeedDifferenceFrontLeft,
    double? SpeedDifferenceFrontRight,
    double? SpeedDifferenceRearLeft,
    double? SpeedDifferenceRearRight,
    double? ProvisionalSignedSlipFrontLeft,
    double? ProvisionalSignedSlipFrontRight,
    double? ProvisionalSignedSlipRearLeft,
    double? ProvisionalSignedSlipRearRight,
    bool SlipValid,
    bool LowSpeed);

public static class EAWrcDiagnosticMath
{
    public const double MinimumSlipSpeedMetresPerSecond = 1.0;

    public static EAWrcDiagnosticMathResult Calculate(EAWrcDiagnosticPacket packet)
    {
        double? longitudinalVelocityValue = EAWrcTelemetryMath.CalculateLongitudinalVelocity(packet);
        if (!longitudinalVelocityValue.HasValue)
            return new(null, null, null, null, null, null, null, null, null, false, false);

        double longitudinalVelocity = longitudinalVelocityValue.Value;

        double? frontLeftDifference = Difference(longitudinalVelocity, packet.VehicleContactPatchForwardSpeedFrontLeft);
        double? frontRightDifference = Difference(longitudinalVelocity, packet.VehicleContactPatchForwardSpeedFrontRight);
        double? rearLeftDifference = Difference(longitudinalVelocity, packet.VehicleContactPatchForwardSpeedBackLeft);
        double? rearRightDifference = Difference(longitudinalVelocity, packet.VehicleContactPatchForwardSpeedBackRight);

        bool lowSpeed = Math.Abs(longitudinalVelocity) < MinimumSlipSpeedMetresPerSecond;
        bool slipValid = !lowSpeed &&
                         frontLeftDifference.HasValue && frontRightDifference.HasValue &&
                         rearLeftDifference.HasValue && rearRightDifference.HasValue;

        if (!slipValid)
        {
            return new(longitudinalVelocity,
                       frontLeftDifference, frontRightDifference, rearLeftDifference, rearRightDifference,
                       null, null, null, null, false, lowSpeed);
        }

        double denominator = Math.Abs(longitudinalVelocity);
        return new(longitudinalVelocity,
                   frontLeftDifference, frontRightDifference, rearLeftDifference, rearRightDifference,
                   frontLeftDifference!.Value / denominator,
                   frontRightDifference!.Value / denominator,
                   rearLeftDifference!.Value / denominator,
                   rearRightDifference!.Value / denominator,
                   true, false);
    }

    private static double? Difference(double longitudinalVelocity, float contactPatchSpeed) =>
        float.IsFinite(contactPatchSpeed) ? longitudinalVelocity - contactPatchSpeed : null;
}

public static class EAWrcTelemetryMath
{
    public static double? CalculateLongitudinalVelocity(EAWrcDiagnosticPacket packet)
    {
        double longitudinalVelocity =
            (double)packet.VehicleVelocityX * packet.VehicleForwardDirectionX +
            (double)packet.VehicleVelocityY * packet.VehicleForwardDirectionY +
            (double)packet.VehicleVelocityZ * packet.VehicleForwardDirectionZ;

        return double.IsFinite(longitudinalVelocity) ? longitudinalVelocity : null;
    }
}
