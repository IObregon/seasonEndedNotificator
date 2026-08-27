using Microsoft.EntityFrameworkCore;

namespace SeasonEnded.Api.Identity;

public sealed class BootstrapAdminCommand(AppDbContext context)
{
    public async Task<BootstrapResult> ExecuteAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        var normalized = email.Trim().ToLowerInvariant();

        var existing = await context.Users
            .FirstOrDefaultAsync(u => u.Email == normalized);

        if (existing is not null)
        {
            if (existing.Role != UserRole.Admin)
                throw new BootstrapConflictException(
                    $"An existing non-admin account with email '{normalized}' conflicts with bootstrap.");

            return new BootstrapResult(Created: false);
        }

        context.Users.Add(new User
        {
            Email = normalized,
            Role = UserRole.Admin,
            Status = "Active"
        });

        await context.SaveChangesAsync();
        return new BootstrapResult(Created: true);
    }
}

public sealed record BootstrapResult(bool Created);
