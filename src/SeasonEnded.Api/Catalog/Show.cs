namespace SeasonEnded.Api.Catalog;

public sealed class Show
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public int ProviderId { get; init; }
    public string Title { get; set; } = "";
    public int? PremiereYear { get; set; }
    public string Status { get; set; } = "";
    public string? ImageUrl { get; set; }
    public List<Season> Seasons { get; } = [];
}

public sealed class Season
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ShowId { get; set; }
    public Show Show { get; set; } = null!;
    public int ProviderSeasonId { get; init; }
    public int Number { get; init; }
    public DateOnly? PremiereDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public DateTimeOffset? CompletedAt { get; set; }
}
