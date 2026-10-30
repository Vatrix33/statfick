using System.Globalization;
using Statventure.UI.Css;

namespace Statventure.UI.Dom;

/// <summary>Which constraints a form control fails (the DOM's ValidityState).</summary>
public readonly record struct ValidityState(
    bool ValueMissing, bool TypeMismatch, bool PatternMismatch, bool TooLong, bool TooShort,
    bool RangeUnderflow, bool RangeOverflow, bool StepMismatch, bool BadInput, bool CustomError)
{
    public bool Valid => !(ValueMissing || TypeMismatch || PatternMismatch || TooLong || TooShort || RangeUnderflow || RangeOverflow || StepMismatch || BadInput || CustomError);
}

/// <summary>
/// Form controls as the HTML standard and Chromium 152 define them: input types and value modes,
/// the value with Chromium's sanitizing per type (and its dirty flag), checkedness (radio groups),
/// option selectedness, min/max/step (Blink's StepRange in decimal arithmetic), form owners,
/// labels, details' open state, and constraint validation with Chromium's messages.
/// </summary>
public partial class UiElement
{
    static readonly HashSet<string> InputTypes =
    [
        "text", "search", "tel", "url", "email", "password", "number", "range", "color", "checkbox", "radio", "file",
        "submit", "image", "reset", "button", "hidden", "date", "month", "week", "time", "datetime-local",
    ];

    static readonly PseudoClassKind[] ValueStates =
    [
        PseudoClassKind.PlaceholderShown, PseudoClassKind.Valid, PseudoClassKind.Invalid, PseudoClassKind.UserValid,
        PseudoClassKind.UserInvalid, PseudoClassKind.InRange, PseudoClassKind.OutOfRange,
    ];

    static readonly PseudoClassKind[] CheckedStates =
    [
        PseudoClassKind.Checked, PseudoClassKind.Indeterminate, PseudoClassKind.Valid, PseudoClassKind.Invalid,
        PseudoClassKind.UserValid, PseudoClassKind.UserInvalid,
    ];

    static readonly PseudoClassKind[] ValidityStates =
        [PseudoClassKind.Valid, PseudoClassKind.Invalid, PseudoClassKind.UserValid, PseudoClassKind.UserInvalid];

    /// <summary>How far the user interacted (Blink's InteractedState): :user-valid and :user-invalid need an edit and a blur (or a change event).</summary>
    enum Interaction { None, Edited, EditedAndBlurred }

    string? _value;
    bool _dirtyValue, _checked, _dirtyChecked, _indeterminate, _selected, _dirtySelected;
    Interaction _interaction;
    bool _forceUserValid, _lastChangeWasUserEdit;
    // The value when the user first edited since the last change event (null: no pending change).
    string? _valueBeforeFirstUserEdit;
    string _customValidity = "";

    // ---------- Types ----------

    /// <summary>An input's type: the type attribute lowercased, "text" when missing or unknown ("" for other elements).</summary>
    public string InputType
    {
        get
        {
            if (Tag != "input")
                return "";
            var type = GetAttribute("type")?.Trim().ToLowerInvariant();
            return type is not null && InputTypes.Contains(type) ? type : "text";
        }
    }

    /// <summary>A control text is typed into: textarea, and text, search, tel, url, email, password and number inputs.</summary>
    public bool IsTextField => Tag == "textarea" || (Tag == "input" && InputType is "text" or "search" or "tel" or "url" or "email" or "password" or "number");

    public bool IsCheckable => Tag == "input" && InputType is "checkbox" or "radio";

    /// <summary>Buttons: &lt;button&gt; and submit, reset, button and image inputs.</summary>
    public bool IsButtonControl => Tag == "button" || (Tag == "input" && InputType is "submit" or "reset" or "button" or "image");

    /// <summary>The button's type (submit, reset or button) for &lt;button&gt;; the input type for inputs.</summary>
    public string ButtonType => Tag == "button"
        ? (GetAttribute("type")?.Trim().ToLowerInvariant() is "reset" or "button" ? GetAttribute("type")!.Trim().ToLowerInvariant() : "submit")
        : InputType;

    public bool IsSubmitButton => (Tag == "button" && ButtonType == "submit") || (Tag == "input" && InputType is "submit" or "image");

    enum ValueMode { Value, Default, DefaultOn, Filename }

    ValueMode Mode => InputType switch
    {
        "hidden" or "submit" or "image" or "reset" or "button" => ValueMode.Default,
        "checkbox" or "radio" => ValueMode.DefaultOn,
        "file" => ValueMode.Filename,
        _ => ValueMode.Value,
    };

    // ---------- Value ----------

    /// <summary>
    /// The control's value (HTMLInputElement/TextAreaElement/SelectElement/OptionElement.value):
    /// for inputs in value mode the dirty value or else the value attribute, sanitized for the type.
    /// Setting it makes the value dirty and fires no events.
    /// </summary>
    public string Value
    {
        get
        {
            switch (Tag)
            {
                case "input":
                    switch (Mode)
                    {
                        case ValueMode.Value:
                            return _dirtyValue ? _value ?? "" : Sanitize(GetAttribute("value") ?? "");
                        case ValueMode.DefaultOn:
                            return GetAttribute("value") ?? "on";
                        case ValueMode.Filename:
                            return "";
                        default:
                            return GetAttribute("value") ?? "";
                    }
                case "textarea":
                    return _dirtyValue ? _value ?? "" : DefaultValue;
                case "select":
                    return SelectedIndex is var index and >= 0 ? Options[index].Value : "";
                case "option":
                    return GetAttribute("value") ?? StripAndCollapse(TextContent);
                case "button":
                    return GetAttribute("value") ?? "";
                default:
                    return GetAttribute("value") ?? "";
            }
        }
        set
        {
            switch (Tag)
            {
                case "input" when Mode == ValueMode.Value:
                case "textarea":
                {
                    var before = Value;
                    SetValueInternal(value, userEdit: false);
                    // A new value from script puts the caret at its end (and ends the typing run).
                    if (Value != before)
                        ValueReplaced();
                    break;
                }
                case "select":
                {
                    var options = Options;
                    var found = false;
                    foreach (var option in options)
                    {
                        var match = !found && option.Value == value;
                        option.SetSelectedness(match);
                        found |= match;
                    }
                    break;
                }
                default:
                    SetAttribute("value", value);
                    break;
            }
        }
    }

