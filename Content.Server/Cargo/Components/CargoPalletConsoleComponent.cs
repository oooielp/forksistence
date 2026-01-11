using Content.Server.Cargo.Systems;
using Content.Shared.Cargo.BUI;
using Content.Shared.Stacks;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Cargo.Components;

[RegisterComponent]
[Access(typeof(CargoSystem))]
public sealed partial class CargoPalletConsoleComponent : Component
{
    [DataField]
    public CargoSaleMode CashMode = CargoSaleMode.Deposit;
    [DataField]
    public int SelectedStation = 0;
}


