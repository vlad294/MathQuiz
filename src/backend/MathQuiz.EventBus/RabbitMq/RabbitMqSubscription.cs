using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace MathQuiz.EventBus.RabbitMq
{
    public class RabbitMqSubscription
    {
        public IModel Channel { get; set; }
        public List<Type> HandlerTypes { get; set; }
    }
}