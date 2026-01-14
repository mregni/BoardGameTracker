using BoardGameTracker.Common;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Models.Bgg;
using BoardGameTracker.Core.Images.Interfaces;

namespace BoardGameTracker.Core.Bgg.AntiCorruption;

public class BggGameTranslator : IBggGameTranslator
{
    private readonly IImageService _imageService;

    public BggGameTranslator(IImageService imageService)
    {
        _imageService = imageService;
    }

    public BggGame TranslateRawGame(BggRawGame rawGame)
    {
        bool IsPersonType(BggRawLink link) =>
            link.Type == Constants.Bgg.Artist ||
            link.Type == Constants.Bgg.Designer ||
            link.Type == Constants.Bgg.Publisher;

        return new BggGame
        {
            BggId = rawGame.Id,
            Names = rawGame.Names.Select(n => n.Value).ToArray(),
            Thumbnail = rawGame.Thumbnail,
            Image = rawGame.Image,
            Description = rawGame.Description,
            YearPublished = rawGame.YearPublished.Value,
            MinPlayers = rawGame.MinPlayers.Value,
            MaxPlayers = rawGame.MaxPlayers.Value,
            MinPlayTime = rawGame.MinPlayTime.Value,
            MaxPlayTime = rawGame.MaxPlayTime.Value,
            MinAge = rawGame.MinAge.Value,
            Rating = rawGame.Statistics.Ratings.Average.Value,
            Weight = rawGame.Statistics.Ratings.AverageWeight.Value,
            People = rawGame.Links
                .Where(IsPersonType)
                .Select(p => new BggPerson
                {
                    Value = p.Value,
                    Id = p.Id,
                    Type = p.Type.ToPersonTypeEnum()
                })
                .ToArray(),
            Categories = rawGame.Links
                .Where(l => l.Type == Constants.Bgg.Category)
                .Select(l => new BggLink { Value = l.Value, Id = l.Id })
                .ToArray(),
            Mechanics = rawGame.Links
                .Where(l => l.Type == Constants.Bgg.Mechanic)
                .Select(l => new BggLink { Value = l.Value, Id = l.Id })
                .ToArray(),
            Expansions = rawGame.Links
                .Where(l => l.Type == Constants.Bgg.Expansion)
                .Select(l => new BggLink { Value = l.Value, Id = l.Id })
                .ToArray()
        };
    }

    public async Task<GameImportData> TranslateFromBggAsync(BggGame bggGame)
    {
        // Validate external data at the boundary
        var gameName = bggGame.Names.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(gameName))
            throw new InvalidOperationException("Game must have a valid name from BGG");

        if (bggGame.MinPlayers > bggGame.MaxPlayers)
            throw new InvalidOperationException($"Invalid player count range from BGG: {bggGame.MinPlayers}-{bggGame.MaxPlayers}");

        if (bggGame.MinPlayTime > bggGame.MaxPlayTime)
            throw new InvalidOperationException($"Invalid play time range from BGG: {bggGame.MinPlayTime}-{bggGame.MaxPlayTime}");

        // Download and store image
        var imageUrl = await _imageService.DownloadImage(bggGame.Image, bggGame.BggId.ToString());

        // Translate to internal model
        return new GameImportData
        {
            Title = gameName,
            BggId = bggGame.BggId,
            Description = bggGame.Description ?? string.Empty,
            YearPublished = bggGame.YearPublished,
            MinPlayers = bggGame.MinPlayers,
            MaxPlayers = bggGame.MaxPlayers,
            MinPlayTime = bggGame.MinPlayTime,
            MaxPlayTime = bggGame.MaxPlayTime,
            MinAge = bggGame.MinAge,
            Rating = bggGame.Rating,
            Weight = bggGame.Weight,
            ImageUrl = imageUrl,
            Categories = TranslateCategories(bggGame.Categories),
            Mechanics = TranslateMechanics(bggGame.Mechanics),
            People = TranslatePeople(bggGame.People),
            Expansions = TranslateExpansions(bggGame.Expansions)
        };
    }


    private IEnumerable<CategoryData> TranslateCategories(BggLink[] bggCategories)
    {
        return bggCategories
            .Where(c => !string.IsNullOrWhiteSpace(c.Value))
            .Select(c => new CategoryData
            {
                Name = c.Value,
                BggId = c.Id
            })
            .ToList();
    }

    private IEnumerable<MechanicData> TranslateMechanics(BggLink[] bggMechanics)
    {
        return bggMechanics
            .Where(m => !string.IsNullOrWhiteSpace(m.Value))
            .Select(m => new MechanicData
            {
                Name = m.Value,
                BggId = m.Id
            })
            .ToList();
    }

    private IEnumerable<PersonData> TranslatePeople(BggPerson[] bggPeople)
    {
        return bggPeople
            .Where(p => !string.IsNullOrWhiteSpace(p.Value))
            .Select(p => new PersonData
            {
                Name = p.Value,
                Type = p.Type.ToString()
            })
            .ToList();
    }

    private IEnumerable<ExpansionData> TranslateExpansions(BggLink[] bggExpansions)
    {
        return bggExpansions
            .Where(e => !string.IsNullOrWhiteSpace(e.Value))
            .Select(e => new ExpansionData
            {
                Title = e.Value,
                BggId = e.Id
            })
            .ToList();
    }
}
