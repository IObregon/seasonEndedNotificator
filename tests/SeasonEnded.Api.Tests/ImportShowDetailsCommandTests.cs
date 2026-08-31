using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Catalog;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Tests;

public sealed class ImportShowDetailsCommandTests
{
    [Fact]
    public async Task Successful_import_replaces_normalized_metadata()
    {
        await using var context = CreateContext();
        var existing = new Show
        {
            ProviderId = 82,
            Title = "Old title",
            Status = "Running"
        };
        existing.Seasons.Add(new Season { ProviderSeasonId = 1, Number = 1 });
        context.Shows.Add(existing);
        await context.SaveChangesAsync();
        var provider = new StubDetails(new ImportedShow(
            82, "Game of Thrones", 2011, "Ended", "show.jpg",
            [new ImportedSeason(8, 8, new DateOnly(2019, 4, 14), new DateOnly(2019, 5, 19))]));

        var result = await new ImportShowDetailsCommand(context, provider)
            .ExecuteAsync(82, CancellationToken.None);

        Assert.Equal("Game of Thrones", result.Title);
        Assert.Equal("Ended", existing.Status);
        Assert.Collection(existing.Seasons, season => Assert.Equal(8, season.Number));
    }

    [Fact]
    public async Task Provider_failure_preserves_existing_metadata()
    {
        await using var context = CreateContext();
        var existing = new Show { ProviderId = 82, Title = "Stored title", Status = "Running" };
        context.Shows.Add(existing);
        await context.SaveChangesAsync();
        var provider = new StubDetails(new TvShowNotFoundException());

        await Assert.ThrowsAsync<TvShowNotFoundException>(() =>
            new ImportShowDetailsCommand(context, provider).ExecuteAsync(82, CancellationToken.None));

        Assert.Equal("Stored title", existing.Title);
        Assert.Equal("Running", existing.Status);
    }

    [Fact]
    public async Task Refresh_preserves_completion_data_for_existing_season()
    {
        await using var context = CreateContext();
        var existing = new Show
        {
            ProviderId = 82,
            Title = "Old title",
            Status = "Running"
        };
        var completedAt = new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero);
        existing.Seasons.Add(new Season
        {
            ProviderSeasonId = 8,
            Number = 8,
            CompletedAt = completedAt
        });
        context.Shows.Add(existing);
        await context.SaveChangesAsync();

        var provider = new StubDetails(new ImportedShow(
            82, "Game of Thrones", 2011, "Ended", "show.jpg",
            [new ImportedSeason(8, 8, new DateOnly(2019, 4, 14), new DateOnly(2019, 5, 19))]));

        var result = await new ImportShowDetailsCommand(context, provider)
            .ExecuteAsync(82, CancellationToken.None);

        Assert.Single(result.Seasons);
        Assert.Equal(completedAt, result.Seasons.First().CompletedAt);
        Assert.Equal(new DateOnly(2019, 4, 14), result.Seasons.First().PremiereDate);
    }

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed class StubDetails : ITvShowDetails
    {
        private readonly ImportedShow? show;
        private readonly Exception? error;

        public StubDetails(ImportedShow show) => this.show = show;
        public StubDetails(Exception error) => this.error = error;

        public Task<ImportedShow> GetAsync(int providerId, CancellationToken cancellationToken) =>
            error is null ? Task.FromResult(show!) : Task.FromException<ImportedShow>(error);
    }
}
