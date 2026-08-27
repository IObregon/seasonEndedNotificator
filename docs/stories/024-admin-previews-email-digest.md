# Story 024: Administrator Previews an Email Digest

**As an** administrator in development, **I want** to generate a sample digest **so that** localized email rendering can be inspected in Mailpit.

## Acceptance Criteria

- Development-only admin action creates preview for English or Spanish.
- Mailpit shows HTML and plain-text versions.
- Preview is clearly marked and creates no production delivery record.
- Endpoint is absent outside Development.

## Dependencies

- Stories 003, 008, and 023.

## Small Safe Steps

### Phase 1: Learn and Build Preview Data

### Step 1: Capture localized digest rendering examples - 2 hours

**Type:** Learning

**Outcome:** Approved English and Spanish HTML/plain-text examples define preview content, links, and preview marking.

**Verify:** Render fixed sample data and review both locale outputs against email requirements.

**Rollback:** Remove examples; application behavior is unchanged.

### Step 2: Add non-persisting preview message builder - 2 hours

**Type:** Earning

**Outcome:** A preview builder produces marked HTML and plain-text messages without creating production delivery records.

**Verify:** Unit tests compare both locales and assert no delivery repository write occurs.

**Rollback:** Remove the builder; production digest rendering remains untouched.

### Phase 2: Expose Only in Development

### Step 3: Add development-only preview action - 2 hours

**Type:** Earning

**Outcome:** Authenticated admin action sends selected-locale preview to configured development mail transport.

**Verify:** Integration test confirms admin success, non-admin rejection, and zero production delivery records.

**Rollback:** Remove route registration; builder can remain unused.

### Step 4: Prove endpoint absence outside Development - 1 hour

**Type:** Earning

**Outcome:** Environment-level test prevents preview route registration in staging or production.

**Verify:** Start application under Development and Production and assert route exists only in Development.

**Rollback:** Remove route entirely if environment isolation cannot be guaranteed.

### Phase 3: Verify Mailpit Output

### Step 5: Send both localized previews to Mailpit - 1 hour

**Type:** Learning

**Outcome:** Verifiable artifact records Mailpit HTML/plain-text inspection for English and Spanish.

**Verify:** Confirm preview label, links, locale text, and both MIME alternatives in Mailpit.

**Rollback:** Delete preview messages from Mailpit; no production state changed.

## Stop Conditions

- Stop if route is discoverable outside Development or preview writes any production delivery/audit record.

## Completion Checklist

- [ ] English and Spanish previews include HTML and plain text.
- [ ] Preview is marked and creates no production delivery record.
- [ ] Endpoint is absent outside Development.
