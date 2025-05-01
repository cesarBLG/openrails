// COPYRIGHT 2025 by the Open Rails project.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orts.Simulation.RollingStocks.SubSystems.PowerTransmissions;
using Orts.Simulation.RollingStocks.SubSystems;
using Orts.Parsers.Msts;
using static Orts.Simulation.RollingStocks.SubSystems.PowerTransmissions.RheostaticMotorController;
using System.IO;
using Microsoft.Xna.Framework;
using ORTS.Common;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using ORTS.Scripting.Api;
using Orts.Simulation.Physics;
using static Orts.Simulation.RollingStocks.MSTSLocomotive;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerTransmissions
{
    public abstract class ElectricMotorController : ISubSystem<ElectricMotorController>, IParsable
    {
        public readonly MSTSLocomotive Locomotive;
        public Train Train => Locomotive.Train;
        public Simulator Simulator => Locomotive.Simulator;
        protected float ThrottlePercent => Locomotive.ThrottlePercent;
        protected float DynamicBrakePercent => Locomotive.DynamicBrakePercent;
        protected Axles LocomotiveAxles => Locomotive.LocomotiveAxles;
        public ElectricMotorController(MSTSLocomotive locomotive)
        {
            Locomotive = locomotive;
        }

        public virtual void Parse(string lowercasetoken, STFReader stf)
        {

        }
        public virtual void Copy(ElectricMotorController other)
        {

        }
        public virtual void Initialize()
        {
        }
        public virtual void InitializeMoving()
        {

        }
        public virtual void Update(float elapsedClockSeconds)
        {
        }
        public virtual void Save(BinaryWriter outf)
        {
        }
        public virtual void Restore(BinaryReader inf)
        {
        }
    }
    public class DefaultMotorController : ElectricMotorController
    {
        public DefaultMotorController(MSTSLocomotive locomotive) : base(locomotive)
        {

        }
        public override void Update(float elapsedClockSeconds)
        {
            base.Update(elapsedClockSeconds);
            
            Locomotive.UpdateTractionForce(elapsedClockSeconds);
            Locomotive.TractiveForceN = Locomotive.TractionForceN * (Locomotive.Direction == Direction.Reverse ? -1 : 1);

            Locomotive.UpdateDynamicBrakeForce(elapsedClockSeconds);
            Locomotive.TractiveForceN -= (Locomotive.SpeedMpS > 0 ? 1 : Locomotive.SpeedMpS < 0 ? -1 : Locomotive.Direction == Direction.Reverse ? -1 : 1) * Locomotive.DynamicBrakeForceN;

            foreach (var motor in Locomotive.TractionMotors)
            {
                float targetForceN = Locomotive.TractiveForceN / Locomotive.TractionMotors.Count;
                var axle = motor.AxleConnected;
                if (motor is InductionMotor ac)
                {
                    ac.TargetForceN = targetForceN;
                    float linToAngFactor = axle.TransmissionRatio / axle.WheelRadiusM;
                    if (Locomotive.SlipControlSystem == SlipControlType.Full)
                    {
                        if (targetForceN > 0) ac.DriveSpeedRadpS = (axle.TrainSpeedMpS + axle.WheelSlipThresholdMpS * 0.95f) * linToAngFactor + ac.OptimalAsyncSpeedRadpS;
                        else if (targetForceN < 0) ac.DriveSpeedRadpS = (axle.TrainSpeedMpS - axle.WheelSlipThresholdMpS * 0.95f) * linToAngFactor - ac.OptimalAsyncSpeedRadpS;
                    }
                    else
                    {
                        if (targetForceN > 0) ac.DriveSpeedRadpS = Locomotive.MaxSpeedMpS * linToAngFactor + ac.OptimalAsyncSpeedRadpS;
                        else if (targetForceN < 0) ac.DriveSpeedRadpS = -Locomotive.MaxSpeedMpS * linToAngFactor - ac.OptimalAsyncSpeedRadpS;
                    }
                }
                else if (motor is SimpleMotor dc)
                {
                    dc.TargetForceN = targetForceN;
                    if (Locomotive.SlipControlSystem == SlipControlType.Full)
                    {
                        // Simple slip control
                        // Motive force is reduced to the maximum adhesive force
                        // In wheelslip situations, motive force is set to zero
                        dc.TargetForceN = Math.Sign(dc.TargetForceN) * Math.Min(axle.MaximumWheelAdhesion * axle.AxleWeightN, Math.Abs(dc.TargetForceN));
                        if (axle.IsWheelSlip) dc.TargetForceN = 0;
                    }
                }
            }
        }
    }
}
