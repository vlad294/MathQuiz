using System.Threading.Tasks;
using MathQuiz.Domain;

namespace MathQuiz.DataAccess.Abstractions
{
    public interface IQuizDao
    {
        Task<Quiz> GetOrCreateQuizForUser(string username);

        Task<Quiz> RemoveUserFromQuiz(string username);

        Task<string> GetUserQuizId(string username);

        Task<Quiz> SetChallengeToQuiz(string quizId, string question, bool isCorrect);

        Task<Quiz> ChallengeUserValidAnswerAndIncreaseScore(string quizId, string username, bool isCorrect);

        Task<Quiz> ChallengeUserInvalidAnswerAndDecreaseScore(string quizId, string username, bool isCorrect);
    }
}