using MediatR;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Domain.Bus;
using RabbitMQ.Domain.Commands;
using RabbitMQ.Domain.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQ.Infra.Bus
{
    public class RabbitMQBus : IEventBus
    {
        private readonly IMediator _mediator;
        private readonly Dictionary<string, List<Type>> _handlers;
        private readonly List<Type> _eventTypes;

        public RabbitMQBus(IMediator mediator,
            Dictionary<string, List<Type>> handlers,
            List<Type> eventTypes)
        {
            _mediator = mediator;
            _handlers = handlers;
            _eventTypes = eventTypes;
        }

        public Task SendCommand<Comm>(Comm command) where Comm : Command
        {
            return _mediator.Send(command);
        }

        // @event is a generic object that inherit from Event class that will publish to the queue
        public void Publish<TypeEvent>(TypeEvent @event) where TypeEvent : Event
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                var eventName = @event.GetType().Name;

                channel.QueueDeclare(eventName, false, false, false, null);
                var message = JsonConvert.SerializeObject(@event);

                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish("", eventName, null, body); // eventName is the name of the routingkey 

            }
        }

        // subscribe to consume the queue 
        public void Subscribe<TypeEvent, EventHandler>()
            where TypeEvent : Event
            where EventHandler : IEventHandler<TypeEvent>
        {
            var eventName = typeof(TypeEvent).Name;
            var handlerType = typeof(EventHandler);

            // verify the the list of de type of events not contains the event to subscribe for
            if (!_eventTypes.Contains(typeof(TypeEvent)))
            {
                _eventTypes.Add(typeof(TypeEvent));
            }

            // verify the handlers not contains a key with eventName and add event e create a new list of types
            if (!_handlers.ContainsKey(eventName))
            {
                _handlers.Add(eventName, new List<Type>());
            }

            if (_handlers[eventName].Any(e => e.GetType() == handlerType))
            {
                throw new ArgumentException($"Handler Type {handlerType.Name} already is registered for '{eventName}'");
            }

            _handlers[eventName].Add(handlerType);

        }

        private void StartBasicConsume<TypeEvent>() where TypeEvent : Event
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                DispatchConsumersAsync = true
            };

            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            var eventName = typeof(TypeEvent).Name;

            channel.QueueDeclare(eventName, false, false, false, null);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.Received += Consumer_received;

            channel.BasicConsume(eventName, true, consumer);

        }

        private async Task Consumer_received(object sender, BasicDeliverEventArgs @event)
        {
            var eventName = @event.RoutingKey;
            var message = Encoding.UTF8.GetString(@event.Body.Span);

            try
            {
                await ProcessEvent(eventName, message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }

        }

        private async Task ProcessEvent(string eventName, string message)
        {
            if (_handlers.ContainsKey(eventName))
            {
                var subscriptions = _handlers[eventName];

                foreach (var sub in subscriptions)
                {
                    var handler = Activator.CreateInstance(sub);
                    if (handler == null) continue;

                    var eventType = _eventTypes.SingleOrDefault(type => type.Name == eventName);
                    var @event = JsonConvert.DeserializeObject(message, eventType);

                    var concreteType = typeof(IEventHandler<>).MakeGenericType(eventType);

                    await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { @event });
                }

            }
        }
    }
}
