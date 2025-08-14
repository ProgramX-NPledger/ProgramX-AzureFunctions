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
        private readonly IJwtAlgorithm _algorithm;
        private readonly IJsonSerializer _serializer;
        private readonly IBase64UrlEncoder _base64Encoder;
        private readonly IJwtEncoder _jwtEncoder;

        public JwtTokenIssuer(IConfiguration configuration)
        {
            _configuration = configuration;
            // JWT specific initialization.
            // https://github.com/jwt-dotnet/jwt
            _algorithm = new HMACSHA256Algorithm();
            _serializer = new JsonNetSerializer();
            _base64Encoder = new JwtBase64UrlEncoder();
            _jwtEncoder = new JwtEncoder(_algorithm, _serializer, _base64Encoder);
        }

        /// <summary>
        ///     This method is intended to be the main entry point for generation of the JWT.
        /// </summary>
        /// <param name="credentials">The user that the token is being issued for.</param>
        /// <returns>A JWT token which can be returned to the user.</returns>
        public string IssueTokenForUser(Credentials credentials, IEnumerable<string> roles)
        {
            // Instead of returning a string, we'll return the JWT with a set of claims about the user
            Dictionary<string, object> claims = new Dictionary<string, object>
            {
                // JSON representation of the user Reference with ID and display name
                { "username", credentials.UserName },
                {
                    "roles", roles
                }
            };

            string token = _jwtEncoder.Encode(claims, _configuration["JwtKey"]);

            return token;
        }
    }
}