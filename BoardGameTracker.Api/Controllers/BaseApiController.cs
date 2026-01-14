using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Validates that an ID is positive (greater than 0)
    /// </summary>
    protected bool IsValidId(int id) => id > 0;

    /// <summary>
    /// Returns a BadRequest with a message if ID is invalid
    /// </summary>
    protected IActionResult ValidateId(int id, string parameterName = "id")
    {
        if (id <= 0)
        {
            return BadRequest(new { error = $"Invalid {parameterName}. Must be greater than 0." });
        }

        return null!; // Validation passed
    }

    /// <summary>
    /// Returns a BadRequest if model state is invalid
    /// </summary>
    protected IActionResult? ValidateModelState()
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return null; // Validation passed
    }

    /// <summary>
    /// Returns a BadRequest if the request body is null
    /// </summary>
    protected IActionResult? ValidateNotNull(object? obj, string objectName = "Request body")
    {
        if (obj == null)
        {
            return BadRequest(new { error = $"{objectName} cannot be null." });
        }

        return null; // Validation passed
    }
}
