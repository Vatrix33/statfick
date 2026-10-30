namespace Statventure.UI.Dom;

/// <summary>
/// What clicks and keys do to elements, as Blink's default event handlers: checkboxes and radios
/// toggle (before the click's listeners, undone if they cancel), labels pass clicks to their
/// control, summaries open and close their details (with an asynchronous toggle event), buttons
/// submit or reset their form (interactive validation, the submit event), Enter in a field
/// submits implicitly, and Space and Enter activate buttons.
/// </summary>
public partial class UiElement
{
    /// <summary>What a checkbox or radio was before a click toggled it (Blink's ClickHandlingState).</summary>
    internal sealed record ClickHandlingState(bool Checked, bool Indeterminate, UiElement? CheckedRadio);

    bool _isOpen;
    UiToggleEvent? _pendingToggle;

    /// <summary>Elements a click activates: inputs, buttons, labels, summaries, links.</summary>
    internal bool HasActivationBehavior => Tag switch
    {
        "input" or "button" or "label" or "summary" => true,
        "a" or "area" => HasAttribute("href"),
        _ => false,
    };

    /// <summary>Blink's form control elements (the ones a summary never toggles for).</summary>
    internal bool IsFormControlElement => Tag is "button" or "fieldset" or "input" or "select" or "textarea" or "output";

    /// <summary>Interactive content: a click on it inside a label stays its own.</summary>
    bool IsInteractiveContent => Tag switch
    {
        "button" or "details" or "label" or "select" or "textarea" => true,
        "input" => InputType != "hidden",
        "a" => HasAttribute("href"),
        _ => false,
    };

    /// <summary>The details' first summary child: the one that opens and closes it.</summary>
    bool IsMainSummary => Tag == "summary" && Parent is { Tag: "details" } details && details.ElementChildren.FirstOrDefault(e => e.Tag == "summary") == this;

    // ---------- Click activation ----------

    /// <summary>Before a click's listeners run: a checkbox flips (and loses indeterminate), a radio is checked.</summary>
    internal ClickHandlingState? LegacyPreActivationBehavior()
    {
        if (Tag != "input")
            return null;
        switch (InputType)
        {
            case "checkbox":
            {
                var state = new ClickHandlingState(Checked, Indeterminate, null);
                SetCheckedness(!state.Checked, dirty: true);
                Indeterminate = false;
                return state;
            }
            case "radio":
            {
                var state = new ClickHandlingState(Checked, false, RadioGroup().FirstOrDefault(r => r.Checked));
                SetCheckedness(true, dirty: true);
                return state;
            }
            default:
                return null;
        }
    }

    /// <summary>After a click's listeners: a cancelled click undoes the toggle; otherwise input and change fire.</summary>
    internal void RunActivationBehavior(UiEvent e, ClickHandlingState? state)
    {
        if (state is null || Tag != "input")
            return;
        if (InputType == "checkbox")
        {
            if (e.DefaultPrevented || e.DefaultHandled)
            {
                Indeterminate = state.Indeterminate;
                SetCheckedness(state.Checked, dirty: true);
            }
            else
            {
                if (e.IsTrusted)
                    SetUserHasEditedTheField();
                DispatchInputAndChangeEventIfNeeded();
            }
            e.SetDefaultHandled();
        }
        else if (InputType == "radio")
        {
            if (e.DefaultPrevented || e.DefaultHandled)
            {
                if (state.CheckedRadio is not { } previous)
                    SetCheckedness(false, dirty: true);
                else if (previous.InputType == "radio" && previous.Form == Form && previous.GetAttribute("name") == GetAttribute("name"))
                    previous.SetCheckedness(true, dirty: true);
            }
            else if (state.Checked != Checked)
            {
                if (e.IsTrusted)
                    SetUserHasEditedTheField();
                DispatchInputAndChangeEventIfNeeded();
            }
            e.SetDefaultHandled();
        }
    }

