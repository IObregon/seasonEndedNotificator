namespace SeasonEnded.Api.Identity;

public sealed class SetUserLanguageCommand(AppDbContext context)
{
    private static readonly HashSet<string> SupportedLanguages = ["en", "es"];

    public async Task<bool> ExecuteAsync(Guid userId, string language)
    {
        if (!SupportedLanguages.Contains(language))
            throw new ArgumentException("Language must be 'en' or 'es'.", nameof(language));

        var user = await context.Users.FindAsync(userId);
        if (user is null || user.Status != "Active")
            return false;

        user.PreferredLanguage = language;
        await context.SaveChangesAsync();
        return true;
    }
}
