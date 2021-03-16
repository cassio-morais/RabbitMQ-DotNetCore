using RabbitMQ.Domain.Events;
using System.Threading.Tasks;

namespace RabbitMQ.Domain.Bus
{
    public interface IEventHandler<in TypeEvent> : IEventHandler
        where TypeEvent : Event
    {
        Task Handle(TypeEvent @event);
    }

    public interface IEventHandler
    {

    }


}
