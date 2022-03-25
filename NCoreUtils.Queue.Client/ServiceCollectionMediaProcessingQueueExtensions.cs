using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.AspNetCore;
using NCoreUtils.AspNetCore.Proto;
using NCoreUtils.Queue;
using NCoreUtils.Queue.Internal;

namespace NCoreUtils
{
    public static class ServiceCollectionMediaProcessingQueueExtensions
    {
        public static IServiceCollection AddMediaProcessingQueueClient(
            this IServiceCollection services,
            IEndpointConfiguration configuration)
            => services.AddProtoClient<IMediaProcessingQueue>(
                configuration,
                MediaProcessingQueueProtoConfiguration.Configure
            );

        public static IServiceCollection AddMediaProcessingQueueClient(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var config = new EndpointConfiguration();
            // configuration.Bind(config);
            if (configuration is IConfigurationSection section)
            {
                var httpClient = section[nameof(EndpointConfiguration.HttpClient)];
                var endpoint = section[nameof(EndpointConfiguration.Endpoint)];
                if (!string.IsNullOrEmpty(httpClient))
                {
                    config.HttpClient = httpClient;
                }
                if (!string.IsNullOrEmpty(endpoint))
                {
                    config.Endpoint = endpoint;
                }
            }
            return services.AddMediaProcessingQueueClient(config);
        }

        public static IServiceCollection AddMediaProcessingQueueClient(
            this IServiceCollection services,
            string endpoint)
        {
            var config = new EndpointConfiguration { Endpoint = endpoint };
            return services.AddMediaProcessingQueueClient(config);
        }
    }
}