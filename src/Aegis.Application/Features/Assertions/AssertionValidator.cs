using Aegis.Contracts.Common;
using Aegis.Contracts.Compatibility;
using Aegis.Domain.ValueObjects;

namespace Aegis.Application.Features.Assertions;

public sealed class AssertionValidator
{
    public IReadOnlyDictionary<string, HashSet<string>> BuildRelationIndex(string model)
    {
        var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var lines = model.Replace("\r", string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        string? currentType = null;

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.StartsWith("type ", StringComparison.OrdinalIgnoreCase))
            {
                currentType = line[5..].Trim();
                if (!result.ContainsKey(currentType))
                {
                    result[currentType] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                continue;
            }

            if (currentType is null || !line.StartsWith("define ", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var definition = line[7..];
            var separatorIndex = definition.IndexOf(':');
            if (separatorIndex > 0)
            {
                result[currentType].Add(definition[..separatorIndex].Trim());
            }
        }

        return result;
    }

    public void Validate(
        AegisCompatAssertionDto assertion,
        IReadOnlyDictionary<string, HashSet<string>> relationIndex)
    {
        ArgumentNullException.ThrowIfNull(assertion);
        if (!SubjectId.TryCreate(assertion.TupleKey.User, out _)
            || !RelationName.TryCreate(assertion.TupleKey.Relation, out _)
            || !ObjectId.TryCreate(assertion.TupleKey.Object, out _))
        {
            throw new CompatibilityApiException(400, "validation_error", "Invalid assertion tuple_key format.");
        }

        ValidateTypeAndRelation(assertion.TupleKey.Object, assertion.TupleKey.Relation, relationIndex);
        foreach (var tuple in assertion.ContextualTuples?.TupleKeys ?? [])
        {
            if (!SubjectId.TryCreate(tuple.User, out _)
                || !RelationName.TryCreate(tuple.Relation, out _)
                || !ObjectId.TryCreate(tuple.Object, out _))
            {
                throw new CompatibilityApiException(400, "validation_error", "Invalid assertion contextual tuple format.");
            }

            ValidateTypeAndRelation(tuple.Object, tuple.Relation, relationIndex);
        }
    }

    public bool IsValid(
        AegisCompatAssertionDto assertion,
        IReadOnlyDictionary<string, HashSet<string>> relationIndex)
    {
        try
        {
            Validate(assertion, relationIndex);
            return true;
        }
        catch (CompatibilityApiException)
        {
            return false;
        }
    }

    private static void ValidateTypeAndRelation(
        string objectRef,
        string relation,
        IReadOnlyDictionary<string, HashSet<string>> relationIndex)
    {
        var typeSeparator = objectRef.IndexOf(':');
        var typeName = typeSeparator > 0 ? objectRef[..typeSeparator] : objectRef;
        if (!relationIndex.TryGetValue(typeName, out var relations))
        {
            throw new CompatibilityApiException(400, "type_not_found", $"type '{typeName}' not found");
        }

        if (!relations.Contains(relation))
        {
            throw new CompatibilityApiException(400, "relation_not_found", $"relation '{typeName}#{relation}' not found");
        }
    }
}
