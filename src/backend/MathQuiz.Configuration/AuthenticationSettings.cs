namespace MathQuiz.Configuration
{
    public class AuthenticationSettings
    {
        public string SigningSecurityKey { get; set; }

        public string Audience { get; set; }

        public string Issuer { get; set; }
    }
}
