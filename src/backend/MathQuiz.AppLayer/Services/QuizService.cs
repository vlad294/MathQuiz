using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MathQuiz.AppLayer.Messages;
using MathQuiz.AppLayer.Services.Dto;
using MathQuiz.Configuration;
using MathQuiz.DataAccess.Storage;
using MathQuiz.Domain;
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

        public async Task<(string, QuizDto)> GetUserQuizWithId(string username)
        {
            var quiz = await _quizDao.GetUserQuiz(username);

            return (quiz.Id, _mapper.Map<QuizDto>(quiz));
        }

        public async Task SetChallengeToQuiz(string quizId, string question, bool isCorrect)
        {
            await _quizDao.SetChallengeToQuiz(quizId, question, isCorrect);
        }

        public async Task<QuizDto> StartQuiz(string username)
        {
            var quiz = await _quizDao.GetUserQuiz(username)
                    ?? await _quizDao.AddUserToQuizOrCreateNew(username);

            _logger.LogInformation("User {User} joined to quiz {QuizId}",
                username, quiz.Id);

            if (quiz.Challenge == null)
            {
                _logger.LogInformation("New quiz {QuizId} created, publishing ChallengeStarting event", quiz.Id);

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

            if (quiz != null)
            {
                _logger.LogInformation("User {User} exited from quiz {QuizId}",
                    username, quiz.Id);

                _eventBus.Publish(new UserDisconnected
                {
                    QuizId = quiz.Id,
                    Username = username
                });
            }
        }

        public async Task HandleUserAnswer(string username, bool answer)
        {
            var quiz = await _quizDao.GetUserQuiz(username);

            if (quiz.Challenge == null)
                return;

            if (quiz.Challenge.IsCorrect == answer)
            {
                await HandleCorrectAnswer(username, answer, quiz);
            }
            else
            {
                await HandleIncorrectAnswer(username, answer, quiz);
            }
        }

        private async Task HandleCorrectAnswer(string username, bool answer, Quiz quiz)
        {
            var updatedQuiz = await _quizDao.CompleteQuizAndIncreaseUserScore(quiz.Id, username);

            if (updatedQuiz != null)
            {
                _logger.LogInformation("User {User} gave correct answer {Answer} for question {Question}",
                    username, answer, quiz.Challenge.Question);

                PublishUserScoreUpdated(updatedQuiz, username);

                _eventBus.Publish(new ChallengeFinished
                {
                    QuizId = quiz.Id
                });

                var delayBetweenGames = _quizSettingsOptions.Value.DelayBetweenGamesInSeconds;
                _eventBus.Publish(new ChallengeStarting
                {
                    QuizId = quiz.Id,
                    StartDate = DateTimeOffset.UtcNow.AddSeconds(delayBetweenGames)
                });
            }
        }

        private async Task HandleIncorrectAnswer(string username, bool answer, Quiz quiz)
        {
            var updatedQuiz = await _quizDao.DecreaseUserScore(quiz.Id, username);

            if (updatedQuiz != null)
            {
                _logger.LogInformation("User {User} gave incorrect answer {Answer} for question {Question}",
                    username, answer, quiz.Challenge.Question);

                PublishUserScoreUpdated(updatedQuiz, username);
            }
        }

        private void PublishUserScoreUpdated(Quiz quiz, string username)
        {
            var updatedUserScore = quiz.Users.FirstOrDefault(x => x.Username == username)?.Score;
            if (updatedUserScore != null)
            {
                _eventBus.Publish(new UserScoreUpdated
                {
                    Username = username,
                    Score = updatedUserScore.Value,
                    QuizId = quiz.Id
                });
            }
        }
    }
}
