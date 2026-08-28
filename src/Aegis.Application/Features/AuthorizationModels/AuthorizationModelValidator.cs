using Aegis.Authorization.Core.Parsing;
using Aegis.Contracts.Administration;
using System.Text.RegularExpressions;

namespace Aegis.Application.Features.AuthorizationModels;

/// <summary>
/// Validates the authorization-model DSL without persistence or transport concerns.
/// </summary>
public sealed class AuthorizationModelValidator
{
    private static readonly Regex TypeRegex = new(@"^\s*type\s+([A-Za-z][A-Za-z0-9_]*)\s*$", RegexOptions.Compiled);
    private static readonly Regex DefineRegex = new(@"^\s*define\s+([A-Za-z][A-Za-z0-9_]*)\s*:\s*(.+)$", RegexOptions.Compiled);

    public AuthorizationModelValidationResultDto Validate(
        ValidateAuthorizationModelRequestDto request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request);

        var errors = new List<AuthorizationModelValidationIssueDto>();
        var warnings = new List<AuthorizationModelValidationIssueDto>();
        var types = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var relationsByType = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var currentType = string.Empty;
        var relationCount = 0;
        var directRelationCount = 0;
        var hasUnion = false;
        var hasIntersection = false;
        var hasExclusion = false;
        var hasTupleToUserset = false;

        if (string.IsNullOrWhiteSpace(request.SchemaVersion))
        {
            errors.Add(new AuthorizationModelValidationIssueDto("SCHEMA_VERSION_REQUIRED", "schemaVersion is required."));
        }
        else if (!request.SchemaVersion.Trim().Equals("1.1", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add(new AuthorizationModelValidationIssueDto("SCHEMA_VERSION_UNRECOGNIZED", "Aegis currently validates against schema version 1.1 semantics."));
        }

        if (string.IsNullOrWhiteSpace(request.Model))
        {
            errors.Add(new AuthorizationModelValidationIssueDto("MODEL_REQUIRED", "model is required."));
        }
        else
        {
            var lines = request.Model.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
            for (var index = 0; index < lines.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var lineNumber = index + 1;
                var line = lines[index];
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                if (trimmed.Equals("model", StringComparison.OrdinalIgnoreCase)
                    || trimmed.Equals("relations", StringComparison.OrdinalIgnoreCase)
                    || trimmed.StartsWith("schema ", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var typeMatch = TypeRegex.Match(line);
                if (typeMatch.Success)
                {
                    currentType = typeMatch.Groups[1].Value;
                    if (!types.Add(currentType))
                    {
                        errors.Add(new AuthorizationModelValidationIssueDto("DUPLICATE_TYPE", $"Type '{currentType}' is defined more than once.", lineNumber));
                    }

                    relationsByType.TryAdd(currentType, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                    continue;
                }

                var defineMatch = DefineRegex.Match(line);
                if (defineMatch.Success)
                {
                    if (string.IsNullOrWhiteSpace(currentType))
                    {
                        errors.Add(new AuthorizationModelValidationIssueDto("RELATION_OUTSIDE_TYPE", "Relation definitions must appear inside a type block.", lineNumber));
                        continue;
                    }

                    var relation = defineMatch.Groups[1].Value;
                    var expression = defineMatch.Groups[2].Value.Trim();
                    if (!relationsByType[currentType].Add(relation))
                    {
                        errors.Add(new AuthorizationModelValidationIssueDto("DUPLICATE_RELATION", $"Relation '{currentType}#{relation}' is defined more than once.", lineNumber));
                    }

                    if (string.IsNullOrWhiteSpace(expression))
                    {
                        errors.Add(new AuthorizationModelValidationIssueDto("EMPTY_RELATION_EXPRESSION", $"Relation '{currentType}#{relation}' has an empty rewrite expression.", lineNumber));
                        continue;
                    }

                    relationCount++;
                    directRelationCount += expression.StartsWith("[", StringComparison.Ordinal) ? 1 : 0;
                    hasUnion |= Regex.IsMatch(expression, @"\bor\b", RegexOptions.IgnoreCase);
                    hasIntersection |= Regex.IsMatch(expression, @"\band\b", RegexOptions.IgnoreCase);
                    hasExclusion |= Regex.IsMatch(expression, @"\bbut\s+not\b", RegexOptions.IgnoreCase);
                    hasTupleToUserset |= Regex.IsMatch(expression, @"\bfrom\b", RegexOptions.IgnoreCase);

                    try
                    {
                        _ = RewriteExpressionParser.Parse(expression);
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new AuthorizationModelValidationIssueDto("INVALID_REWRITE_EXPRESSION", ex.Message, lineNumber));
                    }

                    continue;
                }

                warnings.Add(new AuthorizationModelValidationIssueDto("UNRECOGNIZED_MODEL_LINE", $"Line was ignored by the validator: '{trimmed}'.", lineNumber));
            }
        }

        if (types.Count == 0)
        {
            errors.Add(new AuthorizationModelValidationIssueDto("TYPE_REQUIRED", "At least one type definition is required."));
        }

        if (relationCount == 0)
        {
            warnings.Add(new AuthorizationModelValidationIssueDto("RELATION_RECOMMENDED", "Add at least one relation before using this model for authorization checks."));
        }

        if (directRelationCount == 0)
        {
            warnings.Add(new AuthorizationModelValidationIssueDto("DIRECT_RELATION_RECOMMENDED", "At least one direct assignable relation is recommended for tuple writes."));
        }

        var summary = new AuthorizationModelValidationSummaryDto(
            types.Count,
            relationCount,
            directRelationCount,
            hasUnion,
            hasIntersection,
            hasExclusion,
            hasTupleToUserset);

        return new AuthorizationModelValidationResultDto(errors.Count == 0, summary, errors, warnings);
    }
}
