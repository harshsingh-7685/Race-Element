using RaceElement.Data.Games.EASportsWRC;
using System.Diagnostics;

namespace RaceElement.Tests.Games.EASportsWRC;

public class EAWrcTelemetryFreshnessTests
{
    [Fact]
    public void ReceivedPacketMakesTelemetryFresh()
    {
        EAWrcTelemetryFreshness freshness = new();
        long received = Stopwatch.GetTimestamp();

        freshness.RecordPacket(received);

        Assert.True(freshness.IsFresh(received));
    }

    [Fact]
    public void TelemetryIsStaleAfterFiveHundredMilliseconds()
    {
        EAWrcTelemetryFreshness freshness = new();
        long received = Stopwatch.GetTimestamp();
        freshness.RecordPacket(received);

        Assert.False(freshness.IsFresh(AddMilliseconds(received, 501)));
    }

    [Fact]
    public void NewPacketRestoresFreshTelemetryAfterStalePeriod()
    {
        EAWrcTelemetryFreshness freshness = new();
        long firstPacket = Stopwatch.GetTimestamp();
        freshness.RecordPacket(firstPacket);
        Assert.False(freshness.IsFresh(AddMilliseconds(firstPacket, 501)));

        long nextPacket = AddMilliseconds(firstPacket, 750);
        freshness.RecordPacket(nextPacket);

        Assert.True(freshness.IsFresh(nextPacket));
    }

    private static long AddMilliseconds(long timestamp, int milliseconds) =>
        timestamp + (long)(Stopwatch.Frequency * (milliseconds / 1000d));
}
