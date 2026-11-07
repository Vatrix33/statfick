using System.Globalization;
using Statventure.Records;
using Statventure.UI.Dom;

namespace Statventure.Editor.Form;

/// <summary>
/// The control of a value of one shape (P11; the table in Docs/Editor.md, "What exists"): leaves (a checkbox, a
/// number field with a slider between finite limits, a text field, a select for choices and enum members, 2–4
/// number fields for a vector, a swatch and <c>#rrggbb</c> for colours, a summary for what is read-only) and blocks
/// (a record's rows, a union's subtype and rows, a list's or map's rows with add, remove and move, a nullable's
/// "set" box before its value). <see cref="Head"/> is what shows on the value's own row; a block's
/// <see cref="Body"/> holds its nested rows. Each takes its value from the draft in <see cref="Update"/> and keeps
/// what it shows when the value didn't change (a field's text that doesn't read stays, marked, P12); a change by
/// the user is one commit (P13).
/// </summary>
abstract class ValueEditor
{
    protected ValueEditor(FormView form, Slot slot, ValueShape shape, FieldShape? field)
    {
        Form = form;
        Slot = slot;
        Shape = shape;
        Field = field;
        Head = new UiElement("span");
        Head.ClassName = "controls";
    }

    protected FormView Form { get; }
    protected Slot Slot { get; }
    protected ValueShape Shape { get; }

    /// <summary>The field whose marks hold for this value (for an element or a map's value, the list's or map's field).</summary>
    protected FieldShape? Field { get; }

    /// <summary>The controls on the value's row.</summary>
    public UiElement Head { get; }

    /// <summary>A block's nested rows; null for a leaf.</summary>
    public virtual UiElement? Body => null;

    public bool IsBlock => Body is not null;

    /// <summary>Shows <paramref name="node"/> (the value's draft now).</summary>
    public abstract void Update(DraftValue node);

    protected void Commit(DraftValue value) => Form.Commit(Slot, value);

    protected UiElement Control(UiElement parent, string tag, string? type = null, string? className = null)
    {
        var control = parent.Append(tag, className);
        if (type is not null)
            control.SetAttribute("type", type);
        control.ToggleAttribute("disabled", Form.ReadOnly);
        return control;
    }

    protected UiElement Button(UiElement parent, string text, string title, Action click, string className = "tool")
    {
        var button = parent.Append("button", className, text);
        button.SetAttribute("type", "button");
        button.SetAttribute("title", title);
        button.AddEventListener("click", _ => click());
        return button;
    }

    /// <summary>The unit of the field's numbers after the controls (M11-10).</summary>
    protected void Unit()
    {
        if (Field?.Unit is { } unit)
            Head.Append("span", "unit", unit);
    }

    /// <summary>The editor of a value of <paramref name="shape"/> at <paramref name="slot"/>; <paramref name="field"/>'s marks hold for it.</summary>
    public static ValueEditor For(FormView form, Slot slot, ValueShape shape, FieldShape? field, bool bulk = false)
    {
        if (bulk)
            return new SummaryEditor(form, slot, shape, field, bulk: true);
        return shape.Kind switch
        {
            ValueKind.Bool => new CheckEditor(form, slot, shape, field),
            _ when shape.IsNumber => new NumberEditor(form, slot, shape, field),
            ValueKind.String when field?.Choices is not null => new ChoiceEditor(form, slot, shape, field),
            ValueKind.String => new TextEditor(form, slot, shape, field),
            ValueKind.Enum => new EnumEditor(form, slot, shape, field),
            ValueKind.Vector2 or ValueKind.Vector3 or ValueKind.Vector4 => new VectorEditor(form, slot, shape, field),
            ValueKind.Bytes or ValueKind.Quaternion or ValueKind.Matrix4x4 => new SummaryEditor(form, slot, shape, field),
            ValueKind.Nullable => new NullableEditor(form, slot, shape, field),
            ValueKind.List => new ListEditor(form, slot, shape, field),
            ValueKind.Map or ValueKind.Pairs => new MapEditor(form, slot, shape, field),
            ValueKind.Record => new RecordEditor(form, slot, shape.Record!, shape),
            ValueKind.Union => new UnionEditor(form, slot, shape, field),
            _ => throw new InvalidOperationException($"no editor for {shape}"),
        };
    }

