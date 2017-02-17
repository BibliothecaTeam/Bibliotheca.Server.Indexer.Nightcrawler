using System.Threading.Tasks;
using Bibliotheca.Server.Indexer.Nightcrawler.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bibliotheca.Server.Indexer.Nightcrawler.Api.Controllers
{
    [Authorize]
    [ApiVersion("1.0")]
    [Route("api/queues")]
    public class QueuesController : Controller
    {
        private readonly IQueuesService _queuesService;

        public QueuesController(IQueuesService queuesService)
        {
            _queuesService = queuesService;
        }

        [HttpPost("{projectId}/{branchName}")]
        public async Task<ActionResult> Post(string projectId, string branchName)
        {
            await _queuesService.AddToQueue(projectId, branchName);
            return Ok();
        }
    }
}
