using BoardGameTracker.Common;
using BoardGameTracker.Common.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace BoardGameTracker.Core.Auth;

public static class IdentityResultExtensions
{
    public static void ThrowIfFailed(this IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new ValidationException(ErrorCode(result));
        }
    }

    private static string ErrorCode(IdentityResult result) => result.Errors.FirstOrDefault()?.Code switch
    {
        nameof(IdentityErrorDescriber.PasswordTooShort) => Constants.Errors.PasswordTooShort,
        nameof(IdentityErrorDescriber.PasswordMismatch) => Constants.Errors.IncorrectPassword,
        nameof(IdentityErrorDescriber.DuplicateUserName) => Constants.Errors.UsernameAlreadyExists,
        nameof(IdentityErrorDescriber.InvalidUserName) => Constants.Errors.InvalidUsername,
        nameof(IdentityErrorDescriber.DuplicateEmail) => Constants.Errors.EmailAlreadyExists,
        nameof(IdentityErrorDescriber.InvalidEmail) => Constants.Errors.InvalidEmail,
        _ => Constants.Errors.AccountUpdateFailed
    };
}
