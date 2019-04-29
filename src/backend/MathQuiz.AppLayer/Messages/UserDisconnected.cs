using MathQuiz.EventBus.Abstractions;

namespace MathQuiz.AppLayer.Messages
{
    [IntegrationEvent(AddMachineName = true)]
    public class UserDisconnected
    {
        public string QuizId { get; set; }

        public string Username { get; set; }
    }
}