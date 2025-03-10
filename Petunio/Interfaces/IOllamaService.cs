using System.Xml;

namespace Petunio.Interfaces;

public interface IOllamaService
{
    public Task<XmlDocument> Message(string prompt);
}