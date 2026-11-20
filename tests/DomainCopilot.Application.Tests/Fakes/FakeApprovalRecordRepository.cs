using DomainCopilot.Application.Abstractions.Persistence;
using DomainCopilot.Domain.Approvals;

namespace DomainCopilot.Application.Tests.Fakes;

public sealed class FakeApprovalRecordRepository : IRepository<ApprovalRecord>
{
    public ApprovalRecord? AddedApproval { get; private set; }
    public Task<ApprovalRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(AddedApproval?.Id == id ? AddedApproval : null);
    public void Add(ApprovalRecord aggregate) => AddedApproval = aggregate;
}
