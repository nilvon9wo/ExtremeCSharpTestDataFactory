# The Bundled Providers

XFTY ships two Providers — `Account`, `Contact` — wired together by
`DefaultProviderLookup`. They are deliberately generic starting points. **Copy
them into your project and adjust** rather than depending on their exact
defaults.

| Provider | Class |
|----------|-------|
| Account | `AccountDataProvider` |
| Contact | `ContactDataProvider` |
| Lookup wiring them | `DefaultProviderLookup` |

All three live in `Net.NowhereAtAll.Xfty.Demo`.

`DefaultProviderLookup` is also the copy-me example for
[writing your own lookup](provider-lookups.md) — it is the exact map-plus-utility
pattern with this port's two Providers, and the framework uses it for its own
self-tests.

> A bundled Provider exposing ready-made test-user helpers (an admin-
> equivalent user, role/profile lookups) isn't provided here - there's no
> role/profile-style schema for it to resolve against. See
> [use/test-user-helpers](../use/test-user-helpers.md) and
> [reference/known-issues.md](../reference/known-issues.md). This port's
> demo `User` (`Id`, `FirstName`, `LastName`, `Email`, `ManagerId`) exists only
> to exercise deep/hierarchical relationship paths in tests, and has no bundled
> Provider of its own — register your own `IRecordProvider` for it if your
> tests need one.

---

## Why copy, not depend

- Your project's required fields and validation logic differ from these
  generic defaults — a copied Provider is where that knowledge lives.
- Depending on the shipped defaults couples your tests to XFTY's release notes.

See [provider-variants](provider-variants.md) for registering a second Provider
for one type.
