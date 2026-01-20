using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class ChlorineFluorideProductionReaction : IGasReactionEffect
    {
        public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
        {
            // Prevent immediate recombination into ClF3 if a fire reaction has just occurred this tick
            if (mixture.ReactionResults[(byte)GasReaction.Fire] != 0)
                return ReactionResult.NoReaction;

            var oldHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture, true);
            var temperature = mixture.Temperature;
            var energyReleased = 0f;

            var nCl2 = mixture.GetMoles(Gas.Chlorine);
            var nF2 = mixture.GetMoles(Gas.Fluorine);

            var limiting = MathF.Min(nCl2 / 1f, nF2 / 3f);
            var extent = limiting / Atmospherics.ClF3ProductionRate;

            if (extent > Atmospherics.GasMinMoles)
            {
                mixture.AdjustMoles(Gas.Chlorine, -extent * 1f);
                mixture.AdjustMoles(Gas.Fluorine, -extent * 3f);
                mixture.AdjustMoles(Gas.ChlorineTrifluoride, extent * 2f);

                energyReleased = Atmospherics.FirePlasmaEnergyReleased * extent * 0.8f;
                energyReleased /= heatScale;
            }

            if (energyReleased > 0)
            {
                var newHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture, true);
                if (newHeatCapacity > Atmospherics.MinimumHeatCapacity)
                    mixture.Temperature = (temperature * oldHeatCapacity + energyReleased) / newHeatCapacity;
            }

            return extent > Atmospherics.GasMinMoles ? ReactionResult.Reacting : ReactionResult.NoReaction;
        }
    }
}
