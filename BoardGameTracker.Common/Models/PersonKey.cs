using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Models;

public readonly record struct PersonKey(string Name, PersonType Type);
