using System.Threading.Tasks;
using Bibliotheca.Server.Indexer.Nightcrawler.Core.Jobs;
using Bibliotheca.Server.Indexer.Nightcrawler.Core.Services;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bibliotheca.Server.Indexer.Nightcrawler.Api.Controllers
{
    [Authorize]
    [ApiVersion("1.0")]
    [Route("api/queues/{projectId}/{branchName}")]
    public class QueuesController : Controller
    {
        private readonly IQueuesService _queuesService;

        public QueuesController(IQueuesService queuesService)
        {
            _queuesService = queuesService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string projectId, string branchName)
        {
            var queueStatus = await _queuesService.GetQueueStatusAsync(projectId, branchName);
            if(queueStatus != null)
            {
                return new ObjectResult(queueStatus);
            }

            return NotFound();
        }

        [HttpPost]
        public IActionResult Post(string projectId, string branchName)
        {
            BackgroundJob.Enqueue<IIndexRefreshJob>(x => x.RefreshIndexAsync(null, projectId, branchName));
            return Ok();
        }
    }
}
