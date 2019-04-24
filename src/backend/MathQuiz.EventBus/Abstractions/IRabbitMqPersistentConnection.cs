using System;
using RabbitMQ.Client;

namespace MathQuiz.EventBus.Abstractions
{
    public interface IRabbitMqPersistentConnection
        : IDisposable
    {
        bool IsConnected { get; }

        bool TryConnect();

        IModel CreateModel();
    }
}