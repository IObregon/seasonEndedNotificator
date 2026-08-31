using SeasonEnded.Api.SeasonTracking;

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

    public void UpdateMetadata(string title, int? premiereYear, string status, string? imageUrl)
    {
        Title = title;
        PremiereYear = premiereYear;
        Status = status;
        ImageUrl = imageUrl;
    }
}

public sealed class Season
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ShowId { get; set; }
    public Show Show { get; set; } = null!;
    public int ProviderSeasonId { get; init; }
    public int Number { get; init; }
    public DateOnly? PremiereDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public UncertaintyReason? UncertaintyReason { get; set; }
    public DateTimeOffset? FinaleAirStart { get; set; }
    public int? FinaleRuntimeMinutes { get; set; }

    public void UpdateSchedule(DateOnly? premiereDate, DateOnly? endDate)
    {
        PremiereDate = premiereDate;
        EndDate = endDate;
    }

    public void MarkCompleted(DateTimeOffset completedAt)
    {
        UncertaintyReason = null;
        CompletedAt = completedAt;
    }

    public void RecordUncertainty(UncertaintyReason? reason)
    {
        UncertaintyReason = reason;
    }

    public void RefreshFinaleSchedule(DateTimeOffset airStart, int? runtimeMinutes, UncertaintyReason? uncertainty)
    {
        FinaleAirStart = airStart;
        FinaleRuntimeMinutes = runtimeMinutes;
        UncertaintyReason = uncertainty;
    }
}
