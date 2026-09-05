# Roadmap: Enrichment

**Built.** Shipped as `bundle.Inject(field, config)` / `InjectAll(field)` plus
the standalone `RecordInjector`. Simpler than the Apex original — no JSON
`serialize`/`deserialize` round-trip, since reflection sets any property
directly; see [use/enrichment.md](../use/enrichment.md)'s explanation of what
that eliminates (Blob-carrying, compound-field maps, polymorphic-relationship
name resolution).

See **[docs/use/enrichment.md](../use/enrichment.md)**.
