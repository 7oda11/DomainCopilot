using DomainCopilot.Application.Security;

namespace DomainCopilot.Application.Tests;

public sealed class PiiRedactorTests
{
    [Fact]
    public void Redacts_email_egyptian_national_id_and_mobile_number()
    {
        var redactor = new PiiRedactor();

        var result = redactor.Redact("Contact sara@example.org, ID 29901011234567, mobile 01012345678.");

        Assert.DoesNotContain("sara@example.org", result);
        Assert.DoesNotContain("29901011234567", result);
        Assert.DoesNotContain("01012345678", result);
        Assert.Contains("[REDACTED_EMAIL]", result);
        Assert.Contains("[REDACTED_ID]", result);
        Assert.Contains("[REDACTED_PHONE]", result);
    }
}
