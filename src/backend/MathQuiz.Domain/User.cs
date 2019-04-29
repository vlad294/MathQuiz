using MongoDB.Bson.Serialization.Attributes;

namespace MathQuiz.Domain
{
    public class User
    {
        [BsonId]
        public string Login { get; set; }

        public int Score { get; set; }
    }
}
