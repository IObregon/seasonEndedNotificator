# Season Ended Notificator - Implementation Plan

## 1. Product Scope

Build an invite-only PWA for 25-100 users. Users search for TV shows, follow them, and receive a daily notification after a regular numbered season's final episode has aired in its original broadcast market.

### MVP capabilities

- Email magic-link authentication.
- Admin invitations and multiple administrators.
- TV show search and metadata from TVmaze.
- Persistent show follows covering all future seasons.
- Daily digest at `09:00 UTC`.
- Email, Telegram, and web push delivery.
- Per-user default channel preferences.
- English and Spanish UI and notifications.
- Multiple web push devices per user.
- Installable Vue PWA.
- Admin screens for users, deliveries, metadata refreshes, and system health.
- Self-service account deletion.

### Explicit exclusions

- Streaming availability.
- Episode-level reminders.
- Watch progress.
- Social features.
- Native mobile applications.
- Recommendation engine.
- Per-show channel overrides.
- Public registration.
- Historical notification backfill.

## 2. Notification Rules

A season is eligible for notification when all these conditions hold:

- Its season number is greater than zero.
- It is a regular numbered season, not specials.
- Latest provider data identifies a final regular episode with sufficient confidence.
- Finale airtime has passed in the show's original broadcast timezone.
- If only a date is known, that date has fully ended in the original timezone.
- For full-season batch releases, the release date has fully ended.
- User followed the show before season completion became eligible.
- User has not already received that season event through the selected channel.

Additional behavior:

- Refresh imminent finales before creating the daily digest.
- Honor postponements found during the pre-digest refresh.
- Keep uncertain or contradictory season data out of automatic notifications.
- Following an already-ended show displays its status but sends no retrospective notification.
- Combine all newly ended seasons into one digest per enabled channel.
- Include title, season number, completion date, and internal show link.
- Exclude finale title and synopsis to avoid spoilers.
- Retry transient delivery failures with exponential backoff.

## 3. Technology Stack

### Frontend

- Vue 3.
- Vite.
- TypeScript.
- Vue Router.
- `vue-i18n` for English and Spanish.
- `vite-plugin-pwa` for manifest, service worker, installation, and push support.
- Generated TypeScript API client from backend OpenAPI document.

### Backend

- Current supported LTS release of .NET.
- ASP.NET Core Minimal APIs.
- Domain-Driven Design with bounded contexts and explicit application use cases.
- Entity Framework Core.
- PostgreSQL.
- Hosted background services in the API process for MVP.
- OpenAPI.

### Deployment

- Docker Compose on one VPS.
- Caddy as reverse proxy with automatic HTTPS.
- GitHub Actions for build, test, image publishing, and deployment.
- GitHub Container Registry.

## 4. Backend DDD Architecture

Use DDD to protect core season-completion and notification behavior. Avoid ceremony for simple CRUD. Domain objects own business invariants; application handlers coordinate use cases; infrastructure implements external concerns; API endpoints only translate HTTP requests and responses.

### Dependency direction

```text
API -> Application -> Domain
Infrastructure -> Application + Domain
```

`Domain` has no dependency on ASP.NET Core, EF Core, TVmaze, SMTP, Telegram, web push, or system clock implementations.

### Suggested solution structure

```text
/
  src/
    SeasonEnded.Api/
      Endpoints/
      Authentication/
      Middleware/
      OpenApi/
      Program.cs
    SeasonEnded.Application/
      Abstractions/
      Identity/
      Catalog/
      Subscriptions/
      SeasonTracking/
      Notifications/
      Administration/
    SeasonEnded.Domain/
      Shared/
      Identity/
      Catalog/
      Subscriptions/
      SeasonTracking/
      Notifications/
    SeasonEnded.Infrastructure/
      Persistence/
      Tvmaze/
      Email/
      Telegram/
      WebPush/
      Jobs/
      Time/
    SeasonEnded.Web/
  tests/
    SeasonEnded.Domain.Tests/
    SeasonEnded.Application.Tests/
    SeasonEnded.IntegrationTests/
  deploy/
    Caddyfile
    compose.yml
    compose.production.yml
  .github/
    workflows/
  docs/
    PLAN.md
    stories/
      README.md
  README.md
  .env.example
```

