using System.Globalization;
using System.Text;

namespace Icbank.Platform.Application.Admin.Queries;

/// <summary>
/// Renders <see cref="ActivityLogExportRow"/> rows as CSV directly onto the caller's output
/// stream (the API controller's <c>Response.Body</c>) rather than building the whole file in
/// memory first — mirroring <see cref="Icbank.Platform.Application.AiYear.AiYearArchiveStreamWriter"/>'s
/// "stream, don't buffer the whole export" pattern for the ZIP endpoint. Column order, headers,
/// and quoting mirror the old Node export (<c>admin.ts:637</c>) exactly:
/// <c>["#", "المستخدم", "البريد", "العملية", "النوع", "المعرف", "IP", "التاريخ"]</c>, every field
/// quoted with internal quotes doubled, rows newline-joined. Two things are deliberately *not*
/// ported from Node because the task requires them as new hardening on top of the port: a UTF-8
/// BOM is written first (Node's response body started with <c>"\ufeff" + csv</c> too, but only
/// because the *frontend* prepended it — the .NET port emits the BOM as real leading bytes on the
/// wire so Excel detects UTF-8 correctly even if a caller other than this codebase's own frontend
/// ever downloads the file directly), and every field is passed through
/// <see cref="NeutralizeFormulaInjection"/> before quoting, since an activity log contains
/// user-supplied text (email addresses, entity ids) that was never designed to be safe to open in
/// a spreadsheet application that auto-executes leading <c>=</c>/<c>+</c>/<c>-</c>/<c>@</c> as a formula.
/// </summary>
public static class ActivityLogCsvWriter
{
    private static readonly byte[] Utf8Bom = { 0xEF, 0xBB, 0xBF };
    private static readonly string[] Headers = { "#", "المستخدم", "البريد", "العملية", "النوع", "المعرف", "IP", "التاريخ" };
    private static readonly char[] FormulaInjectionPrefixes = { '=', '+', '-', '@' };

    /// <summary>Writes the UTF-8 BOM, the header row, and one row per <paramref name="rows"/> entry to <paramref name="destination"/>.</summary>
    /// <param name="rows">The already-filtered, already-capped rows to render, in the order they should appear.</param>
    /// <param name="destination">The output stream the CSV is written to (never buffered whole in memory).</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    public static async Task WriteAsync(IReadOnlyList<ActivityLogExportRow> rows, Stream destination, CancellationToken cancellationToken)
    {
        await destination.WriteAsync(Utf8Bom, cancellationToken);

        // Why: leaveOpen so disposing the StreamWriter never closes the caller's Response.Body;
        // AutoFlush is unnecessary since the explicit FlushAsync below covers it.
        var writer = new StreamWriter(destination, Encoding.UTF8, leaveOpen: true);
        await using (writer.ConfigureAwait(false))
        {
            await writer.WriteAsync(BuildRow(Headers));
            foreach (ActivityLogExportRow row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await writer.WriteAsync(BuildRow(ToFields(row)));
            }

            await writer.FlushAsync(cancellationToken);
        }
    }

    private static string BuildRow(IEnumerable<string> fields) => string.Join(',', fields.Select(Escape)) + "\n";

    private static IEnumerable<string> ToFields(ActivityLogExportRow row)
    {
        yield return row.Id.ToString(CultureInfo.InvariantCulture);
        yield return row.UserName ?? "—";
        yield return row.UserEmail ?? "—";
        yield return row.Action;
        yield return row.EntityType ?? "—";
        yield return row.EntityId ?? "—";
        yield return row.IpAddress ?? "—";
        yield return row.CreatedAt.ToString("o", CultureInfo.InvariantCulture);
    }

    private static string Escape(string value)
    {
        var neutralized = NeutralizeFormulaInjection(value);
        return "\"" + neutralized.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    /// <summary>
    /// Prefixes a value with a leading apostrophe if it starts with a character a spreadsheet
    /// application would treat as a formula trigger (<c>=</c>, <c>+</c>, <c>-</c>, <c>@</c>). The
    /// apostrophe is the standard CSV-injection mitigation: Excel/LibreOffice render the field as
    /// literal text starting with that character instead of evaluating it, and the character is
    /// visually a no-op for every legitimate value in this table (none of the exported columns are
    /// expected to start with a minus sign in normal use; a rare legitimate case is still safer
    /// mangled than executed).
    /// </summary>
    /// <param name="value">The raw field value.</param>
    /// <returns>The value, prefixed with <c>'</c> if it began with a formula-trigger character.</returns>
    private static string NeutralizeFormulaInjection(string value) =>
        value.Length > 0 && FormulaInjectionPrefixes.Contains(value[0]) ? "'" + value : value;
}
