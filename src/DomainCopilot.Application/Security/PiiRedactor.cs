using System.Text.RegularExpressions;

namespace DomainCopilot.Application.Security;

public sealed partial class PiiRedactor
{
    public string Redact(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var redacted = EmailPattern().Replace(text, "[REDACTED_EMAIL]");
        redacted = EgyptianNationalIdPattern().Replace(redacted, "[REDACTED_ID]");
        return PhonePattern().Replace(redacted, "[REDACTED_PHONE]");
    }

    [GeneratedRegex(@"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"(?<!\d)(?:2|3)\d{13}(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex EgyptianNationalIdPattern();

    [GeneratedRegex(@"(?<!\d)(?:\+?20|0)?1[0125]\d{8}(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex PhonePattern();
}
