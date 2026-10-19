using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.GameNights;

public sealed record RsvpUpdateResult(GameNightRsvp Rsvp, bool HostNotified);