Organize application and domain code by business capability, not by generic folders such as `Services` or `Managers`.

## 5. Bounded Contexts

### Identity and Access

Responsibilities:

- Invitations.
- User lifecycle.
- Magic-link authentication.
- Roles and account status.
- Language preference.
- Account deletion.

Primary aggregate roots:

- `User`
- `Invitation`

Important invariants:

- Only active invited users can authenticate.
- Invitation and magic-link tokens are single-use and expire.
- Disabled users cannot authenticate or receive notifications.
- At least one active administrator should remain when changing roles or deleting accounts.

Domain events:

- `UserInvited`
- `InvitationAccepted`
- `UserDisabled`
- `UserLanguageChanged`
- `UserDeletionRequested`

### Show Catalog

Responsibilities:

- Provider-independent show identity and display metadata.
- TVmaze identifier mapping.
- Search-result caching.
- Show metadata freshness.

Primary aggregate root:

- `Show`

Keep TVmaze response models in Infrastructure. Translate them through an anti-corruption layer into domain concepts. TVmaze field names and response structures must not leak into Domain.

### Subscriptions

Responsibilities:

- Following and unfollowing shows.
- Recording follow time for notification eligibility.

Primary aggregate root:

- `ShowFollow`

Important invariants:

- One active follow per user and show.
- Follow persists across future seasons.
- Re-follow starts a new eligibility period or explicitly reactivates the existing record with a new `FollowedAt`.

Domain events:

- `ShowFollowed`
- `ShowUnfollowed`

### Season Tracking

This is the core domain.

Responsibilities:

- Seasons and regular episodes.
- Finale identification.
- Original-broadcast time rules.
- Completion confidence.
- Postponement and metadata correction handling.
- Creation of one canonical season-completion event.

Primary aggregate root:

- `TrackedSeason`

Entities and value objects:

- `Episode`
- `SeasonNumber`
- `EpisodeNumber`
- `BroadcastSchedule`
- `ProviderReference`
- `CompletionEvidence`
- `CompletionStatus`
- `CompletionConfidence`

Suggested completion states:

- `Upcoming`
- `Airing`
- `AwaitingConfirmation`
- `Completed`
- `Uncertain`

Important invariants:

- Season zero never completes for notification purposes.
- Specials cannot become a regular-season finale.
- Completion requires sufficient current evidence.
- Date-only schedules become eligible only after the date ends in original timezone.
- A completion transition creates at most one canonical event.
- New provider data may postpone an unconfirmed completion.
- Once notifications have been issued, provider corrections are recorded rather than silently rewriting delivery history.

Domain service:

- `SeasonCompletionPolicy` evaluates schedule, episodes, provider evidence, and current time when logic does not naturally belong to one entity.

Domain event:

- `SeasonCompleted`

`SeasonCompleted` should contain stable internal identifiers and completion facts, not rendered notification text.

### Notifications

Responsibilities:

- Per-user channel preferences.
- Digest eligibility.
- Digest aggregation.
- Localized message model selection.
- Delivery lifecycle and retry classification.
- Idempotency by user, channel, digest date, and season event.

Primary aggregate roots:

- `NotificationPreferences`
- `DigestDelivery`

Entities and value objects:

- `DigestItem`
- `NotificationChannel`
- `DeliveryStatus`
- `DeliveryAttempt`
- `RetrySchedule`

Important invariants:

- One digest per user, channel, and digest date.
- A season-completion event appears at most once in that delivery.
- Only events after the user's current follow time are included.
- Delivery cannot start for disabled channels or disconnected destinations.
- Permanent failures are not automatically retried.

Domain events:

- `DigestPrepared`
- `DeliverySucceeded`
- `DeliveryFailed`

## 6. Application Layer

