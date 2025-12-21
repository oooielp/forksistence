using Content.Server._NF.Bank;
using Content.Server.Access.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Containers;
using Content.Server.CrewRecords.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Chat;
using Content.Shared.Construction;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Coordinates;
using Content.Shared.CrewAssignments.Components;
using Content.Shared.CrewMetaRecords;
using Content.Shared.CrewRecords.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.GridControl.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Invoices.Components;
using Content.Shared.Invoices.Systems;
using Content.Shared.Roles;
using Content.Shared.Station.Components;
using Content.Shared.StationRecords;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using NetCord;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Toolshed.TypeParsers;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static Content.Shared.Access.Components.IdCardConsoleComponent;
using static Content.Shared.Paper.PaperComponent;

namespace Content.Server.Invoices.Systems;

[UsedImplicitly]
public sealed class InvoicePrinterConsoleSystem : SharedInvoicePrinterConsoleSystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly AccessSystem _access = default!;
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly CrewMetaRecordsSystem _crewMeta = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly SharedCargoSystem _cargo = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly BankSystem _bank = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InvoicePrinterConsoleComponent, PrintInvoice>(Print);
        SubscribeLocalEvent<InvoicePrinterConsoleComponent, ChangeInvoiceMode>(ToggleMode);
        SubscribeLocalEvent<InvoicePrinterConsoleComponent, ComponentStartup>(UpdateUserInterface);
        SubscribeLocalEvent<InvoicePrinterConsoleComponent, EntInsertedIntoContainerMessage>(UpdateUserInterface);
        SubscribeLocalEvent<InvoicePrinterConsoleComponent, EntRemovedFromContainerMessage>(UpdateUserInterface);
        Subs.BuiEvents<InvoicePrinterConsoleComponent>(InvoicePrinterConsoleUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(UpdateUserInterface);
        });
        SubscribeLocalEvent<InvoiceComponent, PayInvoice>(OnPayInvoice);
        SubscribeLocalEvent<InvoiceComponent, PayInvoicePersonal>(OnPayInvoicePersonal);
        Subs.BuiEvents<InvoiceComponent>(InvoiceUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(UpdateUserInterface);
        });

    }

    private void Print(EntityUid uid, InvoicePrinterConsoleComponent component, PrintInvoice args)
    {
        var privilegedIdName = string.Empty;
        var privilegedName = string.Empty;
        int taxRate = 0;
        int owningStation = 0;
        if (args.Actor is not { Valid: true } player)
            return;
        int? targetStation = null;
        string? targetPerson = null;
        if(component.StationMode)
        {
            var station = _station.GetOwningStation(uid);
            if (station == null) return;
            if (TryComp<StationDataComponent>(station, out var sD) && sD != null)
            {
                taxRate = sD.SalesTax;
                if (component.StationMode)
                {
                    privilegedName = sD.StationName;
                    owningStation = sD.UID;
                    targetStation = sD.UID;
                }
            }


        }
        else
        {
            var station = _station.GetOwningStation(uid);
            if (station != null)
            {
                if (TryComp<StationDataComponent>(station, out var sD) && sD != null)
                {
                    taxRate = sD.SalesTax;
                    owningStation = sD.UID;

                }
            }
            if (component.PrivilegedIdSlot.Item is { Valid: true } idCard)
            {
                privilegedIdName = Comp<MetaDataComponent>(idCard).EntityName;
                if (TryComp<IdCardComponent>(idCard, out var id) && id.FullName != null)
                {
                    privilegedName = id.FullName;
                    targetPerson = id.FullName;

                }
                else return;
            }
            else return;
        }
        var invoice = _entityManager.SpawnAtPosition("Invoice", player.ToCoordinates());

        if (!_hands.TryPickupAnyHand(player, invoice))
            _transform.SetLocalRotation(invoice, Angle.Zero); // Orient these to grid north instead of map north
        if(TryComp<InvoiceComponent>(invoice, out var invoiceComp)  && invoiceComp != null)
        {
            invoiceComp.TargetPerson = targetPerson;
            invoiceComp.TargetStation = targetStation;
            invoiceComp.InvoiceCost = args.InvoiceCost;
            invoiceComp.InvoiceReason = args.InvoiceReason;
            invoiceComp.TaxOwner = owningStation;
            Dirty(invoice, invoiceComp);
            _audio.PlayEntity(component.PrintSound, args.Actor, invoice);
        }
        UpdateUserInterface(uid, component, args);
    }

    private void ToggleMode(EntityUid uid, InvoicePrinterConsoleComponent component, ChangeInvoiceMode args)
    {
        if (args.Actor is not { Valid: true } player)
            return;
        component.StationMode = !component.StationMode;
        UpdateUserInterface(uid, component, args);
    }
    private void UpdateUserInterface(EntityUid uid, InvoicePrinterConsoleComponent component, EntityEventArgs args)
    {
        var privilegedIdName = string.Empty;
        var privilegedName = string.Empty;
        bool idPresent = false;
        int taxRate = 0;
        if (component.PrivilegedIdSlot.Item is { Valid: true } idCard)
        {
            idPresent = true;
            privilegedIdName = Comp<MetaDataComponent>(idCard).EntityName;
            if (TryComp<IdCardComponent>(idCard, out var id) && id.FullName != null)
            {
                privilegedName = id.FullName;
            }

        }
        var station = _station.GetOwningStation(uid);
        if(station != null)
        {
            if(TryComp<StationDataComponent>(station, out var sD) && sD != null)
            {
                taxRate = sD.SalesTax;
                if (component.StationMode)
                {
                    privilegedName = sD.StationName;
                }
            }
            else
            {
                component.StationMode = false;
            }

        }
        InvoicePrinterConsoleBoundUserInterfaceState newState = new(idPresent, privilegedIdName, privilegedName, component.StationMode, taxRate);
        _userInterface.SetUiState(uid, InvoicePrinterConsoleUiKey.Key, newState);
    }


    private void UpdateUserInterface(EntityUid uid, InvoiceComponent component, BaseBoundUserInterfaceEvent args)
    {
        Dictionary<int, string> possibleStations = new();
        var stations = _station.GetStations();
        var userName = Name(args.Actor);
        string paidTo = "";
        if(component.TargetPerson != null)
        {
            paidTo = component.TargetPerson;
        }
        foreach (var station in stations)
        {
            if(TryComp<StationDataComponent>(station, out var sD) && sD != null)
            {
                if(component.TargetStation != null)
                {
                    if (component.TargetStation == sD.UID && sD.StationName != null) paidTo = sD.StationName;
                }
                if (sD.Owners.Contains(userName))
                {
                    possibleStations.Add(sD.UID, sD.StationName != null ? sD.StationName : "");
                }
                else
                {
                    if(TryComp<CrewRecordsComponent>(station, out var crewRecords) && crewRecords != null)
                    {
                        crewRecords.TryGetRecord(userName, out var crewRecord);
                        if(crewRecord != null)
                        {
                            if(TryComp<CrewAssignmentsComponent>(station, out var crewAssignments) && crewAssignments != null)
                            {
                                if(crewAssignments.TryGetAssignment(crewRecord.AssignmentID, out var assignment) && assignment != null)
                                {
                                    if(assignment.CanSpend)
                                    {
                                        possibleStations.Add(sD.UID, sD.StationName != null ? sD.StationName : "");
                                    }
                                }
                            }

                        }
                    }
                }

            }
        }
        InvoiceBoundUserInterfaceState newState = new(possibleStations,component.InvoiceCost, component.InvoiceReason, paidTo, component.PaidBy, component.Paid, userName);
        _userInterface.SetUiState(uid, InvoiceUiKey.Key, newState);
    }

    private void OnPayInvoice(EntityUid uid, InvoiceComponent component, PayInvoice args)
    {
        if (component.Paid) return;
        var station = _station.GetStationByID(args.Station);
        var userName = Name(args.Actor);
        var cost = component.InvoiceCost;
        var stationName = "";
        var taxAmount = 0;
        bool valid = false;
        EntityUid? taxStation = null;
        if (station != null)
        {
            if(component.TaxOwner != 0)
            {
                taxStation = _station.GetStationByID(component.TaxOwner);
                if(taxStation != null)
                {
                    if (TryComp<StationDataComponent>(taxStation, out var taxSD) && taxSD != null)
                    {
                        if(taxSD.SalesTax > 0)
                        {
                            var taxRate = taxSD.SalesTax;
                            taxAmount = (int)Math.Round((float)cost * ((float)taxRate / 100f));
                        }
                    }
                }
                
            }
            if (TryComp<StationDataComponent>(station, out var sD) && sD != null)
            {
                if (sD.StationName != null) stationName = sD.StationName;
                if (sD.Owners.Contains(userName))
                {
                    valid = true;
                }
                else
                {
                    if (TryComp<CrewRecordsComponent>(station, out var crewRecords) && crewRecords != null)
                    {
                        crewRecords.TryGetRecord(userName, out var crewRecord);
                        if (crewRecord != null)
                        {
                            if (TryComp<CrewAssignmentsComponent>(station, out var crewAssignments) && crewAssignments != null)
                            {
                                if (crewAssignments.TryGetAssignment(crewRecord.AssignmentID, out var assignment) && assignment != null)
                                {
                                    if (assignment.CanSpend)
                                    {
                                        valid = true;
                                    }
                                }
                            }

                        }
                    }
                }

            }
        }
        if(valid && station != null)
        {
            var accountBalance = 0;
            if (TryComp<StationBankAccountComponent>(station, out var stationBank) && stationBank != null)
            {
                accountBalance = _cargo.GetBalanceFromAccount((station.Value, stationBank), "Cargo");
                // Not enough balance
                if (cost > accountBalance)
                {
                    ConsolePopup(args.Actor, Loc.GetString("cargo-console-insufficient-funds", ("cost", cost)));
                    _audio.PlayEntity(component.ErrorSound, args.Actor, uid);
                    return;
                }

                _cargo.UpdateBankAccount((station.Value, stationBank), -cost, "Cargo");
                if(taxAmount > 0 && taxStation != null)
                { 
                    if(TryComp<StationBankAccountComponent>(taxStation, out var taxBank))
                    {
                        _cargo.UpdateBankAccount((taxStation.Value, taxBank), taxAmount, "Cargo");
                        cost -= taxAmount;
                    }
                }
                if(component.TargetStation != null)
                {
                    var target = _station.GetStationByID(component.TargetStation.Value);
                    if (target != null)
                    {
                        if(TryComp<StationBankAccountComponent>(target, out var targetAccount) && targetAccount != null)
                        {
                            _cargo.UpdateBankAccount((target.Value, targetAccount), cost, "Cargo");
                        }
                    }

                }
                else if(component.TargetPerson != null)
                {
                    var target = component.TargetPerson;
                    _bank.TryBankDeposit(target, cost);
                }
                component.Paid = true;
                component.PaidBy = $"{stationName} ({userName})";
                _audio.PlayEntity(component.PaySuccessSound, args.Actor, uid);
                _appearance.SetData(uid, PaperVisuals.Invoice, "paid");
            }
        }
        UpdateUserInterface(uid, component, args);
    }

    private void ConsolePopup(EntityUid actor, string text)
    {
        _popup.PopupCursor(text, actor);

    }


    private void OnPayInvoicePersonal(EntityUid uid, InvoiceComponent component, PayInvoicePersonal args)
    {
        if (component.Paid) return;
        var cost = component.InvoiceCost;
        var userName = Name(args.Actor);
        var taxAmount = 0;
        EntityUid? taxStation = null;
        if (component.TaxOwner != 0)
        {
            taxStation = _station.GetStationByID(component.TaxOwner);
            if (taxStation != null)
            {
                if (TryComp<StationDataComponent>(taxStation, out var taxSD) && taxSD != null)
                {
                    if (taxSD.SalesTax > 0)
                    {
                        var taxRate = taxSD.SalesTax;
                        taxAmount = (int)Math.Round((float)cost * ((float)taxRate / 100f));
                    }
                }
            }
        }
        if (_bank.TryBankWithdraw(args.Actor, component.InvoiceCost))
        {
            if (taxAmount > 0 && taxStation != null)
            {
                if (TryComp<StationBankAccountComponent>(taxStation, out var taxBank))
                {
                    _cargo.UpdateBankAccount((taxStation.Value, taxBank), taxAmount, "Cargo");
                    cost -= taxAmount;
                }
            }
            component.Paid = true;
            component.PaidBy = userName;
            _audio.PlayEntity(component.ErrorSound, args.Actor, uid);
            _appearance.SetData(uid, PaperVisuals.Invoice, "paid");
            if (component.TargetStation != null)
            {
                var target = _station.GetStationByID(component.TargetStation.Value);
                if (target != null)
                {
                    if (TryComp<StationBankAccountComponent>(target, out var targetAccount) && targetAccount != null)
                    {
                        _cargo.UpdateBankAccount((target.Value, targetAccount), cost, "Cargo");
                    }
                }

            }
            else if (component.TargetPerson != null)
            {
                var target = component.TargetPerson;
                _bank.TryBankDeposit(target, cost);
            }

        }
        else
        {
            ConsolePopup(args.Actor, Loc.GetString("cargo-console-insufficient-funds", ("cost", cost)));
            _audio.PlayEntity(component.ErrorSound, args.Actor, uid);
            return;
        }
            UpdateUserInterface(uid, component, args);
    }
}
