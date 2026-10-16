using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Sentry.AspNetCore;
using Sentry.Extensibility;
using Xunit;

namespace BoardGameTracker.Tests.Extensions;

[Collection(EnvironmentVariablesCollection.Name)]
public class WebHostBuilderExtensionsTests
{
    [Fact]
    public void ConfigureSentry_ShouldNeverCaptureRequestBodiesOrPersonalData()
    {
        var options = new SentryAspNetCoreOptions
        {
            MaxRequestBodySize = RequestSize.Always,
            SendDefaultPii = true
        };

        WebHostBuilderExtensions.ConfigureSentry(options);

        options.MaxRequestBodySize.Should().Be(RequestSize.None);
        options.SendDefaultPii.Should().BeFalse();
    }
}