    /// <summary>Values the form never changes (P11: byte arrays, quaternions and matrices, which only imported models have).</summary>
    public static bool IsReadOnlyKind(ValueShape shape) => shape.Kind is ValueKind.Bytes or ValueKind.Quaternion or ValueKind.Matrix4x4;
}

/// <summary>A bool: a checkbox.</summary>
sealed class CheckEditor : ValueEditor
{
    readonly UiElement _box;

    public CheckEditor(FormView form, Slot slot, ValueShape shape, FieldShape? field) : base(form, slot, shape, field)
    {
        _box = Control(Head, "input", "checkbox");
        _box.AddEventListener("change", _ => Commit(DraftValue.Of(_box.Checked)));
    }

    public override void Update(DraftValue node) => _box.Checked = node is DraftLeaf { Value: true };
}

/// <summary>
/// A text field that commits (P12): on its change (Enter, leaving it), when its text reads as a value that differs
/// from the one shown; text that doesn't read stays, marked, and isn't committed. Its text is put back from the
/// draft only when the draft's value changed.
/// </summary>
abstract class TextFieldEditor : ValueEditor
{
    string? _shown;
    DraftValue? _node;

    protected TextFieldEditor(FormView form, Slot slot, ValueShape shape, FieldShape? field) : base(form, slot, shape, field) { }

    /// <summary>The field the text is typed in.</summary>
    protected UiElement Input { get; private set; } = null!;

    protected void Attach(UiElement input)
    {
        Input = input;
        Form.TextField(input);
        input.AddEventListener("change", _ => Read());
        input.AddEventListener<UiKeyboardEvent>("keydown", e =>
        {
            // A required value not given yet is given by Enter even when the text is empty.
            if (e.Key == "Enter" && _node == DraftValue.Missing)
                Read();
        });
    }

    /// <summary>The text that shows <paramref name="node"/>.</summary>
    protected abstract string TextOf(DraftValue node);

    /// <summary>Reads the field's text: the value, or why it can't be read.</summary>
    protected abstract bool TryRead(string text, out DraftValue? value, out string? problem);

    public override void Update(DraftValue node)
    {
        _node = node;
        var text = TextOf(node);
        if (text == _shown)
            return;
        _shown = text;
        Input.Value = text;
        Form.Committed(Input, text);
        Shown(node);
    }

    /// <summary>After the text shows a new value (a swatch follows it).</summary>
    protected virtual void Shown(DraftValue node) { }

    /// <summary>Reads and commits the field's text now (a slider's release).</summary>
    protected void Read()
    {
        var text = Input.EditorText;
        if (!TryRead(text, out var value, out var problem))
        {
            Form.ParseProblem(Input, problem);
            return;
        }
        if (_node is DraftLeaf leaf && value is DraftLeaf read && leaf.Value.Equals(read.Value))
        {
            // The same value typed another way ("1.0" for 1): the field shows it as the form writes it.
            Input.Value = _shown!;
            Form.Committed(Input, _shown!);
            return;
        }
        Commit(value!);
    }
}

/// <summary>A number (P12): a number field with its kind's step (whole numbers 1, floats any) and range, and a slider between finite limits (M11-10).</summary>
sealed class NumberEditor : TextFieldEditor
{
    readonly UiElement? _slider;

