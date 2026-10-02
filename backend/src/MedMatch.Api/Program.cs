using MedMatch.Api.Configuration;
using MedMatch.Api.Endpoints;
using MedMatch.Api.Observability;

var builder = WebApplication.CreateBuilder(args);
builder.AddMedMatchObservability();
builder.AddMedMatchServices();

var app = builder.Build();
app.UseMedMatchPipeline();
await app.InitializeMedMatchDatabaseAsync();
app.MapMedMatchEndpoints();
app.Run();
