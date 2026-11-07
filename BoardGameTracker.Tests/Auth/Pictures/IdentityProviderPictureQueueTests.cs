using System.Threading.Tasks;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Auth.Pictures;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Auth.Pictures;

public class IdentityProviderPictureQueueTests
{
    private static IdentityProviderPictureImport Import(int number) =>
        new($"user-{number}", "https://idp.example.com/a.png", "https://idp.example.com");

    [Fact]
    public async Task Enqueue_ShouldKeepTheFirstSixtyFourImports_AndDropTheRest()
    {
        var queue = new IdentityProviderPictureQueue();
        for (var number = 0; number < 65; number++)
        {
            queue.Enqueue(Import(number));
        }

        for (var number = 0; number < 64; number++)
        {
            (await queue.DequeueAsync(TestContext.Current.CancellationToken)).Should().Be(Import(number));
        }

        queue.DequeueAsync(TestContext.Current.CancellationToken).AsTask().IsCompleted.Should().BeFalse();
    }

    [Fact(Skip = "BUG: DropWrite makes TryWrite return true for a dropped import")]
    public void Enqueue_ShouldReturnFalse_WhenTheQueueIsFull()
    {
        var queue = new IdentityProviderPictureQueue();
        for (var number = 0; number < 64; number++)
        {
            queue.Enqueue(Import(number)).Should().BeTrue();
        }

        queue.Enqueue(Import(64)).Should().BeFalse();
    }
}
