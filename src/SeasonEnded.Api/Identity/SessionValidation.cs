using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace SeasonEnded.Api.Identity;

public static class SessionValidation
{
    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var idValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var policy = context.HttpContext.RequestServices.GetRequiredService<ActiveUserPolicy>();
        if (Guid.TryParse(idValue, out var userId) && await policy.CanUseSessionAsync(userId))
        {
            await RefreshRoleClaimAsync(context, userId);
            return;
        }

        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync();
    }

    private static async Task RefreshRoleClaimAsync(
        CookieValidatePrincipalContext context,
        Guid userId)
    {
        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var user = await db.Users.FindAsync(userId);
        var roleClaim = context.Principal!.FindFirst(ClaimTypes.Role);
        if (roleClaim?.Value == user!.Role.ToString())
            return;

        var identity = (ClaimsIdentity)context.Principal.Identity!;
        if (roleClaim is not null)
            identity.RemoveClaim(roleClaim);
        identity.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString()));
        context.ShouldRenew = true;
    }
}
