using AutoMapper;
using MathQuiz.AppLayer.Services.Dto;
using MathQuiz.Domain;

namespace MathQuiz.AppLayer.Services.Mapper
{
    public class QuizProfile : Profile
    {
        public QuizProfile()
        {
            CreateMap<Quiz, QuizDto>();
            CreateMap<MathChallenge, MathChallengeDto>();
            CreateMap<User, UserDto>();
        }
    }
}
