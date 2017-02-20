using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Bibliotheca.Server.Depository.Abstractions.DataTransferObjects;
using Bibliotheca.Server.Indexer.Abstractions.DataTransferObjects;
using Bibliotheca.Server.Indexer.Nightcrawler.Core.Exceptions;
using Bibliotheca.Server.Indexer.Nightcrawler.Core.Parameters;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace Bibliotheca.Server.Indexer.Nightcrawler.Core.Services
{
    public class GatewayService : IGatewayService
    { 
        private readonly ApplicationParameters _applicationParameters;

        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly IDiscoveryService _discoveryService;

        private readonly ILogger _logger;

        public GatewayService(
            IHttpContextAccessor httpContextAccessor, 
            IOptions<ApplicationParameters> applicationParameters,
            IDiscoveryService discoveryService,
            ILoggerFactory loggerFactory)
        {
            _httpContextAccessor = httpContextAccessor;
            _applicationParameters = applicationParameters.Value;
            _discoveryService = discoveryService;
            _logger = loggerFactory.CreateLogger<GatewayService>();
        }

        public async Task UploadDocumentIndex(string projectId, string branchName, DocumentIndexDto documentIndex)
        {
            var gatewayAddress = await _discoveryService.GetGatewayAddress();
            var uploadIndexAddress = $"{gatewayAddress}/search/projects/{projectId}/branches/{branchName}";
            var client = GetClient();
            
            var documentsList = new List<DocumentIndexDto>();
            documentsList.Add(documentIndex);
            var stringContent = JsonConvert.SerializeObject(documentsList);
            HttpContent content = new StringContent(stringContent, Encoding.UTF8, "application/json");

            HttpResponseMessage httpResponseMessage = await client.PostAsync(uploadIndexAddress, content);
            if(!httpResponseMessage.IsSuccessStatusCode)
            {
                var message = await httpResponseMessage.Content.ReadAsStringAsync();
                throw new UploadDocumentToIndexException($"Document wasn't successfully uploaded. Status code: {httpResponseMessage.StatusCode}. Response message: '{message}'.");
            }
        }

        public async Task<string> GetDocumentContentAsync(string projectId, string branchName, BaseDocumentDto document)
        {
            var fileUri = document.Uri.Replace("/", ":");
            var gatewayAddress = await _discoveryService.GetGatewayAddress();
            var docuemntsAddress = $"{gatewayAddress}/projects/{projectId}/branches/{branchName}/documents/content/{fileUri}";
            var client = GetClient();

            var responseString = await client.GetStringAsync(docuemntsAddress);
            return responseString;
        }

        public async Task<ProjectDto> GetProjectAsync(string projectId)
        {
            var gatewayAddress = await _discoveryService.GetGatewayAddress();
            var projectAddress = $"{gatewayAddress}/projects/{projectId}";
            var client = GetClient();
        
            var responseString = await client.GetStringAsync(projectAddress);
            var deserializedObject = JsonConvert.DeserializeObject<ProjectDto>(responseString);

            return deserializedObject;
        }

        public async Task RemoveIndexAsync(string projectId, string branchName)
        {
            _logger.LogInformation($"Removing index. Prpojec Id: {projectId}. Branch Name: {branchName}.");

            var gatewayAddress = await _discoveryService.GetGatewayAddress();
            var removeIndexAddress = $"{gatewayAddress}/search/projects/{projectId}/branches/{branchName}";
            var client = GetClient();

            HttpResponseMessage httpResponseMessage = await client.DeleteAsync(removeIndexAddress);
            if(!httpResponseMessage.IsSuccessStatusCode)
            {
                var message = await httpResponseMessage.Content.ReadAsStringAsync();
                throw new RemoveIndexException($"Index wasn't successfully removed. Status code: {httpResponseMessage.StatusCode}. Response message: '{message}'.");
            }
        }

        public async Task<IList<BaseDocumentDto>> GetAllDocumentsAsync(string projectId, string branchName)
        {
            var gatewayAddress = await _discoveryService.GetGatewayAddress();
            var docuemntsAddress = $"{gatewayAddress}/projects/{projectId}/branches/{branchName}/documents";
            var client = GetClient();

            var responseString = await client.GetStringAsync(docuemntsAddress);
            var deserializedObject = JsonConvert.DeserializeObject<IList<BaseDocumentDto>>(responseString);

            return deserializedObject;
        }

        private HttpClient GetClient()
        {
            HttpClient client = new HttpClient();

            var customHeaders = GetHttpHeaders();
            RewriteHeader(client, "Authorization", customHeaders);

            return client;
        }

       private void RewriteHeader(HttpClient client, string header, IDictionary<string, StringValues> customHeaders)
        {
            if (customHeaders != null)
            {
                if(customHeaders.ContainsKey(header))
                {
                    var value = customHeaders[header];
                    client.DefaultRequestHeaders.Add(header, value as IList<string>);
                }
            }
        }

        private IDictionary<string, StringValues> GetHttpHeaders()
        {
            var httpContext = _httpContextAccessor.HttpContext;

            IDictionary<string, StringValues> headers = null;
            if (httpContext != null && httpContext.Request != null)
            {
                headers = httpContext.Request.Headers as IDictionary<string, StringValues>;
            }

            return headers;
        }
    }
}