using BoardGameTracker.Common;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class IdentityResultExtensionsTests
{
    [Fact]
    public void ThrowIfFailed_ShouldNotThrow_WhenTheResultSucceeded()
    {
        var act = () => IdentityResult.Success.ThrowIfFailed();

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(nameof(IdentityErrorDescriber.PasswordTooShort), Constants.Errors.PasswordTooShort)]
    [InlineData(nameof(IdentityErrorDescriber.PasswordMismatch), Constants.Errors.IncorrectPassword)]
    [InlineData(nameof(IdentityErrorDescriber.DuplicateUserName), Constants.Errors.UsernameAlreadyExists)]
    [InlineData(nameof(IdentityErrorDescriber.InvalidUserName), Constants.Errors.InvalidUsername)]
    [InlineData(nameof(IdentityErrorDescriber.DuplicateEmail), Constants.Errors.EmailAlreadyExists)]
    [InlineData(nameof(IdentityErrorDescriber.InvalidEmail), Constants.Errors.InvalidEmail)]
    [InlineData(nameof(IdentityErrorDescriber.PasswordRequiresDigit), Constants.Errors.AccountUpdateFailed)]
    [InlineData(nameof(IdentityErrorDescriber.DefaultError), Constants.Errors.AccountUpdateFailed)]
    public void ThrowIfFailed_ShouldThrowTheTranslatableCode_WhenTheFirstErrorHasAKnownCode(string identityCode, string expected)
    {
        var result = IdentityResult.Failed(new IdentityError { Code = identityCode, Description = "English text" });

        var act = () => result.ThrowIfFailed();

        act.Should().Throw<ValidationException>().WithMessage(expected);
    }

    [Fact]
    public void ThrowIfFailed_ShouldUseOnlyTheFirstError_WhenSeveralErrorsAreReported()
    {
        var result = IdentityResult.Failed(
            new IdentityError { Code = nameof(IdentityErrorDescriber.DuplicateEmail), Description = "taken" },
            new IdentityError { Code = nameof(IdentityErrorDescriber.PasswordTooShort), Description = "short" });

        var act = () => result.ThrowIfFailed();

        act.Should().Throw<ValidationException>().WithMessage(Constants.Errors.EmailAlreadyExists);
    }

    [Fact]
    public void ThrowIfFailed_ShouldThrowTheGenericCode_WhenAFailedResultCarriesNoErrors()
    {
        var act = () => IdentityResult.Failed().ThrowIfFailed();

        act.Should().Throw<ValidationException>().WithMessage(Constants.Errors.AccountUpdateFailed);
    }
}
