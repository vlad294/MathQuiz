using System.Threading.Tasks;
using MathQuiz.AppLayer.Messages;
using MathQuiz.EventBus.Abstractions;
using MathQuiz.WebApi.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace MathQuiz.WebApi.IntegrationEvents
{
    public class UserScoreUpdatedEventHandler : IIntegrationEventHandler<UserScoreUpdated>
    {
        private readonly IHubContext<QuizHub, IQuizHubClient> _hubContext;

        public UserScoreUpdatedEventHandler(
            IHubContext<QuizHub, IQuizHubClient> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task Handle(UserScoreUpdated @event)
        {
            return _hubContext.Clients.Group(@event.QuizId)
                .UserScoreUpdated(@event.Username, @event.Score);
        }
    }
}