using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Languages.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Languages;

public class LanguageService : ILanguageService
{
    private readonly ILanguageRepository _languageRepository;
    private readonly ILogger<LanguageService> _logger;

    public LanguageService(ILanguageRepository languageRepository, ILogger<LanguageService> logger)
    {
        _languageRepository = languageRepository;
        _logger = logger;
    }

    public Task<List<Language>> GetAllAsync()
    {
        _logger.LogDebug("Fetching all languages");
        return _languageRepository.GetAllAsync();
    }
}