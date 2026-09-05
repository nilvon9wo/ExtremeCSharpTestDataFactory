# Seeding an Org — Not Ported

Apex's `XFTY_Seeder.seed(bundle)` leaves a generated graph behind in a live
Salesforce scratch org, using a Winter '27 developer preview
(`@IntegrationTest`) that runs real DML with no automatic rollback.

**None of that has a C# analog, and this feature is not ported.** There is no
"org" this port could seed — no persistence layer at all yet (see
[insert-modes](insert-modes.md)), and no equivalent of an `@IntegrationTest`
method that survives past the process that ran it. `seeding/` is out of scope
for this port entirely — see
[reference/known-issues.md](../reference/known-issues.md).

If a future persistence layer (e.g. an EF `DbContext` against a real database)
is ever wired up, seeding a long-lived environment would be a separate,
deliberate feature built against it — not a mechanical port of this page.

See also: [insert-modes](insert-modes.md)
