using System.Threading.Tasks;
using Hangfire.Server;

namespace Bibliotheca.Server.Indexer.Nightcrawler.Api.Jobs
{
    public interface IServiceDiscoveryRegistrationJob
    {
        Task RegisterServiceAsync(PerformContext context);
    }
}