Application use cases coordinate aggregates, repositories, transactions, external ports, and domain events. Use command/query records and focused handlers. A mediator library is optional; do not require one merely to implement CQRS-shaped code.

Representative commands:

```text
InviteUser
AcceptInvitation
RequestMagicLink
ConsumeMagicLink
DisableUser
DeleteOwnAccount
FollowShow
UnfollowShow
RefreshShowMetadata
EvaluateSeasonCompletion
PrepareDailyDigests
DeliverDigest
RetryFailedDelivery
ConnectTelegramAccount
RegisterPushSubscription
RevokePushSubscription
```

Representative queries:

```text
SearchShows
GetShowDetails
GetFollowedShows
GetNotificationPreferences
GetAdminUsers
GetDeliveryLog
GetMetadataIssues
GetSystemHealth
```

Application abstractions:

```text
IUnitOfWork
IUserRepository
IShowRepository
IShowFollowRepository
ITrackedSeasonRepository
IDigestDeliveryRepository
ITvCatalogGateway
IEmailSender
ITelegramSender
IWebPushSender
IClock
IJobLease
IMessageLocalizer
```

Repositories should align with aggregate roots. Do not expose generic repositories or unrestricted `IQueryable` outside Infrastructure.

## 7. Domain Events and Reliable Processing

Persist domain events through an outbox in the same PostgreSQL transaction as aggregate changes.

Example flow:

1. Pre-digest job refreshes TVmaze data through `ITvCatalogGateway`.
2. Application maps provider data into domain values.
3. `TrackedSeason` applies latest evidence.
4. `SeasonCompletionPolicy` confirms completion.
5. Aggregate records `SeasonCompleted`.
6. Aggregate changes and outbox message commit atomically.
7. Outbox processor handles `SeasonCompleted` and makes it available for digest preparation.
8. Digest job selects eligible completion events and creates `DigestDelivery` aggregates.
9. Delivery worker sends through channel adapters and records result.

Use an inbox or processed-message table for event-handler idempotency. PostgreSQL unique constraints remain the final duplicate-prevention boundary.

Avoid distributing these events through an external broker in MVP. PostgreSQL outbox polling is sufficient for expected scale and single-VPS operation.

## 8. Persistence Model

EF Core mappings live in Infrastructure. Domain entities should not require public setters or persistence-specific behavior.

Principal tables:

- `users`
- `invitations`
- `magic_link_tokens`
- `shows`
- `tracked_seasons`
- `episodes`
- `show_follows`
- `notification_preferences`
- `telegram_connections`
- `push_subscriptions`
- `season_completion_events`
- `digest_deliveries`
- `digest_items`
- `delivery_attempts`
- `outbox_messages`
- `processed_messages`
- `job_executions`

Critical constraints:

- Unique TVmaze ID per show.
- Unique provider season reference.
- Unique active follow per user/show.
- Unique completion event per tracked season.
- Unique digest per user/channel/date.
- Unique digest item per delivery/completion event.
- Unique push endpoint.

Use optimistic concurrency tokens for aggregates modified by background jobs. Use short transactions and PostgreSQL advisory locks or persisted leases for singleton scheduled jobs.

## 9. TVmaze Anti-Corruption Layer

Infrastructure owns:

- TVmaze HTTP client.
- Provider DTOs.
- Rate limiting and retry behavior.
- Cache policy.
- Mapping from provider data into application import models.

Run an initial data-quality spike covering:

- Ongoing broadcasts.
- Completed seasons.
- Postponed finales.
- Unknown episode counts.
- Batch releases.
- Split seasons.
- Miniseries.
- Specials and season zero.
- Multiple original broadcast timezones.

Proposed confidence rules:

1. Prefer an explicit season end date and final episode `airstamp`.
2. When declared episode order exists, require known regular episode count to satisfy it.
3. Require a matching final regular episode.
4. Mark incomplete or contradictory records `Uncertain`.
5. Never notify uncertain seasons automatically.
6. Surface uncertain records to administrators.

If TVmaze cannot establish reliable completion, retain it for discovery and assess a second schedule provider. Do not implement dual-provider matching without evidence from the spike.

