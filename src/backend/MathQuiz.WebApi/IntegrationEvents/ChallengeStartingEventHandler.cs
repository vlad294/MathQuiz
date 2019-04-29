using System;
using System.Threading.Tasks;
using MathQuiz.AppLayer.Abstractions;
using MathQuiz.AppLayer.Messages;
using MathQuiz.EventBus.Abstractions;
using Microsoft.Extensions.Logging;

namespace MathQuiz.WebApi.IntegrationEvents
{
    public class ChallengeStartingEventHandler : IIntegrationEventHandler<ChallengeStarting>
    {
        private readonly IMathChallengeService _mathChallengeService;
        private readonly IQuizService _quizService;
        private readonly IEventBus _eventBus;
        private readonly ILogger<ChallengeStartingEventHandler> _logger;

        public ChallengeStartingEventHandler(
            IMathChallengeService mathChallengeService,
            IQuizService quizService,
            IEventBus eventBus, 
            ILogger<ChallengeStartingEventHandler> logger)
        {
            _mathChallengeService = mathChallengeService;
            _quizService = quizService;
            _eventBus = eventBus;
            _logger = logger;
        }

        public async Task Handle(ChallengeStarting @event)
        {
            var delay = @event.StartDate - DateTimeOffset.UtcNow;
            if (delay.TotalMilliseconds > 0)
            {
                _logger.LogInformation("Waiting {WaitSeconds} seconds before starting the quiz {QuizId}",
                    delay.TotalSeconds, @event.QuizId);
                await Task.Delay(delay);
            }

            var newChallenge = _mathChallengeService.CreateChallenge();

            _logger.LogInformation("New challenge {Challenge} with answer {Answer} created for quiz {QuizId}",
                newChallenge.Question, newChallenge.IsCorrect, @event.QuizId);

            await _quizService.SetChallengeToQuiz(
                @event.QuizId, 
                newChallenge.Question, 
                newChallenge.IsCorrect
            );

            _eventBus.Publish(new ChallengeUpdated
            {
                Question = newChallenge.Question,
                QuizId = @event.QuizId
            });
        }
    }
}
