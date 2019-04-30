using System.Threading.Tasks;
using MathQuiz.AppLayer.Services;
using MathQuiz.AppLayer.Services.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MathQuiz.WebApi.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class QuizController : Controller
    {
        private readonly IQuizService _quizService;

        public QuizController(IQuizService quizService)
        {
            _quizService = quizService;
        }

        [HttpPost("start")]
        public Task<QuizDto> Start() => _quizService.StartQuiz(User.Identity.Name);

        [HttpPost("exit")]
        public Task Exit() => _quizService.ExitQuiz(User.Identity.Name);
    }
}