## 10. Scheduled Work

### Daily metadata refresh

Run at approximately `07:00 UTC`:

1. Imminent finales.
2. Followed airing shows.
3. Other followed shows.
4. Stale completed shows at lower frequency.

### Pre-digest confirmation

Run at approximately `08:30 UTC`:

1. Refresh potentially eligible seasons.
2. Apply newest provider evidence.
3. Recalculate completion state.
4. Exclude postponed or uncertain finales.
5. Persist canonical completion events.

### Digest preparation

Run at `09:00 UTC`:

1. Select completion events not previously included for each user.
2. Apply active-user, follow-time, and channel-preference rules.
3. Create one digest per enabled channel.
4. Persist digest and items transactionally.
5. Queue delivery through outbox processing.

### Retry worker

Example retry schedule:

- Immediately.
- After 5 minutes.
- After 30 minutes.
- After 2 hours.
- After 12 hours.
- Mark permanently failed after final attempt.

Retry timeouts, rate limits, and remote server failures. Do not endlessly retry invalid Telegram chats, rejected email addresses, or expired push endpoints.

## 11. Notification Adapters

### Email

Define `IEmailSender` in Application. Infrastructure provides:

- Production transactional email API adapter.
- Local SMTP adapter targeting Mailpit.

Messages have HTML and plain-text bodies. Templates support English and Spanish. Store provider message IDs and sanitized failure details.

### Local email verification with Mailpit

Add Mailpit to development Docker Compose:

```yaml
mailpit:
  image: axllent/mailpit:latest
  ports:
    - "1025:1025"
    - "8025:8025"
```

Development configuration:

```json
{
  "Email": {
    "Provider": "Smtp",
    "Smtp": {
      "Host": "mailpit",
      "Port": 1025,
      "UseTls": false,
      "FromAddress": "notifications@season-ended.local",
      "FromName": "Season Ended"
    }
  }
}
```

When backend runs directly on host instead of Compose, use `localhost` as SMTP host.

Local verification flow:

1. Start development stack with Docker Compose.
2. Open `http://localhost:8025`.
3. Request a magic link, issue an invitation, or trigger a test digest.
4. Confirm message appears in Mailpit inbox.
5. Inspect HTML, plain-text body, recipients, headers, and links.

Mailpit captures messages and sends nothing to internet. Add an admin-only development endpoint or CLI command to create a preview digest for a selected user. Compile this endpoint only for Development or guard it with both environment and admin authorization.

Integration tests should use Mailpit's HTTP API or an in-memory test `IEmailSender` to verify:

- Recipient.
- Subject language.
- HTML and plain-text bodies.
- Magic-link URL.
- Digest item count.
- No duplicate send after retry or application restart.

Never use production email API credentials in local configuration.

### Telegram

Connect accounts through a bot deep-link containing a short-lived, single-use token. A verified webhook associates Telegram chat identity with the authenticated user. Support disconnect and permanent invalid-chat handling.

### Web push

Use standard Web Push with VAPID keys. Store multiple subscriptions per user. Remove subscriptions returning permanent `404` or `410` responses. Push clicks open the relevant internal digest or show page.

## 12. Frontend and PWA

Primary screens:

- Sign in.
- Accept invitation.
- Language onboarding.
- Followed-show dashboard.
- Show search.
- Show details and season history.
- Notification settings.
- Telegram connection.
- Push-device management.
- Account deletion.
- Admin users.
- Admin deliveries.
- Admin metadata inspection.
- Admin system health.

PWA requirements:

- Manifest and icons.
- Install guidance.
- Service worker.
- Offline shell and clear network-error state.
- Push subscription and click handling.
- Update-available prompt.
- Responsive layouts.
- Accessible forms and keyboard navigation.
- English and Spanish copy.

Cache static assets and the application shell. Avoid aggressive caching of authenticated API responses.

## 13. HTTP API

Representative endpoints:

