# Story 023: User Enables Email Notifications

**As a** user, **I want** to enable or disable email delivery **so that** I control where digests arrive.

## Acceptance Criteria

- Settings display current email preference.
- User can toggle email independently.
- Preference persists across sessions.
- Disabled email channel creates no new email delivery.

## Dependencies

- Story 007.

## Small Safe Steps

### Phase 1: Define and Expand

### Step 1: Characterize current email delivery eligibility - 1 hour

**Type:** Learning

**Outcome:** Executable examples document current recipient selection and safe default preference for existing users.

**Verify:** Run examples against representative enabled, disabled, and missing-preference users.

**Rollback:** Remove characterization artifacts; production behavior is unchanged.

### Step 2: Add persisted email preference with safe default - 2 hours

**Type:** Earning

**Outcome:** Additive user setting stores email preference while preserving existing delivery behavior for unset records.

**Verify:** Persistence tests cover create, read, update, and existing users without a stored value.

**Rollback:** Stop reading/writing the additive setting; leave storage in place.

### Phase 2: Deliver Control

### Step 3: Expose current preference and authenticated update - 2 hours

**Type:** Earning

**Outcome:** Settings query and command return and persist only the authenticated user's email preference.

**Verify:** Authorization and round-trip tests prove isolation and persistence across sessions.

**Rollback:** Remove update route and fall back to the safe default.

### Step 4: Add independent email toggle to settings - 2 hours

**Type:** Earning

**Outcome:** Settings UI displays and updates email state without affecting other channels.

**Verify:** UI tests cover initial state, successful toggle, failed save, and reload.

**Rollback:** Hide the toggle; persisted preference and prior UI remain valid.

### Phase 3: Migrate Delivery

### Step 5: Gate new email deliveries by preference - 2 hours

**Type:** Earning

**Outcome:** Recipient selection excludes explicitly disabled users before creating delivery records.

**Verify:** Integration tests prove disabled users create no delivery while enabled and safely defaulted users retain expected behavior.

**Rollback:** Revert the eligibility gate while retaining stored preferences for a later rollout.

### Step 6: Verify preference behavior in staging - 1 hour

**Type:** Learning

**Outcome:** Staging evidence shows enable, disable, session reload, and delivery-selection behavior end to end.

**Verify:** Toggle a test user both ways and inspect delivery records after each selection run.

**Rollback:** Restore the test user's original preference and remove test delivery data.

## Completion Checklist

- [ ] Preference persists and remains independent of other channels.
- [ ] Authenticated users can change only their own setting.
- [ ] Disabled users produce no new email delivery.
