using System.ComponentModel.DataAnnotations;

namespace BoardGameTracker.Common.DTOs.Auth;

public record RegisterRequest(
    [Required, StringLength(256)] string Username,
    [Required, EmailAddress] string Email,
    [Required, StringLength(128, MinimumLength = 8)] string Password,
    string? Role,
    bool CreatePlayer = false,
    [Range(1, int.MaxValue)] int? PlayerId = null) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CreatePlayer && PlayerId.HasValue)
        {
            yield return new ValidationResult(
                "Choose either creating a new player or linking an existing one, not both.",
                [nameof(CreatePlayer), nameof(PlayerId)]);
        }
    }
}
