# Seeding a Long-Lived Environment — Not in Scope

Leaving a generated graph behind in a shared, long-lived environment (a
scratch org, a staging database seeded once for manual QA) is a different job
from what this library does: generating and inserting data **for the
duration of one test run**, via `.SetInsertMode(InsertMode.Now)` and a
configured `IPersistenceGateway` (see [insert-modes](insert-modes.md)).

**Seeding a persistent environment is out of scope for this library
entirely** - see [reference/known-issues.md](../reference/known-issues.md).
It would be a separate, deliberate feature (its own tool, its own rollback/
cleanup story) built on top of the same generation engine, not something this
page's `Now` mode does today or ever aims to.

See also: [insert-modes](insert-modes.md)
