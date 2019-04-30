using MongoDB.Bson.Serialization.Attributes;

namespace MathQuiz.Domain
{
    public class User
    {
        [BsonId]
        public string Username { get; set; }

        public int Score { get; set; }
    }
}
