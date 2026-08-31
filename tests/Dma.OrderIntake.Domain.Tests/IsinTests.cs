using Dma.OrderIntake.Domain;

namespace Dma.OrderIntake.Domain.Tests;

public class IsinTests
{
    [Theory]
    [InlineData("US0378331005")] // Apple
    [InlineData("NL0010273215")] // ASML
    [InlineData("nl0010273215")] // lower case, should normalize
    [InlineData(" NL0010273215 ")] // whitespace, should trim
    public void TryParse_ValidIsin_Succeeds(string input)
    {
        var ok = Isin.TryParse(input, out var isin, out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(input.Trim().ToUpperInvariant(), isin.Value);
    }

    [Theory]
    [InlineData("NL0010273216")] // ASML with tampered check digit
    [InlineData("NL001027321")] // too short
    [InlineData("NL00102732155")] // too long
    [InlineData("1L0010273215")] // country code not letters
    [InlineData("")]
    [InlineData(null)]
    public void TryParse_InvalidIsin_Fails(string? input)
    {
        var ok = Isin.TryParse(input, out _, out var error);

        Assert.False(ok);
        Assert.NotNull(error);
    }
}
