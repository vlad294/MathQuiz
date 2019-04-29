using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MathQuiz.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MathQuiz.WebApi.Authentication
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IJwtSigningEncodingKey _signingEncodingKey;
        private readonly IOptions<AuthenticationSettings> _authenticationSettings;

        public AuthenticationService(IJwtSigningEncodingKey signingEncodingKey, 
            IOptions<AuthenticationSettings> authenticationSettings)
        {
            _signingEncodingKey = signingEncodingKey;
            _authenticationSettings = authenticationSettings;
        }

        public string Authenticate(string username)
        {
            var authSettings = _authenticationSettings.Value;
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.UniqueName, username)
            };

            var token = new JwtSecurityToken(
                issuer: authSettings.Issuer,
                audience: authSettings.Audience,
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: new SigningCredentials(
                    _signingEncodingKey.GetKey(),
                    _signingEncodingKey.SigningAlgorithm)
            );

            var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);
            return jwtToken;
        }
    }
}
