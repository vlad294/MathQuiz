using System.Threading.Tasks;
using MathQuiz.AppLayer.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MathQuiz.WebApi.SignalR
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class QuizHub : Hub<IQuizHubClient>
    {
        private readonly IQuizService _quizService;

        private string Username => Context.User.Identity.Name;

        public QuizHub(IQuizService quizService)
        {
            _quizService = quizService;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();

            var (quizId, quiz) = await _quizService.GetUserQuizWithId(Username);
            await Groups.AddToGroupAsync(Context.ConnectionId, quizId);
            await SendQuestionToConnectedUser(quiz.Challenge.Question);
        }

        public Task SendAnswer(bool answer)
        {
            return _quizService.HandleUserAnswer(Username, answer);
        }

        private async Task SendQuestionToConnectedUser(string question)
        {
            if (!string.IsNullOrEmpty(question))
            {
                await Clients.Client(Context.ConnectionId)
                    .ChallengeUpdated(question);
            }
        }
    }
}