```text
POST   /api/auth/magic-link
GET    /api/auth/magic-link/consume
POST   /api/auth/logout
GET    /api/me
PATCH  /api/me
DELETE /api/me

GET    /api/shows/search
GET    /api/shows/{id}
POST   /api/shows/{id}/follow
DELETE /api/shows/{id}/follow
GET    /api/follows

GET    /api/notification-preferences
PUT    /api/notification-preferences

POST   /api/telegram/connect
DELETE /api/telegram/connection
POST   /api/telegram/webhook

GET    /api/push/subscriptions
POST   /api/push/subscriptions
DELETE /api/push/subscriptions/{id}

GET    /api/admin/users
POST   /api/admin/invitations
DELETE /api/admin/invitations/{id}
PATCH  /api/admin/users/{id}
GET    /api/admin/deliveries
POST   /api/admin/deliveries/{id}/retry
POST   /api/admin/shows/{id}/refresh
GET    /api/admin/metadata/issues
GET    /api/admin/health

POST   /api/dev/email-preview
```

`/api/dev/email-preview` must not be mapped outside Development.

## 14. Security and Privacy

- Secure, `HttpOnly`, `SameSite=Lax` authentication cookies.
- CSRF protection for state-changing requests.
- Single-use hashed invitation and magic-link tokens.
- Generic magic-link request responses to prevent user enumeration.
- Rate limits for authentication, search, refresh, and webhook endpoints.
- HTTPS everywhere outside local development.
- Secrets supplied through environment variables or Docker secrets.
- PostgreSQL unavailable from public network.
- Structured logs excluding tokens, push secrets, and message bodies.
- Audit records for administrator actions.
- Restricted detailed health information.
- Dependency and container vulnerability scans.

Account deletion removes or anonymizes:

- Follows.
- Preferences.
- Telegram connection.
- Push subscriptions.
- Sessions and tokens.
- Personally identifying delivery records where retention is unnecessary.

## 15. Testing Strategy

### Domain tests

Use plain unit tests without database or framework dependencies:

- Season-zero exclusion.
- Specials exclusion.
- Original timezone conversion.
- Date-only waiting rule.
- Batch-release completion.
- Postponement before confirmation.
- Completion confidence.
- Single `SeasonCompleted` event.
- Follow-date eligibility.
- Digest idempotency.
- Retry classification.

Use an injected `IClock` at application boundaries so tests remain deterministic.

### Application tests

Test use-case handlers with fake ports:

- Metadata refresh orchestration.
- Domain-event persistence.
- Digest aggregation.
- Disabled-user exclusion.
- Localization choice.
- Delivery result handling.
- Account deletion workflow.

### Integration tests

Use PostgreSQL through Testcontainers and stub external HTTP services. Cover:

- EF Core aggregate mappings.
- Invitation and magic-link lifecycle.
- Concurrent season completion evaluation.
- Outbox processing and idempotency.
- Unique constraints preventing duplicate digests.
- TVmaze response mapping.
- Email captured by Mailpit or fake SMTP server.
- Telegram webhook connection.
- Push subscription lifecycle.
- Admin authorization.
- Restart during digest delivery.

## 16. Operations

- Nightly encrypted PostgreSQL backup.
- At least seven daily backups copied off VPS.
- Periodic restore test.
- Structured JSON logs with correlation, job, and delivery IDs.
- External uptime check against `/health/ready`.
- Docker health checks.
- Disk-space, backup-failure, and repeated-job-failure alerts.

Health endpoints:

- `/health/live`
- `/health/ready`

Admin health screen includes:

- Last metadata refresh.
- Last pre-digest confirmation.
- Last digest preparation.
- Outbox backlog.
- Retry queue size and oldest retry.
- TVmaze connectivity.
- Email, Telegram, and push configuration.
- Database health.
- Application version.

## 17. CI/CD

GitHub Actions pipeline:

1. Restore .NET and frontend dependencies.
2. Build backend.
3. Run domain and application tests.
4. Run PostgreSQL integration tests.
5. Build and type-check Vue frontend.
6. Run frontend tests.
7. Build and scan container image.
8. Push versioned image to GitHub Container Registry.
9. Deploy through a restricted SSH account.
10. Apply reviewed database migrations.
11. Restart Docker Compose services.
12. Verify readiness endpoint.
13. Retain previous image tag for rollback.

