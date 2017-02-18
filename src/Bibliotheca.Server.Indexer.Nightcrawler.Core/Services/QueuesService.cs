using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bibliotheca.Server.Depository.Abstractions.DataTransferObjects;
using Bibliotheca.Server.Indexer.Abstractions.DataTransferObjects;
using Bibliotheca.Server.Indexer.Nightcrawler.Core.Exceptions;
using Bibliotheca.Server.Indexer.Nightcrawler.Core.Parameters;
using Bibliotheca.Server.ServiceDiscovery.ServiceClient;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace Bibliotheca.Server.Indexer.Nightcrawler.Core.Services
{
    public class QueuesService : IQueuesService
    {
        private readonly IServiceDiscoveryQuery _serviceDiscoveryQuery;

        private readonly ApplicationParameters _applicationParameters;

        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly ILogger _logger;

        public QueuesService(
            IHttpContextAccessor httpContextAccessor, 
            IServiceDiscoveryQuery serviceDiscoveryQuery, 
            IOptions<ApplicationParameters> applicationParameters,
            ILoggerFactory loggerFactory)
        {
            _httpContextAccessor = httpContextAccessor;
            _serviceDiscoveryQuery = serviceDiscoveryQuery;
            _applicationParameters = applicationParameters.Value;
            _logger = loggerFactory.CreateLogger<QueuesService>();
        }

        public async Task AddToQueue(string projectId, string branchName)
        {
            var gatewayAddress = await GetGatewayAddressAsync();
            var project = await GetProjectAsync(gatewayAddress, projectId);
            var allDocuments = await GetAllDocumentsAsync(gatewayAddress, projectId, branchName);

            await RemoveIndexAsync(gatewayAddress, projectId, branchName);

            foreach(var document in allDocuments)
            {
                if(!IsIndexable(document))
                {
                    continue;
                }

                _logger.LogInformation($"Indexing file: {document.Uri}");
                var documentContent = await GetDocumentContentAsync(gatewayAddress, projectId, branchName, document);
                var documentIndex = CreateDocumentIndex(projectId, branchName, project, document, documentContent);

                await UploadDocumentIndex(gatewayAddress, projectId, branchName, documentIndex);
            }

            _logger.LogInformation($"Reindexing finished");
        }

        private bool IsIndexable(BaseDocumentDto document)
        {
            var fileExtension = Path.GetExtension(document.Uri);
            if(fileExtension == ".md")
            {
                return true;
            }

            return false;
        }

        private async Task UploadDocumentIndex(string gatewayAddress, string projectId, string branchName, DocumentIndexDto documentIndex)
        {
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

        private DocumentIndexDto CreateDocumentIndex(string projectId, string branchName, ProjectDto project, BaseDocumentDto document, string documentContent)
        {
            string indexed = ClearContent(documentContent);
            string title = ClearTitle(document.Uri);

            var documentIndexDto = new DocumentIndexDto
            {
                Content = indexed,
                BranchName = branchName,    
                ProjectName = project.Name,
                ProjectId = projectId,
                Title = title,
                Url = document.Uri,
                Id = ToAlphanumeric(document.Uri),
                Tags = project.Tags.ToArray()
            };

            return documentIndexDto;
        }

        private string ClearTitle(string url)
        {
            var title = Path.GetFileNameWithoutExtension(url);
            title = Regex.Replace(title, "[^a-zA-Z0-9- _]", string.Empty);
            title = title.Replace("-", " ");
            title = title.Replace("_", " ");
            title = UppercaseFirst(title);
            return title;
        }

        private static string ClearContent(string content)
        {
            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(content);
            var indexed = htmlDocument.DocumentNode.InnerText;

            return indexed;
        }

        private async Task<string> GetDocumentContentAsync(string gatewayAddress, string projectId, string branchName, BaseDocumentDto document)
        {
            var fileUri = document.Uri.Replace("/", ":");
            var docuemntsAddress = $"{gatewayAddress}/projects/{projectId}/branches/{branchName}/documents/content/{fileUri}";
            var client = GetClient();

            var responseString = await client.GetStringAsync(docuemntsAddress);
            return responseString;
        }

        private async Task<ProjectDto> GetProjectAsync(string gatewayAddress, string projectId)
        {
            var projectAddress = $"{gatewayAddress}/projects/{projectId}";
            var client = GetClient();
        
            var responseString = await client.GetStringAsync(projectAddress);
            var deserializedObject = JsonConvert.DeserializeObject<ProjectDto>(responseString);

            return deserializedObject;
        }

        private async Task RemoveIndexAsync(string gatewayAddress, string projectId, string branchName)
        {
            _logger.LogInformation($"Removing index. Prpojec Id: {projectId}. Branch Name: {branchName}.");

            var removeIndexAddress = $"{gatewayAddress}/search/projects/{projectId}/branches/{branchName}";
            var client = GetClient();

            HttpResponseMessage httpResponseMessage = await client.DeleteAsync(removeIndexAddress);
            if(!httpResponseMessage.IsSuccessStatusCode)
            {
                var message = await httpResponseMessage.Content.ReadAsStringAsync();
                throw new RemoveIndexException($"Index wasn't successfully removed. Status code: {httpResponseMessage.StatusCode}. Response message: '{message}'.");
            }
        }

        private async Task<IList<BaseDocumentDto>> GetAllDocumentsAsync(string gatewayAddress, string projectId, string branchName)
        {
            var docuemntsAddress = $"{gatewayAddress}/projects/{projectId}/branches/{branchName}/documents";
            var client = GetClient();

            var responseString = await client.GetStringAsync(docuemntsAddress);
            var deserializedObject = JsonConvert.DeserializeObject<IList<BaseDocumentDto>>(responseString);

            return deserializedObject;
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

        private async Task<string> GetGatewayAddressAsync()
        {
            var service = await _serviceDiscoveryQuery.GetService(
                new ServerOptions { Address = _applicationParameters.ServiceDiscovery.ServerAddress },
                new string[] { "gateway" }
            );

            if (service == null)
            {
                throw new GatewayServiceNotAvailableException($"Microservice with tag 'gateway' service is not running!");
            }

            var address = $"http://{service.Address}:{service.Port}/api";
            return address;
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

        public string UppercaseFirst(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        public string ToAlphanumeric(string s)
        {
            Regex rgx = new Regex("[^a-zA-Z0-9-]");
            return rgx.Replace(s, string.Empty);
        }
    }
}