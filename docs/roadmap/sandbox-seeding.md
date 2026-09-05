# Roadmap: Org Data Seeding — Not Applicable

Apex's `XFTY_Seeder` prototype (built on the Winter '27 `@IntegrationTest`
preview, on the `org-seeding` branch) leaves a generated graph behind in a
live Salesforce scratch org.

**This has no C# equivalent and is out of scope for this port entirely** —
there is no live, persistent environment to seed, and no analog of
`@IntegrationTest`'s "real DML, no rollback" semantics. See
[use/org-seeding](../use/org-seeding.md) and
[reference/known-issues](../reference/known-issues.md).

If a real persistence layer is ever wired up for this port (see
[roadmap/README.md](README.md)'s open question), seeding a long-lived
database would be a new feature designed against that backend — not a port of
this page.
