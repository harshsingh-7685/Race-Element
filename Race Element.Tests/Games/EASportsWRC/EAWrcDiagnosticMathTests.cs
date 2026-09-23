using RaceElement.Data.Games.EASportsWRC.Diagnostics;

namespace RaceElement.Tests.Games.EASportsWRC;

public class EAWrcDiagnosticMathTests
{
    [Fact]
    public void CalculatesLongitudinalVelocityAsDotProduct()
    {
        EAWrcDiagnosticPacket packet = EAWrcTestPacketFactory.Create(
            velocityX: 2, velocityY: 3, velocityZ: 4,
            forwardX: 0.5f, forwardY: -1, forwardZ: 2,
            cpBackLeft: 0, cpBackRight: 0, cpFrontLeft: 0, cpFrontRight: 0);

        EAWrcDiagnosticMathResult result = EAWrcDiagnosticMath.Calculate(packet);

        Assert.Equal(6, result.LongitudinalVelocity);
    }

    [Fact]
    public void LowSpeedLeavesSlipBlankButRetainsSignedDifferences()
    {
        EAWrcDiagnosticPacket packet = EAWrcTestPacketFactory.Create(
            velocityZ: 0.5f,
            cpFrontLeft: 0.25f, cpFrontRight: 0.75f,
            cpBackLeft: -0.5f, cpBackRight: 1.5f);

        EAWrcDiagnosticMathResult result = EAWrcDiagnosticMath.Calculate(packet);

        Assert.True(result.LowSpeed);
        Assert.False(result.SlipValid);
        Assert.Equal(0.25, result.SpeedDifferenceFrontLeft);
        Assert.Equal(-0.25, result.SpeedDifferenceFrontRight);
        Assert.Equal(1.0, result.SpeedDifferenceRearLeft);
        Assert.Equal(-1.0, result.SpeedDifferenceRearRight);
        Assert.Null(result.ProvisionalSignedSlipFrontLeft);
        Assert.Null(result.ProvisionalSignedSlipRearRight);
    }

    [Fact]
    public void PreservesPositiveAndNegativeSignedSlip()
    {
        EAWrcDiagnosticPacket packet = EAWrcTestPacketFactory.Create(
            velocityZ: 10,
            cpFrontLeft: 0, cpFrontRight: 20,
            cpBackLeft: 5, cpBackRight: 15);

        EAWrcDiagnosticMathResult result = EAWrcDiagnosticMath.Calculate(packet);

        Assert.True(result.SlipValid);
        Assert.Equal(1.0, result.ProvisionalSignedSlipFrontLeft);
        Assert.Equal(-1.0, result.ProvisionalSignedSlipFrontRight);
        Assert.Equal(0.5, result.ProvisionalSignedSlipRearLeft);
        Assert.Equal(-0.5, result.ProvisionalSignedSlipRearRight);
    }

    [Fact]
    public void DoesNotApplyReverseCorrection()
    {
        EAWrcDiagnosticPacket packet = EAWrcTestPacketFactory.Create(
            velocityZ: -10,
            cpFrontLeft: -5, cpFrontRight: -5,
            cpBackLeft: -5, cpBackRight: -5);

        EAWrcDiagnosticMathResult result = EAWrcDiagnosticMath.Calculate(packet);

        Assert.True(result.SlipValid);
        Assert.Equal(-0.5, result.ProvisionalSignedSlipFrontLeft);
        Assert.Equal(-0.5, result.ProvisionalSignedSlipRearLeft);
    }

    [Fact]
    public void NonFiniteVelocityInvalidatesCalculatedFields()
    {
        EAWrcDiagnosticPacket packet = EAWrcTestPacketFactory.Create(velocityX: float.NaN);

        EAWrcDiagnosticMathResult result = EAWrcDiagnosticMath.Calculate(packet);

        Assert.False(result.SlipValid);
        Assert.Null(result.LongitudinalVelocity);
        Assert.Null(result.SpeedDifferenceFrontLeft);
        Assert.Null(result.ProvisionalSignedSlipFrontLeft);
    }

    [Fact]
    public void NonFiniteContactPatchSpeedInvalidatesSlipWithoutDiscardingOtherDifferences()
    {
        EAWrcDiagnosticPacket packet = EAWrcTestPacketFactory.Create(cpFrontLeft: float.PositiveInfinity);

        EAWrcDiagnosticMathResult result = EAWrcDiagnosticMath.Calculate(packet);

        Assert.False(result.SlipValid);
        Assert.Null(result.SpeedDifferenceFrontLeft);
        Assert.Equal(0, result.SpeedDifferenceFrontRight);
        Assert.Null(result.ProvisionalSignedSlipFrontRight);
    }
}
