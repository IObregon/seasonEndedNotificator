# Story 028: User Connects Telegram

**As a** user, **I want** to connect my Telegram chat through a bot link **so that** Telegram can become a delivery channel.

## Acceptance Criteria

- Authenticated user receives short-lived deep-link token.
- Verified webhook binds `/start` chat to that user once.
- Settings show connected state.
- User-supplied chat IDs are never trusted.

## Dependencies

- Story 007.

## Small Safe Steps

### Phase 1: Learn and Expand Secure State

### Step 1: Threat-model Telegram connection flow - 2 hours

**Type:** Learning

**Outcome:** Security note defines token entropy, expiry, one-time use, webhook verification, replay handling, and ownership rules.

**Verify:** Walk through forged chat ID, stolen/expired token, replayed `/start`, and duplicate webhook scenarios.

**Rollback:** Delete note; no production behavior changes.

### Step 2: Add connection-token and destination persistence - 3 hours

**Type:** Earning

**Outcome:** Additive schema stores hashed short-lived tokens and server-verified Telegram destination with uniqueness constraints.

**Verify:** Migration tests and repository tests cover expiry, one-time consumption, and conflicting destination ownership.

**Rollback:** Stop writes and retain additive tables; roll back only before production data exists.

### Phase 2: Expand Behind Disabled Webhook

### Step 3: Generate authenticated short-lived deep links - 2 hours

**Type:** Earning

**Outcome:** Authenticated user can create a bot deep link containing an opaque one-time token, never a trusted chat ID.

**Verify:** Tests prove user isolation, hash-only storage, expiry, and replacement invalidating prior unused token.

**Rollback:** Remove link action and expire outstanding tokens.

### Step 4: Add verified webhook endpoint disabled by default - 3 hours

**Type:** Earning

**Outcome:** Feature-flagged endpoint authenticates Telegram webhook requests and parses only supported `/start` payloads.

**Verify:** Integration tests reject missing/invalid secrets, malformed updates, user-supplied chat IDs, and disabled state.

**Rollback:** Disable flag and remove Telegram webhook registration.

### Step 5: Bind destination through atomic token consumption - 3 hours

**Type:** Earning

**Outcome:** Valid `/start` atomically consumes token once and binds webhook-provided chat identity to correct user.

**Verify:** Concurrent and replay tests prove one binding, no reassignment, and safe idempotent duplicate delivery.

**Rollback:** Disable webhook; revoke test bindings while retaining audit evidence.

### Phase 3: Migrate and Observe

### Step 6: Show connected state in settings - 2 hours

**Type:** Earning

**Outcome:** Settings displays server-derived connected state without exposing token or raw destination details.

**Verify:** UI tests cover disconnected, pending token, connected, expired token, and reload.

**Rollback:** Hide Telegram settings panel; persisted bindings remain intact.

### Step 7: Enable webhook for one staging bot - 1 hour

**Type:** Learning

**Outcome:** Staging evidence proves genuine deep-link connection, replay rejection, and connected-state refresh.

**Verify:** Connect once, replay same link, submit forged request, and confirm only genuine first request binds.

**Rollback:** Disable flag, unregister webhook, revoke staging destination, and expire token.

## Stop Conditions

- Stop if webhook authenticity cannot be verified, token appears in logs, replay changes ownership, or raw user-supplied chat ID is accepted.
- Do not register production webhook until staging forgery and replay checks pass.

## Completion Checklist

- [ ] Tokens are short-lived, opaque, hashed, and single use.
- [ ] Webhook is authenticated and disabled by default during rollout.
- [ ] Settings state comes only from server-verified binding.
