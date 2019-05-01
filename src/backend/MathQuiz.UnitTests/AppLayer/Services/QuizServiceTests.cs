using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MathQuiz.AppLayer.Messages;
using MathQuiz.AppLayer.Services;
using MathQuiz.AppLayer.Services.Dto;
using MathQuiz.Configuration;
using MathQuiz.DataAccess.Storage;
using MathQuiz.Domain;
using MathQuiz.EventBus.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace MathQuiz.UnitTests.AppLayer.Services
{
    public class QuizServiceTests
    {
        private readonly IQuizService _service;
        private readonly Mock<IQuizDao> _quizDaoMock;
        private readonly Mock<IEventBus> _eventBusMock;
        private readonly Mock<IMapper> _mapperMock;
        private const string Username = "kekozavr";
        private const string QuizId = "42";


        public QuizServiceTests()
        {
            _quizDaoMock = new Mock<IQuizDao>();
            _eventBusMock = new Mock<IEventBus>();
            _mapperMock = new Mock<IMapper>();
            var quizSettingsOptionsMock = new Mock<IOptions<QuizSettings>>();
            var loggerMock = new Mock<ILogger<QuizService>>();
            _service = new QuizService(
                _quizDaoMock.Object, 
                _eventBusMock.Object, 
                _mapperMock.Object, 
                quizSettingsOptionsMock.Object, 
                loggerMock.Object
            );

            quizSettingsOptionsMock.SetupGet(m => m.Value).Returns(new QuizSettings
            {
                DelayBetweenGamesInSeconds = 42,
                UsersLimit = 10
            });
        }

        [Fact]
        public async Task ExitQuiz_JustCalled_InvokeDao()
        {
            // Act
            await _service.ExitQuiz(Username);

            // Assert
            _quizDaoMock.Verify(x => x.RemoveUserFromQuiz(Username), Times.Once);
            _quizDaoMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ExitQuiz_UserRemovedFromQuiz_PublishUserDisconnectedEvent()
        {
            // Arrange
            _quizDaoMock
                .Setup(m => m.RemoveUserFromQuiz(Username))
                .ReturnsAsync(new Quiz {Id = QuizId });

            // Act
            await _service.ExitQuiz(Username);

            // Assert
            _eventBusMock.Verify(x => x.Publish(It.Is<UserDisconnected>(
                e => e.Username == Username && e.QuizId == QuizId)));
        }

        [Fact]
        public async Task HandleUserAnswer_QuizWithoutChallenge_DoNothing()
        {
            // Arrange
            _quizDaoMock
                .Setup(m => m.GetUserQuiz(Username))
                .ReturnsAsync(new Quiz { Id = QuizId });

            // Act
            await _service.HandleUserAnswer(Username, true);

            // Assert
            _quizDaoMock.Verify(x => x.GetUserQuiz(Username), Times.Once);
            _quizDaoMock.VerifyNoOtherCalls();
            _eventBusMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task HandleUserAnswer_CorrectAnswer_CompleteQuizAndIncreaseUserScore(bool answer)
        {
            // Arrange
            _quizDaoMock
                .Setup(m => m.GetUserQuiz(Username))
                .ReturnsAsync(new Quiz { Id = QuizId, Challenge = new MathChallenge
                {
                    IsCompleted = false,
                    IsCorrect = answer
                }});

            // Act
            await _service.HandleUserAnswer(Username, answer);

            // Assert
            _quizDaoMock.Verify(x => x.GetUserQuiz(Username), Times.Once);
            _quizDaoMock.Verify(x => x.CompleteQuizAndIncreaseUserScore(QuizId, Username), Times.Once);
            _quizDaoMock.VerifyNoOtherCalls();
            _eventBusMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task HandleUserAnswer_QuizCompletedAndUserScoreIncreased_PublishIntegrationEvents()
        {
            // Arrange
            var userScore = 100;

            _quizDaoMock
                .Setup(m => m.GetUserQuiz(Username))
                .ReturnsAsync(new Quiz
                {
                    Id = QuizId,
                    Challenge = new MathChallenge
                    {
                        IsCompleted = false,
                        IsCorrect = true
                    }
                });

            _quizDaoMock
                .Setup(m => m.CompleteQuizAndIncreaseUserScore(QuizId, Username))
                .ReturnsAsync(new Quiz
                {
                    Id = QuizId,
                    Challenge = new MathChallenge
                    {
                        IsCompleted = false,
                        IsCorrect = true
                    },
                    Users = new List<User>
                    {
                        new User
                        {
                            Username = Username,
                            Score = userScore
                        }
                    }
                });

            // Act
            await _service.HandleUserAnswer(Username, true);

            // Assert
            _eventBusMock.Verify(x => x.Publish(
                It.Is<ChallengeFinished>(e=>e.QuizId == QuizId)));
            _eventBusMock.Verify(x => x.Publish(
                It.Is<ChallengeStarting>(e=>e.QuizId == QuizId)));
            _eventBusMock.Verify(x => x.Publish(
                It.Is<UserScoreUpdated>(e => e.QuizId == QuizId
                                             && e.Username == Username 
                                             && e.Score == userScore)));
            _eventBusMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task HandleUserAnswer_IncorrectAnswer_DecreaseUserScore()
        {
            // Arrange
            _quizDaoMock
                .Setup(m => m.GetUserQuiz(Username))
                .ReturnsAsync(new Quiz
                {
                    Id = QuizId,
                    Challenge = new MathChallenge
                    {
                        IsCompleted = false,
                        IsCorrect = true
                    }
                });

            // Act
            await _service.HandleUserAnswer(Username, false);

            // Assert
            _quizDaoMock.Verify(x => x.GetUserQuiz(Username), Times.Once);
            _quizDaoMock.Verify(x => x.DecreaseUserScore(QuizId, Username), Times.Once);
            _quizDaoMock.VerifyNoOtherCalls();
            _eventBusMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task HandleUserAnswer_UserScoreDecreased_PublishUserScoreUpdated()
        {
            // Arrange
            var userScore = 100;

            _quizDaoMock
                .Setup(m => m.GetUserQuiz(Username))
                .ReturnsAsync(new Quiz
                {
                    Id = QuizId,
                    Challenge = new MathChallenge
                    {
                        IsCompleted = false,
                        IsCorrect = true
                    }
                });

            _quizDaoMock
                .Setup(m => m.DecreaseUserScore(QuizId, Username))
                .ReturnsAsync(new Quiz
                {
                    Id = QuizId,
                    Challenge = new MathChallenge
                    {
                        IsCompleted = false,
                        IsCorrect = true
                    },
                    Users = new List<User>
                    {
                        new User
                        {
                            Username = Username,
                            Score = userScore
                        }
                    }
                });

            // Act
            await _service.HandleUserAnswer(Username, false);

            // Assert
            _eventBusMock.Verify(x => x.Publish(
                It.Is<UserScoreUpdated>(e => e.QuizId == QuizId
                                             && e.Username == Username
                                             && e.Score == userScore)));
            _eventBusMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SetChallengeToQuiz_JustCalled_InvokeDao()
        {
            // Arrange
            var question = "2+2=4?";
            var answer = true;

            // Act
            await _service.SetChallengeToQuiz(QuizId, question, answer);

            // Assert
            _quizDaoMock.Verify(x => x.SetChallengeToQuiz(QuizId, question, answer), Times.Once);
            _quizDaoMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetUserQuizWithId_JustCalled_ReturnQuizIdAndQuizDto()
        {
            // Arrange
            var quizDto = new QuizDto();

            _mapperMock
                .Setup(m => m.Map<QuizDto>(It.IsAny<Quiz>()))
                .Returns(quizDto);

            _quizDaoMock
                .Setup(m => m.GetUserQuiz(Username))
                .ReturnsAsync(new Quiz {Id = QuizId});

            // Act
            var (actualQuizId, actualQuizDto) = await _service.GetUserQuizWithId(Username);

            // Assert
            Assert.Equal(QuizId, actualQuizId);
            Assert.Same(quizDto, actualQuizDto);
            _quizDaoMock.Verify(x => x.GetUserQuiz(Username), Times.Once);
            _quizDaoMock.VerifyNoOtherCalls();
            _mapperMock.Verify(m => m.Map<QuizDto>(It.IsAny<Quiz>()), Times.Once);
            _mapperMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task StartQuiz_UserHasQuiz_DoNotAddUserToQuizOrCreateNew()
        {
            // Arrange
            _quizDaoMock
                .Setup(m => m.GetUserQuiz(Username))
                .ReturnsAsync(new Quiz { Id = QuizId });

            // Act
            await _service.StartQuiz(Username);

            // Assert
            _quizDaoMock.Verify(m => m.GetUserQuiz(Username), Times.Once);
            _quizDaoMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task StartQuiz_UserHasNoQuiz_AddUserToQuizOrCreateNew()
        {
            // Arrange
            _quizDaoMock
                .Setup(m => m.AddUserToQuizOrCreateNew(Username))
                .ReturnsAsync(new Quiz { Id = QuizId });

            // Act
            await _service.StartQuiz(Username);

            // Assert
            _quizDaoMock.Verify(m => m.GetUserQuiz(Username), Times.Once);
            _quizDaoMock.Verify(m => m.AddUserToQuizOrCreateNew(Username), Times.Once);
            _quizDaoMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task StartQuiz_JustCalled_PublishUserConnected()
        {
            // Arrange
            _quizDaoMock
                .Setup(m => m.AddUserToQuizOrCreateNew(Username))
                .ReturnsAsync(new Quiz { Id = QuizId, Challenge = new MathChallenge()});

            // Act
            await _service.StartQuiz(Username);

            // Assert
            _eventBusMock.Verify(x => x.Publish(
                It.Is<UserConnected>(e => e.QuizId == QuizId
                                             && e.Username == Username)));
            _eventBusMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task StartQuiz_QuizWithoutChallenge_PublishChallengeStarting()
        {
            // Arrange
            _quizDaoMock
                .Setup(m => m.AddUserToQuizOrCreateNew(Username))
                .ReturnsAsync(new Quiz { Id = QuizId, Challenge = null});

            // Act
            await _service.StartQuiz(Username);

            // Assert
            _eventBusMock.Verify(x => x.Publish(
                It.Is<ChallengeStarting>(e => e.QuizId == QuizId)));
        }

        [Fact]
        public async Task StartQuiz_JustCalled_ReturnDtoFromMapper()
        {
            // Arrange
            var quiz = new Quiz {Id = QuizId, Challenge = null};
            var quizDto = new QuizDto();

            _mapperMock
                .Setup(m => m.Map<QuizDto>(It.IsAny<Quiz>()))
                .Returns(quizDto);

            _quizDaoMock
                .Setup(m => m.AddUserToQuizOrCreateNew(Username))
                .ReturnsAsync(quiz);

            // Act
            var actualQuizDto = await _service.StartQuiz(Username);

            // Assert
            Assert.Same(quizDto, actualQuizDto);
            _mapperMock.Verify(x => x.Map<QuizDto>(quiz), Times.Once);
            _mapperMock.VerifyNoOtherCalls();
        }
    }
}
