using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MathQuiz.AppLayer.Abstractions;
using MathQuiz.AppLayer.Messages;
using MathQuiz.AppLayer.Services.Dto;
using MathQuiz.Configuration;
using MathQuiz.DataAccess.Abstractions;
using MathQuiz.EventBus.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MathQuiz.AppLayer.Services
{
    public class QuizService : IQuizService
    {
        private readonly IQuizDao _quizDao;
        private readonly IEventBus _eventBus;
        private readonly IMapper _mapper;
        private readonly IOptions<QuizSettings> _quizSettingsOptions;
        private readonly ILogger<QuizService> _logger;

        public QuizService(IQuizDao quizDao, 
            IEventBus eventBus,
            IMapper mapper,
            IOptions<QuizSettings> quizSettingsOptions, 
            ILogger<QuizService> logger)
        {
            _quizDao = quizDao;
            _eventBus = eventBus;
            _quizSettingsOptions = quizSettingsOptions;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task HandleUserAnswer(string username, bool answer)
        {
            var quizId = await _quizDao.GetUserQuizId(username);

            var validAnswerUpdateTask = _quizDao.ChallengeUserValidAnswerAndIncreaseScore(quizId, username, answer);
            var invalidAnswerUpdateTask = _quizDao.ChallengeUserInvalidAnswerAndDecreaseScore(quizId, username, answer);

            var validAnswerUpdate = await validAnswerUpdateTask;
            var invalidAnswerUpdate = await invalidAnswerUpdateTask;
            var quiz = validAnswerUpdate ?? invalidAnswerUpdate;

            if (validAnswerUpdate != null)
            {
                _logger.LogInformation("User {User} gave correct answer {Answer} for question {Question}", 
                    username, answer, validAnswerUpdate.Challenge.Question);

                var delayBetweenGames = _quizSettingsOptions.Value.DelayBetweenGamesInSeconds;
                _eventBus.Publish(new ChallengeStarting
                {
                    QuizId = quizId,
                    StartDate = DateTimeOffset.UtcNow.AddSeconds(delayBetweenGames)
                });
            }

            if (invalidAnswerUpdate != null)
            {
                _logger.LogInformation("User {User} gave incorrect answer {Answer} for question {Question}",
                    username, answer, validAnswerUpdate?.Challenge?.Question);
            }

            var currentUser = quiz?.Users.FirstOrDefault(x => x.Login == username);
            if (currentUser != null)
            {
                _eventBus.Publish(new UserScoreUpdated
                {
                    Username = username,
                    Score = currentUser.Score,
                    QuizId = quizId
                });
            }
        }

        public Task<string> GetUserQuizId(string username)
        {
            return _quizDao.GetUserQuizId(username);
        }

        public async Task SetChallengeToQuiz(string quizId, string question, bool isCorrect)
        {
            await _quizDao.SetChallengeToQuiz(quizId, question, isCorrect);
        }

        public async Task<QuizDto> StartQuiz(string username)
        {
            var quiz = await _quizDao.GetOrCreateQuizForUser(username);

            _logger.LogInformation("User {User} entered to quiz {QuizId}",
                username, quiz.Id);

            if (quiz.Challenge == null)
            {
                _logger.LogInformation("New quiz {QuizId} created, publishing ChallengeStarting event",
                    quiz.Id);
                _eventBus.Publish(new ChallengeStarting
                {
                    QuizId = quiz.Id,
                    StartDate = DateTimeOffset.UtcNow
                });
            }

            _eventBus.Publish(new UserConnected
            {
                QuizId = quiz.Id,
                Username = username
            });

            return _mapper.Map<QuizDto>(quiz);
        }

        public async Task ExitQuiz(string username)
        {
            var quiz = await _quizDao.RemoveUserFromQuiz(username);

            _logger.LogInformation("User {User} exited from quiz {QuizId}",
                username, quiz.Id);

            _eventBus.Publish(new UserDisconnected
            {
                QuizId = quiz.Id,
                Username = username
            });
        }
    }
}
