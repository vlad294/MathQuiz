using MathQuiz.EventBus.Abstractions;

namespace MathQuiz.AppLayer.Messages
{
    [IntegrationEvent(AddMachineName = true)]
    public class ChallengeFinished
    {
        public string QuizId { get; set; }
    }
}
