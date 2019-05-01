using System.Threading.Tasks;
using MathQuiz.AppLayer.Messages;
using MathQuiz.EventBus.Abstractions;
using MathQuiz.WebApi.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace MathQuiz.WebApi.IntegrationEvents
{
    public class ChallengeFinishedEventHandler : IIntegrationEventHandler<ChallengeFinished>
    {
        private readonly IHubContext<QuizHub, IQuizHubClient> _hubContext;

        public ChallengeFinishedEventHandler(
            IHubContext<QuizHub, IQuizHubClient> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task Handle(ChallengeFinished @event)
        {
            return _hubContext.Clients.Group(@event.QuizId)
                .ChallengeFinished();
        }
    }
}