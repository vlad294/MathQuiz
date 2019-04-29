using System;
using MathQuiz.EventBus.Abstractions;

namespace MathQuiz.AppLayer.Messages
{
    [IntegrationEvent(AddMachineName = false)]
    public class ChallengeStarting
    {
        public string QuizId { get; set; }

        public DateTimeOffset StartDate { get; set; }
    }
}
