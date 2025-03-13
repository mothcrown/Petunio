using System.Xml;

namespace Petunio.Interfaces;

public interface IOllamaService
{
    public Task<string> Message(string prompt);
}