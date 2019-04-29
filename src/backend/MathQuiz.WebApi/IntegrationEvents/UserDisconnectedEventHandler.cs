using System.Threading.Tasks;
using MathQuiz.AppLayer.Messages;
using MathQuiz.EventBus.Abstractions;
using MathQuiz.WebApi.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace MathQuiz.WebApi.IntegrationEvents
{
    public class UserDisconnectedEventHandler : IIntegrationEventHandler<UserDisconnected>
    {
        private readonly IHubContext<QuizHub, IQuizHubClient> _hubContext;

        public UserDisconnectedEventHandler(
            IHubContext<QuizHub, IQuizHubClient> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task Handle(UserDisconnected @event)
        {
            await _hubContext.Groups.RemoveFromGroupAsync(@event.Username, @event.QuizId);
            await _hubContext.Clients.Group(@event.QuizId).UserDisconnected(@event.Username);
        }
    }
}