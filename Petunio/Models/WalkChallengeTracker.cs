using System.ComponentModel.DataAnnotations;

namespace Petunio.Models;

public class WalkChallengeTracker
{
    public required string Route { get; set;}
    public bool Active { get; set; }
    public float KilometersWalked { get; set; }
}