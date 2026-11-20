using DomainCopilot.Domain.Approvals;
using DomainCopilot.Domain.Errors;

namespace DomainCopilot.Domain.Tests;

public sealed class ApprovalRecordTests
{
    [Fact]
    public void Edited_approval_requires_final_content()
    {
        var exception = Assert.Throws<DomainRuleViolationException>(() => new ApprovalRecord(
            Guid.NewGuid(), Guid.NewGuid(), "clinician-1", ApprovalAction.EditedAndApproved, DateTimeOffset.UtcNow));

        Assert.Equal("approval.edited_content.required", exception.Code);
    }

    [Fact]
    public void Rejection_requires_a_reason_for_the_audit_record()
    {
        var exception = Assert.Throws<DomainRuleViolationException>(() => new ApprovalRecord(
            Guid.NewGuid(), Guid.NewGuid(), "clinician-1", ApprovalAction.Rejected, DateTimeOffset.UtcNow));

        Assert.Equal("approval.rejection_reason.required", exception.Code);
    }
}
