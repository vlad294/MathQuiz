using Microsoft.IdentityModel.Tokens;

namespace MathQuiz.WebApi.Authentication
{
    public interface IJwtSigningDecodingKey
    {
        SecurityKey GetKey();
    }
}