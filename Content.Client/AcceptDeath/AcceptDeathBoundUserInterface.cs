using Content.Client.Cargo.UI;
using Content.Client.CrewAssignments.UI;
using Content.Shared.AcceptDeath;
using Content.Shared.Cargo.BUI;
using Content.Shared.Cargo.Events;
using Content.Shared.CrewAssignments;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using System.Linq;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Robust.Client.UserInterface.Controls.OptionButton;

namespace Content.Client.AcceptDeath;

[UsedImplicitly]
public sealed class AcceptDeathBoundUserInterface : BoundUserInterface
{
    private IPrototypeManager _prototypeManager = default!;

    [ViewVariables]
    private AcceptDeathMenu? _menu;

    public AcceptDeathBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<AcceptDeathMenu>();
        _menu._owner = this;
        _menu.AcceptDeathButton.OnPressed += OnAcceptDeath;
        _menu.SOSButton.OnPressed += OnSOS;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (_menu == null) return;
        if (state is not AcceptDeathUpdateState cState)
            return;
        _menu.UpdateState(cState);


    }

    public void OnAcceptDeath(ButtonEventArgs args)
    {
        SendMessage(new AcceptDeathFinalizeMessage());
    }

    public void OnSOS(ButtonEventArgs args)
    {
        SendMessage(new AcceptDeathSOSMessage());
    }
}
