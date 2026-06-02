using FluentAssertions;
using LogDecoder.CAN;
using LogDecoder.CAN.General;
using LogDecoder.Parser.Data;

namespace LogDecoder.Parser.Tests.Unit;

[TestFixture]
public class HrcTimeEstimatorTest
{
    [Test]
    public void Observe_AccumulatesMicrosecondsBetweenAnchorAndPacket()
    {
        var anchor = new DateTime(2025, 1, 1, 12, 0, 0);
        var estimator = new HrcTimeEstimator();
        estimator.Anchor(anchor, hrc: 1_000);

        var est = estimator.Observe(hrc: 1_500);

        est.Should().Be(anchor.AddTicks(500 * TimeSpan.TicksPerMicrosecond));
    }

    [Test]
    public void Observe_HrcWrap_ProducesCorrectDelta()
    {
        // Wrap math matches the original CalcHrcDelta semantics: delta = (Max - prev) + hrc,
        // i.e. the rollover from MaxHrcSize to 0 is treated as a 0-µs step (off-by-one
        // versus a true 24-bit counter, but preserved as-is to avoid behaviour drift).
        var anchor = new DateTime(2025, 1, 1, 12, 0, 0);
        var estimator = new HrcTimeEstimator();
        estimator.Anchor(anchor, hrc: CanConfig.MaxHrcSize - 100);

        var est = estimator.Observe(hrc: 50);

        est.Should().Be(anchor.AddTicks(150 * TimeSpan.TicksPerMicrosecond));
    }

    [Test]
    public void Anchor_ResetsAccumulator()
    {
        var t0 = new DateTime(2025, 1, 1, 12, 0, 0);
        var t1 = t0.AddSeconds(1);
        var estimator = new HrcTimeEstimator();
        estimator.Anchor(t0, hrc: 0);
        estimator.Observe(hrc: 500_000);

        estimator.Anchor(t1, hrc: 1_000_000);
        var est = estimator.Observe(hrc: 1_000_500);

        est.Should().Be(t1.AddTicks(500 * TimeSpan.TicksPerMicrosecond));
    }

    [Test]
    public void Observe_BeforeAnchor_Throws()
    {
        var estimator = new HrcTimeEstimator();

        var act = () => estimator.Observe(hrc: 0);

        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void TryComputeHrcDelta_SuspectForwardJump_ReturnsFalse()
    {
        // Triggering the "counter restart" branch requires a forward delta > 1s combined
        // with a small new hrc, which only happens when prevHrc is artificially negative.
        // The branch is preserved from the original CalcHrcDelta but is unreachable with
        // valid 24-bit device HRCs — this test documents the gate.
        var ok = CanUtils.TryComputeHrcDelta(prevHrc: -10, hrc: 999_999, out var delta);

        ok.Should().BeFalse();
        delta.Should().Be(0);
    }

    [Test]
    public void TryComputeHrcDelta_NormalForwardStep_ReturnsTrue()
    {
        var ok = CanUtils.TryComputeHrcDelta(prevHrc: 100, hrc: 250, out var delta);

        ok.Should().BeTrue();
        delta.Should().Be(150);
    }
}
