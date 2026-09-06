# Xfty.Xunit

One xUnit attribute for a [`Xfty`](https://www.nuget.org/packages/Xfty)
consumer's own test suite: `[IsolatesSharedAncestor]`, resetting
`SharedAncestor`'s registry before and after a test.

```bash
dotnet add package Xfty.Xunit
```

## Usage

```csharp
using Net.Nowhereatall.Xfty.Xunit;

[IsolatesSharedAncestor]
public class MyTests
{
    [Fact]
    public void FirstTest()
    {
        SharedAncestor.Put("hq", new Account());
        // ... nothing here leaks into FirstTest, or out to any later test
    }
}
```

Apply it to a whole test class (every method in it) or to one `[Fact]`/
`[Theory]` method directly. Resetting *before* protects this test from
whatever an earlier, non-isolated test left behind; resetting *after*
protects every later test from this one. `SharedAncestor`'s registry is
process-static and does **not** reset between xUnit test methods on its
own the way Apex's statics reset between test methods - the single biggest
"static state" surprise for anyone coming from a per-test-reset runner, and
the reason this package exists.

## Full documentation

- [Shared ancestors](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/blob/master/docs/use/shared-ancestors.md) - what `SharedAncestor` is for, `[IsolatesSharedAncestor]` covered directly
- [Static-state lifetime](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory/blob/master/docs/reference/salesforce-considerations.md) - why this matters at all in a shared xUnit process
- [Everything else `Xfty` does](https://github.com/nilvon9wo/ExtremeCSharpTestDataFactory#readme)
