using System.Text.RegularExpressions;

namespace Dma.OrderIntake.Domain;

// ISO 10383 Market Identifier Code: exactly 4 uppercase letters (e.g. XAMS,
// XLON). Normalization is trim + uppercase — real MIC lookups are case-
// insensitive in practice.
public readonly struct Mic
{
    private static readonly Regex Format = new("^[A-Z]{4}$");

    public string Value { get; }

    private Mic(string value)
    {
        Value = value;
    }

    public static bool TryParse(string? input, out Mic mic, out string? error)
    {
        mic = default;

        if (string.IsNullOrWhiteSpace(input))
        {
            error = "MIC is required.";
            return false;
        }

        var candidate = input.Trim().ToUpperInvariant();

        if (!Format.IsMatch(candidate))
        {
            error = "MIC must be exactly 4 letters (ISO 10383), e.g. XAMS.";
            return false;
        }

        mic = new Mic(candidate);
        error = null;
        return true;
    }

    public override string ToString() => Value;
}
