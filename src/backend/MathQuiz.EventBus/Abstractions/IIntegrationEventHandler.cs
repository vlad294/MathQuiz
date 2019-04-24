using System.Threading.Tasks;

namespace MathQuiz.EventBus.Abstractions
{
    public interface IIntegrationEventHandler<in TIntegrationEvent>
    {
        Task Handle(TIntegrationEvent @event);
    }
}