    public NumberEditor(FormView form, Slot slot, ValueShape shape, FieldShape? field) : base(form, slot, shape, field)
    {
        var input = Control(Head, "input", "number", "number");
        input.SetAttribute("step", shape.IsFloat ? "any" : "1");
        var limits = field?.Limits;
        decimal? min = null, max = null;
        if (shape.IsInteger)
            (min, max) = ValueText.IntegerRange(shape.Kind);
        if (limits is not null && !double.IsInfinity(limits.Min))
            min = min is { } m ? Math.Max(m, Decimal(limits.Min)) : Decimal(limits.Min);
        if (limits is not null && !double.IsInfinity(limits.Max))
            max = max is { } m ? Math.Min(m, Decimal(limits.Max)) : Decimal(limits.Max);
        if (min is { } low)
            input.SetAttribute("min", low.ToString(CultureInfo.InvariantCulture));
        if (max is { } high)
            input.SetAttribute("max", high.ToString(CultureInfo.InvariantCulture));
        Attach(input);
        if (limits is not null && !double.IsInfinity(limits.Min) && !double.IsInfinity(limits.Max))
        {
            // The slider's ends are the limits; dragging shows its value in the field and commits once, on release (P13).
            _slider = Control(Head, "input", "range", "slider");
            _slider.SetAttribute("min", ValueText.Number(limits.Min));
            _slider.SetAttribute("max", ValueText.Number(limits.Max));
            _slider.SetAttribute("step", shape.IsFloat ? "any" : "1");
            _slider.SetAttribute("tabindex", "-1");
            _slider.AddEventListener("input", _ => Input.Value = _slider.Value);
            _slider.AddEventListener("change", _ =>
            {
                Input.Value = _slider.Value;
                Read();
            });
        }
        Unit();
    }

    static decimal Decimal(double value) => value < (double)decimal.MinValue ? decimal.MinValue : value > (double)decimal.MaxValue ? decimal.MaxValue : (decimal)value;

    protected override string TextOf(DraftValue node) => node is DraftLeaf leaf ? ValueText.Number(leaf.Value) : "";

    protected override void Shown(DraftValue node)
    {
        if (_slider is not null)
            _slider.Value = node is DraftLeaf leaf ? ValueText.Number(leaf.Value) : "";
    }

    protected override bool TryRead(string text, out DraftValue? value, out string? problem)
    {
        if (ValueText.TryNumber(text, Shape.Kind, Shape.Type, out var number, out problem))
        {
            value = DraftValue.Of(number!);
            return true;
        }
        problem = text.Trim().Length == 0 ? "needs a number" : $"\"{text.Trim()}\" {problem}";
        value = null;
        return false;
    }
}

/// <summary>A string: a text field; with a colour mark, a swatch before it (P11).</summary>
sealed class TextEditor : TextFieldEditor
{
    readonly UiElement? _swatch;

    public TextEditor(FormView form, Slot slot, ValueShape shape, FieldShape? field) : base(form, slot, shape, field)
    {
        if (field?.IsColour == true)
            _swatch = Head.Append("span", "swatch");
        Attach(Control(Head, "input", "text", "text"));
    }

    protected override string TextOf(DraftValue node) => node is DraftLeaf { Value: string text } ? text : "";

    protected override void Shown(DraftValue node)
    {
        if (_swatch is not null)
        {
            _swatch.SetStyle("background-color", null);
            // The text as a CSS colour (what isn't one shows none); text that would end the declaration is never put in.
            if (node is DraftLeaf { Value: string colour } && colour.IndexOfAny([';', '{', '}', '!', '\\', '\n', '\r']) < 0)
                _swatch.SetStyle("background-color", colour);
        }
    }

    protected override bool TryRead(string text, out DraftValue? value, out string? problem)
    {
        value = DraftValue.Of(text);
        problem = null;
        return true;
    }
}

/// <summary>A colour vector (P11): a swatch and <c>#rrggbb</c> (<c>#rrggbbaa</c> for a Vector4).</summary>
sealed class HexEditor : TextFieldEditor
{
    readonly UiElement _swatch;
    readonly int _size;

    public HexEditor(FormView form, Slot slot, ValueShape shape, FieldShape? field) : base(form, slot, shape, field)
    {
        _size = ValueText.Size(shape.Kind);
        _swatch = Head.Append("span", "swatch");
        Attach(Control(Head, "input", "text", "hex"));
    }

