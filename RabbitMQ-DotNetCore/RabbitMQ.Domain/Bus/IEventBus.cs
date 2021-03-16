using RabbitMQ.Domain.Commands;
using RabbitMQ.Domain.Events;
using System.Threading.Tasks;

namespace RabbitMQ.Domain.Bus
{
    public interface IEventBus
    {
        Task SendCommand<Comm>(Comm command) where Comm : Command;
        void Publish<TypeEvent>(TypeEvent @event) where TypeEvent : Event; // note: event with @ because event is a reserved keyword
        void Subscribe<TypeEvent, EventHandler>()
            where TypeEvent : Event
            where EventHandler : IEventHandler<TypeEvent>;
    }
}
