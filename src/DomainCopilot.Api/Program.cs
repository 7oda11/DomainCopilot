using DomainCopilot.Infrastructure;
using DomainCopilot.Application.Features.Ingestion;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IngestDocumentHandler>();

var app = builder.Build();

app.MapControllers();

app.Run();
