using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.Events;

/// <summary>
///     Set order in database as approved.
/// </summary>
[Serializable, NetSerializable]
public sealed class StationModificationToggleSpend : BoundUserInterfaceMessage
{
    public int AccessID;

    public StationModificationToggleSpend(int id)
    {
        AccessID = id;
    }
}

[Serializable, NetSerializable]
public sealed class StationModificationToggleClaim : BoundUserInterfaceMessage
{
    public int AccessID;

    public StationModificationToggleClaim(int id)
    {
        AccessID = id;
    }
}


[Serializable, NetSerializable]
public sealed class StationModificationToggleAssign : BoundUserInterfaceMessage
{
    public int AccessID;

    public StationModificationToggleAssign(int id)
    {
        AccessID = id;
    }
}
