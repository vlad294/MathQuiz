using System.Threading.Tasks;
using MathQuiz.AppLayer.Messages;
using MathQuiz.EventBus.Abstractions;
using MathQuiz.WebApi.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace MathQuiz.WebApi.IntegrationEvents
{
    public class UserConnectedEventHandler : IIntegrationEventHandler<UserConnected>
    {
        private readonly IHubContext<QuizHub, IQuizHubClient> _hubContext;

        public UserConnectedEventHandler(
            IHubContext<QuizHub, IQuizHubClient> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task Handle(UserConnected @event)
        {
            return _hubContext.Clients.Group(@event.QuizId)
                .UserConnected(@event.Username);
        }
    }
}