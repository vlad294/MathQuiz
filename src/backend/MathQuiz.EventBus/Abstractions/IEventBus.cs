namespace MathQuiz.EventBus.Abstractions
{
    public interface IEventBus
    {
        void Publish<TEvent>(TEvent @event);

        void Subscribe<TEvent, THandler>()
            where THandler : IIntegrationEventHandler<TEvent>;

        void Unsubscribe<TEvent, THandler>()
            where THandler : IIntegrationEventHandler<TEvent>;
    }
}