## 18. Delivery Milestones

Detailed vertical slices live in [`stories/README.md`](stories/README.md). Story files are ordered by recommended delivery sequence; milestones below remain roadmap groupings, not implementation-sized stories.

### Milestone 0: Domain and provider discovery

- Validate TVmaze data against representative shows.
- Define ubiquitous language and context boundaries.
- Write completion policy examples and edge cases.
- Capture sanitized provider fixtures.
- Decide whether TVmaze alone is sufficient.

Exit criterion: team can explain and test when a season changes from airing to completed.

### Milestone 1: Foundation

- Create .NET projects and Vue application.
- Add project dependency rules.
- Configure PostgreSQL, EF Core, Docker Compose, Caddy, and OpenAPI.
- Add Mailpit and verify local invitation email.
- Add CI build and health endpoints.

Exit criterion: application deploys over HTTPS and local emails appear in Mailpit.

### Milestone 2: Identity and access

- Implement `User` and `Invitation` aggregates.
- Add invitation and magic-link use cases.
- Add secure cookie authentication and CSRF protection.
- Add roles, language preference, and account deletion.

Exit criterion: invite-only lifecycle works in English and Spanish.

### Milestone 3: Catalog and subscriptions

- Implement TVmaze anti-corruption layer.
- Add search and show import.
- Implement `ShowFollow` behavior.
- Display season history without retrospective notifications.

Exit criterion: users can find, follow, and unfollow shows reliably.

### Milestone 4: Season tracking domain

- Implement `TrackedSeason`, completion evidence, and completion policy.
- Add metadata refresh and pre-digest jobs.
- Add outbox processing.
- Add uncertain-metadata admin view.

Exit criterion: fixtures transition correctly and emit one completion event.

### Milestone 5: Email digests

- Implement notification preferences and `DigestDelivery` aggregate.
- Add digest preparation and retry workflow.
- Add English and Spanish templates.
- Implement Mailpit and production email adapters.
- Add delivery logs and development preview endpoint.

Exit criterion: one localized email digest is captured locally and production adapter passes sandbox verification.

### Milestone 6: Telegram

- Add bot deep-link connection.
- Verify webhook requests.
- Deliver localized Telegram digests.
- Handle disconnection and invalid chats.

Exit criterion: connected users receive one idempotent Telegram digest.

### Milestone 7: PWA and web push

- Add manifest and service worker.
- Add VAPID configuration.
- Support multiple devices.
- Deliver localized push notifications.
- Remove invalid endpoints.

Exit criterion: installed PWA receives push on supported desktop and mobile browsers.

### Milestone 8: Operational hardening

- Finish admin health and delivery tools.
- Automate backup and restore procedure.
- Add external monitoring and alerts.
- Perform security review and deployment rehearsal.

Exit criterion: recoverable VPS deployment with documented operations.

## 19. MVP Acceptance Criteria

- Only invited users can activate accounts.
- Multiple administrators are supported.
- Users can search for and follow TVmaze shows.
- Follows remain active for future seasons.
- Season zero and specials never trigger notifications.
- Stale schedules do not notify postponed finales.
- Date-only finales wait until the following day.
- Batch releases become eligible after release date ends.
- Daily digest runs at `09:00 UTC`.
- Multiple completions combine into one digest per channel.
- Email, Telegram, and web push support English and Spanish.
- Web push supports multiple devices.
- Delivery is idempotent, retryable, and auditable.
- Domain logic remains independent from EF Core and delivery providers.
- Domain events are stored reliably through a PostgreSQL outbox.
- Local email flows can be inspected at `http://localhost:8025` through Mailpit.
- Users can delete their accounts.
- Admins can inspect users, deliveries, metadata, and system health.
- PWA is installable and responsive.
- Domain, application, and integration tests cover critical behavior.
- Database backup can be restored successfully.
