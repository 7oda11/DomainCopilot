using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace DomainCopilot.Evaluation;

public sealed class EvaluationRunner(HttpClient client, EvaluationScorer scorer)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<EvaluationSummary> RunAsync(Uri endpoint, IReadOnlyList<EvaluationCase> cases,
        CancellationToken cancellationToken)
    {
        var evaluations = new List<CaseEvaluation>(cases.Count);
        foreach (var evaluationCase in cases)
        {
            using var response = await client.PostAsJsonAsync(new Uri(endpoint, "/api/search"),
                new { query = evaluationCase.Query, limit = 5 }, JsonOptions, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Search API failed for case {evaluationCase.Id} with HTTP {(int)response.StatusCode}.");
            }

            var result = await response.Content.ReadFromJsonAsync<SearchApiResponse>(JsonOptions, cancellationToken)
                ?? throw new InvalidDataException($"Search API returned an empty response for case {evaluationCase.Id}.");
            evaluations.Add(scorer.Score(evaluationCase, result));
        }

        var positiveCases = evaluations.Where(item => !item.ExpectedRefusal).ToArray();
        var claims = evaluations.Sum(item => item.TotalClaims);
        var limitations = new[]
        {
            "Groundedness is a lexical support proxy: every expected support term must occur in a cited quote; it is not an LLM answer-quality judgment.",
            "The current search endpoint returns evidence, not generated answers, so this run cannot measure answer-level factuality.",
            "Prompt-injection tags classify test inputs only; workflow resistance requires a separate adversarial run against indexed malicious documents."
        };

        return new EvaluationSummary(DateTimeOffset.UtcNow, endpoint.ToString(), cases.Count,
            Ratio(positiveCases.Count(item => item.RetrievalHit), positiveCases.Length),
            claims == 0 ? null : Ratio(evaluations.Sum(item => item.GroundedClaims), claims),
            Ratio(evaluations.Count(item => item.RefusalCorrect), evaluations.Count),
            cases.Count(item => item.Tags.Contains("adversarial", StringComparer.OrdinalIgnoreCase)),
            cases.Count(item => item.Tags.Contains("prompt-injection", StringComparer.OrdinalIgnoreCase)),
            evaluations, limitations);
    }

    private static double Ratio(int numerator, int denominator) => denominator == 0
        ? throw new InvalidOperationException("Cannot calculate a metric with an empty denominator.")
        : (double)numerator / denominator;
}