    protected override string TextOf(DraftValue node) => node is DraftLeaf leaf ? ValueText.Hex(ValueText.Components(leaf.Value)) : "";

    protected override void Shown(DraftValue node)
    {
        _swatch.SetStyle("background-color", null);
        if (node is DraftLeaf leaf)
            _swatch.SetStyle("background-color", ValueText.Hex(ValueText.Components(leaf.Value)));
    }

    protected override bool TryRead(string text, out DraftValue? value, out string? problem)
    {
        if (ValueText.TryHex(text, _size, out var components))
        {
            value = DraftValue.Of(ValueText.Vector(Shape.Kind, components));
            problem = null;
            return true;
        }
        value = null;
        problem = _size == 4 ? $"\"{text.Trim()}\" isn't a colour (#rrggbb or #rrggbbaa)" : $"\"{text.Trim()}\" isn't a colour (#rrggbb)";
        return false;
    }
}

/// <summary>One component of a vector: a number field (the vector's editor commits the whole vector).</summary>
sealed class ComponentEditor : TextFieldEditor
{
    readonly VectorEditor _vector;
    readonly int _index;

    public ComponentEditor(VectorEditor vector, FormView form, Slot slot, ValueShape shape, int index, UiElement parent) : base(form, slot, shape, null)
    {
        _vector = vector;
        _index = index;
        var input = Control(parent, "input", "number", "component");
        input.SetAttribute("step", "any");
        input.SetAttribute("title", "XYZW"[index].ToString());
        Attach(input);
    }

    protected override string TextOf(DraftValue node) => node is DraftLeaf leaf ? ValueText.Number(ValueText.Components(leaf.Value)[_index]) : "";

    protected override bool TryRead(string text, out DraftValue? value, out string? problem)
    {
        if (!ValueText.TryNumber(text, ValueKind.Float32, typeof(float), out var number, out problem))
        {
            problem = text.Trim().Length == 0 ? "needs a number" : $"\"{text.Trim()}\" {problem}";
            value = null;
            return false;
        }
        value = DraftValue.Of(_vector.With(_index, (float)number!));
        return true;
    }
}

/// <summary>A vector: 2–4 number fields, or a colour field for a colour mark (P11).</summary>
sealed class VectorEditor : ValueEditor
{
    readonly ValueEditor? _hex;
    readonly ComponentEditor[] _components = [];
    DraftValue _node = DraftValue.Missing;

    public VectorEditor(FormView form, Slot slot, ValueShape shape, FieldShape? field) : base(form, slot, shape, field)
    {
        if (field?.IsColour == true && shape.Kind is ValueKind.Vector3 or ValueKind.Vector4)
        {
            _hex = new HexEditor(form, slot, shape, field);
            foreach (var child in _hex.Head.Children.ToList())
                Head.Append(child);
            return;
        }
        _components = [.. Enumerable.Range(0, ValueText.Size(shape.Kind)).Select(i => new ComponentEditor(this, form, slot, shape, i, Head))];
        Unit();
    }

    /// <summary>The vector shown with one component replaced (a vector not given yet has zeros elsewhere).</summary>
    public object With(int index, float value)
    {
        var components = _node is DraftLeaf leaf ? ValueText.Components(leaf.Value) : new float[ValueText.Size(Shape.Kind)];
        components[index] = value;
        return ValueText.Vector(Shape.Kind, components);
    }

    public override void Update(DraftValue node)
    {
        _node = node;
        _hex?.Update(node);
        foreach (var component in _components)
            component.Update(node);
    }
}

/// <summary>Strings among a field's choices: a select (M11-10's Choices mark); a value not among them shows as such.</summary>
sealed class ChoiceEditor : ValueEditor
{
    readonly UiElement _select;
    List<string?> _values = [];

    public ChoiceEditor(FormView form, Slot slot, ValueShape shape, FieldShape? field) : base(form, slot, shape, field)
    {
        _select = Control(Head, "select", className: "choice");
        _select.AddEventListener("change", _ =>
        {
            if (_select.SelectedIndex is var index and >= 0 && _values[index] is { } choice)
                Commit(DraftValue.Of(choice));
        });
    }

