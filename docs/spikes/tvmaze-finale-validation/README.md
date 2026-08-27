# TVmaze Finale Validation Spike

## Decision

TVmaze is suitable for search and schedule refresh, but **insufficient by itself for automatic finale confirmation**. A second-source spike is required. See [ADR 0001](../../decisions/0001-use-tvmaze-with-conservative-completion.md).

Captured: `2026-08-27`.

## Goal

Determine whether TVmaze provides enough evidence to emit a trustworthy season-completion event without treating the latest currently known episode as a finale.

## Evidence Index

| Case | Expected | Fixture | Independent evidence |
|---|---|---|---|
| Normal completed season | `Completed` | [`game-of-thrones-s08.json`](fixtures/game-of-thrones-s08.json) | [HBO Season 8](https://www.hbo.com/game-of-thrones/season-8) |
| Ongoing season | `NotCompleted` | [`americas-got-talent-s21.json`](fixtures/americas-got-talent-s21.json) | [NBC show page](https://www.nbc.com/americas-got-talent) |
| Significant special | `Ineligible` | [`sherlock-significant-special.json`](fixtures/sherlock-significant-special.json) | [BBC episode page](https://www.bbc.co.uk/programmes/p0390wnv) |
| Season zero | `Ineligible` | [`game-of-thrones-season-zero.json`](fixtures/game-of-thrones-season-zero.json) | `Unknown`: provider classification test only |
| Apparent full-season batch | `Uncertain` | [`the-residence-s01.json`](fixtures/the-residence-s01.json) | [Netflix title page](https://www.netflix.com/title/81005297) lists episodes but retained evidence does not explicitly date full batch |
| Split release | `Completed` only after second batch | [`bridgerton-s03.json`](fixtures/bridgerton-s03.json) | [Netflix Tudum](https://www.netflix.com/tudum/articles/bridgerton-season-3-filming-cast-news) |
| Date-only weekly release | `Completed` after finale date ends UTC | [`the-boys-s04.json`](fixtures/the-boys-s04.json) | [Amazon release schedule](https://www.aboutamazon.com/news/entertainment/the-boys-season-4-prime-video) |
| Future timezone edge | `Uncertain` | [`drag-race-down-under-vs-the-world-s01.json`](fixtures/drag-race-down-under-vs-the-world-s01.json) | [Stan title page](https://www.stan.com.au/watch/drag-race-down-under-vs-the-world) |
| Postponed finale, prior expectation | `NotCompleted` after refresh | [`supernatural-s15-before-postponement.json`](fixtures/supernatural-s15-before-postponement.json) | [POPSUGAR old/new dates](https://www.popsugar.com/entertainment/supernatural-series-finale-air-date-47312828) |
| Postponed finale, current state | `Completed` | [`supernatural-s15-after-reschedule.json`](fixtures/supernatural-s15-after-reschedule.json) | [Deadline revised schedule](https://deadline.com/2020/08/cw-fall-premiere-dates-supernatural-swamp-thing-devils-pandora-masters-of-illusion-1203015255/) |
| Episode-order contradiction | `Uncertain` without additional evidence | [`the-office-order-mismatch.json`](fixtures/the-office-order-mismatch.json) | `Unknown`: provider contradiction test only |
| Season end-date distortion | `Uncertain` without additional evidence | [`breaking-bad-s05-enddate-mismatch.json`](fixtures/breaking-bad-s05-enddate-mismatch.json) | `Unknown`: provider contradiction test only |

## Provider Endpoints

- `GET /shows/:id`
- `GET /shows/:id/seasons`
- `GET /shows/:id/episodes?specials=1`
- `GET /seasons/:id/episodes`
- `GET /updates/shows`

TVmaze API documentation: <https://www.tvmaze.com/api>

## Provider Findings

- Episodes expose `season`, `number`, `type`, `airdate`, `airtime`, and `airstamp`.
- Seasons expose `number`, `premiereDate`, `endDate`, and `episodeOrder`.
- Network and local web-channel records can expose IANA timezone through country.
- Global web channels use `country=null`; they provide no original timezone.
- Global streaming episodes often have empty `airtime` while `airstamp` is noon UTC. Treat this as date-only, not a trustworthy release instant.
- TVmaze has no explicit finale, batch-release, split-part, postponement, or revision-history field.
- `episodeOrder` and observed regular episode count sometimes disagree.
- `endDate` can include content outside the numbered regular run.
- `/updates/shows` signals current-data changes but contains no old values.
- Public API is cached for up to one hour and allows at least 20 calls per 10 seconds per IP. Clients must handle `429` with backoff; an identifying User-Agent is strongly recommended.
- Data is CC BY-SA. Attribution is required, and ShareAlike obligations need legal/product confirmation before persisted derived data is shipped.

## Product Decisions

- Original network/local-channel timezone controls precise and date-only eligibility after finale identity is established.
- Global web-channel records with no trustworthy airtime use the user-selected fallback: after listed date ends in UTC. This controls timing only; it does not prove an episode is a finale.
- Empty `airtime` overrides synthetic-looking `airstamp`; it is a date-only record.
- Regular episodes only. Season zero and every non-`regular` episode are ineligible.
- No notification is emitted from `endDate`, latest-known episode, episode count, or `episodeOrder` alone.
- An external explicit finale signal is required for automatic completion. Until another source is selected, candidate seasons remain `Uncertain`.
- Provider data is refreshed before digest preparation. A future revised schedule cancels stale eligibility.
- Local normalized snapshots retain schedule changes because TVmaze cannot reconstruct history.

## Remaining Risk

TVmaze cannot prove that an announced episode is a finale even when `episodeOrder` and observed count agree; another episode can be added later. TVmaze-alone candidates therefore remain `Uncertain`. This triggers a second-provider spike before Story 017 implementation.
