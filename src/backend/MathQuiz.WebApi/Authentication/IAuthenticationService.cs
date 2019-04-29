namespace MathQuiz.WebApi.Authentication
{
    public interface IAuthenticationService
    {
        string Authenticate(string username);
    }
}