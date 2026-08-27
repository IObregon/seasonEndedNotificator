# Decision Questions and Outcomes

## Questions

| Question | Minimum acceptable evidence |
|---|---|
| Which numbered season does episode belong to? | Positive episode and season number with matching provider season |
| Is episode regular content? | Episode `type=regular` |
| Is candidate final regular episode? | Explicit finale designation from an independent source; TVmaze count/order only corroborates it |
| Did candidate finish airing? | Trustworthy `airstamp` plus runtime has passed, or conservative date-only eligibility boundary has passed |
| What timezone applies? | Network/local web-channel IANA timezone; UTC fallback for global channel with no country |
| Is timestamp trustworthy? | Non-empty `airtime`; otherwise treat record as date-only |
| Was finale postponed? | Latest fetched candidate schedule supersedes locally stored expectation before event confirmation |
| Was entire batch released? | All expected regular episodes exist and share passed release date; count agrees with `episodeOrder` |
| Is split release complete? | Entire expected count exists; first cluster alone never qualifies |
| Can provider changes be detected? | `/updates/shows` or scheduled refresh triggers comparison with stored normalized snapshot |

## Outcomes

### `Completed`

Explicit finale evidence exists, all TVmaze corroborating evidence agrees, candidate end boundary has passed, and no later regular episode is known.

### `NotCompleted`

Evidence is coherent but candidate eligibility boundary is future, expected regular episodes are missing, or a later regular episode is scheduled.

### `Uncertain`

Mandatory evidence is missing or contradictory. Examples: null/order mismatch, season end date beyond final numbered episode, unknown timezone for local channel, or impossible numbering.

### `Ineligible`

Season number is zero or episode type is not `regular`.

## Safety Bias

False finale notifications are worse than delayed notifications. Unknown and contradictory cases therefore become `Uncertain`, never `Completed`.
