using MathQuiz.AppLayer.Services;
using Xunit;

namespace MathQuiz.UnitTests.AppLayer.Services
{
    public class MathChallengeServiceTests
    {
        private readonly IMathChallengeService _service;

        public MathChallengeServiceTests()
        {
            _service = new MathChallengeService();
        }

        [Fact]
        public void CreateChallenge_JustCalled_NotNullChallenge()
        {
            // Act
            var challenge = _service.CreateChallenge();

            // Assert
            Assert.NotNull(challenge);
            Assert.NotEmpty(challenge.Question);
            Assert.False(challenge.IsCompleted);
        }
    }
}
