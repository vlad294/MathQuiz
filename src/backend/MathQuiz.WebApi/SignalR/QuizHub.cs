using System.Threading.Tasks;
using MathQuiz.AppLayer.Abstractions;
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
            var quizId = await _quizService.GetUserQuizId(Username);
            await Groups.AddToGroupAsync(Context.ConnectionId, quizId);
            await base.OnConnectedAsync();
        }

        public Task SendAnswer(bool isCorrect)
        {
            return _quizService.HandleUserAnswer(Username, isCorrect);
        }
    }
}
