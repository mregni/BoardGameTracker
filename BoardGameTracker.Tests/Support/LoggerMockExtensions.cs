using Microsoft.Extensions.Logging;
using Moq;

namespace BoardGameTracker.Tests.Support;

public static class LoggerMockExtensions
{
    public static void VerifyLog<T>(this Mock<ILogger<T>> logger, LogLevel level, string message, Times? times = null)
    {
        logger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString() == message),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times ?? Times.Once());
    }

    public static void VerifyLog<T>(this Mock<ILogger<T>> logger, LogLevel level, string message, Exception exception, Times? times = null)
    {
        logger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString() == message),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times ?? Times.Once());
    }
}
