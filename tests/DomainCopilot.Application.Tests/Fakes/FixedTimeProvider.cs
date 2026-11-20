namespace DomainCopilot.Application.Tests.Fakes;

public sealed class FixedTimeProvider(DateTimeOffset currentTimeUtc) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => currentTimeUtc;
}
