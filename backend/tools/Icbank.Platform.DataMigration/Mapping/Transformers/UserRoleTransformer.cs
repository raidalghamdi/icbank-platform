using Icbank.Platform.DataMigration.Mapping.Dtos;
using Icbank.Platform.DataMigration.Source;

namespace Icbank.Platform.DataMigration.Mapping.Transformers;

/// <summary>
/// Pure transformer from a raw <c>user_roles</c> row to <see cref="MappedUserRole"/>.
/// </summary>
/// <remarks>
/// <para><b>Multi-role decision (task requirement 3):</b> the Node API only ever reads the first
/// role for a user (<c>.limit(1)</c> — AMBIGUOUS-3 in DATA-MODEL.md / AUTH-PORT-NOTES.md), but the
/// <c>user_roles</c> table itself has always supported many-to-many and nothing in the schema
/// prevented multiple rows per user. This transformer therefore migrates <b>every</b> row from
/// <c>user_roles</c> for every user, unmodified — one destination <c>UserRole</c> row per source
/// row. It does not drop or deduplicate to "first role only".</para>
/// <para>This is deliberate and matches the .NET port's own decision (AUTH-PORT-NOTES.md:
/// "multi-role union instead of silently dropping roles") — the destination Application layer
/// already unions all of a user's role permissions, so carrying over every existing role
/// assignment is what makes that new union behavior meaningful for pre-existing users instead of
/// only ever seeing one role. Any user who already had more than one <c>user_roles</c> row in
/// Postgres (even though the old UI never surfaced more than one) will, after migration, have
/// their <b>effective permissions strictly expand or stay the same</b> versus before — never
/// shrink. This is a behavior change worth flagging to product; see
/// spec/DATA-MIGRATION-NOTES.md.</para>
/// </remarks>
public static class UserRoleTransformer
{
    /// <summary>Transforms one raw <c>user_roles</c> row.</summary>
    /// <param name="row">The raw source row.</param>
    /// <returns>The mapped, destination-ready DTO.</returns>
    public static MappedUserRole Transform(SourceRow row)
    {
        DateTime assignedAt = row.GetRawTimestamp("assigned_at") ?? row.GetRawTimestamp("created_at")
            ?? throw new InvalidOperationException("user_roles row has neither assigned_at nor created_at.");

        return new MappedUserRole(
            SourceId: row.GetInt32("id"),
            UserSourceId: row.GetInt32("user_id"),
            RoleSourceId: row.GetInt32("role_id"),
            AssignedBySourceId: row.GetNullableInt32("assigned_by"),
            AssignedAtUtc: assignedAt);
    }
}