    public override void Update(DraftValue node)
    {
        var current = node is DraftLeaf { Value: string text } ? text : null;
        var values = new List<string?>();
        if (current is null)
            values.Add(null);
        values.AddRange(Field!.Choices!);
        if (current is not null && !Field.Choices!.Contains(current))
            values.Add(current);
        if (!values.SequenceEqual(_values))
        {
            _values = values;
            _select.ClearChildren();
            foreach (var value in values)
                _select.Append("option", text: value is null ? "" : value.Length == 0 ? "(empty)" : Field.Choices!.Contains(value) ? value : $"{value} (not a choice)");
        }
        _select.SelectedIndex = values.IndexOf(current);
    }
}

/// <summary>An enum: a select of its members by their display names; a value that isn't one shows as its number.</summary>
sealed class EnumEditor : ValueEditor
{
    readonly UiElement _select;
    List<object?> _values = [];

    public EnumEditor(FormView form, Slot slot, ValueShape shape, FieldShape? field) : base(form, slot, shape, field)
    {
        _select = Control(Head, "select", className: "choice");
        _select.AddEventListener("change", _ =>
        {
            if (_select.SelectedIndex is var index and >= 0 && _values[index] is { } member)
                Commit(DraftValue.Of(member));
        });
    }

    public override void Update(DraftValue node)
    {
        var current = node is DraftLeaf leaf ? leaf.Value : null;
        var values = new List<object?>();
        if (current is null)
            values.Add(null);
        values.AddRange(Shape.Members.Select(m => m.Value));
        if (current is not null && !values.Contains(current))
            values.Add(current);
        if (!values.SequenceEqual(_values))
        {
            _values = values;
            _select.ClearChildren();
            foreach (var value in values)
                _select.Append("option", text: value is null ? "" : Shape.Members.FirstOrDefault(m => m.Value.Equals(value))?.DisplayName
                    ?? $"{Convert.ChangeType(value, Enum.GetUnderlyingType(Shape.Type), CultureInfo.InvariantCulture)} (not a member)");
        }
        _select.SelectedIndex = values.IndexOf(current);
    }
}

/// <summary>What the form shows but never changes (P11, P21): a bulk field's summary, a byte array's size, a quaternion's or matrix's numbers.</summary>
sealed class SummaryEditor : ValueEditor
{
    readonly UiElement _text;
    readonly bool _bulk;

    public SummaryEditor(FormView form, Slot slot, ValueShape shape, FieldShape? field, bool bulk = false) : base(form, slot, shape, field)
    {
        _bulk = bulk;
        _text = Head.Append("span", "summary");
    }

    public override void Update(DraftValue node)
    {
        var text = node switch
        {
            DraftLeaf leaf when _bulk => Field!.Summarize(leaf.Value) ?? "",
            DraftLeaf leaf => ValueText.ReadOnly(leaf.Value),
            _ when node == DraftValue.Null => "none",
            _ => "not given",
        };
        _text.TextContent = text;
        _text.SetAttribute("title", text);
    }
}

/// <summary>A nullable value (P11): a "set" box, and the value's own editor while it is set.</summary>
sealed class NullableEditor : ValueEditor
{
    readonly UiElement _set;
    readonly UiElement? _body;
    readonly UiElement _innerHead;
    ValueEditor? _inner;
    UiElement? _none;

    public NullableEditor(FormView form, Slot slot, ValueShape shape, FieldShape? field) : base(form, slot, shape, field)
    {
        _set = Control(Head, "input", "checkbox", "set");
        _set.SetAttribute("title", "Set");
        _set.AddEventListener("change", _ => Commit(_set.Checked ? ValueText.Start(Shape.Element!) : DraftValue.Null));
        _innerHead = Head.Append("span", "inner");
        var element = shape.Element!;
        if (element.Kind is ValueKind.List or ValueKind.Map or ValueKind.Pairs or ValueKind.Record or ValueKind.Union)
            _body = new UiElement("div") { ClassName = "nullable-body" };
    }

