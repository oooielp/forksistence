using Content.Shared.Shuttles.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Events;

[Serializable, NetSerializable]
public sealed class ShuttleConsoleDampingMessage : BoundUserInterfaceMessage
{
    public ShuttleDampingMode DampingMode;
}
