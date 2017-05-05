using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Bibliotheca.Server.Indexer.Nightcrawler.Core.DataTransferObjects;
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

        private readonly HttpClient _httpClient;

        public GatewayService(
            IHttpContextAccessor httpContextAccessor, 
            IOptions<ApplicationParameters> applicationParameters,
            IDiscoveryService discoveryService,
            ILoggerFactory loggerFactory,
            HttpClient httpClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _applicationParameters = applicationParameters.Value;
            _discoveryService = discoveryService;
            _logger = loggerFactory.CreateLogger<GatewayService>();
            _httpClient = httpClient;
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

            var response = await client.GetAsync(docuemntsAddress);
            if(response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                return responseString;
            }

            var message = await response.Content.ReadAsStringAsync();
            throw new DowonloadDocumentContentException($"Document wasn't successfully downloaded. Status code: {response.StatusCode}. Response message: '{message}'.");
        }

        public async Task<ProjectDto> GetProjectAsync(string projectId)
        {
            var gatewayAddress = await _discoveryService.GetGatewayAddress();
            var projectAddress = $"{gatewayAddress}/projects/{projectId}";
            var client = GetClient();
        
            var response = await client.GetAsync(projectAddress);
            if(response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var deserializedObject = JsonConvert.DeserializeObject<ProjectDto>(responseString);
                return deserializedObject;
            }

            var message = await response.Content.ReadAsStringAsync();
            throw new DownloadProjectDataException($"Project wasn't successfully downloaded. Status code: {response.StatusCode}. Response message: '{message}'.");
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

            var response = await client.GetAsync(docuemntsAddress);
            if(response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var deserializedObject = JsonConvert.DeserializeObject<IList<BaseDocumentDto>>(responseString);
                return deserializedObject;
            }

            var message = await response.Content.ReadAsStringAsync();
            throw new DownloadDocumentsException($"Documents list wasn't successfully downloaded. Status code: {response.StatusCode}. Response message: '{message}'.");
        }

        private BaseHttpClient GetClient()
        {
            var customHeaders = GetHttpHeaders();
            var client = new BaseHttpClient(_httpClient, customHeaders, _applicationParameters.SecureToken);
            return client;
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