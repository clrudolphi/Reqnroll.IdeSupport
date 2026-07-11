namespace Reqnroll.IdeSupport.LSP.Core.Parsing.Gherkin;

/// <summary>
/// A Gherkin dialect provider that falls back to a language's base dialect (e.g. "en" for
/// "en-US") when no exact regional dialect is registered.
/// </summary>
public class ReqnrollGherkinDialectProvider : GherkinDialectProvider
{
    /// <summary>Creates a dialect provider with the given default language.</summary>
    public ReqnrollGherkinDialectProvider(string defaultLanguage) : base(defaultLanguage)
    {
    }

    /// <summary>
    /// Resolves the dialect for <paramref name="language"/>, retrying with the base language
    /// (before the first "-") if the exact regional dialect isn't found.
    /// </summary>
    protected override bool TryGetDialect(string language, Location? location, out GherkinDialect dialect)
    {
        if (language.Contains("-"))
        {
            if (base.TryGetDialect(language, location, out dialect))
                return true;

            var languageBase = language.Split('-')[0];
            if (!base.TryGetDialect(languageBase, location, out var languageBaseDialect))
                return false;

            dialect = new GherkinDialect(language, languageBaseDialect.FeatureKeywords,
                languageBaseDialect.RuleKeywords, languageBaseDialect.BackgroundKeywords,
                languageBaseDialect.ScenarioKeywords, languageBaseDialect.ScenarioOutlineKeywords,
                languageBaseDialect.ExamplesKeywords, languageBaseDialect.GivenStepKeywords,
                languageBaseDialect.WhenStepKeywords, languageBaseDialect.ThenStepKeywords,
                languageBaseDialect.AndStepKeywords, languageBaseDialect.ButStepKeywords);
            return true;
        }

        return base.TryGetDialect(language, location, out dialect);
    }

    /// <summary>Returns the default dialect for the given language.</summary>
    public static GherkinDialect GetDialect(string language)
    {
        var provider = Get(language);
        return provider.DefaultDialect;
    }

    /// <summary>Creates a new dialect provider for the given default language.</summary>
    public static GherkinDialectProvider Get(string defaultLanguage) =>
        //TODO: cache!
        new ReqnrollGherkinDialectProvider(defaultLanguage);
}
