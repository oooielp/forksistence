using Content.Shared.Cargo.Prototypes;
using Content.Shared.CrewAccesses.Components;
using Content.Shared.CrewAssignments.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.BUI;

[NetSerializable, Serializable]
public sealed class StationModificationInterfaceState : BoundUserInterfaceState
{
    public string Name;
    public NetEntity Station;
    public List<string> Owners;
    public Dictionary<string, CrewAccess> CrewAccess;
    public Dictionary<int, CrewAssignment> CrewAssignments;
    public int ImportTax;
    public int ExportTax;
    public int SalesTax;

    public StationModificationInterfaceState(string name, NetEntity station, List<string> owners, Dictionary<string, CrewAccess> crewAccess, Dictionary<int, CrewAssignment> crewAssignments, int importTax, int exportTax, int salesTax)
    {
        Name = name;
        Station = station;
        Owners = owners;
        CrewAccess = crewAccess;
        CrewAssignments = crewAssignments;
        ImportTax = importTax;
        ExportTax = exportTax;
        SalesTax = salesTax;
    }
}
