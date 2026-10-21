using System.ComponentModel.DataAnnotations;

namespace BoardGameTracker.Common.DTOs.Auth;

public class LinkPlayerAccountRequest
{
    [StringLength(450)]
    public string? UserId { get; set; }
}
