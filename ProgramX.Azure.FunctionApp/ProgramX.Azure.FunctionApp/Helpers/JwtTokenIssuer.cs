using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using ProgramX.Azure.FunctionApp.Constants;
using ProgramX.Azure.FunctionApp.Model.Requests;

namespace ProgramX.Azure.FunctionApp.Helpers
{
    /// <summary>
    ///     Wrapper class for encapsulating the token issuance logic.
    /// </summary>
    public class JwtTokenIssuer
    {
        private readonly IConfiguration _configuration;
        private readonly IJwtEncoder _jwtEncoder;

        public JwtTokenIssuer(IConfiguration configuration)
        {
            _configuration = configuration;
            // JWT specific initialization.
            // https://github.com/jwt-dotnet/jwt
            IJwtAlgorithm algorithm = new HMACSHA256Algorithm();
            IJsonSerializer serializer = new JsonNetSerializer();
            IBase64UrlEncoder base64Encoder = new JwtBase64UrlEncoder();
            _jwtEncoder = new JwtEncoder(algorithm, serializer, base64Encoder);
        }

        public IJwtEncoder JwtEncoder => _jwtEncoder;

        /// <summary>
        ///     This method is intended to be the main entry point for generation of the JWT.
        /// </summary>
        /// <param name="credentials">The user that the token is being issued for.</param>
        /// <returns>A JWT token which can be returned to the user.</returns>
        public string IssueTokenForUser(Credentials credentials, IEnumerable<string> roles, string? jwtKey = null)
        {
            if (string.IsNullOrWhiteSpace(jwtKey)) jwtKey = _configuration["JwtKey"];
            // Instead of returning a string, we'll return the JWT with a set of claims about the user
            Dictionary<string, object> claims = new Dictionary<string, object>
            {
                // JSON representation of the user Reference with ID and display name
                { "username", credentials.UserName },
                {
                    "roles", roles
                }
            };

            string token = _jwtEncoder.Encode(claims, jwtKey);

            return token;
        }
    }
}