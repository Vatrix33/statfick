using Statventure.Records;

namespace Statventure.Editor.Form;

/// <summary>
/// Where a value sits in an entry's draft: the steps from the root record, each a record's field, a list's element,
/// or a map entry's key or value, by position (so two entries with the same key, a problem the form shows, are
/// still told apart). A nullable's value and a union's subtype record add no step: their editors share their
/// slot. Gives the value in a draft, a draft with it replaced, and its path as problems name it.
/// </summary>
abstract class Slot
{
    public static Slot Root { get; } = new RootSlot();

    public abstract DraftValue Get(RecordDraft root);

    public abstract RecordDraft Set(RecordDraft root, DraftValue value);

    /// <summary>The path problems have for this place (<c>$.layers[0].length</c>; a map's key: the map's own path, where the build reports keys).</summary>
    public abstract RecordPath Path(RecordDraft root);

    public Slot Field(int index) => new FieldSlot(this, index);

    public Slot Item(int index) => new ItemSlot(this, index);

    public Slot Key(int index) => new KeySlot(this, index);

    public Slot Value(int index, ValueShape key) => new EntryValueSlot(this, index, key);

    sealed class RootSlot : Slot
    {
        public override DraftValue Get(RecordDraft root) => root;
        public override RecordDraft Set(RecordDraft root, DraftValue value) => (RecordDraft)value;
        public override RecordPath Path(RecordDraft root) => RecordPath.Root;
    }

    sealed class FieldSlot(Slot parent, int index) : Slot
    {
        public override DraftValue Get(RecordDraft root) => ((RecordDraft)parent.Get(root)).Fields[index];

        public override RecordDraft Set(RecordDraft root, DraftValue value)
        {
            var record = (RecordDraft)parent.Get(root);
            return parent.Set(root, record.With(record.Shape.Fields[index], value));
        }

        public override RecordPath Path(RecordDraft root) => parent.Path(root).Field(((RecordDraft)parent.Get(root)).Shape.Fields[index].JsonName);
    }

    sealed class ItemSlot(Slot parent, int index) : Slot
    {
        public override DraftValue Get(RecordDraft root) => ((DraftList)parent.Get(root))[index];
        public override RecordDraft Set(RecordDraft root, DraftValue value) => parent.Set(root, ((DraftList)parent.Get(root)).With(index, value));
        public override RecordPath Path(RecordDraft root) => parent.Path(root).Index(index);
    }

    sealed class KeySlot(Slot parent, int index) : Slot
    {
        public override DraftValue Get(RecordDraft root) => DraftValue.Of(((DraftMap)parent.Get(root))[index].Key);

        public override RecordDraft Set(RecordDraft root, DraftValue value)
        {
            var map = (DraftMap)parent.Get(root);
            return parent.Set(root, map.With(index, map[index] with { Key = ((DraftLeaf)value).Value }));
        }

        public override RecordPath Path(RecordDraft root) => parent.Path(root);
    }

    sealed class EntryValueSlot(Slot parent, int index, ValueShape key) : Slot
    {
        public override DraftValue Get(RecordDraft root) => ((DraftMap)parent.Get(root))[index].Value;

        public override RecordDraft Set(RecordDraft root, DraftValue value)
        {
            var map = (DraftMap)parent.Get(root);
            return parent.Set(root, map.With(index, map[index] with { Value = value }));
        }

        public override RecordPath Path(RecordDraft root) =>
            parent.Path(root).Key(ValueText.KeyText(key, ((DraftMap)parent.Get(root))[index].Key));
    }
}
