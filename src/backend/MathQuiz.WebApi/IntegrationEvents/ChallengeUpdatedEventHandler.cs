using System.Threading.Tasks;
using MathQuiz.AppLayer.Messages;
using MathQuiz.EventBus.Abstractions;
using MathQuiz.WebApi.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace MathQuiz.WebApi.IntegrationEvents
{
    public class ChallengeUpdatedEventHandler : IIntegrationEventHandler<ChallengeUpdated>
    {
        private readonly IHubContext<QuizHub, IQuizHubClient> _hubContext;

        public ChallengeUpdatedEventHandler(
            IHubContext<QuizHub, IQuizHubClient> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task Handle(ChallengeUpdated @event)
        {
            return _hubContext.Clients.Group(@event.QuizId)
                .ChallengeUpdated(@event.Question);
        }
    }
}