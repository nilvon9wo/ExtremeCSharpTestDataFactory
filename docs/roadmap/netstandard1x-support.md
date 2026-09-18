# Design: A netstandard1.x Floor for Core `Xfty`

Status: **idea, not committed to.** Scoped and measured (call sites counted,
both gaps confirmed by actually compiling against netstandard1.3/1.6), but
not started - see [Worth it?](#worth-it) for why.

---

## The gap

Core `Xfty` multi-targets `netstandard2.0;net8.0;net10.0` today - see
[roadmap/README.md](README.md#built)'s multi-targeting row. `netstandard2.0`
is already a low floor (.NET Framework 4.6.1+, current Mono/Xamarin, older
.NET Core), but the netstandard ladder goes lower still: `netstandard1.0`
through `1.6`, each a smaller BCL surface, aimed at platforms like Windows
Phone 8.1 and pre-Fall-Creators-Update UWP. Nothing in this solution targets
that range today.

## Why netstandard2.0 is the floor today

Checked directly (a throwaway probe project, not assumed) rather than
inferred from the version number alone:

- **`dotnet build` itself discourages it.** Targeting anything below
  netstandard2.0 emits `NETSDK1215: Targeting .NET Standard prior to 2.0 is
  no longer recommended` from the SDK.
- **Reflection discovery moved.** `Type.GetProperties()`, `GetConstructor()`,
  `GetMethod()`, and the `Type`-level boolean flags (`IsValueType`,
  `IsGenericType`, …) don't exist directly on `Type` below netstandard2.0 -
  they live on `TypeInfo` instead (`type.GetTypeInfo().DeclaredProperties`,
  etc.). Confirmed by compiling against netstandard1.3/1.6 directly: those
  calls fail with `CS1061`, not a warning.
- **The uninitialized-object fallback has no equivalent at all.**
  `System.Runtime.Serialization` (home of `FormatterServices.GetUninitializedObject`,
  the fallback `BlankInstances.Uninitialized` uses today for a type with no
  parameterless constructor) doesn't exist below netstandard2.0 - confirmed
  the same way, `CS0234`. This isn't a differently-shaped API to bridge to,
  the capability itself is absent from the contract.

## What a real implementation would need

**The `TypeInfo` redirect** - smaller than it sounds. Every `Type`-level
reflection call in core `Xfty` is already enumerated (grep, not estimated):

```
Core/InverseAlignment.cs, Engine/RecordCloneFactory.cs, Engine/RecordFactory.cs,
Enrichment/InjectionPathResolver.cs, Field.cs, Internal/BlankInstances.cs,
Persistence/DepthBatchedInserter.cs, Persistence/PersistenceGatewayExtensions.cs,
Relationships/SharedAncestor.cs, Relationships/SharedAncestor.Resolution.cs
```

9 files, ~16 call sites total. The actual per-field work - `PropertyInfo.GetValue`/
`SetValue`, `ConstructorInfo.Invoke`, `ConstructorInfo.GetParameters()` - needs
*no* changes at all (verified by compiling that surface directly against
netstandard1.3/1.6; it's unchanged from netstandard2.0). The fix is the same
pattern already used for `GetValueOrDefault`/`ToHashSet` in
`Internal/CollectionCompatExtensions.cs`: `#if`-guarded extension methods with
the *same names* (`GetProperties`, `GetConstructor`, …) delegating to
`GetTypeInfo()`, so none of the 16 call sites themselves change.

**The uninitialized-object fallback** - not a polyfill, a different
algorithm. `BlankInstances.Uninitialized` would need to reflectively find the
type's own constructor (`TypeInfo.DeclaredConstructors`) and invoke it with a
default value per parameter, instead of bypassing construction entirely -
workable, since every field gets overwritten by XFTY's own reflection pass
immediately after either way (see that class's own docstring). But it's a
genuine behaviour difference on this one TFM, not a transparent shim: it
*runs* the constructor body, where the current approach runs none of it. Safe
for a typical `record class Foo(string X, int Y)` with a trivial positional
constructor; not equivalent for a type whose constructor validates its
arguments (throws on null, say) - that case would need its own decision
(accept the throw on netstandard1.x, or document the limitation) and its own
test, not a copy-paste of the netstandard2.0 branch's behavior.

Rough total: **a day, maybe two**, including tests for both.

## Worth it?

Not against known or even suspected demand. Two things point the same
direction, not just one:

- **The remaining audience is close to zero.** netstandard1.x's own target
  platforms - Windows Phone 8.1, early UWP - are discontinued. This is a
  different situation from `Xfty.EntityFramework6`'s `net461` floor, where a
  large, currently-active population of real .NET Framework projects was the
  whole reason for doing that work in the first place - see
  [roadmap/README.md](README.md#built)'s classic-EF6 row.
- **No one has asked.** Unlike the EF6 case, there's no consumer, issue, or
  even a hypothetical scenario prompting this - it surfaced from a "how far
  could we go" conversation, not a need.

If that changes - a real project, a real platform, a real reason - the scope
above is the starting point: it's already measured, not just estimated.

See also: [roadmap/README.md](README.md).
