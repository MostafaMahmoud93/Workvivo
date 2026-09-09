using Shouldly;
using Workvivo.Domain.Entities.Recognition;
using Xunit;

namespace Workvivo.Domain.Tests.Entities;

public class RecognitionTests
{
    [Fact]
    public void Recognising_yourself_is_rejected()
    {
        // Points feed a leaderboard. Without this, the leaderboard is a measure of who
        // is willing to award themselves points.
        var employeeId = Guid.NewGuid();

        Should.Throw<InvalidOperationException>(
            () => Recognition.EnsureNotSelfRecognition(employeeId, employeeId));
    }

    [Fact]
    public void Recognising_a_colleague_is_allowed()
    {
        Should.NotThrow(
            () => Recognition.EnsureNotSelfRecognition(Guid.NewGuid(), Guid.NewGuid()));
    }
}
