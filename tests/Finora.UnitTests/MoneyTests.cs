using System.Globalization;
using Finora.Domain;

namespace Finora.UnitTests;

public sealed class MoneyTests
{
    [Theory]
    [InlineData("12.34", 1234)]
    [InlineData("0.01", 1)]
    [InlineData("-4.50", -450)]
    public void FromMajorUnits_UsesDecimalMinorUnits(string text, long expected)
    {
        var major = decimal.Parse(text, CultureInfo.InvariantCulture);
        var money = Money.FromMajorUnits(major, "INR");
        Assert.Equal(expected, money.MinorUnits);
        Assert.Equal("INR", money.Currency);
        Assert.Equal(2, money.DecimalPlaces);
    }

    [Fact]
    public void FromMajorUnits_RoundsAwayFromZeroAtMinorBoundary()
    {
        Assert.Equal(101, Money.FromMajorUnits(1.005m, "INR").MinorUnits);
        Assert.Equal(-101, Money.FromMajorUnits(-1.005m, "INR").MinorUnits);
    }

    [Theory]
    [InlineData("JPY", "1234.6", 1235, 0)]
    [InlineData("INR", "1234.56", 123456, 2)]
    [InlineData("KWD", "12.3456", 12346, 3)]
    [InlineData("CLF", "1.23456", 12346, 4)]
    public void CurrencyMinorUnitPrecision_IsAppliedByDefault(string currency, string majorText, long expectedMinor, int expectedPlaces)
    {
        var major = decimal.Parse(majorText, CultureInfo.InvariantCulture);
        var money = Money.FromMajorUnits(major, currency);

        Assert.Equal(expectedMinor, money.MinorUnits);
        Assert.Equal(expectedPlaces, money.DecimalPlaces);
        Assert.Equal(expectedPlaces, CurrencyMinorUnits.GetDecimalPlaces(currency));
    }

    [Fact]
    public void ExplicitPrecision_RemainsAvailableForNonStandardAccountingUnits()
    {
        var money = Money.FromMajorUnits(1.23456m, "X123", 4);
        Assert.Equal(12346, money.MinorUnits);
        Assert.Equal(1.2346m, money.ToMajorUnits(4));
    }

    [Fact]
    public void Format_UsesCurrencySpecificDefaultPrecision()
    {
        var culture = CultureInfo.GetCultureInfo("en-US");
        Assert.Equal("JPY 1,235", new Money(1235, "JPY").Format(culture));
        Assert.Equal("INR 12.35", new Money(1235, "INR").Format(culture));
        Assert.Equal("KWD 1.235", new Money(1235, "KWD").Format(culture));
        Assert.Equal("CLF 1.2345", new Money(12345, "CLF").Format(culture));
    }
}
