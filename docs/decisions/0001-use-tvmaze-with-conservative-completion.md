# ADR 0001: Use TVmaze for Catalog, Not Finale Authority

- Status: Accepted
- Date: 2026-08-27

## Context

MVP needs free show discovery and episode schedules. It must avoid notifying users before a finale airs. TVmaze provides broad TV metadata without API keys, but does not identify finales or preserve schedule history.

Spike evidence is indexed in [`../spikes/tvmaze-finale-validation/README.md`](../spikes/tvmaze-finale-validation/README.md).

## Decision

Use TVmaze as MVP catalog and schedule provider. Do not use it alone as finale authority. Persist normalized local snapshots, refresh before digest creation, and withhold candidates as `Uncertain` unless an independent source explicitly establishes finale identity.

For global web channels lacking timezone and trustworthy airtime, use user-selected UTC end-of-date fallback after finale identity is independently established.

Attribute TVmaze in UI. Confirm how persisted normalized/derived data will satisfy CC BY-SA ShareAlike obligations before release.

## Consequences

### Positive

- No API key or provider subscription is needed.
- Search, seasons, episodes, timestamps, and update signals come from one API.
- Refusing TVmaze-only finale inference lowers false-positive risk.
- Anti-corruption layer keeps provider replacement possible.

### Negative

- All finales remain uncertain until another source establishes finale identity.
- Batch and split release intent is inferred from counts and dates, not explicit metadata.
- Local snapshots are required to detect schedule changes.
- One-hour provider caching prevents instant correction visibility.

## Rejected Alternatives

- **Trust `endDate`:** historical records show it can include non-regular content.
- **Trust latest known episode:** future episodes may not yet be entered.
- **Trust `episodeOrder` alone:** counts and numbering can disagree.
- **Treat TVmaze as sole authority:** fails spike stop condition because no explicit finale marker exists.
- **Mark every global streaming release uncertain:** safe but unnecessarily excludes common date-based releases; UTC next-day fallback is deterministic and user-approved.

## Revisit When

- Second-source spike identifies a sustainable explicit finale signal.
- TVmaze changes API fields, license, limits, or availability.
- Another provider supplies explicit finale semantics with sustainable cost.
