using System;
using System.Threading.Tasks;
using Bibliotheca.Server.Indexer.Nightcrawler.Core.Exceptions;
using Bibliotheca.Server.Indexer.Nightcrawler.Core.Parameters;
using Bibliotheca.Server.ServiceDiscovery.ServiceClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bibliotheca.Server.Indexer.Nightcrawler.Core.Services
{
    public class DiscoveryService : IDiscoveryService
    {
        private readonly IServiceDiscoveryQuery _serviceDiscoveryQuery;

        private readonly ApplicationParameters _applicationParameters;

        private readonly IMemoryCache _memoryCache;

        private readonly ILogger<DiscoveryService> _logger;

        public DiscoveryService(
            IServiceDiscoveryQuery serviceDiscoveryQuery, 
            IOptions<ApplicationParameters> applicationParameters,
            IMemoryCache memoryCache,
            ILogger<DiscoveryService> logger)
        {
            _serviceDiscoveryQuery = serviceDiscoveryQuery;
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

                var instance = await _serviceDiscoveryQuery.GetServiceInstanceAsync(
                    new ServerOptions { Address = _applicationParameters.ServiceDiscovery.ServerAddress },
                    new string[] { "gateway" }
                );
                if (instance == null)
                {
                    _logger.LogWarning($"Address for 'gateway' microservice wasn't retrieved.");
                    throw new GatewayServiceNotAvailableException($"Microservice with tag 'gateway' service is not running!");
                }

                var address = $"http://{instance.Address}:{instance.Port}/api/";
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