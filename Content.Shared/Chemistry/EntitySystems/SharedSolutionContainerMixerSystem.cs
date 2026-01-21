using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Fluids;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using System.Collections;

namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
/// This handles <see cref="SolutionContainerMixerComponent"/>
/// </summary>
public abstract class SharedSolutionContainerMixerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<SolutionContainerMixerComponent, ActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<SolutionContainerMixerComponent, ContainerIsRemovingAttemptEvent>(OnRemoveAttempt);
        SubscribeLocalEvent<SolutionContainerMixerComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
    }

    private void OnActivateInWorld(Entity<SolutionContainerMixerComponent> entity, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        TryStartMix(entity, args.User);
        args.Handled = true;
    }

    private void OnRemoveAttempt(Entity<SolutionContainerMixerComponent> ent, ref ContainerIsRemovingAttemptEvent args)
    {
        // Check if the removed container is in the list of containers to mix
        if (ent.Comp.ContainerIds.Contains(args.Container.ID) && ent.Comp.Mixing)
            args.Cancel();
    }

    private void OnInsertAttempt(Entity<SolutionContainerMixerComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (ent.Comp.Mixing)
            args.Cancel();
    }

    private bool IsPrime(int number)
    {
        if (number == 1)
            return false;
        if (number == 2)
            return true;

        var limit = Math.Ceiling(Math.Sqrt(number));

        for (int i = 2; i <= limit; i++)
        {
            if (number % i == 0)
                return false;
        }
        return true;

    }

    protected virtual bool HasPower(Entity<SolutionContainerMixerComponent> entity)
    {
        return true;
    }

    public void TryStartMix(Entity<SolutionContainerMixerComponent> entity, EntityUid? user)
    {
        var (uid, comp) = entity;
        if (comp.Mixing)
            return;

        if (!HasPower(entity))
        {
            if (user != null)
                _popup.PopupClient(Loc.GetString("solution-container-mixer-no-power"), entity, user.Value);
            return;
        }

        // Check all configured containers to see if any have contents
        int insertedContainers = 0;
        foreach (var containerId in comp.ContainerIds)
        {
            if (_container.TryGetContainer(uid, containerId, out var container) && container.Count > 0)
                insertedContainers++;
        }

        if (insertedContainers == 0)
        {
            if (user != null)
                _popup.PopupClient(Loc.GetString("solution-container-mixer-popup-nothing-to-mix"), entity, user.Value);
            return;
        }

        comp.Mixing = true;
        if (_net.IsServer)
            comp.MixingSoundEntity = _audio.PlayPvs(comp.MixingSound, entity, comp.MixingSound?.Params.WithLoop(true));
        comp.MixTimeEnd = _timing.CurTime + comp.MixDuration * insertedContainers;
        _appearance.SetData(entity, SolutionContainerMixerVisuals.Mixing, true);
        Dirty(uid, comp);
    }

    public void StopMix(Entity<SolutionContainerMixerComponent> entity)
    {
        var (uid, comp) = entity;
        if (!comp.Mixing)
            return;
        _audio.Stop(comp.MixingSoundEntity);
        _appearance.SetData(entity, SolutionContainerMixerVisuals.Mixing, false);
        comp.Mixing = false;
        comp.MixingSoundEntity = null;
        Dirty(uid, comp);
    }

    public void FinishMix(Entity<SolutionContainerMixerComponent> entity)
    {
        var (uid, comp) = entity;
        if (!comp.Mixing)
            return;
        StopMix(entity);

        if (!TryComp<ReactionMixerComponent>(entity, out var reactionMixer))
            return;

        // Process mixing reactions for all configured containers
        Dictionary<Entity<SolutionComponent>, int> insertedContainerSolutions = new();
        foreach (var containerId in comp.ContainerIds)
        {
            if (!_container.TryGetContainer(uid, containerId, out var container))
                continue;

            foreach (var ent in container.ContainedEntities)
            {
                if (!_solution.TryGetFitsInDispenser(ent, out var solnComp, out var soln))
                    continue;

                insertedContainerSolutions.Add(solnComp.Value, soln.Volume.Value);
            }
        }

        if (insertedContainerSolutions.Count == 0)
            return;

        // Spill the contents if the inserted vials are not balanced.
        // If I were a real programmer this would use some kind of math to figure out
        // if a given number of vials can be balanced given some quantity of maximum slots
        // ...but I'm not, so it's hardcoded for 6 slots.
        // Thanks, Iswar! ( https://mattbaker.blog/2018/06/25/the-balanced-centrifuge-problem/ )
        int n = comp.ContainerIds.Count;
        int k = insertedContainerSolutions.Count;
        if (IsPrime(n) && n != k)
        {
            foreach (var pair in insertedContainerSolutions)
            {
                var puddleSolution = _solution.SplitSolution(pair.Key, pair.Value);
                _puddle.TrySpillAt(entity, puddleSolution, out _);
            }
            return;
        }
        ArrayList<int> primeDivisors = new();
        for (int i = 1; i < n; i += 2)
        {
            if (IsPrime(i) && n % i == 0)
            {
                primeDivisors.Add(i);
            }
        }
        int nCopy = n;
        int kCopy = k;
        for (int i = 0; i < primeDivisors.Count; i++)
        {
            for (int j = i; j < primeDivisors.Count; j++)
            {
                int left = primeDivisors[i];
                int right = primeDivisors[j];
                int sum = left + right;
                if (sum == nCopy)
                    n = 0;
                if (sum == nCopy - kCopy)
                    k = 0;
                if (n == 0 && k == 0)
                    break;
            }
        }
        if (n != 0 || k != 0)
        {
            foreach (var pair in insertedContainerSolutions)
            {
                var puddleSolution = _solution.SplitSolution(pair.Key, pair.Value);
                _puddle.TrySpillAt(entity, puddleSolution, out _);
            }
            return;
        }
        foreach (var pair in insertedContainerSolutions)
        {
            _solution.UpdateChemicals(pair.Key, true, reactionMixer);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SolutionContainerMixerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Mixing)
                continue;

            if (_timing.CurTime < comp.MixTimeEnd)
                continue;

            FinishMix((uid, comp));
        }
    }
}
