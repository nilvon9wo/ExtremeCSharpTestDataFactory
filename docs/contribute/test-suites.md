# Test Organization

`Xfty.Test/` mirrors `Xfty/`'s folder structure one-for-one — `Xfty/Core/Bundle.cs`
→ `Xfty.Test/Core/BundleTest.cs` — rather than Apex's `ApexTestSuite` grouping.
xUnit's own filter syntax (`dotnet test --filter "..."`) covers what suites
covered in Apex: run everything, run one class, run one namespace.

| Folder | Contents |
|--------|----------|
| `Core/` | `RecordProvider`, `Bundle`, `MasterTemplate`, `GenerationContext`, `ChildProvider`, and the rest of the public surface. |
| `Engine/` | The generation pipeline's phase classes — ancestor generation, the value passes, the cycle guard. |
| `Enrichment/` | `Inject`/`InjectAll`, `InjectConfig`, `SObjectInjector`, and their supporting pieces. |
| `Relationships/` | `DefaultRelationship`, `SharedAncestor`, `SharedAncestorHierarchy`. |
| `Lookup/` | `LookupKey`, `FlavouredLookupKey`, variant resolution. |
| `Persistence/` | `IdMocker`, `DepthBatchedInserter`, `DeferredInserter`, `DeferredInsertBuffer`. |
| `Predicates/` | The `IRecordPredicate` implementations and factories. |
| `Values/` | The bundled `IValueExpression`/`IContextAwareExpression`/`IDeferredExpression` implementations. |
| `Demo/` | Tests for this port's own bundled `AccountDataProvider`/`ContactDataProvider`/`DefaultProviderLookup`. |
| `PerformanceTest.cs` (top level) | Volume/wall-clock tests — see below. |

```bash
dotnet test Xfty.slnx --filter "Category!=Performance"                        # everything except performance
dotnet test Xfty.slnx --filter "FullyQualifiedName~Xfty.Test.Relationships"   # one namespace
```

---

## The `Performance` trait

`PerformanceTest.cs` is tagged `[Trait("Category", "Performance")]` and run as
a **separate, `continue-on-error` CI step** — the same role Apex's `XFTY_Load`
suite played running only in a scheduled workflow rather than on every push.
It measures wall-clock time (`Stopwatch`) and rough allocation
(`GC.GetTotalMemory`) against deliberately generous ceilings, since there are
no governor limits to push toward in this port — see
[reference/volume-and-limits](../reference/volume-and-limits.md).

---

## Keep test classes single-purpose

A class that mixes fundamentally different scenarios is split — e.g.
`RecordFactoryTest` (the no-persistence-layer matrix: inclusivity, insert
modes, `IncludeOptional`) is distinct from `RecordProviderScenarioTest`
(end-to-end "does the whole flow work" cases) and `RecordProviderApiTest` (one
test per fluent-API affordance). Each test class lives in the folder that
mirrors the class it exercises.
