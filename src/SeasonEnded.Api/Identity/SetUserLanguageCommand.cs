namespace SeasonEnded.Api.Identity;

public sealed class SetUserLanguageCommand(AppDbContext context)
{
    public async Task<bool> ExecuteAsync(Guid userId, string language)
    {
        if (language is not ("en" or "es"))
            throw new ArgumentException("Language must be 'en' or 'es'.", nameof(language));

        var user = await context.Users.FindAsync(userId);
        if (user is null || user.Status != "Active")
            return false;

        user.PreferredLanguage = language;
        await context.SaveChangesAsync();
        return true;
    }
}
