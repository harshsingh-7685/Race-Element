using RaceElement.Data.Games.EASportsWRC.Diagnostics;

namespace RaceElement.Tests.Games.EASportsWRC;

public class EAWrcPacketSequenceTests
{
    [Fact]
    public void ClassifiesFirstAndInOrderPackets()
    {
        EAWrcPacketSequenceTracker tracker = new();

        EAWrcSequenceObservation first = tracker.Observe(EAWrcTestPacketFactory.Create(packetUid: 10, frameCount: 100, gameTotalTime: 10));
        EAWrcSequenceObservation next = tracker.Observe(EAWrcTestPacketFactory.Create(packetUid: 11, frameCount: 101, gameTotalTime: 11));

        Assert.Equal(EAWrcSequenceClassification.First, first.Classification);
        Assert.Null(first.PreviousPacketUid);
        Assert.Equal(EAWrcSequenceClassification.InOrder, next.Classification);
        Assert.Equal(1UL, next.ForwardUidDelta);
        Assert.Equal(0UL, next.MissingPacketCount);
    }

    [Fact]
    public void ClassifiesGapWithoutFilteringPacket()
    {
        EAWrcPacketSequenceTracker tracker = new();
        tracker.Observe(EAWrcTestPacketFactory.Create(packetUid: 10));

        EAWrcSequenceObservation observation = tracker.Observe(EAWrcTestPacketFactory.Create(packetUid: 14));

        Assert.Equal(EAWrcSequenceClassification.Gap, observation.Classification);
        Assert.Equal(4UL, observation.ForwardUidDelta);
        Assert.Equal(3UL, observation.MissingPacketCount);
    }

    [Fact]
    public void ClassifiesDuplicate()
    {
        EAWrcPacketSequenceTracker tracker = new();
        tracker.Observe(EAWrcTestPacketFactory.Create(packetUid: 10));

        EAWrcSequenceObservation observation = tracker.Observe(EAWrcTestPacketFactory.Create(packetUid: 10));

        Assert.Equal(EAWrcSequenceClassification.Duplicate, observation.Classification);
    }

    [Fact]
    public void ClassifiesBackwardArrival()
    {
        EAWrcPacketSequenceTracker tracker = new();
        tracker.Observe(EAWrcTestPacketFactory.Create(packetUid: 10, frameCount: 100, gameTotalTime: 10));

        EAWrcSequenceObservation observation = tracker.Observe(EAWrcTestPacketFactory.Create(packetUid: 8, frameCount: 101, gameTotalTime: 11));

        Assert.Equal(EAWrcSequenceClassification.Backward, observation.Classification);
    }

    [Fact]
    public void ClassifiesSimpleSuspectedReset()
    {
        EAWrcPacketSequenceTracker tracker = new();
        tracker.Observe(EAWrcTestPacketFactory.Create(packetUid: 100, frameCount: 1000, gameTotalTime: 100));

        EAWrcSequenceObservation observation = tracker.Observe(EAWrcTestPacketFactory.Create(packetUid: 1, frameCount: 1, gameTotalTime: 1));

        Assert.Equal(EAWrcSequenceClassification.SuspectedReset, observation.Classification);
    }

    [Fact]
    public void ClassifiesUInt64WrapAndReportsGap()
    {
        EAWrcPacketSequenceTracker tracker = new();
        tracker.Observe(EAWrcTestPacketFactory.Create(packetUid: ulong.MaxValue - 1, frameCount: 100, gameTotalTime: 10));

        EAWrcSequenceObservation observation = tracker.Observe(EAWrcTestPacketFactory.Create(packetUid: 1, frameCount: 101, gameTotalTime: 11));

        Assert.Equal(EAWrcSequenceClassification.UInt64Wrap, observation.Classification);
        Assert.Equal(3UL, observation.ForwardUidDelta);
        Assert.Equal(2UL, observation.MissingPacketCount);
    }
}
