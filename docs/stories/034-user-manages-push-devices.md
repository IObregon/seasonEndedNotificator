# Story 034: User Manages Multiple Push Devices

**As a** user, **I want** to view and revoke push devices **so that** only browsers I control receive notifications.

## Acceptance Criteria

- Settings list active devices with label and last success.
- Additional browser can register independently.
- Revoking one device leaves others active.
- Same digest can deliver once to each active endpoint under one channel delivery policy.

## Dependencies

- Story 033.

## Small Safe Steps

### Phase 1: Establish Device Semantics

### Step 1: Define Device Labels and Activity Rules - 1 hour

**Type:** Learning

**Outcome:** Decision note defines safe labels, active state, last-success meaning, and per-endpoint delivery uniqueness.

**Verify:** Map rules to existing subscription and delivery records using representative multi-device examples.

**Rollback:** Remove note; runtime remains unchanged.

### Step 2: Add Non-Sensitive Device Metadata - 2 hours

**Type:** Earning

**Outcome:** Additive fields store user-editable label and last-success timestamp without exposing endpoint or keys.

**Verify:** Apply migration to populated test database and confirm old records remain valid with defaults.

**Rollback:** Stop metadata writes and remove additive fields after callers are disabled.

### Phase 2: Deliver Independent Management

### Step 3: Add Current-User Device List API - 2 hours

**Type:** Earning

**Outcome:** Authenticated API returns only current user's active devices with sanitized label and last success.

**Verify:** Test two users and confirm neither endpoint URLs nor cross-user devices appear.

**Rollback:** Disable route; registration and delivery continue unchanged.

### Step 4: Add Device Settings List - 2 hours

**Type:** Earning

**Outcome:** Settings page lists active devices and supports clear empty, loading, and error states.

**Verify:** Render zero, one, and multiple devices on desktop and mobile viewport.

**Rollback:** Hide settings section while API remains harmless and unused.

### Step 5: Revoke One Device by Stable Identifier - 2 hours

**Type:** Earning

**Outcome:** User can idempotently revoke one owned device without changing sibling subscriptions.

**Verify:** Revoke one of two devices twice and confirm other device stays active and deliverable.

**Rollback:** Hide action and restore mistakenly revoked record from audit data.

### Phase 3: Verify Per-Device Delivery

### Step 6: Prove One Delivery per Active Endpoint - 3 hours

**Type:** Earning

**Outcome:** Digest creates one uniquely keyed delivery per active endpoint under channel policy.

**Verify:** Run digest for two active and one revoked device; confirm exactly two sends and safe replay.

**Rollback:** Disable multi-device fan-out flag and return to current-device behavior.

## Stop Conditions

- Stop rollout if identifiers permit cross-user access or revoking one device changes another endpoint.

## Completion Checklist

- [ ] Device data is sanitized and scoped to authenticated user.
- [ ] Independent registration, listing, and revocation pass multi-device tests.
- [ ] Digest replay cannot duplicate per-endpoint delivery.
