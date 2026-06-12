using System.Text.Json;
using Aegis.Authorization.ABAC;
using Aegis.Authorization.Core.Models;

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

    [Fact]
    public void EvaluateExpression_ReturnsTrue_ForContextAndObjectPredicate()
    {
        var request = Request(new Dictionary<string, JsonElement>
        {
            ["department"] = Json("\"finance\""),
            ["risk"] = Json("2")
        });

        var result = ContextConditionEvaluator.Evaluate(
            "context.department == \"finance\" and object.type == \"invoice\" and context.risk <= 3",
            request);

        Assert.True(result);
    }

    [Fact]
    public void EvaluateExpression_ReturnsFalse_WhenPredicateDoesNotMatch()
    {
        var request = Request(new Dictionary<string, JsonElement>
        {
            ["department"] = Json("\"engineering\"")
        });

        var result = ContextConditionEvaluator.Evaluate(
            "context.department == \"finance\" and subject.type == \"user\"",
            request);

        Assert.False(result);
    }

    [Fact]
    public void EvaluateExpression_SupportsOrAndNot()
    {
        var request = Request(new Dictionary<string, JsonElement>
        {
            ["after_hours"] = Json("false")
        });

        var result = ContextConditionEvaluator.Evaluate(
            "subject == \"user:alice\" or not context.after_hours",
            request);

        Assert.True(result);
    }

    private static JsonElement Json(string raw)
    {
        return JsonDocument.Parse(raw).RootElement.Clone();
    }

    private static CheckRequest Request(IReadOnlyDictionary<string, JsonElement> context)
    {
        return new CheckRequest(
            "tenant-a",
            new Subject("user:alice"),
            "view",
            new ObjectRef("invoice:2026"),
            Context: context,
            StoreId: "store-a");
    }
}
