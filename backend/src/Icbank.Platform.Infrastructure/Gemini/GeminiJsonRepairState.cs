using System.Text;

namespace Icbank.Platform.Infrastructure.Gemini;

/// <summary>Tracks nesting while scanning model output so a truncated payload can be closed correctly.</summary>
/// <remarks>
/// Objects and arrays are tracked on one stack rather than as a single depth counter, and an
/// element boundary is recognised at every nesting level rather than only at the root. A counter
/// that only recognised <c>{}</c> treated the elements of a top-level <c>"variants": [...]</c>
/// array as root boundaries, so a response cut off mid-array was "repaired" into
/// <c>{"extracted":{…},"variants":[{…}}</c> — still unparseable, which surfaced to the designer
/// as a blank 500 rather than a usable set of styles. Trimming to the deepest complete element
/// also keeps the variants that did arrive instead of discarding the whole array.
/// </remarks>
internal sealed class GeminiJsonRepairState
{
    private readonly Stack<char> _open = new();
    private bool _inString;
    private bool _escaped;

    /// <summary>Advances the scanner by one character.</summary>
    /// <param name="c">The character to consume.</param>
    /// <returns><see langword="true"/> when the character ends a complete element at any nesting level.</returns>
    internal bool Advance(char c)
    {
        if (_inString)
        {
            AdvanceInsideString(c);
            return false;
        }

        switch (c)
        {
            case '"':
                _inString = true;
                return false;
            case '{':
            case '[':
                _open.Push(c);
                return false;
            case '}':
            case ']':
                return Close();
            case ',':
                return _open.Count > 0;
            default:
                return false;
        }
    }

    /// <summary>Produces the closing characters needed to terminate every container still open.</summary>
    /// <returns>The closing sequence, innermost first.</returns>
    internal string CloseRemaining()
    {
        var builder = new StringBuilder(_open.Count);
        foreach (var open in _open)
        {
            builder.Append(open == '[' ? ']' : '}');
        }

        return builder.ToString();
    }

    private void AdvanceInsideString(char c)
    {
        if (_escaped)
        {
            _escaped = false;
            return;
        }

        if (c == '\\')
        {
            _escaped = true;
            return;
        }

        if (c == '"')
        {
            _inString = false;
        }
    }

    private bool Close()
    {
        if (_open.Count == 0)
        {
            return false;
        }

        _open.Pop();
        return true;
    }
}
