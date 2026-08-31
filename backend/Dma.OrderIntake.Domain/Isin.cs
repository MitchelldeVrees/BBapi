using System.Text;
using System.Text.RegularExpressions;

namespace Dma.OrderIntake.Domain;

// ISO 6166 identifier: 2-letter country code + 9-character alphanumeric NSIN +
// 1 check digit. Pure validation logic — no knowledge of any security master,
// mock or real, lives here. Any IInstrumentResolver implementation reuses this
// instead of reimplementing the check-digit algorithm.
public readonly struct Isin
{
    private static readonly Regex Format = new("^[A-Z]{2}[A-Z0-9]{9}[0-9]$");

    public string Value { get; }

    private Isin(string value)
    {
        Value = value;
    }

    public static bool TryParse(string? input, out Isin isin, out string? error)
    {
        isin = default;

        if (string.IsNullOrWhiteSpace(input))
        {
            error = "ISIN is required.";
            return false;
        }

        var candidate = input.Trim().ToUpperInvariant();

        if (!Format.IsMatch(candidate))
        {
            error = "ISIN must be 2 letters, 9 alphanumeric characters and 1 check digit (12 characters total).";
            return false;
        }

        if (!HasValidCheckDigit(candidate))
        {
            error = "ISIN check digit is invalid.";
            return false;
        }

        isin = new Isin(candidate);
        error = null;
        return true;
    }

    // ISO 6166 Annex A check-digit algorithm (a Luhn/mod-10 variant): letters
    // become digits (A=10 ... Z=35), then every second digit counting from the
    // rightmost (the check digit itself, never doubled) is doubled.
    private static bool HasValidCheckDigit(string isin)
    {
        var digits = new StringBuilder();
        foreach (var c in isin)
        {
            digits.Append(char.IsDigit(c) ? c.ToString() : (c - 'A' + 10).ToString());
        }

        var digitString = digits.ToString();
        var sum = 0;
        var doubleDigit = false;

        for (var i = digitString.Length - 1; i >= 0; i--)
        {
            var digit = digitString[i] - '0';
            if (doubleDigit)
            {
                digit *= 2;
                if (digit > 9)
                {
                    digit -= 9;
                }
            }

            sum += digit;
            doubleDigit = !doubleDigit;
        }

        return sum % 10 == 0;
    }

    public override string ToString() => Value;
}