    public override UiElement? Body => _body;

    public override void Update(DraftValue node)
    {
        var set = node != DraftValue.Null && node != DraftValue.Missing;
        _set.Checked = set;
        if (!set)
        {
            if (_none is null)
            {
                _innerHead.ClearChildren();
                _body?.ClearChildren();
                _inner = null;
                _none = _innerHead.Append("span", "none");
            }
            _none.TextContent = node == DraftValue.Missing ? "not given" : "none";
            return;
        }
        if (_inner is null)
        {
            _innerHead.ClearChildren();
            _none = null;
            _inner = For(Form, Slot, Shape.Element!, Field);
            foreach (var child in _inner.Head.Children.ToList())
                _innerHead.Append(child);
            if (_inner.Body is { } body)
                _body!.Append(body);
        }
        _inner.Update(node);
    }
}

/// <summary>A record: its fields' rows, in number order (a group, collapsible); one not given yet offers to make it (its blank draft).</summary>
sealed class RecordEditor : ValueEditor
{
    readonly RecordShape _record;
    readonly UiElement _rows;
    ValueEditor[]? _fields;
    UiElement? _make;

    public RecordEditor(FormView form, Slot slot, RecordShape record, ValueShape? shape) : base(form, slot, shape!, null)
    {
        _record = record;
        _rows = new UiElement("div") { ClassName = "rows" };
    }

    public override UiElement? Body => _rows;

    public RecordShape Record => _record;

    public override void Update(DraftValue node)
    {
        if (node is not RecordDraft draft)
        {
            if (_fields is not null)
            {
                _rows.ClearChildren();
                _fields = null;
            }
            if (_make is null && !Form.ReadOnly)
                _make = Button(Head, "Make", $"Make a {_record.DisplayName.ToLowerInvariant()} from its defaults", () => Commit(RecordDraft.Blank(_record)), "tool");
            return;
        }
        _make?.Remove();
        _make = null;
        if (_fields is null)
        {
            _fields = new ValueEditor[_record.Fields.Count];
            foreach (var field in _record.Fields)
            {
                var editor = For(Form, Slot.Field(field.Index), field.Value, field, bulk: field.IsBulk);
                _fields[field.Index] = editor;
                var row = Rows.Make(_rows, Form, Slot.Field(field.Index), editor, name =>
                {
                    name.TextContent = field.DisplayName;
                    if (field.Help is { } help)
                        name.SetAttribute("title", help);
                });
                row.SetAttribute("data-field", field.JsonName);
            }
        }
        for (var i = 0; i < _fields.Length; i++)
            _fields[i].Update(draft.Fields[i]);
    }
}

/// <summary>A union (P11): a select of its subtypes and the chosen one's rows; switching keeps the values of fields with the same name and kind.</summary>
sealed class UnionEditor : ValueEditor
{
    readonly UiElement _select;
    readonly UiElement _body;
    readonly IReadOnlyList<RecordShape> _subtypes;
    RecordEditor? _record;
    DraftValue _node = DraftValue.Missing;
    bool _placeholder;

    public UnionEditor(FormView form, Slot slot, ValueShape shape, FieldShape? field) : base(form, slot, shape, field)
    {
        _subtypes = shape.Record!.Subtypes;
        _select = Control(Head, "select", className: "subtype");
        _select.SetAttribute("title", $"The kind of {shape.Record.DisplayName.ToLowerInvariant()}");
        Options(placeholder: false);
        _select.AddEventListener("change", _ =>
        {
            var index = _select.SelectedIndex - (_placeholder ? 1 : 0);
            if (index >= 0)
                Switch(_subtypes[index]);
        });
        _body = new UiElement("div") { ClassName = "union-body" };
    }

    public override UiElement? Body => _body;

    void Options(bool placeholder)
    {
        _placeholder = placeholder;
        _select.ClearChildren();
        if (placeholder)
            _select.Append("option");
        foreach (var subtype in _subtypes)
            _select.Append("option", text: subtype.DisplayName);
    }

