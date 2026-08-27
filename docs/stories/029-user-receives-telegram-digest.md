# Story 029: User Receives a Telegram Digest

**As a** connected user, **I want** one localized Telegram digest **so that** I can learn about ended seasons in Telegram.

## Acceptance Criteria

- Telegram preference can be enabled only when connected.
- Existing digest eligibility and grouping rules are reused.
- Delivery status and Telegram message ID are recorded.
- Duplicate processing does not duplicate message.

## Dependencies

- Stories 025 and 028.

## Small Safe Steps

### Phase 1: Learn and Expand Channel State

### Step 1: Specify Telegram eligibility and message constraints - 2 hours

**Type:** Learning

**Outcome:** Executable examples define connected/enabled eligibility, shared digest grouping, locale, length limits, links, and duplicate processing.

**Verify:** Review examples for disconnected users, disabled preference, multiple items, and repeated runs.

**Rollback:** Remove examples; runtime behavior is unchanged.

### Step 2: Add Telegram preference and delivery identity - 3 hours

**Type:** Earning

**Outcome:** Additive persistence stores preference, unique Telegram delivery identity, status, and provider message ID.

**Verify:** Migration and repository tests cover existing users, unique conflicts, status transitions, and message ID storage.

**Rollback:** Stop writes and retain additive records; default channel state remains disabled.

### Phase 2: Build Without Production Sends

### Step 3: Reuse digest eligibility for Telegram recipients - 2 hours

**Type:** Earning

**Outcome:** Channel selection reuses existing digest items and includes only connected users with Telegram enabled.

**Verify:** Integration tests compare email and Telegram item grouping while varying connection and preference states.

**Rollback:** Remove Telegram channel selection; email behavior remains unchanged.

### Step 4: Build localized Telegram digest text - 2 hours

**Type:** Earning

**Outcome:** English and Spanish builders produce one bounded message with required season information and internal links.

**Verify:** Approved outputs cover both locales, escaping, one/many items, and configured length boundary.

**Rollback:** Remove Telegram builder; selection artifacts remain unused.

### Step 5: Add idempotent Telegram sender disabled by default - 3 hours

**Type:** Earning

**Outcome:** Feature-flagged sender reserves unique delivery, sends once, and records status plus Telegram message ID.

**Verify:** Provider-stub tests cover flag-off, success, failure, restart, and concurrent duplicate processing.

**Rollback:** Disable sender flag; reserved records remain auditable and email delivery is unaffected.

### Phase 3: Migrate and Observe

### Step 6: Enable one staging Telegram recipient - 1 hour

**Type:** Learning

**Outcome:** Staging artifact proves localized grouped delivery and persisted provider identity for one connected user.

**Verify:** Process same digest twice and confirm one Telegram message and one delivery record.

**Rollback:** Disable sender, disable test preference, and remove isolated staging fixture.

### Step 7: Enable production sender with kill switch - 1 hour

**Type:** Earning

**Outcome:** Eligible connected users receive one localized Telegram digest while existing email flow remains unchanged.

**Verify:** Monitor initial sends for eligibility, message IDs, failure rate, and duplicate count.

**Rollback:** Disable Telegram sender flag; preserve connection, preference, and delivery history.

## Stop Conditions

- Stop if disconnected/disabled users are selected, provider limits are exceeded, or repeated processing sends duplicate messages.
- Do not enable production until staging duplicate-processing check passes.

## Completion Checklist

- [ ] Telegram eligibility reuses existing digest grouping rules.
- [ ] Localized message and provider message ID are verified.
- [ ] Unique delivery identity prevents duplicate messages.
