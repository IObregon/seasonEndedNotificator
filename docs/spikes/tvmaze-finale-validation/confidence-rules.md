# Candidate Completion Rules

Evaluate rules in order against latest normalized TVmaze snapshot.

1. Return `Ineligible` when season number is zero or candidate type is not `regular`.
2. Return `Uncertain` when season, candidate number, or candidate air date is missing.
3. Return `Uncertain` when no trustworthy timezone can be established for a network or local web channel.
4. Ignore `endDate` as standalone proof. It may corroborate candidate date only.
5. Ignore latest-known episode as standalone proof. Future episodes may be unannounced.
6. Require explicit finale identity from an independent source for automatic completion. TVmaze alone cannot provide this evidence.
7. Use non-null positive `episodeOrder`, observed regular count, and candidate number only as corroboration.
8. Return `Uncertain` when count, order, numbering, or `endDate` contradict one another.
9. Return `NotCompleted` only when coherent evidence establishes a future candidate or expected episodes. Return `Uncertain` when missing episodes may indicate incomplete provider data.
10. If `airtime` is non-empty, require `airstamp` plus episode runtime to have passed. If runtime is unavailable, apply a conservative configurable buffer.
11. If `airtime` is empty, ignore `airstamp` time component and wait until `airdate` ends in applicable timezone.
12. For global web channels with `country=null`, date-only boundary is end of listed date in UTC.
13. Batch release qualifies only when an independent source explicitly confirms full-season release, all expected regular episodes are present, and their common release date has ended.
14. Split release's first cluster remains `NotCompleted` because regular count is below `episodeOrder`.
15. Refresh candidate before confirmation. If schedule moved into future, return `NotCompleted` and do not emit stale event.
16. Emit at most one `SeasonCompleted` event for confirmed season transition.

## Fixture Results

| Fixture | Result | Reason |
|---|---|---|
| Game of Thrones S8 | `Completed` | HBO independently identifies final chapter; TVmaze count/order corroborates it; end boundary passed |
| America's Got Talent S21 | `NotCompleted` | Known finale timestamp is future as of capture |
| Sherlock special | `Ineligible` | `significant_special`, null episode number |
| The Residence S1 | `Uncertain` | Retained Netflix evidence lists eight episodes but does not explicitly establish full-season release date |
| Bridgerton S3 after first batch | `NotCompleted` | 4 observed regular episodes below order 8 |
| Bridgerton S3 after second batch | `Completed` | Netflix explicitly states two batches of four and Part 2 date; TVmaze count/order corroborates it; date passed |
| The Boys S4 | `Completed` | 8 regular episodes equal order 8; date-only finale date passed UTC |
| Drag Race Down Under vs The World S1 | `Uncertain` | Future candidate exists, but count/order and premiere data contradict |
| Supernatural stored old schedule plus refreshed future schedule | `NotCompleted` | Latest snapshot supersedes stale date before confirmation |
| Supernatural current historical snapshot | `Completed` | Independent reporting identifies series finale; TVmaze corroborates revised schedule; end boundary passed |
| The Office S7/S9 | `Uncertain` | Episode count/number and `episodeOrder` disagree |
| Breaking Bad S5 | `Uncertain` | Season `endDate` is years after highest regular episode |

## Revisit Triggers

- Material false negatives among followed shows.
- TVmaze adds explicit finale/release-mode/revision metadata.
- Product chooses broad coverage over conservative confidence.
- Second provider can close uncertainty without fragile show matching.

## TVmaze-Only Result

Without independent explicit finale evidence, all otherwise plausible completion candidates remain `Uncertain`. This spike therefore requires a second-source investigation before production completion logic is implemented.
