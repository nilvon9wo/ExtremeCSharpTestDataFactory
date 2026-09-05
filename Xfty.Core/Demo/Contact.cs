namespace Net.Nowhereatall.Xfty.Core.Demo;

/// <summary>
/// A minimal stand-in for the Salesforce standard Contact object - the other
/// half of the Contact/Account demo pair (see <see cref="Account"/>).
///
/// Declared as a positional <c>record class</c> rather than a plain class: its
/// properties are compiler-generated <c>init</c>-only, and it gets value
/// equality/<c>ToString</c> for free. Reflection-based field access
/// (<see cref="Field"/>, the predicates) reads a record's generated
/// properties exactly like any other property - nothing about this type needs
/// special-casing anywhere else in the library.
/// </summary>
public sealed record class Contact(string? FirstName, string? LastName, string? Email, string? AccountName);
