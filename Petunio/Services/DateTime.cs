using Petunio.Interfaces;

namespace Petunio.Services;

public class DateTime : IDateTime
{
    public System.DateTime Now => System.DateTime.Now;
}