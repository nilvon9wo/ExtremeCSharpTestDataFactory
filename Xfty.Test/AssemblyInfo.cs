using Xunit;

// SharedAncestor / SharedAncestorResolver keep flyweight registries as static
// state - deliberately, since Apex resets statics between test methods and
// this is the closest C# equivalent (see SharedAncestor's doc comment). Apex
// test methods also never run concurrently with each other, so that design
// never had to be thread-safe. xUnit's default of running test classes in
// parallel breaks that assumption and produces cross-test races (one test's
// in-flight resolution tripping another test's cycle detection). Serializing
// the run restores the single-threaded-per-org semantics the design assumes,
// rather than bolting locking onto a registry Apex never needed to make
// thread-safe.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
