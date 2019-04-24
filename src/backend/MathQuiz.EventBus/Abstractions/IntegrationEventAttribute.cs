using System;

namespace MathQuiz.EventBus.Abstractions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
    public class IntegrationEventAttribute : Attribute
    {
        public bool AddMachineName { get; set; }

        public string QueueName { get; set; }

        public string ExchangeName { get; set; }
    }
}