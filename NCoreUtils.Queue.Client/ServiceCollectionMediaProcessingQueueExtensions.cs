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
            configuration.Bind(config);
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