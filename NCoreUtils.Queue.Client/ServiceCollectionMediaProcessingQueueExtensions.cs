using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.AspNetCore;
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
    }
}