    /// <summary>
    /// What a text field shows and edits (Blink's inner editor value): the value, except in a number
    /// field holding text that isn't a number yet (its value is then "").
    /// </summary>
    public string EditorText => IsTextField ? _editorText ?? Value : "";

    string? _editorText;

    /// <summary>How far a single-line field's text is scrolled to keep the caret in view (px).</summary>
    internal float EditorScrollLeft { get; set; }

    /// <summary>
    /// The editor's text changed by the user: the value follows. A number field's value is its text
    /// read as an en-US number (Blink's ConvertFromVisibleValue: "12." is 12), "" with bad input.
    /// </summary>
    internal void SetEditorText(string text)
    {
        var standard = InputType == "number" ? ConvertFromVisibleNumber(text) : text;
        var sanitized = Tag == "input" ? Sanitize(standard) : standard;
        var bad = InputType == "number" && standard.Length > 0 && !IsValidNumber(standard);
        var states = EditorText != text || BadInput != bad;
        BadInput = bad;
        SetValueInternal(sanitized, userEdit: true, keepEditor: true);
        _editorText = text == Value ? null : text;
        if (states)
            FormStateChanged(ValueStates);
        if (Tag == "textarea")
            Document?.TextAreaTextChanged(this);
    }

    /// <summary>Locale::ConvertFromLocalizedNumber for en-US: spaces dropped, a leading minus kept, a trailing full stop dropped; anything else unchanged.</summary>
    static string ConvertFromVisibleNumber(string visible)
    {
        if (visible.Length == 0 || visible.Contains('e') || visible.Contains('E'))
            return visible;
        var input = string.Concat(visible.Where(c => c is not (' ' or '\t' or '\n' or '\f' or '\r')));
        if (input.Length == 0)
            return input;
        var negative = input[0] == '-';
        var start = negative ? 1 : 0;
        if (!negative && input.Length - start >= 2 && input[start] == '+')
            start++;
        var builder = new System.Text.StringBuilder();
        if (negative)
            builder.Append('-');
        var separators = 0;
        for (var i = start; i < input.Length; i++)
        {
            var c = input[i];
            if (c == '.')
            {
                separators++;
                builder.Append('.');
            }
            else if (char.IsAsciiDigit(c))
                builder.Append(c);
            else
                return input;
        }
        var converted = builder.ToString();
        if (converted.Length >= 2 && converted.EndsWith('.') && separators < 2)
            converted = converted[..^1];
        return converted;
    }

    /// <summary>
    /// Sets the value (sanitized), dirty. A change by the user (typing, stepping) remembers the value
    /// before it, so a change event follows when the field commits (Blink's SetNonAttributeValueByUserEdit).
    /// </summary>
    internal void SetValueInternal(string value, bool userEdit, bool keepEditor = false)
    {
        var sanitized = Tag == "input" ? Sanitize(value) : value.Replace("\r\n", "\n").Replace('\r', '\n');
        var old = Value;
        if (userEdit)
            _valueBeforeFirstUserEdit ??= old;
        if (!keepEditor)
        {
            _editorText = null;
            BadInput = false;
        }
        _value = sanitized;
        _dirtyValue = true;
        _lastChangeWasUserEdit = userEdit;
        if (userEdit && _valueBeforeFirstUserEdit == sanitized)
            _valueBeforeFirstUserEdit = null;
        if (old != sanitized)
            FormStateChanged(ValueStates);
        if (Tag == "textarea")
            Document?.TextAreaTextChanged(this);
        ValueChanged?.Invoke(this);
    }

    // ---------- Input and change events ----------

    /// <summary>Fires input (bubbling, as the user's edits do).</summary>
    internal void DispatchInputEvent() => DispatchEvent(new UiEvent("input", bubbles: true, cancelable: false) { IsTrusted = true });

    /// <summary>Fires change; after the user's edits it also starts :user-valid/:user-invalid matching.</summary>
    internal void DispatchChangeEvent()
    {
        if (_interaction >= Interaction.Edited)
            SetUserHasEditedTheFieldAndBlurred();
        DispatchEvent(new UiEvent("change", bubbles: true, cancelable: false) { IsTrusted = true });
    }

    /// <summary>A text control commits (blur, Enter, a step): change fires if the value differs from before the user's first edit.</summary>
    internal void DispatchFormControlChangeEvent()
    {
        if (_valueBeforeFirstUserEdit is { } before && before != Value)
        {
            _valueBeforeFirstUserEdit = null;
            DispatchChangeEvent();
        }
        else
            _valueBeforeFirstUserEdit = null;
    }

    /// <summary>Checkboxes and radios after a click: input then change (only when connected; a radio being unchecked sends none).</summary>
    internal void DispatchInputAndChangeEventIfNeeded()
    {
        if (Document is null || (InputType == "radio" && !Checked))
            return;
        DispatchInputEvent();
        DispatchChangeEvent();
    }

    /// <summary>Raised when the value changes (the text editor follows it).</summary>
    internal event Action<UiElement>? ValueChanged;

    /// <summary>The default value: the value attribute (inputs), the text content (textarea).</summary>
    public string DefaultValue
    {
        get => Tag == "textarea" ? TextContent.StartsWith('\n') ? TextContent[1..] : TextContent : GetAttribute("value") ?? "";
        set
        {
            if (Tag == "textarea")
                TextContent = value;
            else
                SetAttribute("value", value);
        }
    }

