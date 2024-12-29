using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Languages.Interfaces;

namespace BoardGameTracker.Core.Languages;

public class LanguageService : ILanguageService
{
    private readonly ILanguageRepository _languageRepository;
    
    public LanguageService(MainDbContext context, ILanguageRepository languageRepository)
    {
        _languageRepository = languageRepository;
    }

    public Task<List<Language>> GetAllAsync()
    {
        return _languageRepository.GetAllAsync();
    }
}