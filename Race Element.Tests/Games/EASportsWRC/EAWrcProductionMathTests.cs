using RaceElement.Data.Games.EASportsWRC;

namespace RaceElement.Tests.Games.EASportsWRC;

public class EAWrcProductionMathTests
{
    [Fact]
    public void ForwardRollingHasZeroSlip()
    {
        EAWrcProductionSlipResult result = Calculate(10, 10);

        Assert.True(result.IsValid);
        AssertAll(result, 0);
    }

    [Fact]
    public void ForwardBrakingHasPositiveSignedSlip()
    {
        EAWrcProductionSlipResult result = Calculate(10, 5);

        AssertAll(result, 0.5f);
    }

    [Fact]
    public void ForwardWheelspinHasNegativeSignedSlip()
    {
        EAWrcProductionSlipResult result = Calculate(10, 15);

        AssertAll(result, -0.5f);
    }

    [Fact]
    public void ReverseRollingHasZeroSlip()
    {
        EAWrcProductionSlipResult result = Calculate(-10, -10);

        AssertAll(result, 0);
    }

    [Fact]
    public void ReverseBrakingHasPositiveSignedSlip()
    {
        EAWrcProductionSlipResult result = Calculate(-10, -5);

        AssertAll(result, 0.5f);
    }

    [Fact]
    public void ReverseWheelspinHasNegativeSignedSlip()
    {
        EAWrcProductionSlipResult result = Calculate(-10, -15);

        AssertAll(result, -0.5f);
    }

    [Fact]
    public void LowSpeedSuppressesSlip()
    {
        EAWrcProductionSlipResult result = Calculate(0.5f, 4);

        Assert.False(result.IsValid);
        AssertAll(result, 0);
    }

    [Fact]
    public void MapsBackLeftAndBackRightToRearLeftAndRearRight()
    {
        var packet = EAWrcTestPacketFactory.Create(
            velocityZ: 10,
            cpFrontLeft: 9,
            cpFrontRight: 8,
            cpBackLeft: 7,
            cpBackRight: 6);

        EAWrcProductionSlipResult result = EAWrcProductionMath.Calculate(packet);

        Assert.Equal(0.1f, result.SignedSlipFrontLeft, 5);
        Assert.Equal(0.2f, result.SignedSlipFrontRight, 5);
        Assert.Equal(0.3f, result.SignedSlipRearLeft, 5);
        Assert.Equal(0.4f, result.SignedSlipRearRight, 5);
        Assert.Equal([0.1f, 0.2f, 0.3f, 0.4f], result.AbsoluteRaceElementOrder(), new FloatComparer(0.00001f));
    }

    [Fact]
    public void DoesNotClampLargeSlip()
    {
        EAWrcProductionSlipResult result = Calculate(10, 30);

        AssertAll(result, -2);
    }

    private static EAWrcProductionSlipResult Calculate(float longitudinalVelocity, float contactPatchSpeed) =>
        EAWrcProductionMath.Calculate(EAWrcTestPacketFactory.Create(
            velocityZ: longitudinalVelocity,
            cpFrontLeft: contactPatchSpeed,
            cpFrontRight: contactPatchSpeed,
            cpBackLeft: contactPatchSpeed,
            cpBackRight: contactPatchSpeed));

    private static void AssertAll(EAWrcProductionSlipResult result, float expected)
    {
        Assert.Equal(expected, result.SignedSlipFrontLeft, 5);
        Assert.Equal(expected, result.SignedSlipFrontRight, 5);
        Assert.Equal(expected, result.SignedSlipRearLeft, 5);
        Assert.Equal(expected, result.SignedSlipRearRight, 5);
    }

    private sealed class FloatComparer(float tolerance) : IEqualityComparer<float>
    {
        public bool Equals(float x, float y) => Math.Abs(x - y) <= tolerance;
        public int GetHashCode(float value) => value.GetHashCode();
    }
}
