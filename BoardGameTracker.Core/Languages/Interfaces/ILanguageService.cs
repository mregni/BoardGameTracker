using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Languages.Interfaces;

public interface ILanguageService 
{
    Task<List<Language>> GetAllAsync();
}