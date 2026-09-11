using MedMatch.Domain;
using MedMatch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedMatch.Api.Services;

/// <summary>
/// Canonical, seeded translations for the standard diagnosis tags. The English
/// name remains the canonical key used for matching; these localized labels are
/// returned to clients based on their Accept-Language header.
/// </summary>
public static class DiagnosisTagLocalization
{
    public static readonly string[] Cultures = ["es", "de", "it"];

    private static readonly Dictionary<string, (string Es, string De, string It)> Seed = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Type 1 Diabetes"] = ("Diabetes tipo 1", "Diabetes Typ 1", "Diabete di tipo 1"),
        ["Type 2 Diabetes"] = ("Diabetes tipo 2", "Diabetes Typ 2", "Diabete di tipo 2"),
        ["Gestational Diabetes"] = ("Diabetes gestacional", "Schwangerschaftsdiabetes", "Diabete gestazionale"),
        ["Asthma"] = ("Asma", "Asthma", "Asma"),
        ["COPD"] = ("EPOC", "COPD", "BPCO"),
        ["Hypertension"] = ("Hipertensión", "Bluthochdruck", "Ipertensione"),
        ["Coronary Artery Disease"] = ("Enfermedad coronaria", "Koronare Herzkrankheit", "Malattia coronarica"),
        ["Heart Failure"] = ("Insuficiencia cardíaca", "Herzinsuffizienz", "Insufficienza cardiaca"),
        ["Atrial Fibrillation"] = ("Fibrilación auricular", "Vorhofflimmern", "Fibrillazione atriale"),
        ["Stroke"] = ("Ictus", "Schlaganfall", "Ictus"),
        ["Migraine"] = ("Migraña", "Migräne", "Emicrania"),
        ["Epilepsy"] = ("Epilepsia", "Epilepsie", "Epilessia"),
        ["Multiple Sclerosis"] = ("Esclerosis múltiple", "Multiple Sklerose", "Sclerosi multipla"),
        ["Parkinson's Disease"] = ("Enfermedad de Parkinson", "Parkinson-Krankheit", "Malattia di Parkinson"),
        ["Alzheimer's Disease"] = ("Enfermedad de Alzheimer", "Alzheimer-Krankheit", "Malattia di Alzheimer"),
        ["Depression"] = ("Depresión", "Depression", "Depressione"),
        ["Anxiety Disorder"] = ("Trastorno de ansiedad", "Angststörung", "Disturbo d'ansia"),
        ["Bipolar Disorder"] = ("Trastorno bipolar", "Bipolare Störung", "Disturbo bipolare"),
        ["Schizophrenia"] = ("Esquizofrenia", "Schizophrenie", "Schizofrenia"),
        ["PTSD"] = ("TEPT", "PTBS", "DPTS"),
        ["ADHD"] = ("TDAH", "ADHS", "ADHD"),
        ["Autism Spectrum Disorder"] = ("Trastorno del espectro autista", "Autismus-Spektrum-Störung", "Disturbo dello spettro autistico"),
        ["Rheumatoid Arthritis"] = ("Artritis reumatoide", "Rheumatoide Arthritis", "Artrite reumatoide"),
        ["Osteoarthritis"] = ("Osteoartritis", "Arthrose", "Osteoartrite"),
        ["Lupus"] = ("Lupus", "Lupus", "Lupus"),
        ["Fibromyalgia"] = ("Fibromialgia", "Fibromyalgie", "Fibromialgia"),
        ["Chronic Fatigue Syndrome"] = ("Síndrome de fatiga crónica", "Chronisches Erschöpfungssyndrom", "Sindrome da fatica cronica"),
        ["Crohn's Disease"] = ("Enfermedad de Crohn", "Morbus Crohn", "Malattia di Crohn"),
        ["Ulcerative Colitis"] = ("Colitis ulcerosa", "Colitis ulcerosa", "Colite ulcerosa"),
        ["Celiac Disease"] = ("Enfermedad celíaca", "Zöliakie", "Celiachia"),
        ["Irritable Bowel Syndrome"] = ("Síndrome del intestino irritable", "Reizdarmsyndrom", "Sindrome dell'intestino irritabile"),
        ["Hypothyroidism"] = ("Hipotiroidismo", "Hypothyreose", "Ipotiroidismo"),
        ["Hyperthyroidism"] = ("Hipertiroidismo", "Hyperthyreose", "Ipertiroidismo"),
        ["Hashimoto's Disease"] = ("Enfermedad de Hashimoto", "Hashimoto-Thyreoiditis", "Tiroidite di Hashimoto"),
        ["Breast Cancer"] = ("Cáncer de mama", "Brustkrebs", "Cancro al seno"),
        ["Lung Cancer"] = ("Cáncer de pulmón", "Lungenkrebs", "Cancro al polmone"),
        ["Prostate Cancer"] = ("Cáncer de próstata", "Prostatakrebs", "Cancro alla prostata"),
        ["Colorectal Cancer"] = ("Cáncer colorrectal", "Darmkrebs", "Cancro del colon-retto"),
        ["Leukemia"] = ("Leucemia", "Leukämie", "Leucemia"),
        ["Lymphoma"] = ("Linfoma", "Lymphom", "Linfoma"),
        ["Endometriosis"] = ("Endometriosis", "Endometriose", "Endometriosi"),
        ["PCOS"] = ("SOP", "PCOS", "PCOS"),
        ["Chronic Kidney Disease"] = ("Enfermedad renal crónica", "Chronische Nierenerkrankung", "Malattia renale cronica"),
        ["Kidney Stones"] = ("Cálculos renales", "Nierensteine", "Calcoli renali"),
        ["Glaucoma"] = ("Glaucoma", "Glaukom", "Glaucoma"),
        ["Cataracts"] = ("Cataratas", "Katarakt", "Cataratta"),
        ["Psoriasis"] = ("Psoriasis", "Psoriasis", "Psoriasi"),
        ["Eczema"] = ("Eccema", "Ekzem", "Eczema"),
        ["Sleep Apnea"] = ("Apnea del sueño", "Schlafapnoe", "Apnea notturna"),
        ["Chronic Pain"] = ("Dolor crónico", "Chronische Schmerzen", "Dolore cronico"),
        ["Long COVID"] = ("COVID persistente", "Long COVID", "Long COVID"),
        ["HIV"] = ("VIH", "HIV", "HIV"),
        ["Hepatitis C"] = ("Hepatitis C", "Hepatitis C", "Epatite C"),
    };

    public static async Task SeedAsync(MedMatchDbContext db, CancellationToken ct)
    {
        var tags = await db.DiagnosisTags.Include(x => x.Translations).ToListAsync(ct);
        var byName = tags.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

        var added = false;
        foreach (var (name, (es, de, it)) in Seed)
        {
            if (!byName.TryGetValue(name, out var tag)) continue;
            var values = new Dictionary<string, string> { ["es"] = es, ["de"] = de, ["it"] = it };
            foreach (var (culture, text) in values)
            {
                if (tag.Translations.Any(x => x.Culture == culture)) continue;
                tag.Translations.Add(new DiagnosisTagTranslation { Culture = culture, Name = text });
                added = true;
            }
        }

        if (added) await db.SaveChangesAsync(ct);
    }
}
