namespace Petunio.Interfaces;

public interface IWalkChallengeService
{
    public Task<string> UpdateTrack(string discordUserId, float distance);
    public bool RouteExists(string route);
    public bool SetRouteActive(string discordUserId, string route);
    public List<string> GetRoutes();
}