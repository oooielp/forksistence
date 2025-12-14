using Content.Client.Eui;
using Content.Shared.CrewAssignments.Systems;
using Content.Shared.Eui;
using Content.Shared.Fax;
using JetBrains.Annotations;

namespace Content.Client.CrewAssignments.AdminUI;

[UsedImplicitly]
public sealed class AdminWorldObjectivesEui : BaseEui
{
    private readonly AdminWorldObjectivesWindow _window;

    public AdminWorldObjectivesEui()
    {
        _window = new AdminWorldObjectivesWindow(this);
        _window.OnClose += () => SendMessage(new AdminWorldObjectivesEuiMsg.Close());
        _window.OnFollowWorldObjectives += entity => SendMessage(new AdminWorldObjectivesEuiMsg.Follow(entity));
        _window.OnMessageSend += args => SendMessage(new AdminWorldObjectivesEuiMsg.Send(args.entity, args.title,
                    args.stampedBy, args.message, args.stampSprite, args.stampColor, args.locked));
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not AdminWorldObjectivesEuiState cast)
            return;
        _window.PopulateWorldObjectives(cast.Entries, cast.CompletedEntries);
    }
}
