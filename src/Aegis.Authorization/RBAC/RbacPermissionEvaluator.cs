using Aegis.Authorization.ABAC;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;

namespace Aegis.Authorization.RBAC
{
    /// <summary>
    /// Evaluates RBAC grants with deny-first precedence and optional ABAC conditions.
    /// </summary>
    public sealed class RbacPermissionEvaluator
        : IRbacProvider
    {
        private readonly Func<string, Subject, CancellationToken, Task<IReadOnlyList<RbacPermissionGrant>>> _grantProvider;

        /// <summary>
        /// Creates an RBAC evaluator from a grant provider callback.
        /// </summary>
        public RbacPermissionEvaluator(
            Func<string, Subject, CancellationToken, Task<IReadOnlyList<RbacPermissionGrant>>> grantProvider)
        {
            _grantProvider = grantProvider;
        }

        /// <summary>
        /// Evaluates one authorization request against RBAC grants.
        /// </summary>
        public async Task<bool> HasPermissionAsync(
            CheckRequest request,
            CancellationToken cancellationToken = default)
        {
            var grants = await _grantProvider(request.TenantId, request.Subject, cancellationToken);
            if (grants.Count == 0)
            {
                return false;
            }

            if (grants.Any(x => x.IsDeny && MatchesGrant(x, request)))
            {
                return false;
            }

            return grants.Any(x => !x.IsDeny && MatchesGrant(x, request));
        }

        private static bool MatchesGrant(RbacPermissionGrant grant, CheckRequest request)
        {
            if (!MatchesSubject(grant.SubjectPattern, request.Subject.Value))
            {
                return false;
            }

            if (!MatchesRelation(grant.RelationPattern, request.Relation))
            {
                return false;
            }

            if (!MatchesObject(grant.ObjectPattern, request.Object.Value))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(grant.ConditionName))
            {
                return true;
            }

            return ContextConditionEvaluator.Evaluate(grant.ConditionName, request.Context);
        }

        private static bool MatchesSubject(string subjectPattern, string subjectRef)
        {
            if (string.IsNullOrWhiteSpace(subjectPattern) || subjectPattern == "*")
            {
                return true;
            }

            if (subjectPattern.Equals(subjectRef, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var subjectType = GetTypeName(subjectRef);
            return subjectPattern.Equals(subjectType, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesRelation(string relationPattern, string relation)
        {
            if (string.IsNullOrWhiteSpace(relationPattern) || relationPattern == "*")
            {
                return true;
            }

            return relationPattern.Equals(relation, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesObject(string objectPattern, string objectRef)
        {
            if (string.IsNullOrWhiteSpace(objectPattern) || objectPattern == "*")
            {
                return true;
            }

            if (objectPattern.Equals(objectRef, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var objectType = GetTypeName(objectRef);
            return objectPattern.Equals($"{objectType}:*", StringComparison.OrdinalIgnoreCase)
                || objectPattern.Equals(objectType, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetTypeName(string value)
        {
            var split = value.IndexOf(':');
            return split > 0 ? value[..split] : value;
        }
    }

    /// <summary>
    /// One RBAC permission grant entry.
    /// </summary>
    /// <param name="SubjectPattern">Exact subject, subject type, or <c>*</c>.</param>
    /// <param name="RelationPattern">Relation name or <c>*</c>.</param>
    /// <param name="ObjectPattern">Exact object, object type, <c>type:*</c>, or <c>*</c>.</param>
    /// <param name="IsDeny">When true, grant acts as deny override.</param>
    /// <param name="ConditionName">Optional ABAC condition name from request context.</param>
    public sealed record RbacPermissionGrant(
        string SubjectPattern,
        string RelationPattern,
        string ObjectPattern,
        bool IsDeny = false,
        string? ConditionName = null);
}