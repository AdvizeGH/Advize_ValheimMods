using System;

namespace Advize_Armoire;

public sealed class AppearanceSlot(Func<ItemDrop.ItemData.SharedData, bool> slotCriteria = null, bool canBeHidden = false)
{
    private Func<ItemDrop.ItemData.SharedData, bool> IsValidForSlot = slotCriteria ?? (valid => false);
    public string ItemName = string.Empty;
    public int ItemVariant;
    public bool Hidden;
    public bool CanBeHidden = canBeHidden;

    public void ResetSlot()
    {
        ItemName = string.Empty;
        ItemVariant = 0;
        Hidden = false;
    }

    public AppearanceSlot(AppearanceSlot other) : this(other.IsValidForSlot, other.CanBeHidden)
    {
        ItemName = other.ItemName;
        ItemVariant = other.ItemVariant;
        Hidden = other.Hidden;
    }

    public bool IsMatch(ItemDrop.ItemData.SharedData sd) => IsValidForSlot(sd);
}