    /// <summary>Sends the element a click as a key or a label does (Blink's DispatchSimulatedClick): none for disabled controls, none while one is under way.</summary>
    internal void DispatchSimulatedClick(UiEvent? underlying)
    {
        if (IsDisabled || _dispatchingSimulatedClick)
            return;
        _dispatchingSimulatedClick = true;
        try
        {
            if (IsActive && Document?.Input is { } input)
                input.ClearKeyboardActive(this);
            var mouse = underlying as UiMouseEvent;
            var key = underlying as UiKeyboardEvent;
            DispatchEvent(new UiMouseEvent("click")
            {
                IsTrusted = underlying?.IsTrusted ?? false,
                Position = mouse?.Position ?? default,
                LayoutPosition = mouse?.LayoutPosition ?? default,
                Detail = mouse?.Detail ?? 0,
                ShiftKey = mouse?.ShiftKey ?? key?.ShiftKey ?? false,
                CtrlKey = mouse?.CtrlKey ?? key?.CtrlKey ?? false,
                AltKey = mouse?.AltKey ?? key?.AltKey ?? false,
                MetaKey = mouse?.MetaKey ?? key?.MetaKey ?? false,
            });
        }
        finally
        {
            _dispatchingSimulatedClick = false;
        }
    }

    bool _dispatchingSimulatedClick;

    /// <summary>HTMLElement.click(): a click from script (untrusted, but it still activates).</summary>
    public void Click() => DispatchSimulatedClick(null);

    /// <summary>Element.matches(): does the selector match this element?</summary>
    public bool Matches(string selector) =>
        (Css.SelectorParser.ParseList(selector) ?? throw new ArgumentException("invalid selector: " + selector)).Matches(this);

    // ---------- Default event handlers ----------

    /// <summary>The element's default action for an event that reached it uncancelled (Blink's DefaultEventHandler).</summary>
    internal void DefaultEventHandler(UiEvent e)
    {
        switch (e.Type)
        {
            case "click" when e is UiMouseEvent { Button: 0 } click && Tag != "select":
                if (Tag == "label")
                    LabelClick(click);
                else if (Tag == "input")
                    Document?.Input.ControlClick(this, click);
                break;
            case "DOMActivate":
                Activate(e);
                break;
            case "mousedown" when e is UiMouseEvent down && Tag == "input":
                Document?.Input.ControlMouseDown(this, down);
                break;
            case "mousedown" when e is UiMouseEvent down && Tag == "select" && !IsDisabled:
                Document?.Input.SelectMouseDown(this, down);
                break;
            case "click" or "change" when Tag == "select":
                // HTMLSelectElement::DefaultEventHandler: clicks and changes count as the user's edits.
                SetUserHasEditedTheField();
                break;
            case "mousemove" when e is UiMouseEvent move && Tag == "input":
                Document?.Input.ControlMouseMove(this, move);
                break;
            case "mouseup" when e is UiMouseEvent up && Tag == "input":
                Document?.Input.ControlMouseUp(this, up);
                break;
            case "wheel" when e is UiWheelEvent wheel && Tag == "input":
                Document?.Input.ControlWheel(this, wheel);
                break;
            case "keydown" when e is UiKeyboardEvent key:
                if (Document?.Input is { } input)
                    input.ElementKeyDown(this, key);
                break;
            case "keypress" when e is UiKeyboardEvent key:
                if (Document?.Input is { } pressInput)
                    pressInput.ElementKeyPress(this, key);
                break;
            case "keyup" when e is UiKeyboardEvent key:
                if (Document?.Input is { } upInput)
                    upInput.ElementKeyUp(this, key);
                break;
        }
        if (e.DefaultHandled || e.Target != this)
            return;
        // Node::DefaultEventHandler, for the target only: a click activates (DOMActivate); keys do the page's defaults.
        switch (e.Type)
        {
            case "click" when e is UiMouseEvent mouse:
                if (!DispatchEvent(new UiActivateEvent(e, mouse.Detail, e.Bubbles) { IsTrusted = true }))
                    e.SetDefaultHandled();
                break;
            case "keydown" or "keypress" or "keyup" when e is UiKeyboardEvent keyboard:
                Document?.Input.DefaultKeyboardEventHandler(this, keyboard);
                break;
        }
    }

    /// <summary>A click on a label: its control is focused (if a click could focus it) and clicked, unless the click was on the control or other interactive content inside the label.</summary>
    void LabelClick(UiMouseEvent e)
    {
        if (Control is not { } control || e.Target is not { } target)
            return;
        if (target == control || target.IsDescendantOf(control))
            return;
        for (var node = target; node is not null && node != this; node = node.Parent)
            if (node.IsInteractiveContent)
                return;
        if (control.IsFocusable && Document is { } document)
            document.Input.Focus(control, e.IsTrusted ? FocusOrigin.Pointer : FocusOrigin.Script);
        control.DispatchSimulatedClick(e);
        e.SetDefaultHandled();
    }

