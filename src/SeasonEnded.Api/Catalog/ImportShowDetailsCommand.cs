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
            .Include(item => item.Seasons)
            .FirstOrDefaultAsync(item => item.ProviderId == providerId, cancellationToken);

        if (show is null)
        {
            show = new Show { ProviderId = imported.ProviderId };
            context.Shows.Add(show);
        }

        show.Title = imported.Title;
        show.PremiereYear = imported.PremiereYear;
        show.Status = imported.Status;
        show.ImageUrl = imported.ImageUrl;
        show.Seasons.Clear();
        var newSeasons = imported.Seasons.Select(season => new Season
        {
            ShowId = show.Id,
            ProviderSeasonId = season.ProviderSeasonId,
            Number = season.Number,
            PremiereDate = season.PremiereDate,
            EndDate = season.EndDate
        }).ToList();
        show.Seasons.AddRange(newSeasons);
        context.Seasons.AddRange(newSeasons);

        await context.SaveChangesAsync(cancellationToken);
        return show;
    }
}
