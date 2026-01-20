using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class HydrogenFireReaction : IGasReactionEffect
    {
        public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
        {
            var energyReleased = 0f;
            var oldHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture, true);
            var temperature = mixture.Temperature;
            var location = holder as TileAtmosphere;
            mixture.ReactionResults[(byte)GasReaction.Fire] = 0;

            // More hydrogen burns at higher temperatures (like plasma)
            // If a hotspot exists, use its temperature for the reaction
            var reactionTemperature = temperature;
            if (location?.Hotspot.Valid == true)
            {
                reactionTemperature = location.Hotspot.Temperature;
            }

            var temperatureScale = 0f;

            if (reactionTemperature > Atmospherics.PlasmaUpperTemperature)
                temperatureScale = 1f;
            else if (reactionTemperature > Atmospherics.PlasmaMinimumBurnTemperature)
            {
                temperatureScale = (reactionTemperature - Atmospherics.PlasmaMinimumBurnTemperature) /
                                   (Atmospherics.PlasmaUpperTemperature - Atmospherics.PlasmaMinimumBurnTemperature);
            }
            // else temperatureScale stays 0

            if (temperatureScale > 0)
            {
                var initialOxygenMoles = mixture.GetMoles(Gas.Oxygen);
                var initialHydrogenMoles = mixture.GetMoles(Gas.Hydrogen);

                // 2H2 + O2 -> 2H2O
                // Burn rate scales with temperature
                var hydrogenBurnRate = initialHydrogenMoles * temperatureScale * 0.1f; // Base burn rate 10% per cycle

                // Limited by available oxygen (stoichiometric ratio 2:1)
                var maxBurnByOxygen = initialOxygenMoles * 2f;
                hydrogenBurnRate = MathF.Min(hydrogenBurnRate, maxBurnByOxygen);

                if (hydrogenBurnRate > Atmospherics.MinimumHeatCapacity)
                {
                    var oxygenUsed = hydrogenBurnRate * 0.5f;

                    mixture.SetMoles(Gas.Hydrogen, initialHydrogenMoles - hydrogenBurnRate);
                    mixture.SetMoles(Gas.Oxygen, initialOxygenMoles - oxygenUsed);
                    mixture.AdjustMoles(Gas.WaterVapor, hydrogenBurnRate);

                    energyReleased = Atmospherics.FireHydrogenEnergyReleased * hydrogenBurnRate;
                    energyReleased /= heatScale;
                    mixture.ReactionResults[(byte)GasReaction.Fire] = hydrogenBurnRate + oxygenUsed;
                }
            }

            if (energyReleased > 0)
            {
                var newHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture, true);
                if (newHeatCapacity > Atmospherics.MinimumHeatCapacity)
                    mixture.Temperature = (temperature * oldHeatCapacity + energyReleased) / newHeatCapacity;
            }

            if (location != null)
            {
                temperature = mixture.Temperature;
                if (temperature > Atmospherics.FireMinimumTemperatureToExist)
                {
                    atmosphereSystem.HotspotExpose(location, temperature, mixture.Volume, fuelGas: Gas.Hydrogen);
                }
            }

            return mixture.ReactionResults[(byte)GasReaction.Fire] != 0 ? ReactionResult.Reacting : ReactionResult.NoReaction;
        }
    }
}
