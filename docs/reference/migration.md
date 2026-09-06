# Migration

This is a fresh C# port (`Net.NowhereAtAll.Xfty`) — there is no prior release
of it to migrate *from*. Apex's migration guide here covered breaking changes
between two Apex releases (3.5 → 4.0); none of that history applies to a
codebase that started from the 4.0-era Apex source as its one-time reference
point.

If you are coming from the **Apex original** rather than an earlier version of
this port, you are not migrating so much as reading a different API entirely —
see [salesforce-considerations](salesforce-considerations.md) for what carries
over and what doesn't, and [known-issues](known-issues.md) for the full list of
capability gaps (record types, org seeding, a real `Now` persistence layer, and
more).

Once this port has its own tagged releases, breaking changes between *those*
will be documented here.
