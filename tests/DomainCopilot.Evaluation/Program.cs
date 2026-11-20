using System.Net.Http.Headers;
using System.Text.Json;

namespace DomainCopilot.Evaluation;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static async Task<int> Main(string[] args)
    {
        if (args.Length > 1 || (args.Length == 1 && args[0] != "--validate-dataset"))
        {
            Console.Error.WriteLine("Usage: dotnet run --project tests/DomainCopilot.Evaluation [--validate-dataset]");
            return 2;
        }

        var casesPath = Path.Combine(AppContext.BaseDirectory, "evaluation-cases.json");
        var cases = JsonSerializer.Deserialize<List<EvaluationCase>>(await File.ReadAllTextAsync(casesPath), JsonOptions)
            ?? throw new InvalidDataException("Evaluation dataset is empty.");
        ValidateDataset(cases);
        if (args.Length == 1)
        {
            Console.WriteLine($"Dataset valid: {cases.Count} cases; {cases.Count(item => item.Tags.Contains("adversarial"))} adversarial; {cases.Count(item => item.Tags.Contains("prompt-injection"))} prompt-injection.");
            return 0;
        }

        if (!Uri.TryCreate(Environment.GetEnvironmentVariable("EVALUATION_API_URL") ?? "http://localhost:8080", UriKind.Absolute, out var endpoint))
        {
            Console.Error.WriteLine("EVALUATION_API_URL must be an absolute URL.");
            return 2;
        }

        var token = Environment.GetEnvironmentVariable("EVALUATION_BEARER_TOKEN");
        if (string.IsNullOrWhiteSpace(token))
        {
            Console.Error.WriteLine("Set EVALUATION_BEARER_TOKEN to a test identity-provider token; tokens are never written to the report.");
            return 2;
        }

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var runner = new EvaluationRunner(client, new EvaluationScorer());
        var summary = await runner.RunAsync(endpoint, cases, CancellationToken.None);
        var reportPath = Path.GetFullPath(Environment.GetEnvironmentVariable("EVALUATION_REPORT_PATH") ?? "evaluation-report.json");
        await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(summary, JsonOptions));
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            summary.TotalCases,
            summary.AdversarialCases,
            summary.PromptInjectionCases,
            summary.RetrievalHitRate,
            summary.GroundednessRate,
            summary.RefusalCorrectnessRate,
            Report = reportPath
        }, JsonOptions));
        return 0;
    }

    private static void ValidateDataset(IReadOnlyList<EvaluationCase> cases)
    {
        if (cases.Count < 25) throw new InvalidDataException("At least 25 evaluation cases are required.");
        if (cases.Select(item => item.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != cases.Count)
            throw new InvalidDataException("Evaluation case IDs must be unique.");
        if (cases.Count(item => item.Tags.Contains("adversarial", StringComparer.OrdinalIgnoreCase)) < 5)
            throw new InvalidDataException("At least five adversarial cases are required.");
        if (cases.Count(item => item.Tags.Contains("prompt-injection", StringComparer.OrdinalIgnoreCase)) < 3)
            throw new InvalidDataException("At least three prompt-injection cases are required.");
        if (!cases.Any(item => !item.ExpectedRefusal) || !cases.Any(item => item.ExpectedRefusal))
            throw new InvalidDataException("The dataset needs both answerable and refusal cases.");
        if (cases.Any(item => string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.Query)))
            throw new InvalidDataException("Every case needs a stable ID and a non-empty query.");
    }
}
