using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MathQuiz.Domain
{
    public class Quiz
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public MathChallenge Challenge { get; set; }

        public List<User> Users { get; set; }
    }
}
