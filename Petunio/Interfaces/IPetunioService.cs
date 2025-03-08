using System.Xml;

namespace Petunio.Interfaces;

public interface IPetunioService
{
    public Task<XmlDocument> Message(string prompt);
}