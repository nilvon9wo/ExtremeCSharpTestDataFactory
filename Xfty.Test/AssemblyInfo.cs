using Xunit;

// SharedAncestor / SharedAncestorResolver keep flyweight registries as static
// state - deliberately (see SharedAncestor's doc comment): a test method is
// assumed to run alone, with the registry reset at the start of the next one,
// so the design never had to be thread-safe. xUnit's default of running test
// classes in parallel breaks that assumption and produces cross-test races
// (one test's in-flight resolution tripping another test's cycle detection).
// Serializing the run restores the single-test-at-a-time semantics the design
// assumes, rather than bolting locking onto a registry that was never meant
// to be thread-safe.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