    /// <summary>The chosen subtype's blank, with the values of the old one's fields of the same JSON name and value shape (P11).</summary>
    void Switch(RecordShape subtype)
    {
        var draft = RecordDraft.Blank(subtype);
        if (_node is RecordDraft old)
            foreach (var field in subtype.Fields)
                if (old.Shape.Field(field.JsonName) is { } same && same.Value.Signature == field.Value.Signature)
                    draft = draft.With(field, old[same]);
        Commit(draft);
    }

    public override void Update(DraftValue node)
    {
        _node = node;
        if (node is not RecordDraft draft)
        {
            if (!_placeholder)
                Options(placeholder: true);
            _select.SelectedIndex = 0;
            _body.ClearChildren();
            _record = null;
            return;
        }
        if (_placeholder)
            Options(placeholder: false);
        _select.SelectedIndex = _subtypes.ToList().IndexOf(draft.Shape);
        if (_record?.Record != draft.Shape)
        {
            _body.ClearChildren();
            _record = new RecordEditor(Form, Slot, draft.Shape, null);
            _body.Append(_record.Body!);
        }
        _record.Update(draft);
    }
}

/// <summary>A list (P11): its elements' rows (by index, from 0 as the engine's messages count), each with move up, move down and remove; add at the end.</summary>
sealed class ListEditor : ValueEditor
{
    readonly UiElement _items;
    readonly UiElement _count;
    readonly UiElement? _add;
    readonly bool _fixed;
    readonly List<(ValueEditor Editor, UiElement? Up, UiElement? Down)> _rows = [];
    DraftValue _node = DraftValue.Missing;

    public ListEditor(FormView form, Slot slot, ValueShape shape, FieldShape? field) : base(form, slot, shape, field)
    {
        _fixed = form.ReadOnly || IsReadOnlyKind(shape.Element!);
        _count = Head.Append("span", "count");
        if (!_fixed)
            _add = Button(Head, "Add", "Add an element at the end", Add);
        _items = new UiElement("div") { ClassName = "items" };
    }

    public override UiElement? Body => _items;

    void Add()
    {
        var list = _node as DraftList ?? DraftList.Empty;
        Commit(list.Insert(list.Count, ValueText.Start(Shape.Element!)));
    }

    public override void Update(DraftValue node)
    {
        _node = node;
        var list = node as DraftList;
        _count.TextContent = list is null ? node == DraftValue.Null ? "none" : "not given"
            : list.Count == 0 ? "no elements" : list.Count == 1 ? "1 element" : $"{list.Count} elements";
        var count = list?.Count ?? 0;
        if (count != _rows.Count)
        {
            _items.ClearChildren();
            _rows.Clear();
            for (var i = 0; i < count; i++)
            {
                var index = i;
                var editor = For(Form, Slot.Item(index), Shape.Element!, Field);
                UiElement? up = null, down = null;
                Rows.Make(_items, Form, Slot.Item(index), editor, name => name.TextContent = index.ToString(CultureInfo.InvariantCulture), tools =>
                {
                    if (_fixed)
                        return;
                    up = Button(tools, "Up", "Move up", () => Commit(((DraftList)_node).Move(index, index - 1)));
                    down = Button(tools, "Down", "Move down", () => Commit(((DraftList)_node).Move(index, index + 1)));
                    Button(tools, "Remove", "Remove this element", () => Commit(((DraftList)_node).RemoveAt(index)));
                }, "item");
                _rows.Add((editor, up, down));
            }
        }
        for (var i = 0; i < count; i++)
        {
            var (editor, up, down) = _rows[i];
            up?.ToggleAttribute("disabled", i == 0);
            down?.ToggleAttribute("disabled", i == count - 1);
            editor.Update(list![i]);
        }
    }
}

/// <summary>A dictionary or ordered pairs (P11): key and value rows, each with remove (and move up and down for pairs, whose order counts); add at the end.</summary>
sealed class MapEditor : ValueEditor
{
    readonly UiElement _entries;
    readonly UiElement _count;
    readonly bool _pairs;
    readonly List<(ValueEditor Key, ValueEditor Value, UiElement? Up, UiElement? Down)> _rows = [];
    DraftValue _node = DraftValue.Missing;

