using System.Text.RegularExpressions;

namespace Memoryboard
{
    public static partial class EmailValidator
    {
        [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
        private static partial Regex EmailRegex();

        public static bool IsEmailValid(string email)
        {
            return EmailRegex().IsMatch(email); // Check if the email matches the pattern
        }

    }
}
