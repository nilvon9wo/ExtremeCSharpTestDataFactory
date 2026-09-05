# Open Questions

Apex's one open question — "does XFTY commit to a deployable, non-`@IsTest`
distribution?" (blocking org seeding and an AppExchange listing) — **does not
carry over.** C# has no `@IsTest`-style annotation that keeps compiled code out
of a normal build; `Xfty.csproj` produces ordinary code from the start. There
is nothing to decide here.

The real open question for this port is architectural, not a distribution
policy: **what does a real persistence layer look like, and when does it get
built?** That single piece of work would unblock `InsertMode.Now`,
`DeferredInserter.Flush()`, `.DepthBatched()`'s only functional mode, and
(eventually) something like org seeding. It is not designed yet — see
[roadmap/README.md](README.md) and [csharp-port-idea.md](../../csharp-port-idea.md)
at the repo root.
