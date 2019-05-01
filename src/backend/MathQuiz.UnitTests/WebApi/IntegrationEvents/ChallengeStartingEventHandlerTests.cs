using System;
using System.Threading.Tasks;
using MathQuiz.AppLayer.Messages;
using MathQuiz.AppLayer.Services;
using MathQuiz.Domain;
using MathQuiz.EventBus.Abstractions;
using MathQuiz.WebApi.IntegrationEvents;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MathQuiz.UnitTests.WebApi.IntegrationEvents
{
    public class ChallengeStartingEventHandlerTests
    {
        private readonly IIntegrationEventHandler<ChallengeStarting> _handler;
        private readonly Mock<IMathChallengeService> _mathChallengeServiceMock;
        private readonly Mock<IQuizService> _quizServiceMock;
        private readonly Mock<IEventBus> _eventBusMock;

        public ChallengeStartingEventHandlerTests()
        {
            _mathChallengeServiceMock = new Mock<IMathChallengeService>();
            _quizServiceMock = new Mock<IQuizService>();
            _eventBusMock = new Mock<IEventBus>();
            var loggerMock = new Mock<ILogger<ChallengeStartingEventHandler>>();

            _handler = new ChallengeStartingEventHandler(
                _mathChallengeServiceMock.Object,
                _quizServiceMock.Object,
                _eventBusMock.Object,
                loggerMock.Object
            );
        }

        [Fact]
        public async Task Handle_JustCalled_CreateChallengeAndSetToQuiz()
        {
            // Arrange
            var quizId = "42";
            var mathChallenge = new MathChallenge()
            {
                Question = "2+2=4?",
                IsCompleted = false,
                IsCorrect = true
            };

            _mathChallengeServiceMock
                .Setup(m => m.CreateChallenge())
                .Returns(mathChallenge);

            var @event = new ChallengeStarting
            {
                QuizId = quizId,
                StartDate = DateTimeOffset.UtcNow
            };

            // Act
            await _handler.Handle(@event);

            // Assert
            _mathChallengeServiceMock.Verify(x => x.CreateChallenge());
            _mathChallengeServiceMock.VerifyNoOtherCalls();
            _quizServiceMock.Verify(x => x.SetChallengeToQuiz(quizId, mathChallenge.Question, mathChallenge.IsCorrect));
            _quizServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Handle_JustCalled_PublishChallengeUpdatedEvent()
        {
            // Arrange
            var quizId = "42";
            var mathChallenge = new MathChallenge()
            {
                Question = "2+2=4?",
                IsCompleted = false,
                IsCorrect = true
            };

            _mathChallengeServiceMock
                .Setup(m => m.CreateChallenge())
                .Returns(mathChallenge);

            var @event = new ChallengeStarting
            {
                QuizId = quizId,
                StartDate = DateTimeOffset.UtcNow
            };

            // Act
            await _handler.Handle(@event);

            // Assert
            _eventBusMock.Verify(x => x.Publish(
                It.Is<ChallengeUpdated>(e => e.QuizId == quizId && e.Question == mathChallenge.Question)));
            _eventBusMock.VerifyNoOtherCalls();
        }
    }
}
