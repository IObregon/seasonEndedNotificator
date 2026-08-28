# Story 030: User Disconnects Telegram

**As a** user, **I want** to disconnect Telegram **so that** future Telegram messages stop.

## Acceptance Criteria

- User can revoke current connection.
- Telegram preference becomes disabled.
- Existing history remains auditable without active destination.
- Permanent invalid-chat response also revokes destination.

## Dependencies

- Story 029.

## Small Safe Steps

### Phase 1: Learn Revocation Boundaries

### Step 1: Define disconnect and in-flight delivery races - 2 hours

**Type:** Learning

**Outcome:** Executable scenarios define manual revocation, preference disablement, audit retention, invalid-chat response, and concurrent send behavior.

**Verify:** Review scenarios for disconnect before reservation, after reservation, during provider call, and repeated revocation.

**Rollback:** Remove scenarios; runtime behavior is unchanged.

### Step 2: Add atomic destination revocation operation - 3 hours

**Type:** Earning

**Outcome:** Domain operation deactivates current destination and disables Telegram preference while retaining delivery history.

**Verify:** Transaction tests prove both state changes commit together, repeated calls are idempotent, and history remains queryable.

**Rollback:** Remove operation before exposing callers; existing connection remains unchanged.

### Phase 2: Deliver User Control

### Step 3: Add authenticated disconnect command - 2 hours

**Type:** Earning

**Outcome:** Authenticated user can revoke only their current Telegram destination through settings API.

**Verify:** Authorization tests cover own destination, another user's destination, already disconnected state, and post-revocation eligibility.

**Rollback:** Remove command route; no automated revocation path depends on it.

### Step 4: Add disconnect control and confirmation state - 2 hours

**Type:** Earning

**Outcome:** Connected users can confirm disconnect and immediately see disconnected/disabled state.

**Verify:** UI tests cover cancel, success, failed request, repeated click, and reload.

**Rollback:** Hide control; API and persisted state remain valid.

### Phase 3: Expand Provider-Driven Revocation

### Step 5: Classify permanent invalid-chat responses - 1 hour

**Type:** Learning

**Outcome:** Provider response table distinguishes permanent invalid destination from transient failures without broad revocation.

**Verify:** Adapter tests map documented blocked/not-found responses and reject timeout, rate-limit, and server errors.

**Rollback:** Remove classification; manual disconnect remains available.

### Step 6: Auto-revoke invalid chats behind disabled flag - 2 hours

**Type:** Earning

**Outcome:** Disabled-by-default handler invokes same atomic revocation operation only for permanent invalid-chat responses.

**Verify:** Integration tests prove permanent response revokes once, transient response does not revoke, and history remains auditable.

**Rollback:** Disable provider-revocation flag; manual disconnect remains operational.

### Step 7: Enable provider revocation after staging verification - 1 hour

**Type:** Earning

**Outcome:** Confirmed permanent invalid destinations automatically stop future Telegram selection.

**Verify:** Inject staging permanent/transient responses, then monitor initial production classifications and revocation count.

**Rollback:** Disable provider-revocation flag without restoring invalid destinations automatically.

## Stop Conditions

- Stop if transient responses revoke destinations, disconnect leaves preference enabled, or audit history is deleted.
- Do not enable provider-driven revocation until exact response classification is covered by adapter tests.

## Completion Checklist

- [x] Manual disconnect atomically revokes destination and disables preference.
- [x] Existing history remains auditable.
- [x] Only permanent invalid-chat responses trigger automatic revocation.

## Result

Completed on `2026-08-28`. `DisconnectTelegramCommand` removes `TelegramDestination` and sets `TelegramNotificationsEnabled = false` atomically. Delivery history remains auditable. `DELETE /api/telegram/connection` requires authentication, revokes only own destination. Idempotent for already-disconnected users.