    /// <summary>DOMActivate: summaries toggle their details; submit and reset buttons act on their form.</summary>
    void Activate(UiEvent e)
    {
        if (IsMainSummary && !IsClickableControl(e.Target))
        {
            if (Parent is { } details)
                details.Open = !details.Open;
            e.SetDefaultHandled();
            return;
        }
        if (!IsButtonControl || IsDisabled || Form is not { } form)
            return;
        switch (ButtonType)
        {
            case "submit" or "image":
                form.PrepareForSubmission(this);
                e.SetDefaultHandled();
                break;
            case "reset":
                form.Reset();
                e.SetDefaultHandled();
                break;
        }
    }

    /// <summary>Element::IsClickableControl: a form control, or something with activation behavior between it and this element.</summary>
    bool IsClickableControl(UiElement? node)
    {
        if (node is null)
            return false;
        if (node.IsFormControlElement)
            return true;
        for (var n = node; n is not null && n != this; n = n.Parent)
            if (n.HasActivationBehavior)
                return true;
        return false;
    }

    // ---------- Details ----------

    /// <summary>
    /// The open attribute of a details changed: a toggle event follows at the next frame (two
    /// changes before it make one event from the first state); opening one of a named group
    /// closes the others.
    /// </summary>
    void DetailsOpenChanged()
    {
        var open = HasAttribute("open");
        if (open == _isOpen)
            return;
        _isOpen = open;
        var oldState = _pendingToggle?.OldState ?? (open ? "closed" : "open");
        _pendingToggle = new UiToggleEvent(oldState, open ? "open" : "closed") { IsTrusted = true };
        PostToggleTask();
        if (open && GetAttribute("name") is { Length: > 0 } name && Document is { } document)
            foreach (var other in document.AllElements().Where(d => d.Tag == "details" && d != this && d.GetAttribute("name") == name).ToList())
                other.Open = false;
    }

    bool _toggleTaskPosted;

    /// <summary>The toggle event goes out as a task (once the details are in a document: parsed ones get theirs when inserted).</summary>
    void PostToggleTask()
    {
        if (_toggleTaskPosted || Document is not { } document)
            return;
        _toggleTaskPosted = true;
        document.PostTask(() =>
        {
            _toggleTaskPosted = false;
            if (_pendingToggle is { } toggle)
            {
                _pendingToggle = null;
                DispatchEvent(toggle);
            }
        });
    }

    // ---------- Forms ----------

    /// <summary>
    /// Submitting (Blink's PrepareForSubmission): unless novalidate, the controls are checked and
    /// the first invalid one reported (no submit then); else the submit event fires with its
    /// submitter. The engine doesn't navigate: a submit nobody cancels just ends there.
    /// </summary>
    internal void PrepareForSubmission(UiElement? submitter)
    {
        if (Tag != "form" || Document is not { } document || _submitting)
            return;
        foreach (var control in Elements)
            if (control.Tag is "input" or "select" or "textarea")
                control.ForceUserValid();
        var skipValidation = HasAttribute("novalidate") || (submitter?.HasAttribute("formnovalidate") ?? false);
        if (!skipValidation && !ValidateInteractively())
            return;
        _submitting = true;
        try
        {
            if (DispatchEvent(new UiSubmitEvent(submitter) { IsTrusted = true }))
                Core.Log.Info("UI", $"{this} was submitted and no listener cancelled it (the engine doesn't navigate).");
        }
        finally
        {
            _submitting = false;
        }
    }

    bool _submitting, _resetting;

    /// <summary>Interactive validation: invalid events, then the first unhandled invalid control that can take focus is reported.</summary>
    bool ValidateInteractively()
    {
        if (Document is { ValidationBubbleAnchor: { } anchor } document && anchor.Form == this)
            document.HideValidationBubble();
        if (CheckValidity(out var unhandled))
            return true;
        foreach (var control in unhandled)
            if (control.IsFocusable)
            {
                Document?.ReportInvalid(control);
                break;
            }
        foreach (var control in unhandled)
            if (!control.IsFocusable)
                Core.Log.Warning("UI", $"An invalid form control with name='{control.GetAttribute("name")}' is not focusable.");
        return false;
    }