    /// <summary>Chromium's value sanitization for the input type.</summary>
    string Sanitize(string value)
    {
        switch (InputType)
        {
            case "text" or "search" or "tel" or "password":
                return value.Replace("\r", "").Replace("\n", "");
            case "url" or "email":
                return value.Replace("\r", "").Replace("\n", "").Trim(' ', '\t', '\n', '\f', '\r');
            case "number":
                return value.Length == 0 || IsValidNumber(value) ? value : "";
            case "range":
            {
                var range = StepRangeOf(rejectAny: true);
                var number = ParseNumber(value) ?? range.DefaultValue;
                return Serialize(range.Clamp(number));
            }
            case "color":
            {
                var v = value.Trim();
                return v.Length == 7 && v[0] == '#' && v[1..].All(Uri.IsHexDigit) ? v.ToLowerInvariant() : "#000000";
            }
            default:
                return value;
        }
    }

    static string StripAndCollapse(string text) =>
        string.Join(' ', text.Split((char[])[' ', '\t', '\n', '\f', '\r'], StringSplitOptions.RemoveEmptyEntries));

    // ---------- Numbers ----------

    /// <summary>HTML's rules for parsing floating-point numbers (Blink's ParseToDoubleForNumberType): no leading +, no trailing dot, finite.</summary>
    internal static bool IsValidNumber(string text) => ParseNumber(text) is not null;

