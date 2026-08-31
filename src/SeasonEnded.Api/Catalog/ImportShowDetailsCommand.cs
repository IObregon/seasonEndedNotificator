using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Catalog;

public sealed class ImportShowDetailsCommand(
    AppDbContext context,
    ITvShowDetails provider)
{
    public async Task<Show> ExecuteAsync(
        int providerId,
        CancellationToken cancellationToken)
    {
        var imported = await provider.GetAsync(providerId, cancellationToken);
        var show = await context.Shows
            .FirstOrDefaultAsync(item => item.ProviderId == providerId, cancellationToken);

        var existingSeasons = show is null
            ? new List<Season>()
            : await context.Seasons
                .Where(s => s.ShowId == show.Id)
                .ToListAsync(cancellationToken);

        if (show is null)
        {
            show = new Show { ProviderId = imported.ProviderId };
            context.Shows.Add(show);
        }

        show.Title = imported.Title;
        show.PremiereYear = imported.PremiereYear;
        show.Status = imported.Status;
        show.ImageUrl = imported.ImageUrl;

        var existingByProviderId = existingSeasons.ToDictionary(s => s.ProviderSeasonId);
        var importedIds = imported.Seasons.Select(s => s.ProviderSeasonId).ToHashSet();

        foreach (var existing in existingSeasons)
        {
            if (!importedIds.Contains(existing.ProviderSeasonId))
                context.Remove(existing);
        }

        foreach (var importedSeason in imported.Seasons)
        {
            if (existingByProviderId.TryGetValue(importedSeason.ProviderSeasonId, out var season))
            {
                season.PremiereDate = importedSeason.PremiereDate;
                season.EndDate = importedSeason.EndDate;
            }
            else
            {
                context.Seasons.Add(new Season
                {
                    ShowId = show.Id,
                    ProviderSeasonId = importedSeason.ProviderSeasonId,
                    Number = importedSeason.Number,
                    PremiereDate = importedSeason.PremiereDate,
                    EndDate = importedSeason.EndDate
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        return await context.Shows
            .Include(s => s.Seasons)
            .FirstAsync(s => s.Id == show.Id, cancellationToken);
    }
}
