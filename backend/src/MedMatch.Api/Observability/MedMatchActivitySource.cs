using System.Diagnostics;

namespace MedMatch.Api.Observability;

public static class MedMatchActivitySource
{
    public const string Name = "MedMatch";
    public const string Version = "1.0.0";

    public static readonly ActivitySource Source = new(Name, Version);
}

