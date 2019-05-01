using System.Threading.Tasks;

namespace MathQuiz.WebApi.SignalR
{
    public interface IQuizHubClient
    {
        Task ChallengeFinished();

        Task ChallengeUpdated(string question);

        Task UserScoreUpdated(string username, int score);

        Task UserConnected(string username);

        Task UserDisconnected(string username);
    }
}