    internal static decimal? ParseNumber(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return null;
        var first = text[0];
        if (first != '-' && first != '.' && !char.IsAsciiDigit(first))
            return null;
        if (text.EndsWith('.') || char.IsWhiteSpace(text[^1]))
            return null;
        if (!double.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var number) || !double.IsFinite(number))
            return null;
        if (decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var exact))
            return exact == 0 ? 0 : exact;
        try
        {
            return (decimal)number;
        }
        catch (OverflowException)
        {
            return number < 0 ? decimal.MinValue : decimal.MaxValue;
        }
    }

    /// <summary>Blink's SerializeForNumberType: the shortest plain form.</summary>
    internal static string Serialize(decimal value)
    {
        var text = value.ToString("0.############################", CultureInfo.InvariantCulture);
        return text == "-0" ? "0" : text;
    }

    /// <summary>Blink's StepRange (in decimal arithmetic, as Blink's Decimal).</summary>
    internal readonly record struct StepRange(decimal Minimum, decimal Maximum, decimal Step, decimal StepBase, bool HasStep, bool HasMin, bool HasMax)
    {
        static decimal Round(decimal v) => Math.Round(v, MidpointRounding.AwayFromZero);

        public decimal RoundByStep(decimal value, decimal @base) => @base + Round((value - @base) / Step) * Step;

        public decimal Clamp(decimal value)
        {
            var inRange = Math.Max(Minimum, Math.Min(value, Maximum));
            if (!HasStep)
                return inRange;
            var rounded = RoundByStep(inRange, StepBase);
            var clamped = rounded > Maximum ? rounded - Step : rounded < Minimum ? rounded + Step : rounded;
            return clamped < Minimum || clamped > Maximum ? inRange : clamped;
        }

        public decimal DefaultValue => Clamp((Minimum + Maximum) / 2);

        public bool StepMismatch(decimal value)
        {
            if (!HasStep)
                return false;
            var distance = Math.Abs(value - StepBase);
            var remainder = Math.Abs(distance - Step * Round(distance / Step));
            return remainder > 0 && remainder < Step;
        }

        public decimal AlignValueForStep(decimal current, decimal value) =>
            value >= 1e21m ? value : StepMismatch(current) ? value : RoundByStep(value, StepBase);

        public decimal? StepSnappedMaximum
        {
            get
            {
                if (!HasMax)
                    return Maximum;
                var divided = (Maximum - StepBase) / Step;
                var aligned = divided == Math.Floor(divided) ? Maximum : StepBase + Math.Floor(divided) * Step;
                if (aligned > Maximum)
                    aligned -= Step;
                return aligned < Minimum ? null : aligned;
            }
        }
    }

    /// <summary>The element's step range (number and range inputs).</summary>
    internal StepRange StepRangeOf(bool rejectAny)
    {
        var range = InputType == "range";
        var stepText = GetAttribute("step");
        var any = string.Equals(stepText?.Trim(), "any", StringComparison.OrdinalIgnoreCase);
        decimal step = 1;
        var hasStep = true;
        if (any)
        {
            if (rejectAny)
                hasStep = false;
        }
        else if (ParseNumber(stepText) is { } parsed && parsed > 0)
            step = parsed;
        var stepBase = ParseNumber(GetAttribute("min")) ?? ParseNumber(GetAttribute("value")) ?? 0;
        var min = ParseNumber(GetAttribute("min"));
        var max = ParseNumber(GetAttribute("max"));
        if (range)
        {
            var minimum = min ?? 0;
            var maximum = max ?? 100;
            if (maximum < minimum)
                maximum = minimum;
            return new StepRange(minimum, maximum, step, stepBase, hasStep, true, true);
        }
        return new StepRange(min ?? decimal.MinValue, max ?? decimal.MaxValue, step, stepBase, hasStep, min is not null, max is not null);
    }

    /// <summary>valueAsNumber (number and range inputs; NaN otherwise or when the value isn't a number).</summary>
    public double ValueAsNumber
    {
        get => InputType is "number" or "range" && ParseNumber(Value) is { } n ? (double)n : double.NaN;
        set
        {
            if (InputType is not ("number" or "range"))
                throw new InvalidOperationException("This input element does not support Number values.");
            Value = double.IsFinite(value) ? Serialize((decimal)value) : "";
        }
    }

    bool IsSteppable => InputType is "number" or "range";

    /// <summary>HTMLInputElement.stepUp (no events).</summary>
    public void StepUp(int count = 1) => ApplyStep(count, fromUser: false);

    public void StepDown(int count = 1) => ApplyStep(-count, fromUser: false);

    /// <summary>
    /// A step by the user (spin buttons, arrow keys; Blink's StepUpFromLayoutObject): from an empty
    /// value it starts silently at 0 (or near the limit it steps away from), and out-of-range
    /// values jump to the limit. Returns whether the step changed the value (the caller fires
    /// input, or change when the field isn't focused).
    /// </summary>
    internal bool StepFromUser(int count)
    {
        if (!IsSteppable || count == 0)
            return false;
        var range = StepRangeOf(rejectAny: false);
        if (!range.HasStep)
            return false;
        var step = range.Step;
        var sign = Math.Sign(count);
        var current = ParseNumber(Value);
        if (current is null)
        {
            decimal start = 0;
            var nextDiff = step * count;
            // (Without min or max the limits are out of reach.)
            if (range.HasMin && start < range.Minimum - nextDiff)
                start = range.Minimum - nextDiff;
            if (range.HasMax && start > range.Maximum - nextDiff)
                start = range.Maximum - nextDiff;
            SetValueInternal(Serialize(start), userEdit: false);
            current = start;
        }
        var before = Value;
        if ((sign > 0 && current < range.Minimum) || (sign < 0 && current > range.Maximum))
        {
            SetValueInternal(Serialize(sign > 0 ? range.Minimum : range.Maximum), userEdit: true);
            if (Value != before)
                ValueReplaced();
            return Value != before;
        }
        if ((sign > 0 && current >= range.Maximum) || (sign < 0 && current <= range.Minimum))
            return false;
        if (StepTarget(range, count, current.Value, wasInvalid: false) is not { } target)
            return false;
        // The step is offered first (beforeinput at the focused element, cancelable).
        var data = Serialize(target);
        var at = Document?.FocusedElement is { } focused && focused.Document == Document ? focused : Document?.Body;
        if (at is not null && !at.DispatchEvent(new UiInputEvent("beforeinput", cancelable: true) { InputType = "insertText", Data = data, IsTrusted = true }))
            return false;
        SetValueInternal(data, userEdit: true);
        if (Value != before)
            ValueReplaced();
        return Value != before;
    }

    /// <summary>A user's step and its events (Blink's kDispatchChangeEvent): input while the field has focus, else change.</summary>
    internal void StepFromUserWithEvents(int count)
    {
        if (!StepFromUser(count))
            return;
        if (IsFocused)
            DispatchInputEvent();
        else
            DispatchFormControlChangeEvent();
    }

    void ApplyStep(int count, bool fromUser)
    {
        if (!IsSteppable)
            throw new InvalidOperationException("This form element is not steppable.");
        var range = StepRangeOf(rejectAny: !fromUser);
        if (!range.HasStep)
            throw new InvalidOperationException("This form element does not have an allowed value step.");
        var parsed = ParseNumber(Value);
        if (StepTarget(range, count, parsed ?? 0, parsed is null) is { } value)
            SetValueInternal(Serialize(value), userEdit: fromUser);
    }

    /// <summary>Where a step of <paramref name="count"/> leads (Blink's InputType::ApplyStep), or null when it doesn't apply.</summary>
    decimal? StepTarget(StepRange range, int count, decimal current, bool wasInvalid)
    {
        if (range.Minimum > range.Maximum || range.StepSnappedMaximum is not { } alignedMaximum)
            return null;
        var any = string.Equals(GetAttribute("step")?.Trim(), "any", StringComparison.OrdinalIgnoreCase);
        var value = current;
        // An off-step value first moves to the step in that direction (which counts as one step).
        if (!any && range.StepMismatch(current))
        {
            if (count < 0)
            {
                value = range.StepBase + Math.Floor((value - range.StepBase) / range.Step) * range.Step;
                count++;
            }
            else if (count > 0)
            {
                value = range.StepBase + Math.Ceiling((value - range.StepBase) / range.Step) * range.Step;
                count--;
            }
        }
        value += range.Step * count;
        if (!any)
            value = range.AlignValueForStep(current, value);
        if (value < range.Minimum)
            value = range.StepBase + Math.Ceiling((range.Minimum - range.StepBase) / range.Step) * range.Step;
        if (value > range.Maximum)
            value = alignedMaximum;
        if (!wasInvalid && ((count < 0 && current < value) || (count > 0 && current > value)))
            return null;
        return value;
    }

    // ---------- Checkedness ----------

    /// <summary>Checkboxes and radios: checked (the dirty checkedness, else the checked attribute). Setting it unchecks the rest of a radio group.</summary>
    public bool Checked
    {
        get => _dirtyChecked ? _checked : HasAttribute("checked");
        set => SetCheckedness(value, dirty: true);
    }

    public bool DefaultChecked
    {
        get => HasAttribute("checked");
        set => ToggleAttribute("checked", value);
    }

    internal void SetCheckedness(bool value, bool dirty)
    {
        var old = Checked;
        _checked = value;
        if (dirty)
            _dirtyChecked = true;
        else if (!_dirtyChecked)
            _dirtyChecked = HasAttribute("checked") != value;
        if (value && InputType == "radio")
            foreach (var other in RadioGroup())
                if (other != this && other.Checked)
                {
                    other._checked = false;
                    other._dirtyChecked = true;
                    other.FormStateChanged(CheckedStates);
                    other.CheckedChanged?.Invoke(other);
                }
        if (old != value)
        {
            FormStateChanged(CheckedStates);
            if (InputType == "radio")
                foreach (var other in RadioGroup())
                    if (other != this)
                        other.FormStateChanged(ValidityStates);
            CheckedChanged?.Invoke(this);
        }
    }

    /// <summary>Raised when checkedness changes.</summary>
    internal event Action<UiElement>? CheckedChanged;

    /// <summary>checkbox.indeterminate (only shown, never submitted).</summary>
    public bool Indeterminate
    {
        get => _indeterminate;
        set
        {
            if (_indeterminate == value)
                return;
            _indeterminate = value;
            FormStateChanged([PseudoClassKind.Indeterminate]);
        }
    }

    /// <summary>The radios in this one's group: same name, same form owner, in the same document (or detached tree).</summary>
    public IReadOnlyList<UiElement> RadioGroup()
    {
        if (InputType != "radio")
            return [];
        var name = GetAttribute("name");
        if (string.IsNullOrEmpty(name))
            return [this];
        var form = Form;
        UiElement root = this;
        while (root.Parent is { } up)
            root = up;
        return root.DescendantsAndSelf().Where(e => e.InputType == "radio" && e.GetAttribute("name") == name && e.Form == form).ToList();
    }

    IEnumerable<UiElement> DescendantsAndSelf()
    {
        yield return this;
        foreach (var descendant in Descendants())
            yield return descendant;
    }

    /// <summary>:checked: checked checkboxes and radios, selected options.</summary>
    public bool IsChecked => Tag == "option" ? Selected : IsCheckable && Checked;

    internal bool MatchesIndeterminate => (Tag == "input" && InputType == "checkbox" && _indeterminate)
        || (Tag == "input" && InputType == "radio" && !RadioGroup().Any(r => r.Checked))
        || (Tag == "progress" && ParseNumber(GetAttribute("value")) is null);

    // ---------- Select and options ----------

    /// <summary>A select's options (in tree order, optgroups included).</summary>
    public IReadOnlyList<UiElement> Options => Tag == "select" ? Descendants().Where(e => e.Tag == "option").ToList() : [];

    UiElement? OwnerSelect
    {
        get
        {
            for (var p = Parent; p is not null; p = p.Parent)
            {
                if (p.Tag == "select")
                    return p;
                if (p.Tag != "optgroup")
                    return null;
            }
            return null;
        }
    }

    bool OptionSelectedness => _dirtySelected ? _selected : HasAttribute("selected");

    /// <summary>option.selected. In a single select the last selected option wins, else the first enabled one is shown selected.</summary>
    public bool Selected
    {
        get
        {
            if (Tag != "option")
                return false;
            if (OwnerSelect is { } select && !select.HasAttribute("multiple"))
                return select.SelectedIndex is var index and >= 0 && select.Options[index] == this;
            return OptionSelectedness;
        }
        set => SetSelectedness(value);
    }

    public bool DefaultSelected
    {
        get => HasAttribute("selected");
        set => ToggleAttribute("selected", value);
    }

    internal void SetSelectedness(bool value)
    {
        if (Tag != "option")
            return;
        var select = OwnerSelect;
        var before = select?.SelectedIndex;
        if (value && select is not null && !select.HasAttribute("multiple"))
            foreach (var other in select.Options)
                if (other != this)
                {
                    other._selected = false;
                    other._dirtySelected = true;
                }
        _selected = value;
        _dirtySelected = true;
        if (select is not null && before != select.SelectedIndex)
            select.SelectionChanged();
        else
            FormStateChanged([PseudoClassKind.Checked]);
    }

    void SelectionChanged()
    {
        LastOnChangeOption = SelectedOption;
        foreach (var option in Options)
            option.FormStateChanged([PseudoClassKind.Checked]);
        FormStateChanged(ValidityStates);
        ValueChanged?.Invoke(this);
    }

    /// <summary>
    /// The selected option's index in a select (-1 for none): for a single select, the last option
    /// with selectedness, else the first option that isn't disabled.
    /// </summary>
    public int SelectedIndex
    {
        get
        {
            if (Tag != "select")
                return -1;
            var options = Options;
            var multiple = HasAttribute("multiple");
            var last = -1;
            for (var i = 0; i < options.Count; i++)
                if (options[i].OptionSelectedness)
                {
                    if (multiple)
                        return i;
                    last = i;
                }
            if (last >= 0 || multiple || DisplaySize > 1)
                return last;
            for (var i = 0; i < options.Count; i++)
                if (!options[i].IsOptionDisabled)
                    return i;
            return -1;
        }
        set
        {
            var options = Options;
            var before = SelectedIndex;
            for (var i = 0; i < options.Count; i++)
            {
                options[i]._selected = i == value;
                options[i]._dirtySelected = true;
            }
            if (before != SelectedIndex)
                SelectionChanged();
        }
    }

    /// <summary>The select's size attribute (1 for a drop-down).</summary>
    int DisplaySize => int.TryParse(GetAttribute("size"), out var size) && size > 0 ? size : HasAttribute("multiple") ? 4 : 1;

    public UiElement? SelectedOption => Tag == "select" && SelectedIndex is var i and >= 0 ? Options[i] : null;

    bool IsOptionDisabled => HasAttribute("disabled") || (Parent is { Tag: "optgroup" } group && group.HasAttribute("disabled"));

    /// <summary>An option the user can't pick (disabled, or in a disabled group).</summary>
    internal bool IsOptionDisabledForUser => Tag == "option" && IsOptionDisabled;

    /// <summary>The select this option belongs to.</summary>
    internal UiElement? SelectOwner => OwnerSelect;

    /// <summary>A select's list items (Blink's GetListItems): its options and groups, in tree order.</summary>
    internal List<UiElement> ListItems => Tag == "select" ? Descendants().Where(e => e.Tag is "option" or "optgroup").ToList() : [];

    /// <summary>A drop-down select (one row, not multiple): a menu list.</summary>
    internal bool UsesMenuList => Tag == "select" && !HasAttribute("multiple") && DisplaySize <= 1;

    /// <summary>The option's label (Blink's DisplayLabel): the label attribute if it isn't empty, else its text with spaces collapsed.</summary>
    public string OptionLabel => GetAttribute("label") is { Length: > 0 } label ? label : StripAndCollapse(TextContent);

    /// <summary>The option input and change were last fired for (or the selection last made): Blink's last_on_change_option_.</summary>
    internal UiElement? LastOnChangeOption { get; set; }

    /// <summary>
    /// An option came into this select (HTMLSelectElement::OptionInserted): a selected one is now the
    /// last change option (SelectOption), else, with none yet, the default selection is (ResetToDefaultSelection).
    /// </summary>
    internal void OptionInserted(UiElement option)
    {
        if (option.OptionSelectedness)
            LastOnChangeOption = option;
        else if (LastOnChangeOption is null)
            LastOnChangeOption = SelectedOption;
    }

    /// <summary>
    /// An option left this select (HTMLSelectElement::OptionRemoved): a single select whose selected
    /// option it was, or that had no last change option, takes the default selection.
    /// </summary>
    internal void OptionRemoved(UiElement option, bool wasSelected)
    {
        if (!HasAttribute("multiple") && (wasSelected || LastOnChangeOption is null))
            LastOnChangeOption = SelectedOption;
        if (LastOnChangeOption == option)
            LastOnChangeOption = null;
    }

    // ---------- Form owner, labels, details ----------

    /// <summary>The form the control belongs to: its form attribute's form, else the nearest form ancestor.</summary>
    public UiElement? Form
    {
        get
        {
            if (!IsFormControl && Tag is not ("output" or "object" or "img" or "label"))
                return null;
            if (GetAttribute("form") is { } id)
                return Document?.GetElementById(id) is { Tag: "form" } named ? named : null;
            for (var p = Parent; p is not null; p = p.Parent)
                if (p.Tag == "form")
                    return p;
            return null;
        }
    }

    /// <summary>A form's controls, in tree order.</summary>
    public IReadOnlyList<UiElement> Elements
    {
        get
        {
            if (Tag != "form")
                return [];
            UiElement root = this;
            while (root.Parent is { } up)
                root = up;
            return root.Descendants().Where(e => e.Tag is "button" or "fieldset" or "input" or "object" or "output" or "select" or "textarea" && e.Form == this).ToList();
        }
    }

    static bool IsLabelable(UiElement e) => e.Tag is "button" or "meter" or "output" or "progress" or "select" or "textarea" || (e.Tag == "input" && e.InputType != "hidden");

    /// <summary>A label's control: the element its for attribute names, else its first labelable descendant.</summary>
    public UiElement? Control
    {
        get
        {
            if (Tag != "label")
                return null;
            if (GetAttribute("for") is { } id)
                return Document?.GetElementById(id) is { } target && IsLabelable(target) ? target : null;
            return Descendants().FirstOrDefault(IsLabelable);
        }
    }

    /// <summary>The labels of a labelable element.</summary>
    public IReadOnlyList<UiElement> Labels
    {
        get
        {
            if (!IsLabelable(this) || Document is not { } document)
                return [];
            return document.AllElements().Where(e => e.Tag == "label" && e.Control == this).ToList();
        }
    }

    /// <summary>details.open (and dialog.open).</summary>
    public bool Open
    {
        get => HasAttribute("open");
        set => ToggleAttribute("open", value);
    }

    // ---------- States ----------

    public bool ReadOnly
    {
        get => HasAttribute("readonly");
        set => ToggleAttribute("readonly", value);
    }

    public bool Required
    {
        get => HasAttribute("required");
        set => ToggleAttribute("required", value);
    }

    bool SupportsReadOnly => Tag == "textarea" || (Tag == "input" && InputType is "text" or "search" or "tel" or "url" or "email" or "password" or "number" or "date" or "month" or "week" or "time" or "datetime-local");

    bool SupportsRequired => Tag is "select" or "textarea" || (Tag == "input" && InputType is not ("range" or "color" or "hidden" or "submit" or "image" or "reset" or "button"));

    /// <summary>:read-write: editable text controls (and contenteditable elements).</summary>
    internal bool IsReadWrite =>
        (SupportsReadOnly && !ReadOnly && !IsDisabled) || (GetAttribute("contenteditable") is { } editable && editable.ToLowerInvariant() is "" or "true" or "plaintext-only");

    internal bool IsRequiredControl => SupportsRequired && Required;

    internal bool IsOptionalControl => (Tag is "select" or "textarea" || Tag == "input") && !IsRequiredControl;

    /// <summary>The control takes part in constraint validation (willValidate).</summary>
    public bool WillValidate =>
        (Tag is "select" or "textarea" or "button" or "input") && !IsDisabled && !(Tag == "button" && ButtonType != "submit")
        && !(Tag == "input" && InputType is "hidden" or "reset" or "button" or "image") && !(SupportsReadOnly && ReadOnly)
        && !Ancestors().Any(a => a.Tag == "datalist");

    /// <summary>:default: the form's default button, checked-by-default checkboxes and radios, selected-by-default options.</summary>
    internal bool MatchesDefault =>
        (IsCheckable && HasAttribute("checked")) || (Tag == "option" && HasAttribute("selected"))
        || (IsSubmitButton && Form is { } form && form.DefaultButton == this);

    /// <summary>A form's default button: its first submit button in tree order.</summary>
    public UiElement? DefaultButton => Tag == "form" ? Elements.FirstOrDefault(e => e.IsSubmitButton) : null;

    internal bool MatchesValidity(bool valid)
    {
        if (Tag is "form" or "fieldset")
        {
            var controls = Tag == "form" ? Elements : Descendants().Where(e => e.WillValidate).ToList();
            var allValid = controls.All(c => !c.WillValidate || c.Validity.Valid);
            return allValid == valid;
        }
        if (!WillValidate)
            return false;
        return Validity.Valid == valid;
    }

    internal bool MatchesUserValidity(bool valid) =>
        (_interaction == Interaction.EditedAndBlurred || _forceUserValid) && WillValidate && Validity.Valid == valid;

    /// <summary>The user edited the control (typing, a click on a checkbox, a key on a select).</summary>
    internal void SetUserHasEditedTheField()
    {
        if (_interaction < Interaction.Edited)
            _interaction = Interaction.Edited;
    }

    /// <summary>The user edited and then left the control (or a change event fired): :user-valid/:user-invalid match from now on.</summary>
    internal void SetUserHasEditedTheFieldAndBlurred()
    {
        if (_interaction >= Interaction.EditedAndBlurred)
            return;
        _interaction = Interaction.EditedAndBlurred;
        FormStateChanged(ValidityStates);
    }

    /// <summary>A submit attempt: the form's controls match :user-valid/:user-invalid even untouched.</summary>
    internal void ForceUserValid()
    {
        _forceUserValid = true;
        FormStateChanged(ValidityStates);
    }

    void ClearUserHasEditedTheField()
    {
        _interaction = Interaction.None;
        _forceUserValid = false;
        FormStateChanged(ValidityStates);
    }

    internal bool MatchesRange(bool inRange)
    {
        if (!IsSteppable)
            return false;
        var range = StepRangeOf(rejectAny: true);
        if (!range.HasMin && !range.HasMax)
            return false;
        var validity = Validity;
        var outOfRange = validity.RangeUnderflow || validity.RangeOverflow;
        return inRange ? !outOfRange : outOfRange;
    }

    /// <summary>Restyles what depends on this control's form state (and its form's and fieldsets' :valid/:invalid).</summary>
    internal void FormStateChanged(PseudoClassKind[] states)
    {
        if (Document is not { } document)
            return;
        document.StateChanged(this, states);
        if (states.Contains(PseudoClassKind.Valid))
            for (var p = Parent; p is not null; p = p.Parent)
                if (p.Tag is "form" or "fieldset")
                    document.StateChanged(p, ValidityStates);
        if (Form is { } form && !Ancestors().Contains(form))
            document.StateChanged(form, ValidityStates);
    }

    /// <summary>Form attributes changed: the states and values that follow them.</summary>
    internal void FormAttributeChanged(string name)
    {
        switch (name)
        {
            case "checked" when IsCheckable && !_dirtyChecked:
                if (HasAttribute("checked") && InputType == "radio")
                    SetCheckedness(true, dirty: false);
                break;
            case "selected" when Tag == "option" && !_dirtySelected:
                if (OwnerSelect is { } select)
                    select.SelectionChanged();
                break;
            case "value" when Tag == "input" && !_dirtyValue:
                ValueChanged?.Invoke(this);
                break;
            case "type" when Tag == "input":
                ValueChanged?.Invoke(this);
                break;
            case "open" when Tag == "details":
                DetailsOpenChanged();
                break;
        }
    }

    // ---------- Constraint validation ----------

    /// <summary>What the control fails (Chromium's checks for the types the engine has).</summary>
    public ValidityState Validity
    {
        get
        {
            if (!WillValidate)
                return default;
            var custom = _customValidity.Length > 0;
            var value = Value;
            switch (Tag)
            {
                case "select":
                {
                    var missing = false;
                    if (Required && !HasAttribute("multiple") && DisplaySize == 1)
                    {
                        var index = SelectedIndex;
                        // The placeholder option: the first option with an empty value, when it is the first child.
                        missing = index < 0 || (index == 0 && Options[0].Value.Length == 0 && Options[0].Parent == this);
                    }
                    return new ValidityState(missing, false, false, false, false, false, false, false, false, custom);
                }
                case "textarea":
                {
                    var length = value.Length;
                    var max = MaxLength;
                    var min = MinLength;
                    var tooLong = _lastChangeWasUserEdit && max >= 0 && length > max;
                    var tooShort = _lastChangeWasUserEdit && min > 0 && length > 0 && length < min;
                    return new ValidityState(Required && !ReadOnly && value.Length == 0, false, false, tooLong, tooShort, false, false, false, false, custom);
                }
                case "input":
                {
                    var type = InputType;
                    if (type == "checkbox")
                        return new ValidityState(Required && !Checked, false, false, false, false, false, false, false, false, custom);
                    if (type == "radio")
                    {
                        var group = RadioGroup();
                        var missing = group.Any(r => r.Required) && !group.Any(r => r.Checked);
                        return new ValidityState(missing, false, false, false, false, false, false, false, false, custom);
                    }
                    var valueMissing = SupportsRequired && Required && value.Length == 0 && !(type == "number" && BadInput);
                    var typeMismatch = value.Length > 0 && ((type == "email" && !IsValidEmailList(value)) || (type == "url" && !Uri.TryCreate(value, UriKind.Absolute, out _)));
                    var patternMismatch = value.Length > 0 && type is "text" or "search" or "tel" or "url" or "email" or "password"
                        && GetAttribute("pattern") is { } pattern && !MatchesPattern(value, pattern);
                    var length = value.Length;
                    var textual = type is "text" or "search" or "tel" or "url" or "email" or "password";
                    var tooLong = textual && _lastChangeWasUserEdit && MaxLength is >= 0 and var max && length > max;
                    var tooShort = textual && _lastChangeWasUserEdit && MinLength is > 0 and var min && length > 0 && length < min;
                    bool underflow = false, overflow = false, stepMismatch = false;
                    if (IsSteppable && ParseNumber(value) is { } number)
                    {
                        var range = StepRangeOf(rejectAny: true);
                        underflow = range.HasMin && number < range.Minimum;
                        overflow = range.HasMax && number > range.Maximum;
                        stepMismatch = range.StepMismatch(number);
                    }
                    return new ValidityState(valueMissing, typeMismatch, patternMismatch, tooLong, tooShort, underflow, overflow, stepMismatch, type == "number" && BadInput, custom);
                }
                default:
                    return new ValidityState(false, false, false, false, false, false, false, false, false, custom);
            }
        }
    }

    /// <summary>The number field's text isn't a number (set by the editor).</summary>
    internal bool BadInput { get; set; }

    public int MaxLength
    {
        get => int.TryParse(GetAttribute("maxlength"), NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : -1;
        set => SetAttribute("maxlength", value.ToString(CultureInfo.InvariantCulture));
    }

    public int MinLength
    {
        get => int.TryParse(GetAttribute("minlength"), NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : -1;
        set => SetAttribute("minlength", value.ToString(CultureInfo.InvariantCulture));
    }

    static bool IsValidEmailList(string value)
    {
        static bool Valid(string address)
        {
            var at = address.IndexOf('@');
            if (at <= 0 || at != address.LastIndexOf('@') || at == address.Length - 1)
                return false;
            var local = address[..at];
            var domain = address[(at + 1)..];
            const string localChars = "!#$%&'*+/=?^_`{|}~.-";
            if (!local.All(c => char.IsAsciiLetterOrDigit(c) || localChars.Contains(c)))
                return false;
            foreach (var label in domain.Split('.'))
                if (label.Length == 0 || label.Length > 63 || label[0] == '-' || label[^1] == '-' || !label.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
                    return false;
            return true;
        }
        return value.Split(',').All(a => Valid(a.Trim()));
    }

    static bool MatchesPattern(string value, string pattern)
    {
        try
        {
            return System.Text.RegularExpressions.Regex.IsMatch(value, "^(?:" + pattern + ")$");
        }
        catch (ArgumentException)
        {
            return true;
        }
    }

    /// <summary>setCustomValidity: a message makes the control invalid until cleared with "".</summary>
    public void SetCustomValidity(string message)
    {
        _customValidity = message ?? "";
        FormStateChanged(ValidityStates);
    }

    /// <summary>validationMessage: Chromium's English message for the first failed constraint.</summary>
    public string ValidationMessage
    {
        get
        {
            if (!WillValidate)
                return "";
            var v = Validity;
            if (v.CustomError)
                return _customValidity;
            if (v.ValueMissing)
                return Tag switch
                {
                    "select" => "Please select an item in the list.",
                    _ => InputType switch
                    {
                        "checkbox" => "Please check this box if you want to proceed.",
                        "radio" => "Please select one of these options.",
                        "file" => "Please select a file.",
                        _ => "Please fill out this field.",
                    },
                };
            if (v.BadInput)
                return "Please enter a number.";
            if (v.TypeMismatch)
                return InputType == "email" ? EmailMessage(Value) : "Please enter a URL.";
            if (v.PatternMismatch)
                return GetAttribute("title") is { Length: > 0 } title ? "Please match the requested format.\n" + title : "Please match the requested format.";
            if (v.TooLong)
                return $"Please shorten this text to {MaxLength} characters or less (you are currently using {Value.Length} characters).";
            if (v.TooShort)
                return Value.Length == 1
                    ? $"Please lengthen this text to {MinLength} characters or more (you are currently using 1 character)."
                    : $"Please lengthen this text to {MinLength} characters or more (you are currently using {Value.Length} characters).";
            var range = StepRangeOf(rejectAny: true);
            if (v.RangeUnderflow)
                return range.HasMax && range.Minimum == range.Maximum ? $"Value must be {Serialize(range.Minimum)}." : $"Value must be greater than or equal to {Serialize(range.Minimum)}.";
            if (v.RangeOverflow)
                return range.HasMin && range.Minimum == range.Maximum ? $"Value must be {Serialize(range.Maximum)}." : $"Value must be less than or equal to {Serialize(range.Maximum)}.";
            if (v.StepMismatch && ParseNumber(Value) is { } number)
            {
                var low = range.StepBase + Math.Floor((number - range.StepBase) / range.Step) * range.Step;
                var high = low + range.Step;
                if ((range.HasMin && low < range.Minimum) || (range.HasMax && high > range.Maximum))
                {
                    var nearest = range.HasMin && low < range.Minimum ? high : low;
                    return $"Please enter a valid value. The nearest valid value is {Serialize(nearest)}.";
                }
                return $"Please enter a valid value. The two nearest valid values are {Serialize(low)} and {Serialize(high)}.";
            }
            return "";
        }
    }

    /// <summary>The message a tooltip shows for an invalid control (Blink's validation message without its second line; a custom one whole).</summary>
    internal string ToolTipValidationMessage =>
        WillValidate && Validity.CustomError ? _customValidity : ValidationMessage.Split('\n', 2)[0];

    static string EmailMessage(string value)
    {
        foreach (var address in value.Split(',').Select(a => a.Trim()))
        {
            var at = address.IndexOf('@');
            if (address.Length == 0)
                return "Please enter a non-empty email address.";
            if (at < 0)
                return $"Please include an '@' in the email address. '{address}' is missing an '@'.";
            if (at == 0)
                return $"Please enter a part followed by '@'. '{address}' is incomplete.";
            if (at == address.Length - 1)
                return $"Please enter a part following '@'. '{address}' is incomplete.";
        }
        return "Please enter an email address.";
    }

    /// <summary>checkValidity: fires "invalid" (cancelable) when invalid; returns validity. A form checks all its controls.</summary>
    public bool CheckValidity() => CheckValidity(out _);

    internal bool CheckValidity(out List<UiElement> unhandled)
    {
        unhandled = [];
        if (Tag == "form")
        {
            var valid = true;
            foreach (var control in Elements.ToList())
                if (control.WillValidate && !control.Validity.Valid)
                {
                    valid = false;
                    if (control.DispatchEvent(new UiEvent("invalid", bubbles: false, cancelable: true) { IsTrusted = true }))
                        unhandled.Add(control);
                }
            return valid;
        }
        if (!WillValidate || Validity.Valid)
            return true;
        if (DispatchEvent(new UiEvent("invalid", bubbles: false, cancelable: true) { IsTrusted = true }))
            unhandled.Add(this);
        return false;
    }

    /// <summary>reportValidity: as checkValidity, then the first invalid control is focused and its message shown (the document's validation bubble).</summary>
    public bool ReportValidity()
    {
        var valid = CheckValidity(out var unhandled);
        if (!valid && unhandled.Count > 0 && Document is { } document)
            document.ReportInvalid(unhandled[0]);
        return valid;
    }
}
