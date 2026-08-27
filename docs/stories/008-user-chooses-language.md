# Story 008: User Chooses Interface Language

**As a** user, **I want** to choose English or Spanish **so that** product text matches my preference.

## Acceptance Criteria

- Initial preference can be selected after activation.
- User can switch language later.
- Preference persists across sessions.
- Authentication screens and email templates use selected language where known.

## Dependencies

- Story 007.

## Small Safe Steps

### Phase 1: Define Locale Coverage

#### Inventory User-Facing Authentication Text - 2 hours

**Type:** Learning

**Outcome:** A checked-in inventory lists authentication screens, email templates, fallback locale, and every English/Spanish key needed by this story.

**Verify:** Review each current auth screen and template against the inventory; no raw user-facing string is unclassified.

**Rollback:** Remove the inventory; runtime behavior does not change.

#### Add Translation Catalogs with English Fallback - 3 hours

**Type:** Earning

**Outcome:** Authentication UI and email rendering can resolve English and Spanish keys, defaulting safely to English.

**Verify:** Catalog tests require matching keys and render both locales plus an unsupported-locale fallback.

**Rollback:** Restore literal English content and remove locale catalog registration.

### Phase 2: Persist and Apply Preference

#### Add Nullable Language Preference - 2 hours

**Type:** Earning

**Outcome:** An additive user preference stores `en` or `es`; null preserves current English behavior.

**Verify:** Apply and roll back the migration on a disposable database and test allowed-value validation.

**Rollback:** Stop reading the column, then roll back it after confirming no required preference data remains.

#### Add Authenticated Language Selector - 3 hours

**Type:** Earning

**Outcome:** Activated users can select and later switch language, with the choice surviving a new session.

**Verify:** Select each locale, sign out and in, and assert persisted preference and rendered UI match.

**Rollback:** Hide the selector and fall back to English while retaining the harmless preference value.

### Phase 3: Localize Known-User Communication

#### Render Authentication Email in Stored Language - 2 hours

**Type:** Earning

**Outcome:** Email templates use the known user's preference and English when no preference is known.

**Verify:** Capture English, Spanish, and unknown-user-safe flows in Mailpit and compare subject and both MIME bodies.

**Rollback:** Restore English-only templates without altering stored preferences.

## Stop Conditions

- Stop if missing translations can expose keys or break authentication.
- Stop if locale selection changes authorization or account-disclosure behavior.

## Completion Checklist

- [ ] User can select English or Spanish after activation.
- [ ] Preference survives later sessions and switches.
- [ ] Authentication screens use selected language.
- [ ] Known-user emails use preference with English fallback.