    public MapEditor(FormView form, Slot slot, ValueShape shape, FieldShape? field) : base(form, slot, shape, field)
    {
        _pairs = shape.Kind == ValueKind.Pairs;
        _count = Head.Append("span", "count");
        if (!form.ReadOnly)
            Button(Head, "Add", "Add an entry at the end", Add);
        _entries = new UiElement("div") { ClassName = "entries" };
    }

    public override UiElement? Body => _entries;

    void Add()
    {
        var map = _node as DraftMap ?? DraftMap.Empty;
        Commit(map.Insert(map.Count, new DraftEntry(ValueText.NewKey(Shape.Key!, [.. map.Entries.Select(e => e.Key)]), ValueText.Start(Shape.Element!))));
    }

    public override void Update(DraftValue node)
    {
        _node = node;
        var map = node as DraftMap;
        _count.TextContent = map is null ? node == DraftValue.Null ? "none" : "not given"
            : map.Count == 0 ? "no entries" : map.Count == 1 ? "1 entry" : $"{map.Count} entries";
        var count = map?.Count ?? 0;
        if (count != _rows.Count)
        {
            _entries.ClearChildren();
            _rows.Clear();
            for (var i = 0; i < count; i++)
            {
                var index = i;
                var key = For(Form, Slot.Key(index), Shape.Key!, null);
                var value = For(Form, Slot.Value(index, Shape.Key!), Shape.Element!, Field);
                UiElement? up = null, down = null;
                Rows.Make(_entries, Form, Slot.Value(index, Shape.Key!), value, name =>
                {
                    name.AddClass("key");
                    foreach (var child in key.Head.Children.ToList())
                        name.Append(child);
                }, tools =>
                {
                    if (Form.ReadOnly)
                        return;
                    if (_pairs)
                    {
                        up = Button(tools, "Up", "Move up", () => Commit(((DraftMap)_node).Move(index, index - 1)));
                        down = Button(tools, "Down", "Move down", () => Commit(((DraftMap)_node).Move(index, index + 1)));
                    }
                    Button(tools, "Remove", "Remove this entry", () => Commit(((DraftMap)_node).RemoveAt(index)));
                }, "entry");
                _rows.Add((key, value, up, down));
            }
        }
        for (var i = 0; i < count; i++)
        {
            var (key, value, up, down) = _rows[i];
            up?.ToggleAttribute("disabled", i == 0);
            down?.ToggleAttribute("disabled", i == count - 1);
            key.Update(DraftValue.Of(map![i].Key));
            value.Update(map[i].Value);
        }
    }
}

/// <summary>
/// The form's rows: a leaf's row (<c>div.row</c>: a line with its name, its controls and tools) or a block's group
/// (<c>details.group</c>, open: a summary with the name, the controls and tools, then the nested rows). Every row is
/// registered with its slot, so the problems at its path show under its line (P16).
/// </summary>
static class Rows
{
    public static UiElement Make(UiElement parent, FormView form, Slot slot, ValueEditor editor, Action<UiElement> name, Action<UiElement>? tools = null, string? kind = null)
    {
        UiElement row, line;
        if (editor.IsBlock)
        {
            row = parent.Append("details", "group");
            row.ToggleAttribute("open", true);
            line = row.Append("summary", "line");
        }
        else
        {
            row = parent.Append("div", "row");
            line = row.Append("div", "line");
        }
        if (kind is not null)
            row.AddClass(kind);
        name(line.Append("span", "name"));
        line.Append(editor.Head);
        if (tools is not null)
        {
            var box = new UiElement("span") { ClassName = "tools" };
            tools(box);
            if (box.Children.Count > 0)
                line.Append(box);
        }
        if (editor.Body is { } body)
            row.Append(body);
        form.Row(row, slot);
        return row;
    }
}
