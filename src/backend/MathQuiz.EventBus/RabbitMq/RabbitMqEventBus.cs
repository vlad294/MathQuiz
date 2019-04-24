using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MathQuiz.EventBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace MathQuiz.EventBus.RabbitMq
{
    public class RabbitMqEventBus : IEventBus
    {
        private readonly IRabbitMqPersistentConnection _persistentConnection;
        private readonly ILogger<RabbitMqEventBus> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<Type, RabbitMqSubscription> _subscriptions;
        private readonly int _retryCount;

        public RabbitMqEventBus(IRabbitMqPersistentConnection persistentConnection, ILogger<RabbitMqEventBus> logger, IServiceProvider serviceProvider)
        {
            _persistentConnection = persistentConnection;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _retryCount = 5;
            _subscriptions = new Dictionary<Type, RabbitMqSubscription>();
        }

        public void Publish<TEvent>(TEvent @event)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            var policy = Policy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    _logger.LogWarning(ex, "Could not publish event: {EventType} after {Timeout}s ({ExceptionMessage})",
                        typeof(TEvent).Name,
                        $"{time.TotalSeconds:n1}",
                        ex.Message
                    );
                });

            using (var channel = _persistentConnection.CreateModel())
            {
                var exchangeName = GetExchangeName<TEvent>();

                channel.ExchangeDeclare(exchange: exchangeName, type: "fanout");

                var message = JsonConvert.SerializeObject(@event);
                var body = Encoding.UTF8.GetBytes(message);

                policy.Execute(() =>
                {
                    // ReSharper disable AccessToDisposedClosure
                    var properties = channel.CreateBasicProperties();

                    properties.DeliveryMode = 2; // persistent

                    channel.BasicPublish(exchange: exchangeName,
                        routingKey: string.Empty,
                        mandatory: true,
                        basicProperties: properties,
                        body: body);

                    _logger.LogInformation("Message {EventType} published to exchange {Exchange}.",
                        typeof(TEvent).Name, exchangeName);
                    // ReSharper restore AccessToDisposedClosure
                });
            }
        }

        public void Subscribe<TEvent, THandler>()
            where THandler : IIntegrationEventHandler<TEvent>
        {
            if (_subscriptions.TryGetValue(typeof(TEvent), out var subscription))
            {
                subscription.HandlerTypes.Add(typeof(THandler));
            }
            else
            {
                subscription = new RabbitMqSubscription
                {
                    Channel = CreateConsumerChannel<TEvent>(),
                    HandlerTypes = new List<Type> { typeof(THandler) }
                };

                subscription.Channel.CallbackException += (sender, ea) =>
                {
                    subscription.Channel.Dispose();
                    subscription.Channel = CreateConsumerChannel<TEvent>();
                };

                _subscriptions[typeof(TEvent)] = subscription;
            }
        }

        public void Unsubscribe<TEvent, THandler>()
            where THandler : IIntegrationEventHandler<TEvent>
        {
            if (_subscriptions.TryGetValue(typeof(TEvent), out var subscription))
            {
                subscription.HandlerTypes.Remove(typeof(THandler));
            }
        }

        private IModel CreateConsumerChannel<TEvent>()
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            var channel = _persistentConnection.CreateModel();
            var exchangeName = GetExchangeName<TEvent>();
            var queueName = GetQueueName<TEvent>();

            channel.ExchangeDeclare(exchange: exchangeName, type: "fanout");

            channel.QueueDeclare(queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            channel.QueueBind(queue: queueName,
                exchange: exchangeName,
                routingKey: string.Empty);


            _logger.LogInformation("Queue {Queue} bound to exchange {Exchange} for event {EventType}.", 
                queueName, exchangeName, typeof(TEvent).Name);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                var message = Encoding.UTF8.GetString(ea.Body);
                _logger.LogInformation("Message {EventType} received.", typeof(TEvent).Name);

                await ProcessEvent<TEvent>(message);

                channel.BasicAck(ea.DeliveryTag, multiple: false);

                _logger.LogInformation("Message {EventType} acknowledged.", typeof(TEvent).Name);
            };

            channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

            return channel;
        }

        private async Task ProcessEvent<TEvent>(string message)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var handlerTypes = _subscriptions[typeof(TEvent)].HandlerTypes;
                foreach (var handlerType in handlerTypes)
                {
                    var handler = scope.ServiceProvider.GetRequiredService(handlerType);
                    if (handler == null) continue;
                    var integrationEvent = JsonConvert.DeserializeObject(message, typeof(TEvent));
                    var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(typeof(TEvent));
                    await (Task)concreteType.GetMethod(nameof(IIntegrationEventHandler<object>.Handle))
                        .Invoke(handler, new[] { integrationEvent });
                }
            }
        }

        private string GetExchangeName<TEvent>()
        {
            var eventAttribute = typeof(TEvent).GetCustomAttribute<IntegrationEventAttribute>();
            return eventAttribute?.ExchangeName ?? typeof(TEvent).Name;
        }

        private string GetQueueName<TEvent>()
        {
            var eventAttribute = typeof(TEvent).GetCustomAttribute<IntegrationEventAttribute>();

            if (eventAttribute == null)
            {
                return typeof(TEvent).Name;
            }

            var queueName = eventAttribute.QueueName ?? typeof(TEvent).Name;

            return eventAttribute.AddMachineName 
                ? $"{queueName}_{Environment.MachineName}" 
                : queueName;
        }
    }
}