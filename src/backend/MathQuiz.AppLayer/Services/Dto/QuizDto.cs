using System.Collections.Generic;

namespace MathQuiz.AppLayer.Services.Dto
{
    public class QuizDto
    {
        public MathChallengeDto Challenge { get; set; }

        public List<UserDto> Users { get; set; }
    }
}
