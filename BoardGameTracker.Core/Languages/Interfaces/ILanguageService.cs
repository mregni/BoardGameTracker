using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore.Interfaces;

namespace BoardGameTracker.Core.Languages.Interfaces;

public interface ILanguageService 
{
    Task<List<Language>> GetAllAsync();
}