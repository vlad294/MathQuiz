using MathQuiz.EventBus.Abstractions;

namespace MathQuiz.AppLayer.Messages
{
    [IntegrationEvent(AddMachineName = true)]
    public class UserConnected
    {
        public string QuizId { get; set; }

        public string Username { get; set; }
    }
}