using System.Diagnostics.Metrics;

namespace MedMatch.Api.Observability;

public static class MedMatchMetrics
{
    public const string MeterName = "MedMatch.Api";
    public const string MeterVersion = "1.0.0";

    public static readonly Meter Meter = new(MeterName, MeterVersion);

    public static readonly Counter<long> UserRegistrationsCounter = Meter.CreateCounter<long>(
        "medmatch.user.registrations.total",
        description: "Total number of user registrations");

    public static readonly Counter<long> UserLoginsCounter = Meter.CreateCounter<long>(
        "medmatch.user.logins.total",
        description: "Total number of user login attempts");

    public static readonly Counter<long> ReviewsCreatedCounter = Meter.CreateCounter<long>(
        "medmatch.reviews.created.total",
        description: "Total number of reviews created");

    public static readonly Counter<long> RecommendationsModeratedCounter = Meter.CreateCounter<long>(
        "medmatch.recommendations.moderated.total",
        description: "Total number of recommendations moderated");

    public static readonly Counter<long> MatchesComputedCounter = Meter.CreateCounter<long>(
        "medmatch.matches.computed.total",
        description: "Total number of patient matches computed");

    public static readonly Counter<long> TranslationsRequestedCounter = Meter.CreateCounter<long>(
        "medmatch.translations.requested.total",
        description: "Total number of translation requests made");

    public static readonly Histogram<double> MatchComputationDuration = Meter.CreateHistogram<double>(
        "medmatch.match.computation.duration.ms",
        unit: "ms",
        description: "Duration of patient matching computation in milliseconds");

    public static void RecordRegistration() => UserRegistrationsCounter.Add(1);

    public static void RecordLogin(bool success) =>
        UserLoginsCounter.Add(1, new KeyValuePair<string, object?>("status", success ? "success" : "failure"));

    public static void RecordReviewCreated(bool isClinic) =>
        ReviewsCreatedCounter.Add(1, new KeyValuePair<string, object?>("target_type", isClinic ? "clinic" : "doctor"));

    public static void RecordRecommendation(string status) =>
        RecommendationsModeratedCounter.Add(1, new KeyValuePair<string, object?>("status", status.ToLowerInvariant()));

    public static void RecordMatchesComputed(int count, double durationMs)
    {
        MatchesComputedCounter.Add(count);
        MatchComputationDuration.Record(durationMs);
    }

    public static void RecordTranslation(string targetLanguage) =>
        TranslationsRequestedCounter.Add(1, new KeyValuePair<string, object?>("target_language", targetLanguage.ToLowerInvariant()));
}

