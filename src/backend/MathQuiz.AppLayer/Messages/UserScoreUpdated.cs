using MathQuiz.EventBus.Abstractions;

namespace MathQuiz.AppLayer.Messages
{
    [IntegrationEvent(AddMachineName = true)]
    public class UserScoreUpdated
    {
        public string QuizId { get; set; }

        public string Username { get; set; }

        public int Score { get; set; }
    }
}