using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class ChlorineTrifluorideReaction : IGasReactionEffect
    {
        public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
        {
            var energyReleased = 0f;
            var oldHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture, true);
            var temperature = mixture.Temperature;
            var location = holder as TileAtmosphere;
            mixture.ReactionResults[(byte)GasReaction.Fire] = 0;

            var initialCLF3Moles = mixture.GetMoles(Gas.ChlorineTrifluoride);

            var decompositionRate = initialCLF3Moles * 0.15f;

            if (decompositionRate > Atmospherics.MinimumHeatCapacity)
            {
                mixture.SetMoles(Gas.ChlorineTrifluoride, initialCLF3Moles - decompositionRate);
                mixture.AdjustMoles(Gas.Chlorine, decompositionRate * 0.5f);
                mixture.AdjustMoles(Gas.Fluorine, decompositionRate * 1.5f);

                energyReleased = 315000f * decompositionRate;
                energyReleased /= heatScale;
                mixture.ReactionResults[(byte)GasReaction.Fire] = decompositionRate * 1.5f;
            }

            if (energyReleased > 0)
            {
                var newHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture, true);
                if (newHeatCapacity > Atmospherics.MinimumHeatCapacity)
                    mixture.Temperature = (temperature * oldHeatCapacity + energyReleased) / newHeatCapacity;

                mixture.Temperature = MathF.Max(mixture.Temperature, Atmospherics.PlasmaMinimumBurnTemperature + 900f);
            }

            if (location != null && decompositionRate > Atmospherics.MinimumHeatCapacity)
            {
                var exposedTemp = MathF.Max(Atmospherics.PlasmaMinimumBurnTemperature + 600f, mixture.Temperature);
                atmosphereSystem.HotspotExpose(location, exposedTemp, mixture.Volume, fuelGas: Gas.ChlorineTrifluoride);
            }

            return mixture.ReactionResults[(byte)GasReaction.Fire] != 0 ? (ReactionResult.Reacting | ReactionResult.StopReactions) : ReactionResult.NoReaction;
        }
    }
}
