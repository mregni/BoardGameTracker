using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Languages.Interfaces;

namespace BoardGameTracker.Core.Languages;

public class LanguageRepository : CrudHelper<Language>, ILanguageRepository
{
    public LanguageRepository(MainDbContext context) : base(context)
    {
    }
}