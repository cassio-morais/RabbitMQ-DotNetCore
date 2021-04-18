using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Domain.Bus;
using RabbitMQ.Infra.Bus;

namespace RabbitMQ.Infra.IoC
{
    public class DependencyContainer
    {
        public static void RegisterServices(IServiceCollection services)
        {
            services.AddTransient<IEventBus, RabbitMQBus>();
        }
    }
}
