namespace BoardGameTracker.Core.Email.Interfaces;

public interface IPublicUrlBuilder
{
    Task<string> BuildRsvpUrlAsync(Guid linkId);
    Task<string> BuildResetUrlAsync(string userId, string token);
}
