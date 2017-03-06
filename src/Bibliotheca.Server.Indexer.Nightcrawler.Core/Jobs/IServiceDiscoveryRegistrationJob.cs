using System.Threading.Tasks;
using Hangfire.Server;

namespace Bibliotheca.Server.Indexer.Nightcrawler.Core.Jobs
{
    public interface IServiceDiscoveryRegistrationJob
    {
        Task RegisterServiceAsync(PerformContext context);
    }
}