using JWT.Algorithms;
using JWT.Builder;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using ProgramX.Azure.FunctionApp.Constants;

namespace ProgramX.Azure.FunctionApp.Model;

/// <summary>
/// Wrapper class for encapsulating claims parsing.
/// </summary>
public class AuthenticationInfo
{
    /// <summary>
    /// Whether the authentication token is valid.
    /// </summary>
    public bool IsValid { get; }
    
    /// <summary>
    /// The username of the authenticated user.
    /// </summary>
    public string? Username { get; }

    /// <summary>
    /// The roles of the authenticated user.
    /// </summary>
    public IEnumerable<string> Roles { get; } = [];

    /// <summary>
    /// Constructor that parses the JWT token and extracts the claims.
    /// </summary>
    /// <param name="jwtToken">The JWT token.</param>
    /// <param name="jwtKey">Key used to verify the JWT token.</param>
    public AuthenticationInfo(string jwtToken, string jwtKey)
    {

        // Check if we can decode the header.
        IDictionary<string, object> claims;

        try
        {
            // Validate the token and decode the claims.
            claims = new JwtBuilder()
                .WithAlgorithm(new HMACSHA256Algorithm())
                .WithSecret(jwtKey)
                .MustVerifySignature()
                .Decode<IDictionary<string, object>>(jwtToken);
        }
        catch(Exception)
        {
            IsValid = false;
            return;
        }

        // Check if we have user claim.
        if (!claims.ContainsKey("username"))
        {
            IsValid = false;
            return;
        }

        IsValid = true;
        Username = Convert.ToString(claims["username"])!;
        if (!claims.TryGetValue("roles", out var claim)) return;
        Roles = Convert.ToString(claim)!.Split('[', ',', ']').Select(q=>q.Replace("\"","")).Where(q=>!string.IsNullOrWhiteSpace(q));
    }
}