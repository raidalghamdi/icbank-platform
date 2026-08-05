namespace Icbank.Platform.DataMigration.Mapping.Dtos;

/// <summary>Pure DTO produced by <see cref="Transformers.UserRoleTransformer"/>.</summary>
/// <param name="SourceId">The source Postgres <c>user_roles.id</c>.</param>
/// <param name="UserSourceId">The source <c>users.id</c> this assignment belongs to.</param>
/// <param name="RoleSourceId">The source <c>roles.id</c> assigned.</param>
/// <param name="AssignedBySourceId">The source id of the user who made the assignment, if known.</param>
/// <param name="AssignedAtUtc">The assignment timestamp.</param>
public sealed record MappedUserRole(
    int SourceId,
    int UserSourceId,
    int RoleSourceId,
    int? AssignedBySourceId,
    DateTime AssignedAtUtc);
