﻿// COPYRIGHT 2021 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

using System;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;

namespace ORTS.Scripting.Api
{
    public abstract class PassengerCarPowerSupply : PowerSupply
    {
        // Internal members and methods (inaccessible from script)
        internal ScriptedPassengerCarPowerSupply PcpsHost => Host as ScriptedPassengerCarPowerSupply;
        internal MSTSWagon Wagon => PcpsHost.Wagon;

        /// <summary>
        /// Current state of the ventilation system
        /// </summary>
        public PowerSupplyState CurrentVentilationState() => PcpsHost.VentilationState;

        /// <summary>
        /// Current state of the heating system
        /// </summary>
        public PowerSupplyState CurrentHeatingState() => PcpsHost.HeatingState;

        /// <summary>
        /// Current state of the air conditioning system
        /// </summary>
        public PowerSupplyState CurrentAirConditioningState() => PcpsHost.AirConditioningState;

        /// <summary>
        /// Current power consumed on the electric power supply line
        /// </summary>
        public float CurrentElectricTrainSupplyPowerW() => PcpsHost.ElectricTrainSupplyPowerW;

        /// <summary>
        /// Current thermal power generated by the heating and air conditioning systems
        /// Positive if heating
        /// Negative if air conditioning (cooling)
        /// </summary>
        public float CurrentHeatFlowRateW() => PcpsHost.HeatFlowRateW;

        /// <summary>
        /// Systems power on delay when electric train supply has been switched on
        /// </summary>
        public float PowerOnDelayS() => PcpsHost.PowerOnDelayS;

        /// <summary>
        /// Power consumed all the time on the electric train supply line
        /// </summary>
        public float ContinuousPowerW() => PcpsHost.ContinuousPowerW;

        /// <summary>
        /// Power consumed when heating is on
        /// </summary>
        public float HeatingPowerW() => PcpsHost.HeatingPowerW;

        /// <summary>
        /// Power consumed when air conditioning is on
        /// </summary>
        public float AirConditioningPowerW() => PcpsHost.AirConditioningPowerW;

        /// <summary>
        /// Yield of the air conditioning system
        /// </summary>
        public float AirConditioningYield() => PcpsHost.AirConditioningYield;

        /// <summary>
        /// Desired temperature inside the passenger car
        /// </summary>
        public float DesiredTemperatureC() => Wagon.DesiredCompartmentTempSetpointC;

        /// <summary>
        /// Current temperature inside the passenger car
        /// </summary>
        public float InsideTemperatureC() => Wagon.CarInsideTempC;

        /// <summary>
        /// Current temperature outside the passenger car
        /// </summary>
        public float OutsideTemperatureC() => Wagon.CarOutsideTempC;

        /// <summary>
        /// Sets the current state of the ventilation system
        /// </summary>
        public void SetCurrentVentilationState(PowerSupplyState state)
        {
            PcpsHost.VentilationState = state;
        }

        /// <summary>
        /// Sets the current state of the heating system
        /// </summary>
        public void SetCurrentHeatingState(PowerSupplyState state)
        {
            PcpsHost.HeatingState = state;
        }

        /// <summary>
        /// Sets the current state of the air conditioning system
        /// </summary>
        public void SetCurrentAirConditioningState(PowerSupplyState state)
        {
            PcpsHost.AirConditioningState = state;
        }

        /// <summary>
        /// Sets the current power consumed on the electric power supply line
        /// </summary>
        public void SetCurrentElectricTrainSupplyPowerW(float value)
        {
            if (value >= 0f)
            {
                PcpsHost.ElectricTrainSupplyPowerW = value;
            }
        }

        /// <summary>
        /// Sets the current thermal power generated by the heating and air conditioning systems
        /// Positive if heating
        /// Negative if air conditioning (cooling)
        /// </summary>
        public void SetCurrentHeatFlowRateW(float value)
        {
            PcpsHost.HeatFlowRateW = value;
        }
    }
}
