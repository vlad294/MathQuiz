using System.Threading.Tasks;
using MathQuiz.Configuration;
using MathQuiz.Domain;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace MathQuiz.DataAccess.Storage
{
    public class QuizDao : IQuizDao
    {
        private readonly IMongoCollection<Quiz> _collection;
        private const string CollectionName = "Games";
        private readonly IOptions<QuizSettings> _quizSettingsOptions;

        public QuizDao(IMongoDatabase mongoDatabase,
            IOptions<QuizSettings> quizSettingsOptions)
        {
            _quizSettingsOptions = quizSettingsOptions;
            _collection = mongoDatabase.GetCollection<Quiz>(CollectionName);
        }

        public Task<Quiz> AddUserToQuizOrCreateNew(string username)
        {
            var usersCountLimit = _quizSettingsOptions.Value.UsersLimit;
            return _collection.FindOneAndUpdateAsync(
                Builders<Quiz>.Filter.Not(
                    Builders<Quiz>.Filter.Exists(x => x.Users[usersCountLimit - 1])
                ),
                Builders<Quiz>.Update
                    .AddToSet(x => x.Users, new User
                    {
                        Username = username,
                        Score = 0
                    }),
                new FindOneAndUpdateOptions<Quiz>
                {
                    ReturnDocument = ReturnDocument.After,
                    IsUpsert = true
                }
            );
        }

        public Task<Quiz> GetUserQuiz(string username)
        {
            return _collection.Find(
                Builders<Quiz>.Filter.ElemMatch(x => x.Users, x => x.Username == username)
            ).SingleOrDefaultAsync();
        }

        public Task<Quiz> RemoveUserFromQuiz(string username)
        {
            return _collection.FindOneAndUpdateAsync(
                Builders<Quiz>.Filter.ElemMatch(x => x.Users, x => x.Username == username),
                Builders<Quiz>.Update.PullFilter(x => x.Users, u => u.Username == username)
            );
        }

        public Task<Quiz> SetChallengeToQuiz(string quizId, string question, bool isCorrect)
        {
            return _collection.FindOneAndUpdateAsync(
                Builders<Quiz>.Filter.Eq(x => x.Id, quizId),
                Builders<Quiz>.Update.Set(x => x.Challenge, new MathChallenge
                {
                    IsCompleted = false,
                    Question = question,
                    IsCorrect = isCorrect
                }));
        }

        public Task<Quiz> CompleteQuizAndIncreaseUserScore(string quizId, string username)
        {
            return _collection.FindOneAndUpdateAsync(
                Builders<Quiz>.Filter.And(
                    Builders<Quiz>.Filter.Eq(x => x.Id, quizId),
                    Builders<Quiz>.Filter.Eq(x => x.Challenge.IsCompleted, false),
                    Builders<Quiz>.Filter.ElemMatch(x => x.Users, x => x.Username == username)
                ),
                Builders<Quiz>.Update
                    .Inc(x => x.Users[-1].Score, 1)
                    .Set(x => x.Challenge.IsCompleted, true),
                new FindOneAndUpdateOptions<Quiz>
                {
                    ReturnDocument = ReturnDocument.After
                }
            );
        }

        public Task<Quiz> DecreaseUserScore(string quizId, string username)
        {
            return _collection.FindOneAndUpdateAsync(
                Builders<Quiz>.Filter.And(
                    Builders<Quiz>.Filter.Eq(x => x.Id, quizId),
                    Builders<Quiz>.Filter.Eq(x => x.Challenge.IsCompleted, false),
                    Builders<Quiz>.Filter.ElemMatch(x => x.Users, x => x.Username == username)
                ),
                Builders<Quiz>.Update.Inc(x => x.Users[-1].Score, -1),
                new FindOneAndUpdateOptions<Quiz>
                {
                    ReturnDocument = ReturnDocument.After
                }
            );
        }
    }
}
