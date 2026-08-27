# ADR 0002: Use TMDB as Finale Authority

- Status: Accepted with pre-production verification condition
- Date: 2026-08-27

## Context

TVmaze provides discovery and broadcast schedules but cannot explicitly identify a season finale. Story 001a compared TMDB and Trakt using official public schemas and fixtures because API credentials were unavailable.

Evidence: [`../spikes/finale-authority-selection/README.md`](../spikes/finale-authority-selection/README.md).

## Decision

Retain TVmaze for search, show metadata, precise airtimes, and schedule refresh. Use TMDB season details as independent finale authority when `episodes[].episode_type` equals `finale`.

Map TVmaze to TMDB by exact TheTVDB ID, then IMDb ID. Reject ambiguous or title-only matches. Refresh TMDB evidence before emitting completion because finale classification and dates are mutable.

Trakt remains fallback candidate because it provides richer `season_finale` and `series_finale` semantics, but public evidence did not include a concrete real finale response fixture.

## Consequences

- Production needs a TMDB application credential.
- UI must satisfy both TVmaze and TMDB attribution requirements.
- Provider matching becomes an explicit uncertainty boundary.
- Missing finale type never means “not finale”; it means unproven.
- Counts and latest-known episodes remain corroboration only.
- A credentialed test against Story 001 fixtures is required before enabling production notifications.

## Revisit When

- TMDB fixture verification fails for representative release patterns.
- Finale classification coverage produces unacceptable uncertainty.
- TMDB terms, rate limits, or commercial requirements change.
- Trakt live evidence demonstrates materially better coverage and sustainable mapping.
