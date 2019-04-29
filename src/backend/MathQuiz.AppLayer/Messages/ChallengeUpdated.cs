using MathQuiz.EventBus.Abstractions;

namespace MathQuiz.AppLayer.Messages
{
    [IntegrationEvent(AddMachineName = true)]
    public class ChallengeUpdated
    {
        public string QuizId { get; set; }

        public string Question { get; set; }
    }
}
