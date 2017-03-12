using System.Threading.Tasks;
using Bibliotheca.Server.Indexer.Nightcrawler.Core.Services;
using Hangfire.Server;

namespace Bibliotheca.Server.Indexer.Nightcrawler.Core.Jobs
{
    public class IndexRefreshJob : IIndexRefreshJob
    {
        private readonly IQueuesService _queuesService;

        public IndexRefreshJob(IQueuesService queuesService)
        {
            _queuesService = queuesService;
        }

        public async Task RefreshIndexAsync(PerformContext context, string projectId, string branchName)
        {
            await _queuesService.AddToQueueAsync(projectId, branchName);
        }
    }
}