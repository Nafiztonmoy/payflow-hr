namespace PayFlow.Domain.Models;

public class TaxBracket
{
    public decimal LowerBound { get; set; }
    public decimal? UpperBound { get; set; }
    public decimal Rate { get; set; } // e.g. 0.10 for 10%
    public string Description { get; set; } = string.Empty;
}

public class TaxRuleConfig
{
    public decimal StandardDeduction { get; set; } = 0m;
    public List<TaxBracket> Brackets { get; set; } = new();
}

public static class RoundingPolicy
{
    public static decimal RoundCurrency(decimal amount)
    {
        // Standard banker's rounding or MidpointRounding.AwayFromZero to 2 decimals
        return Math.Round(amount, 2, MidpointRounding.AwayFromZero);
    }
}
