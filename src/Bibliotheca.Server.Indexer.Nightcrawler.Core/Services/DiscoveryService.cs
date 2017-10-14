using System;
using System.Linq;
using System.Threading.Tasks;
using Bibliotheca.Server.Indexer.Nightcrawler.Core.Exceptions;
using Bibliotheca.Server.Indexer.Nightcrawler.Core.Parameters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neutrino.AspNetCore.Client;
using Flurl;

namespace Bibliotheca.Server.Indexer.Nightcrawler.Core.Services
{
    public class DiscoveryService : IDiscoveryService
    {
        private readonly INeutrinoClient _neutrinoClient;

        private readonly ApplicationParameters _applicationParameters;

        private readonly IMemoryCache _memoryCache;

        private readonly ILogger<DiscoveryService> _logger;

        public DiscoveryService(
            INeutrinoClient neutrinoClient, 
            IOptions<ApplicationParameters> applicationParameters,
            IMemoryCache memoryCache,
            ILogger<DiscoveryService> logger)
        {
            _neutrinoClient = neutrinoClient;
            _applicationParameters = applicationParameters.Value;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<string> GetGatewayAddress()
        {
            string gatewayAddress;
            if (!_memoryCache.TryGetValue("gateway-adress", out gatewayAddress))
            {
                gatewayAddress = await DownloadGatewayAddressAsync();

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(10));

                _memoryCache.Set("gateway-adress", gatewayAddress, cacheEntryOptions);
            }

            return gatewayAddress;
        }

        private async Task<string> DownloadGatewayAddressAsync()
        {
            try
            {    
                _logger.LogInformation($"Getting address for 'gateway' microservice.");

                var services = await _neutrinoClient.GetServicesByServiceTypeAsync("gateway");
                if (services == null || services.Count == 0)
                {
                    _logger.LogWarning($"Address for 'gateway' microservice wasn't retrieved.");
                    throw new GatewayServiceNotAvailableException($"Microservice with tag 'gateway' service is not running!");
                }

                var instance = services.FirstOrDefault();
                var address = instance.Address.AppendPathSegment("api/");
                _logger.LogInformation($"Address for 'gateway' microservice was retrieved ({address}).");
                return address;
            }
            catch(Exception exception)
            {
                _logger.LogError($"Address for 'gateway' microservice wasn't retrieved. There was an exception during retrieving address.");
                _logger.LogError($"Exception: {exception}");
                if(exception.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {exception.InnerException}");
                }

                return null;
            }
        }
    }
}