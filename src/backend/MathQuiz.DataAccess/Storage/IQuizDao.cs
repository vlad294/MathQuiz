using System.Threading.Tasks;
using MathQuiz.Domain;

namespace MathQuiz.DataAccess.Storage
{
    public interface IQuizDao
    {
        Task<Quiz> AddUserToQuizOrCreateNew(string username);

        Task<Quiz> RemoveUserFromQuiz(string username);

        Task<Quiz> GetUserQuiz(string username);

        Task<Quiz> SetChallengeToQuiz(string quizId, string question, bool isCorrect);

        Task<Quiz> CompleteQuizAndIncreaseUserScore(string quizId, string username);

        Task<Quiz> DecreaseUserScore(string quizId, string username);
    }
}