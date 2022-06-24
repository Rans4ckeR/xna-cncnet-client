using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ClientCore;

public class ProfanityFilter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProfanityFilter"/> class.
    /// Creates a new profanity filter with a default set of censored words.
    /// </summary>
    public ProfanityFilter()
    {
        CensoredWords = new List<string>()
        {
            "cunt*",
            "*nigg*",
            "paki*",
            "shit",
            "fuck*",
            "admin*",
            "allahu*",
            "akbar",
            "twat",
            "cock",
            "pussy",
            "hitler*",
            "anal"
        };
    }

    public IList<string> CensoredWords { get; private set; }

    public ProfanityFilter(IEnumerable<string> censoredWords!!)
    {
        CensoredWords = new List<string>(censoredWords);
    }

    public bool IsOffensive(string text)
    {
        string censoredText = text;
        foreach (string censoredWord in CensoredWords)
        {
            string regularExpression = ProfanityFilter.ToRegexPattern(censoredWord);
            censoredText = Regex.Replace(censoredText, regularExpression, string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            if (string.IsNullOrEmpty(censoredText))
                return true;
        }

        return false;
    }

    public string CensorText(string text!!)
    {
        string censoredText = text;
        foreach (string censoredWord in CensoredWords)
        {
            string regularExpression = ProfanityFilter.ToRegexPattern(censoredWord);
            censoredText = Regex.Replace(censoredText, regularExpression, StarCensoredMatch,
              RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        return censoredText;
    }

    private static string StarCensoredMatch(Match m)
    {
        string word = m.Captures[0].Value;
        return new string('*', word.Length);
    }

    private static string ToRegexPattern(string wildcardSearch)
    {
        string regexPattern = Regex.Escape(wildcardSearch);
        regexPattern = regexPattern.Replace(@"\*", ".*?");
        regexPattern = regexPattern.Replace(@"\?", ".");
        if (regexPattern.StartsWith(".*?"))
        {
            regexPattern = regexPattern.Substring(3);
            regexPattern = @"(^\b)*?" + regexPattern;
        }

        regexPattern = @"\b" + regexPattern + @"\b";
        return regexPattern;
    }
}