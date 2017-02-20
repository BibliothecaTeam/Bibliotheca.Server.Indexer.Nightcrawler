using System.Threading.Tasks;

namespace Bibliotheca.Server.Indexer.Nightcrawler.Core.Services
{
    public interface IDiscoveryService
    {
        Task<string> GetGatewayAddress();
    }
}