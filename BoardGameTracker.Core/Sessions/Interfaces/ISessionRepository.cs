using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore.Interfaces;

namespace BoardGameTracker.Core.Sessions.Interfaces;

public interface ISessionRepository: ICrudHelper<Session>
{
    Task<int> CountAsync();
    Task<double> GetTotalPlayTime();
    Task<double> GetMeanPlayTime();
}