using AutoMapper;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Bgg;
using BoardGameTracker.Common.ViewModels;

namespace BoardGameTracker.Common;

public class MapProfiles : Profile
{
    public MapProfiles()
    {
        CreateMap<BggRawGame, BggGame>()
            .ForMember(x => x.BggId, x => x.MapFrom(y => y.Id))
            .ForMember(x => x.Names, x => x.MapFrom(y => y.Names.Select(z => z.Value)))
            .ForMember(x => x.YearPublished, x => x.MapFrom(y => y.YearPublished.Value))
            .ForMember(x => x.MinPlayers, x => x.MapFrom(y => y.MinPlayers.Value))
            .ForMember(x => x.MaxPlayers, x => x.MapFrom(y => y.MaxPlayers.Value))
            .ForMember(x => x.MinPlayTime, x => x.MapFrom(y => y.MinPlayTime.Value))
            .ForMember(x => x.MaxPlayTime, x => x.MapFrom(y => y.MaxPlayTime.Value))
            .ForMember(x => x.MinAge, x => x.MapFrom(y => y.MinAge.Value))
            .ForMember(x => x.Rating, x => x.MapFrom(y => y.Statistics.Ratings.Average.Value))
            .ForMember(x => x.Weight, x => x.MapFrom(y => y.Statistics.Ratings.AverageWeight.Value))
            .ForMember(x => x.People, x => x.MapFrom(y => y.Links.Where(IsPersonType)))
            .ForMember(x => x.Categories, x => x.MapFrom(y => y.Links.Where(z => z.Type == Constants.Bgg.Category)))
            .ForMember(x => x.Mechanics, x => x.MapFrom(y => y.Links.Where(z => z.Type == Constants.Bgg.Mechanic)));

        CreateMap<BggRawLink, BggLink>();

        CreateMap<BggRawLink, BggPerson>()
            .ForMember(x => x.Type, x => x.MapFrom(y => y.Type.StringToPersonTypeEnum()))
            .ForMember(x => x.Id, x => x.Ignore());

        CreateMap<BggLink, GameCategory>()
            .ForMember(x => x.Name, x => x.MapFrom(y => y.Value));
        
        CreateMap<BggLink, GameMechanic>()
            .ForMember(x => x.Name, x => x.MapFrom(y => y.Value));
        
        CreateMap<BggPerson, Person>()
            .ForMember(x => x.Name, x => x.MapFrom(y => y.Value));

        CreateMap<BggGame, Game>()
            .ForMember(x => x.Title, x => x.MapFrom(y => y.Names.FirstOrDefault()))
            .ForMember(x => x.Image, x => x.Ignore());
        
        
        //ViewModels
        CreateMap<Game, GameViewModel>();
        CreateMap<GameCategory, GameLinkViewModel>();
        CreateMap<GameMechanic, GameLinkViewModel>();
        CreateMap<Person, GamePersonViewModel>();
        CreateMap<Player, PlayerViewModel>();
        CreateMap<Location, LocationViewModel>()
            .ForMember(x => x.PlayCount, x => x.MapFrom(y => y.Plays.Count))
            .ReverseMap()
            .ForMember(x => x.Plays, x => x.Ignore());
        
        CreateMap<PlayerCreationViewModel, Player>()
            .ForMember(x => x.Id, x => x.MapFrom(y => (int?)null));

        CreateMap<PlayViewModel, Play>()
            .ForMember(x => x.End, x => x.MapFrom(y => y.Start.AddMinutes(y.Minutes)))
            .ReverseMap()
            .ForMember(x => x.Minutes, x => x.MapFrom(y => (y.End - y.Start).TotalMinutes));
        CreateMap<PlayerPlayViewModel, PlayerPlay>().ReverseMap();

        CreateMap<PlayerStatistics, PlayerStatisticsViewModel>();
        CreateMap<GameStatistics, GameStatisticsViewModel>()
            .ForMember(x => x.TotalPlayedTime, x => x.MapFrom(y => y.TotalPlayedTime.TotalMinutes));
    }
    
    private static bool IsPersonType(BggRawLink link)
    {
        var peopleTypes = new[] {Constants.Bgg.Artist, Constants.Bgg.Designer, Constants.Bgg.Publisher};
        return peopleTypes.Contains(link.Type);
    }
}