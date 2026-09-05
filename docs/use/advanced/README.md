# Advanced — combining features

Each page here is a scenario that uses several XFTY features together. Read the
individual feature pages in [../](../) first.

| Page | Combines |
|------|----------|
| [unit-vs-integration](unit-vs-integration.md) | one set of Providers, `Mock` ↔ `Now` (design intent — `Now` is not usable yet in this port) |
| [large-graphs](large-graphs.md) | inclusivity + `PreventCascade` to keep generation cheap |
| [deep-setup-chains](deep-setup-chains.md) | xUnit's per-test-method instance as the `@TestSetup` replacement, and `Deferred` across helper methods |
| [matching-values](matching-values.md) | context-aware values + shared ancestors to keep a validation-rule field pair in sync |
