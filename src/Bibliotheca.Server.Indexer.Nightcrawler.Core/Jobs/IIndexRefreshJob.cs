using System.Threading.Tasks;
using Hangfire.Server;

namespace Bibliotheca.Server.Indexer.Nightcrawler.Core.Jobs
{
    public interface IIndexRefreshJob
    {
        Task RefreshIndexAsync(PerformContext context, string projectId, string branchName);
    }
}