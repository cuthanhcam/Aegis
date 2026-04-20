using System.Text.Json;
using Aegis.Authorization.ABAC;

namespace Aegis.UnitTests.Authorization;

public class ContextConditionEvaluatorTests
{
    [Fact]
    public void Evaluate_ReturnsTrue_ForBooleanTrue()
    {
        var context = new Dictionary<string, JsonElement>
        {
            ["is_owner"] = Json("true")
        };

        var result = ContextConditionEvaluator.Evaluate("is_owner", context);

        Assert.True(result);
    }

    [Fact]
    public void Evaluate_ReturnsFalse_WhenMissingCondition()
    {
        var context = new Dictionary<string, JsonElement>
        {
            ["is_owner"] = Json("true")
        };

        var result = ContextConditionEvaluator.Evaluate("is_admin", context);

        Assert.False(result);
    }

    [Fact]
    public void Evaluate_ReturnsTrue_ForStringTrue()
    {
        var context = new Dictionary<string, JsonElement>
        {
            ["policy_enabled"] = Json("\"true\"")
        };

        var result = ContextConditionEvaluator.Evaluate("policy_enabled", context);

        Assert.True(result);
    }

    [Theory]
    [InlineData("0", false)]
    [InlineData("1", true)]
    [InlineData("2", true)]
    public void Evaluate_ParsesNumericFlags(string rawValue, bool expected)
    {
        var context = new Dictionary<string, JsonElement>
        {
            ["risk_level"] = Json(rawValue)
        };

        var result = ContextConditionEvaluator.Evaluate("risk_level", context);

        Assert.Equal(expected, result);
    }

    private static JsonElement Json(string raw)
    {
        return JsonDocument.Parse(raw).RootElement.Clone();
    }
}
