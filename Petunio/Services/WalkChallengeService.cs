using Petunio.Helpers;
using Petunio.Interfaces;
using Petunio.Models;

namespace Petunio.Services;

public class WalkChallengeService(ILogger<WalkChallengeService> logger) : IWalkChallengeService
{
    
    private const string ROUTE_DIR = "WalkChallengeRoutes";
    private const string TRACKERS_DIR = "WalkChallengeTrackers";
    
    public async Task<string> UpdateTrack(string discordUserId, float distance)
    {
        var filePath = Path.Combine(TRACKERS_DIR, $"{discordUserId}.json");
        if (!File.Exists(filePath)) return "You need to set a route as active first!";
        
        var tracks = JsonHelper.ReadJson<List<WalkChallengeTracker>>(filePath);
        var activeTracker = tracks.FirstOrDefault(x => x.Active);
        activeTracker!.KilometersWalked += distance;
        JsonHelper.SaveJson(filePath, tracks);
        
        string description = GetMilepostDescription(activeTracker);

        return description;

    }

    private string GetMilepostDescription(WalkChallengeTracker activeTracker)
    {
        return "Description";
    }

    public bool SetRouteActive(string discordUserId, string route)
    {
        var filePath = Path.Combine(TRACKERS_DIR, $"{discordUserId}.json");
        if (!File.Exists(filePath)) CreateWalkChallengeUser(route, filePath);
        
        var tracks = JsonHelper.ReadJson<List<WalkChallengeTracker>>(filePath);
        foreach (var walkChallengeTrack in tracks)
        {
            walkChallengeTrack.Active = walkChallengeTrack.Route == route;
        }
        
        JsonHelper.SaveJson(filePath, tracks);
        return true;
    }

    public List<string> GetRoutes()
    {
        return Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), ROUTE_DIR))
            .Select(file => Path.GetFileNameWithoutExtension(file))
            .ToList();
    }

    public bool RouteExists(string route)
    {
        var routes = GetRoutes();
        return routes.Any(r => r == route);
    }

    private static void CreateWalkChallengeUser(string route, string filePath)
    {
        File.Create(filePath).Dispose();
        List<WalkChallengeTracker> tracks = [new () { Route = route, Active = true, KilometersWalked = 0 }];
        JsonHelper.SaveJson(filePath, tracks);
    }
}