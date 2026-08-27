# Finale Authority Selection

Captured: `2026-08-27`.

## Evidence Level

No API credentials were available. Evaluation uses official public documentation, schemas, and documented fixtures rather than live API captures. This limitation is explicit in the decision and requires a credentialed smoke test before production rollout.

## Acceptance Gate

| Requirement | Pass condition |
|---|---|
| Explicit finale identity | Episode payload contains a semantic finale field; count/order/latest episode is insufficient |
| Regular season distinction | Season zero/specials can be excluded |
| Mapping | Exact IMDb or TheTVDB identifier mapping from TVmaze; title-only matching rejected |
| Corrections | Current metadata can be refreshed and changes can be detected |
| Timing | Finale identity remains independent from date/time precision |
| Cost | Near-zero noncommercial MVP use is permitted |
| Terms | Attribution and usage restrictions are documented before integration |

## Candidate Results

### TMDB

Result: **Pass and selected**.

Official season-details fixture for Game of Thrones season 1 contains:

```json
{
  "episode_number": 10,
  "episode_type": "finale",
  "season_number": 1
}
```

Episode 9 in the same fixture has `episode_type: "standard"`. This is direct semantic identity, not last-episode inference.

Use endpoint:

```text
GET /3/tv/{series_id}/season/{season_number}
```

Do not use standalone episode details as authority because its public schema does not expose `episode_type`.

Mapping evidence:

- Series external IDs expose `imdb_id` and `tvdb_id`.
- Season external IDs expose `tvdb_id`.
- Episode external IDs expose IMDb and TheTVDB IDs.
- `GET /3/find/{external_id}` supports exact reverse lookup.

Correction evidence:

- Season and episode change endpoints expose recent changes.
- Change queries cover at most 14 days.
- Metadata is mutable and community maintained; finale evidence must be refreshed before notification.

Operational terms:

- Application authentication required.
- Noncommercial API use is free with attribution.
- Commercial use requires agreement with TMDB.
- Current practical upper limit is approximately 40 requests per second; handle `429`.
- Required attribution includes approved TMDB logo and non-endorsement notice.
- Cached data is subject to TMDB terms, including retention restrictions.

Primary evidence:

- <https://developer.themoviedb.org/reference/tv-season-details>
- <https://developer.themoviedb.org/reference/tv-series-external-ids>
- <https://developer.themoviedb.org/reference/tv-season-external-ids>
- <https://developer.themoviedb.org/reference/tv-episode-external-ids>
- <https://developer.themoviedb.org/reference/find-by-id>
- <https://developer.themoviedb.org/reference/tv-season-changes-by-id>
- <https://developer.themoviedb.org/docs/rate-limiting>
- <https://developer.themoviedb.org/docs/faq>

### Trakt

Result: **Pass as fallback**.

Official schema defines explicit episode types:

```text
standard
series_premiere
season_premiere
mid_season_finale
mid_season_premiere
season_finale
series_finale
```

Use episode summary with `extended=full`. Finale calendars identify candidates, but grouped values such as `full_season` and `multiple_episodes` are not finale identity; child episodes still require `season_finale` or `series_finale`.

Mapping evidence:

- Show and episode IDs can expose IMDb, TheTVDB, and TMDB IDs.
- External-ID lookup is available.
- IDs are nullable; title-only fallback is rejected.

Correction evidence:

- Show update feeds and episode `updated_at` support refresh.
- No revision history or prior values are documented.

Operational terms:

- Registered app client ID required.
- Unauthenticated application GET limit is documented as 500 calls per 5 minutes.
- Standard noncommercial API access appears free; commercial redistribution requires direct approval.
- Trakt branding requirements apply.

Primary evidence:

- <https://docs.trakt.tv/reference/getshowsepisodesummary.md>
- <https://docs.trakt.tv/reference/getcalendarsfinales.md>
- <https://docs.trakt.tv/docs/standard-media-objects.md>
- <https://docs.trakt.tv/reference/getsearchlookup.md>
- <https://docs.trakt.tv/docs/caching-and-fresh-data.md>
- <https://docs.trakt.tv/docs/rate-limiting.md>
- <https://docs.trakt.tv/docs/create-an-app.md>

## Story 001 Fixture Matrix

These are contract evaluations against identical Story 001 categories, not live captures.

| Category | TMDB | Trakt | Decision |
|---|---|---|---|
| Normal finale | Concrete fixture uses `finale` | Schema uses `season_finale` | Both pass |
| Series finale | Generic `finale`; series status corroborates only | Explicit `series_finale` | Trakt richer, TMDB still proves finale |
| Ongoing episode | `standard` means current non-finale classification | `standard` means current non-finale classification | Both pass; missing value is uncertain |
| Batch release | Finale-tagged child plus release date | Finale-tagged child; ignore `full_season` grouping | Conditional pass |
| Split release | Finale tag independent of date gap | Distinguishes `mid_season_finale` and `season_finale` | Both pass; Trakt richer |
| Date-only | Identity from type; timing remains date-only | Identity from type; timing may remain date-only | Both pass identity only |
| Special | Exclude `season_number=0` | Exclude season zero | Both pass exclusion |
| Postponed | Refresh mutable air date and change feed | Refresh current summary/update feed | Neither proves postponement history |
| Contradictory metadata | Quarantine conflict | Quarantine conflict | Neither supplies provenance arbitration |

## Mapping Policy

1. Use TVmaze TheTVDB ID for TMDB exact lookup.
2. Fall back to IMDb ID.
3. Verify returned title and original premiere year.
4. Reject zero, multiple, contradictory, or title-only matches.
5. Persist provider IDs only after deterministic match.

## Authority Rule

For MVP, a finale is explicitly identified only when TMDB season details contain:

```text
episodes[].episode_type == "finale"
```

Counts, highest episode number, season end date, show status, and absence of a next episode never establish finale identity.

Missing or unknown `episode_type`, mapping ambiguity, schedule contradiction, or provider disagreement yields `Uncertain`.

## Residual Risks

- Public fixtures cover one concrete normal finale, not every release pattern.
- `episode_type` has no documented enum in TMDB OpenAPI despite fixture use.
- TMDB data is mutable and community edited.
- Credentialed fixture verification remains mandatory before production activation.
- Legal review remains required for commercial use and caching terms.
