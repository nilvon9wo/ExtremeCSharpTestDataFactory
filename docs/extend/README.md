# Extending XFTY for your project

You are here to **teach XFTY about your project's record types** — write
Providers, register variants, add custom value expressions. (If you just want
to *use* XFTY to write tests, go to [../use/](../use/).)

| Page | Covers |
|------|--------|
| [providers](providers.md) | Implement `IRecordProvider` for a new record type — Master Template, primary target field, relationship design, testing. |
| [provider-lookups](provider-lookups.md) | Write your project's `IProviderLookup` over a `Dictionary` + `ProviderLookups`. |
| [provider-variants](provider-variants.md) | Register more than one Provider per type — `FlavouredLookupKey`, `IRecordPredicate`, a `*LookupKeys` constants class, resolution and specificity. |
| [custom-value-expressions](custom-value-expressions.md) | Implement `IValueExpression`, `IContextAwareExpression`, or `IDeferredExpression`. |
| [shared-ancestors-in-templates](shared-ancestors-in-templates.md) | Put a `SharedAncestor` in a *shipped* Master Template — and when not to. |
| [bundled-providers](bundled-providers.md) | The two shipped Providers + `DefaultProviderLookup` — copy-and-adjust, don't depend on. |

Working on XFTY's own engine instead? → [../contribute/](../contribute/).
