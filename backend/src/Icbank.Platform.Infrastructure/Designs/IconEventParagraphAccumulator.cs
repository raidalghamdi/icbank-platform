namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>Keeps paragraph grouping and inline-contact state together while a body is parsed.</summary>
internal sealed class IconEventParagraphAccumulator
{
    private readonly List<IconEventParagraphBlock> _blocks = new();
    private readonly List<string> _bullets = new();
    private readonly string? _email;
    private readonly string? _phone;
    private readonly List<string> _text = new();
    private bool _emailUsedInline;
    private bool _phoneUsedInline;

    internal IconEventParagraphAccumulator(string? email, string? phone)
    {
        _email = email;
        _phone = phone;
    }

    internal void AddBullet(string value) => _bullets.Add(value);

    internal void AddSubHeading(string value) => _blocks.Add(new IconEventParagraphBlock("sub-heading", value));

    internal void AddText(string value)
    {
        _blocks.Add(new IconEventParagraphBlock("text", value));
        AddInlineContacts(value);
    }

    internal void AddTextLine(string value) => _text.Add(value);

    internal void FlushBullets()
    {
        if (_bullets.Count == 0)
        {
            return;
        }

        _blocks.Add(new IconEventParagraphBlock("bullet-list", string.Empty, _bullets.ToArray()));
        AddInlineContacts(_bullets[^1]);
        _bullets.Clear();
    }

    internal void FlushText()
    {
        var combined = string.Join(" ", _text).Trim();
        if (combined.Length > 0)
        {
            AddText(combined);
        }

        _text.Clear();
    }

    internal IconEventParagraphFlow ToFlow() => new(_blocks, _emailUsedInline, _phoneUsedInline);

    private void AddInlineContacts(string value)
    {
        if (!string.IsNullOrWhiteSpace(_email) && !_emailUsedInline && IconEventParagraphFlowBuilder.IsEmailMention(value))
        {
            _blocks.Add(new IconEventParagraphBlock("email-chip", _email));
            _emailUsedInline = true;
        }

        if (!string.IsNullOrWhiteSpace(_phone) && !_phoneUsedInline && IconEventParagraphFlowBuilder.IsPhoneMention(value))
        {
            _blocks.Add(new IconEventParagraphBlock("phone-chip", _phone));
            _phoneUsedInline = true;
        }
    }
}
