using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BoardGameTracker.Common.DTOs.Auth;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class RegisterRequestTests
{
    [Theory]
    [InlineData(false, null)]
    [InlineData(true, null)]
    [InlineData(false, 4)]
    public void Validate_ShouldPass_WhenAtMostOneWayToGetAPlayerIsChosen(bool createPlayer, int? playerId)
    {
        Validate(new RegisterRequest("jane", "jane@example.com", "long-enough", null, createPlayer, playerId)).Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldReject_WhenANewPlayerIsCreatedAndAnExistingOneIsLinked()
    {
        var error = Validate(new RegisterRequest("jane", "jane@example.com", "long-enough", null, true, 4)).Should().ContainSingle().Subject;

        error.ErrorMessage.Should().Be("Choose either creating a new player or linking an existing one, not both.");
        error.MemberNames.Should().Equal(nameof(RegisterRequest.CreatePlayer), nameof(RegisterRequest.PlayerId));
    }

    private static List<ValidationResult> Validate(RegisterRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }
}
