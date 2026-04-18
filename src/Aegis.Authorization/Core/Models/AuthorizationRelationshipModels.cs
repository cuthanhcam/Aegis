namespace Aegis.Authorization.Core.Models
{
    /// <summary>
    /// Effect applied by a relationship tuple during evaluation.
    /// </summary>
    public enum RelationshipEffect
    {
        Allow,
        Deny,
    }

    /// <summary>
    /// Strongly typed subject wrapper used by authorization models.
    /// </summary>
    public sealed record Subject(string Value);

    /// <summary>
    /// Strongly typed object reference wrapper in &lt;type&gt;:&lt;id&gt; shape.
    /// </summary>
    public sealed record ObjectRef(string Value);

    /// <summary>
    /// Relationship tuple persisted or evaluated by the authorization engine.
    /// </summary>
    public sealed record RelationshipTuple(
        Subject Subject,
        string Relation,
        ObjectRef Object,
        RelationshipEffect Effect,
        DateTimeOffset CreatedAt);

    /// <summary>
    /// One change entry emitted by relationship mutation flows.
    /// </summary>
    public sealed record RelationshipChange(
        string TenantId,
        Subject Subject,
        string Relation,
        ObjectRef Object,
        string Operation,
        DateTimeOffset CreatedAt);
}
