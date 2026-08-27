# Story Map

Each linked file is one deployable, testable slice intended to take less than 2-3 days. Delivery order favors early feedback: validate TVmaze risk, ship a running skeleton, prove local email, then complete one email-notification path before adding automation and more channels.

## Discovery and foundation

1. [Validate TVmaze finale data](001-validate-tvmaze-finale-data.md)
2. [Select a finale authority](001a-select-finale-authority.md)
3. [Deploy a health-checking application](002-deploy-health-checking-application.md)
4. [Capture a local email](003-capture-local-email.md)

Story 001 result: TVmaze accepted for catalog/schedules and rejected as sole finale authority. Story 001a now blocks Story 017.

## Identity and access

4. [Bootstrap the first administrator](004-bootstrap-first-administrator.md)
5. [Administrator invites a user](005-admin-invites-user.md)
6. [Invitee activates an account](006-invitee-activates-account.md)
7. [User signs in by magic link](007-user-signs-in-by-magic-link.md)
8. [User chooses interface language](008-user-chooses-language.md)
9. [Administrator disables a user](009-admin-disables-user.md)
10. [Administrator grants admin role](010-admin-grants-role.md)
11. [User deletes own account](011-user-deletes-account.md)

## Catalog and follows

12. [User searches for a show](012-user-searches-shows.md)
13. [User views show details](013-user-views-show-details.md)
14. [User follows a show](014-user-follows-show.md)
15. [User unfollows a show](015-user-unfollows-show.md)
16. [User sees an already-ended season](016-user-sees-ended-season.md)

## Season completion

17. [Confirm a timestamped finale](017-confirm-timestamped-finale.md)
18. [Confirm a date-only finale](018-confirm-date-only-finale.md)
19. [Confirm a batch-release season](019-confirm-batch-release.md)
20. [Withhold an uncertain finale](020-withhold-uncertain-finale.md)
21. [Honor a postponed finale](021-honor-postponed-finale.md)
22. [Refresh followed-show metadata daily](022-refresh-metadata-daily.md)

## Email notification path

23. [User enables email notifications](023-user-enables-email.md)
24. [Administrator previews an email digest](024-admin-previews-email-digest.md)
25. [User receives a manual email digest](025-user-receives-manual-email-digest.md)
26. [User receives the scheduled email digest](026-user-receives-scheduled-email-digest.md)
27. [System retries a transient email failure](027-retry-transient-email-failure.md)

## Telegram

28. [User connects Telegram](028-user-connects-telegram.md)
29. [User receives a Telegram digest](029-user-receives-telegram-digest.md)
30. [User disconnects Telegram](030-user-disconnects-telegram.md)

## PWA and web push

31. [User installs the PWA](031-user-installs-pwa.md)
32. [User registers one push device](032-user-registers-push-device.md)
33. [User receives a web push digest](033-user-receives-push-digest.md)
34. [User manages multiple push devices](034-user-manages-push-devices.md)

## Administration and operations

35. [Administrator inspects delivery failures](035-admin-inspects-delivery-failures.md)
36. [Administrator refreshes uncertain metadata](036-admin-refreshes-metadata.md)
37. [Administrator checks system health](037-admin-checks-system-health.md)
38. [Operator restores a database backup](038-operator-restores-backup.md)
39. [CI deploys a verified release](039-ci-deploys-release.md)

## Smallest valuable release

Stories 1-3 produce fastest learning. Stories 4-7, 12-14, 17, 23, and 25 form first end-to-end user path: authenticate, follow a show, confirm its finale, and receive an email digest manually. Automation, edge cases, Telegram, push, and richer administration follow only after that path works.
