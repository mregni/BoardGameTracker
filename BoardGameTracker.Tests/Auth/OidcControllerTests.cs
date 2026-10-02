using System;
using System.Net.Http;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Exceptions;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class OidcControllerTests
{
    public static TheoryData<Exception, string> Errors => new()
    {
        { new DomainException(Constants.Errors.OidcAlreadyLinked), Constants.Errors.OidcAlreadyLinked },
        { new ValidationException(Constants.Errors.InvalidAuthSession), Constants.Errors.InvalidAuthSession },
        { new AuthenticationFailedException(Constants.Errors.AccountLockedOut), Constants.Errors.AccountLockedOut },
        { new EntityNotFoundException("ApplicationUser", "42"), Constants.Errors.InvalidAuthSession },
        { new ValidationException("Invalid BGG API key. Please check your API key in settings."), Constants.Errors.OidcFailed },
        { new DomainException("Provider returned an unexpected token response"), Constants.Errors.OidcFailed },
        { new InvalidOperationException("error.looks-like-a-key"), Constants.Errors.OidcFailed },
        { new HttpRequestException("Connection refused (idp.internal:443)"), Constants.Errors.OidcFailed },
    };

    [Theory]
    [MemberData(nameof(Errors))]
    public void ErrorKey_ShouldOnlyForwardTranslationKeysFromExpectedExceptions(Exception exception, string expected)
    {
        OidcController.ErrorKey(exception).Should().Be(expected);
    }
}
