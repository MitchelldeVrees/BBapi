using Dma.OrderIntake.Domain;

namespace Dma.OrderIntake.Domain.Tests;

public class MicTests
{
    [Theory]
    [InlineData("XAMS", "XAMS")]
    [InlineData("xams", "XAMS")]
    [InlineData(" XAMS ", "XAMS")]
    public void TryParse_ValidMic_NormalizesToUppercase(string input, string expected)
    {
        var ok = Mic.TryParse(input, out var mic, out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(expected, mic.Value);
    }

    [Theory]
    [InlineData("XAM")]
    [InlineData("XAMST")]
    [InlineData("XA1S")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParse_InvalidMic_Fails(string? input)
    {
        var ok = Mic.TryParse(input, out _, out var error);

        Assert.False(ok);
        Assert.NotNull(error);
    }
}
