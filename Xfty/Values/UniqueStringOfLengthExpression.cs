namespace Net.Nowhereatall.Xfty.Values;

/// <summary>
/// An <see cref="IValueExpression"/> producing fixed-length, uppercase,
/// unique-within-one-process strings ("AAA", "AAB", ... for length 3) - a
/// base-26 counter over A-Z, counted separately per requested length.
/// </summary>
public sealed class UniqueStringOfLengthExpression : IValueExpression
{
    private const int AAsciiCode = 65;
    private const int AlphabetLength = 26;

    private static readonly Dictionary<int, int> LengthToCounter = [];

    private readonly int length;

    public UniqueStringOfLengthExpression(int length) => this.length = length;

    public object Get()
    {
        int counter = LengthToCounter.GetValueOrDefault(this.length);
        LengthToCounter[this.length] = counter + 1;
        return GenerateNextString(counter, this.length);
    }

    private static string GenerateNextString(int counter, int remainingLength) =>
        remainingLength == 0
            ? string.Empty
            : (char)(AAsciiCode + (counter % AlphabetLength))
              + GenerateNextString(counter / AlphabetLength, remainingLength - 1);
}
