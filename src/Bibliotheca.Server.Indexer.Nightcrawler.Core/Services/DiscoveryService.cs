using System;
using System.Threading.Tasks;
using Bibliotheca.Server.Indexer.Nightcrawler.Core.Exceptions;
using Bibliotheca.Server.Indexer.Nightcrawler.Core.Parameters;
using Bibliotheca.Server.ServiceDiscovery.ServiceClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Bibliotheca.Server.Indexer.Nightcrawler.Core.Services
{
    public class DiscoveryService : IDiscoveryService
    {
        private readonly IServiceDiscoveryQuery _serviceDiscoveryQuery;

        private readonly ApplicationParameters _applicationParameters;

        private readonly IMemoryCache _memoryCache;

        public DiscoveryService(
            IServiceDiscoveryQuery serviceDiscoveryQuery, 
            IOptions<ApplicationParameters> applicationParameters,
            IMemoryCache memoryCache)
        {
            _serviceDiscoveryQuery = serviceDiscoveryQuery;
            _applicationParameters = applicationParameters.Value;
            _memoryCache = memoryCache;
        }

        public async Task<string> GetGatewayAddress()
        {
            string gatewayAddress;
            if (!_memoryCache.TryGetValue("gateway-adress", out gatewayAddress))
            {
                gatewayAddress = await DownloadGatewayAddress();

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(10));

                _memoryCache.Set("gateway-adress", gatewayAddress, cacheEntryOptions);
            }

            return gatewayAddress;
        }

        private async Task<string> DownloadGatewayAddress()
        {
            var service = await _serviceDiscoveryQuery.GetServiceAsync(
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
    }
}