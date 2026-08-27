# Time Boundary Examples

## Date-Only Global Release

Selected fallback: end of listed date in UTC.

| Air date | Evaluation instant | Result |
|---|---|---|
| `2024-07-18` | `2024-07-18T23:59:59Z` | `NotCompleted` |
| `2024-07-18` | `2024-07-19T00:00:00Z` | Eligible only if finale identity was independently established |

## Precise Original-Zone Broadcast

Completion uses episode end, not broadcast start. For Game of Thrones S8E6, TVmaze gives start `2019-05-20T01:00:00Z` and runtime must be added before eligibility.

| Evaluation instant | Result |
|---|---|
| One second before computed episode end | `NotCompleted` |
| At computed episode end | Eligible only with independent finale identity |

## Daylight-Saving Boundary

Use IANA zone conversion, never a fixed offset. Test clocks immediately before and after local midnight on both DST transition dates in `America/New_York`. Date-only eligibility depends on local calendar rollover, even when local day has 23 or 25 hours.

### Spring Transition Date

`2024-03-10` begins at `2024-03-10T05:00:00Z` in `America/New_York`. DST advances later that local day.

| Listed air date | Evaluation instant | New York local time | Result |
|---|---|---|---|
| `2024-03-09` | `2024-03-10T04:59:59Z` | `2024-03-09 23:59:59 -05:00` | `NotCompleted` |
| `2024-03-09` | `2024-03-10T05:00:00Z` | `2024-03-10 00:00:00 -05:00` | Date boundary passed |

### Fall Transition Date

`2024-11-03` begins at `2024-11-03T04:00:00Z` in `America/New_York`. DST retreats later that local day.

| Listed air date | Evaluation instant | New York local time | Result |
|---|---|---|---|
| `2024-11-02` | `2024-11-03T03:59:59Z` | `2024-11-02 23:59:59 -04:00` | `NotCompleted` |
| `2024-11-02` | `2024-11-03T04:00:00Z` | `2024-11-03 00:00:00 -04:00` | Date boundary passed |

These examples establish calendar boundaries only. Completion still requires independent finale identity.

## Unknown Timezone

- Network or local web channel without trustworthy timezone: `Uncertain`.
- Global web channel with `country=null`: UTC date-only fallback applies only after finale identity is independently established.
