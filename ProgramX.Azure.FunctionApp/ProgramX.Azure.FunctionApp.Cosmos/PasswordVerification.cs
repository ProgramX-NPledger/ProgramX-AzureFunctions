using System.Security.Cryptography;

namespace ProgramX.Azure.FunctionApp.Cosmos;

/// <summary>
/// Utility methods for password verification.
/// </summary>
public static class PasswordVerification
{
    /// <summary>
    /// Determine if a provided password matches the stored password hash and salt.
    /// </summary>
    /// <param name="submittedPassword">The submitted password.</param>
    /// <param name="actualPasswordHash">Stored hash of the current password.</param>
    /// <param name="actualPasswordSalt">Stored salt used to generate the hash of the current password.</param>
    /// <returns><c>True</c> if the submitted password matches the stored password hash and salt, otherwise <c>False</c>.</returns>
    public static bool PasswordsMatch(string submittedPassword, byte[] actualPasswordHash, byte[] actualPasswordSalt)
    {
        using var hmac = new HMACSHA512(actualPasswordSalt);
        var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(submittedPassword));

        for (var i = 0; i < computedHash.Length; i++)
            if (computedHash[i] != actualPasswordHash[i])
            {
                return false;
            }

        return true;
    }
}