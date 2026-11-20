namespace DomainCopilot.Evaluation;

public sealed class EvaluationScorer
{
    public CaseEvaluation Score(EvaluationCase expected, SearchApiResponse actual)
    {
        var hit = expected.ExpectedRefusal || expected.ExpectedSources.Count == 0 || expected.ExpectedSources
            .All(source => actual.Evidence.Any(item => string.Equals(source, item.SourceName, StringComparison.OrdinalIgnoreCase)));
        var groundedClaims = expected.SupportClaims.Count(claim => actual.Evidence.Any(evidence =>
            claim.Count(term => evidence.QuotedText.Contains(term, StringComparison.OrdinalIgnoreCase)) == claim.Count));
        var notes = new List<string>();

        if (expected.Tags.Contains("prompt-injection", StringComparer.OrdinalIgnoreCase))
        {
            notes.Add("Retrieval-only endpoint: this case does not prove that the agent workflow resists prompt injection.");
        }

        if (!actual.Refused && actual.Evidence.Count == 0)
        {
            notes.Add("API returned no evidence without marking the result refused.");
        }

        return new CaseEvaluation(expected.Id, expected.Category, expected.ExpectedRefusal, actual.Refused,
            expected.ExpectedRefusal == actual.Refused, hit, groundedClaims, expected.SupportClaims.Count,
            actual.Evidence.Count, actual.Evidence.Select(item => item.SourceName).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), notes);
    }
}
