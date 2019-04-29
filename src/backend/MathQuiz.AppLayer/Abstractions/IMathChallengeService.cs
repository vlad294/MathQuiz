using MathQuiz.Domain;

namespace MathQuiz.AppLayer.Abstractions
{
    public interface IMathChallengeService
    {
        MathChallenge CreateChallenge();
    }
}