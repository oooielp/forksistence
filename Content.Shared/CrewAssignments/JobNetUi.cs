using Content.Shared.CrewAssignments.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.CrewAssignments;

[Serializable, NetSerializable]
public enum JobNetUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class JobNetUpdateState : BoundUserInterfaceState
{
    public Dictionary<int, string>? Stations { get; set; }
    public string? AssignmentName;
    public int? Wage;
    public int SelectedStation;
    public TimeSpan? RemainingMinutes;
    public List<WorldObjectivesEntry> CurrentObjectives;
    public List<WorldObjectivesEntry> CompletedObjectives;

    public JobNetUpdateState(Dictionary<int, string>? stations, string? assignmentName, int? wage, int selectedStation, TimeSpan? remainingMinutes, List<WorldObjectivesEntry> currentObjectives, List<WorldObjectivesEntry> completedObjectives)
    {
        Stations = stations;
        AssignmentName = assignmentName;
        Wage = wage;
        SelectedStation = selectedStation;
        RemainingMinutes = remainingMinutes;
        CurrentObjectives = currentObjectives;
        CompletedObjectives = completedObjectives;
    }
}

[Serializable, NetSerializable]
public sealed class JobNetRequestUpdateInterfaceMessage : BoundUserInterfaceMessage
{

}

[Serializable, NetSerializable]
public sealed class JobNetSelectMessage : BoundUserInterfaceMessage
{
    public int ID;
    public JobNetSelectMessage(int id)
    {
        ID = id;
    }
}