    /// <summary>form.requestSubmit(): submits as the button would (validation, the submit event).</summary>
    public void RequestSubmit(UiElement? submitter = null)
    {
        if (Tag != "form")
            return;
        if (submitter is not null && (!submitter.IsSubmitButton || submitter.Form != this))
            throw new ArgumentException("The specified element is not a submit button of this form.", nameof(submitter));
        PrepareForSubmission(submitter);
    }

    /// <summary>
    /// Enter in a form's field (Blink's SubmitImplicitly): the form's first submit button is
    /// clicked if it is enabled (nothing if it is disabled); with no submit button the form submits
    /// only when that field is its only one that blocks implicit submission.
    /// </summary>
    internal void SubmitImplicitly(UiEvent underlying, bool fromTrigger)
    {
        var triggers = 0;
        foreach (var control in Elements)
        {
            if (control.IsSubmitButton)
            {
                if (!control.IsDisabled)
                {
                    control.DispatchSimulatedClick(underlying);
                    return;
                }
                if (fromTrigger)
                    return;
            }
            else if (control.CanTriggerImplicitSubmission)
                triggers++;
        }
        if (fromTrigger && triggers == 1)
            PrepareForSubmission(null);
    }

    /// <summary>Text-like inputs, the fields that block implicit submission (Blink's IsTextField).</summary>
    internal bool CanTriggerImplicitSubmission => Tag == "input" && InputType is "text" or "search" or "tel" or "url" or "email" or "password" or "number";

    /// <summary>form.reset(): a cancelable reset event, then every control goes back to its defaults.</summary>
    public void Reset()
    {
        if (Tag != "form" || _resetting)
            return;
        _resetting = true;
        try
        {
            if (!DispatchEvent(new UiEvent("reset", bubbles: true, cancelable: true) { IsTrusted = true }))
                return;
            foreach (var control in Elements)
                control.ResetControl();
        }
        finally
        {
            _resetting = false;
        }
    }

    /// <summary>A control's reset algorithm: the default value, checkedness or selection, and no user interaction.</summary>
    void ResetControl()
    {
        switch (Tag)
        {
            case "input":
                if (Mode == ValueMode.Value)
                {
                    var before = Value;
                    _editorText = null;
                    BadInput = false;
                    _value = null;
                    _dirtyValue = false;
                    _valueBeforeFirstUserEdit = null;
                    _lastChangeWasUserEdit = false;
                    if (before != Value)
                    {
                        FormStateChanged(ValueStates);
                        ValueReplaced();
                    }
                    ValueChanged?.Invoke(this);
                }
                if (IsCheckable)
                {
                    SetCheckedness(HasAttribute("checked"), dirty: false);
                    _dirtyChecked = false;
                }
                break;
            case "textarea":
            {
                var before = Value;
                _value = null;
                _dirtyValue = false;
                _valueBeforeFirstUserEdit = null;
                _lastChangeWasUserEdit = false;
                if (before != Value)
                {
                    FormStateChanged(ValueStates);
                    ValueReplaced();
                }
                ValueChanged?.Invoke(this);
                break;
            }
            case "select":
            {
                var before = SelectedIndex;
                foreach (var option in Options)
                {
                    option._selected = option.HasAttribute("selected");
                    option._dirtySelected = false;
                }
                if (before != SelectedIndex)
                    SelectionChanged();
                break;
            }
        }
        if (Tag is "input" or "textarea" or "select")
            ClearUserHasEditedTheField();
    }

    /// <summary>The element gained or lost focus (before its focus or blur event): a text field commits, leaving marks it blurred after an edit.</summary>
    internal void FocusChanged(bool focused)
    {
        if (focused)
            return;
        // Leaving a one-line field scrolls its text back to the start.
        if (IsTextField && Tag == "input" && EditorScrollLeft != 0)
        {
            EditorScrollLeft = 0;
            Document?.Repaint();
        }
        if (IsTextField)
            DispatchFormControlChangeEvent();
        // MenuListSelectType::DidBlur: a selection nobody announced yet is announced.
        if (Tag == "select" && UsesMenuList && SelectedOption is var selected && LastOnChangeOption != selected)
        {
            LastOnChangeOption = selected;
            DispatchInputEvent();
            DispatchChangeEvent();
        }
        // (Inputs and textareas; a select only starts matching with a change event.)
        if (Tag is "input" or "textarea" && _interaction >= Interaction.Edited)
            SetUserHasEditedTheFieldAndBlurred();
    }
}
