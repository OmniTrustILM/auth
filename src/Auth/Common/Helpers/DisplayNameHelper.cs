using System.Text.RegularExpressions;

namespace Auth.Common.Helpers
{
    public static class DisplayNameHelper
    {
        public static string GetDisplayName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            var words = Regex.Matches(name, "(^[a-z]+|[A-Z]+(?![a-z])|[A-Z][a-z]+)")
                .OfType<Match>()
                .Select(m =>
                    {
                        var charArray = m.Value.ToCharArray();
                        charArray[0] = char.ToUpper(charArray[0]);
                        return new string(charArray);
                    }
                ).ToArray();

            return string.Join(" ", words);
        }
    }
}
