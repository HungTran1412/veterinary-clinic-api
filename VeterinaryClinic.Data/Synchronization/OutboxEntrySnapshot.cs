using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace VeterinaryClinic.Data.Synchronization;

internal sealed class OutboxEntrySnapshot
{
    private readonly EntityEntry _entry;
    private readonly EntityState _state;
    private readonly PropertyValues _currentValues;
    private readonly PropertyValues _originalValues;
    private readonly Dictionary<string, (bool Temporary, bool Modified)> _flags;

    public OutboxEntrySnapshot(EntityEntry entry)
    {
        _entry = entry;
        _state = entry.State;
        _currentValues = entry.CurrentValues.Clone();
        _originalValues = entry.OriginalValues.Clone();
        _flags = entry.Properties.ToDictionary(property => property.Metadata.Name,
            property => (property.IsTemporary, property.IsModified));
    }

    public void Restore()
    {
        _entry.CurrentValues.SetValues(_currentValues);
        _entry.OriginalValues.SetValues(_originalValues);
        _entry.State = _state;
        foreach (var property in _entry.Properties)
        {
            var flags = _flags[property.Metadata.Name];
            property.IsTemporary = flags.Temporary;
            property.IsModified = flags.Modified;
        }
    }
}
