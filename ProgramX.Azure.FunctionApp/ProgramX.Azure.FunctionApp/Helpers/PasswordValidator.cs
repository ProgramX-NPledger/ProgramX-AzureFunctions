using ProgramX.Azure.FunctionApp.Model.Exceptions;

namespace ProgramX.Azure.FunctionApp.Helpers;

public class PasswordValidator
{
    public int MinimumPasswordLength { get; set; } = 8;
    public int MinimumLowerCaseCharacters { get; set; } = 1;
    public int MinimumUpperCaseCharacters { get; set; } = 1;
    public int MinimumNumberCharacters { get; set; } = 1;
    public int MinimumSpecialCharacters { get; set; } = 1;
    
    public PasswordValidator()
    {
        
    }
    
    /// <summary>
    /// Asserts valid password, meeting requirements.
    /// </summary>
    /// <param name="password">Password to verify</param>
    /// <exception cref="InvalidPasswordUpdateException">Thrown if the password does not meet the requirements.</exception>
    public void AssertValidPassword(string password)
    {
        var passwordStrengthViolations = new List<string>();
        if (password.Length < MinimumPasswordLength) passwordStrengthViolations.Add($"Password must be at least {MinimumPasswordLength} characters long.");

        var lowerCaseChars = "abcdefghijklmnopqrstuvwxyz";
        var lowerCaseCharactersCount = password.Where(c => lowerCaseChars.Contains(c)).Count();
        if (lowerCaseCharactersCount < MinimumLowerCaseCharacters) passwordStrengthViolations.Add($"Password must contain at least {MinimumLowerCaseCharacters} lower-case characters.");
        
        var upperCaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var upperCaseCharactersCount = password.Where(c => upperCaseChars.Contains(c)).Count();
        if (upperCaseCharactersCount < MinimumUpperCaseCharacters) passwordStrengthViolations.Add($"Password must contain at least {MinimumUpperCaseCharacters} upper-case characters.");

        var specialCharacters = "`¬`!\"£$%^&*()_+-=[]{};'#:@~,./<>?";
        var specialCharactersCount = password.Where(c => specialCharacters.Contains(c)).Count();
        if (specialCharactersCount < MinimumSpecialCharacters) passwordStrengthViolations.Add($"Password must contain at least {MinimumSpecialCharacters} special characters.");

        var numberCharacters = "0123456789";
        var numberCharactersCount = password.Where(c => numberCharacters.Contains(c)).Count();
        if (numberCharactersCount < MinimumNumberCharacters) passwordStrengthViolations.Add($"Password must contain at least {MinimumNumberCharacters} number characters.");

        var legalCharacters = lowerCaseChars + upperCaseChars + specialCharacters + numberCharacters;
        var invalidCharactersCount = password.Where(c => !legalCharacters.Contains(c)).Count();
        if (invalidCharactersCount > 0) passwordStrengthViolations.Add($"Password must contain only legal characters.");
        
        if (passwordStrengthViolations.Count > 0) throw new InvalidPasswordUpdateException(InvalidPasswordUpdateReason.WeakPassword, string.Join(", ", passwordStrengthViolations));
        
    }
}