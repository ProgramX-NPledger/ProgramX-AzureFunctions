using JWT.Algorithms;
using JWT.Builder;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using ProgramX.Azure.FunctionApp.Constants;

namespace ProgramX.Azure.FunctionApp.Model;

/// <summary>
///     Wrapper class for encapsulating claims parsing.
/// </summary>
public class AuthenticationInfo
{
    public bool IsValid { get; }
    public string Username { get; }
    public IEnumerable<string> Roles { get; }

    public AuthenticationInfo(string jwtToken, string jwtKey)
    {

        // Check if we can decode the header.
        IDictionary<string, object> claims = null;

        try
        {
            // Validate the token and decode the claims.
            claims = new JwtBuilder()
                .WithAlgorithm(new HMACSHA256Algorithm())
                .WithSecret(jwtKey)
                .MustVerifySignature()
                .Decode<IDictionary<string, object>>(jwtToken);
        }
        catch(Exception exception)
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
        Roles = Convert.ToString(claims["roles"]).Split(new char[] { '[',',',']'}).Select(q=>q.Replace("\"","")).Where(q=>!string.IsNullOrWhiteSpace(q));
    }
}