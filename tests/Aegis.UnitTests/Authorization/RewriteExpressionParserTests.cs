using Aegis.Authorization.Core.Parsing;

namespace Aegis.UnitTests.Authorization;

public class RewriteExpressionParserTests
{
    [Fact]
    public void Parse_SplitsBracketOptions_ByComma()
    {
        var terms = RewriteExpressionParser.Parse("[user, group#member]");

        Assert.Equal(2, terms.Count);
        Assert.Contains(terms, t => t.Includes.Count == 1 && t.Includes[0] == "user");
        Assert.Contains(terms, t => t.Includes.Count == 1 && t.Includes[0] == "group#member");
    }

    [Fact]
    public void Parse_ExpandsParenthesizedUnionWithAndCrossProduct()
    {
        var terms = RewriteExpressionParser.Parse("(editor or owner) and reviewer");

        Assert.Equal(2, terms.Count);
        Assert.Contains(terms, t => t.Includes.SequenceEqual(new[] { "editor", "reviewer" }));
        Assert.Contains(terms, t => t.Includes.SequenceEqual(new[] { "owner", "reviewer" }));
    }

    [Fact]
    public void Parse_HandlesButNotExcludeClauses()
    {
        var terms = RewriteExpressionParser.Parse("editor but not banned and suspended");

        var term = Assert.Single(terms);
        Assert.Single(term.Includes);
        Assert.Equal("editor", term.Includes[0]);
        Assert.Single(term.ExcludeClauses);
        Assert.Equal(new[] { "banned", "suspended" }, term.ExcludeClauses[0]);
    }
}
