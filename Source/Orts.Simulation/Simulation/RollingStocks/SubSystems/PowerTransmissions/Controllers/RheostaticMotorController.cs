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


using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Orts.Parsers.Msts;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks.SubSystems.Controllers;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerTransmissions
{
    public class RheostaticMotorController : ElectricMotorController
    {
        MSTSElectricLocomotive ElectricLocomotive => Locomotive as MSTSElectricLocomotive;
        public enum ElectricMotorArrangement
        {
            None,
            Series,
            SeriesParallel,
            Parallel,
        }
        Dictionary<float,float> ThrottleToFieldWeakening;
        Dictionary<float,float> ThrottleToStartingResistance;
        Dictionary<ElectricMotorArrangement, float> MotorArrangementThrottleThreshold;
        MSTSNotchController FieldWeakeningController;
        MSTSNotchController StartingResistanceController;
        bool AutomaticControl;
        bool AutoNotch;
        float AutoNotchStepSize;
        bool AutoResetFieldWeakening;
        public float MaxStartingResistorOhms;
        public float StartingResistorOhms { get; private set; }
        public float FieldWeakeningPercent { get; private set; }
        public ElectricMotorArrangement CurrentMotorArrangement { get; private set; }
        public RheostaticMotorController()
        {
            AutoNotch = true;
            AutoResetFieldWeakening = true;
            FieldWeakeningController = new MSTSNotchController(new MSTSNotch[] {
                new MSTSNotch(0, false, 0),
                new MSTSNotch(0.2f, false, 0),
                new MSTSNotch(0.33f, false, 0),
            }.ToList())
            {
                MinimumValue = 0,
                MaximumValue = 0.33f,
                StepSize = 0.1f,
            };
            var notches = new List<MSTSNotch>();
            for (int i=0; i<=10; i++)
            {
                notches.Append(new MSTSNotch(i / 10.0f, false, 0));
            }
            StartingResistanceController = new MSTSNotchController(notches)
            {
                MinimumValue = 0,
                MaximumValue = 1,
                StepSize = 0.1f,
            };
            MaxStartingResistorOhms = 300;
        }
        public override void Parse(string lowercasetoken, STFReader stf)
        {

        }
        public override void Copy(ElectricMotorController other)
        {
            if (other is RheostaticMotorController o)
            {

            }
        }
        public override void Initialize()
        {
        }
        public override void InitializeMoving()
        {
            
        }
        private float previousThrottlePercent;
        public override void Update(float elapsedClockSeconds)
        {
            bool throttleChanged = previousThrottlePercent != ThrottlePercent;
            if (ThrottlePercent == 0 && DynamicBrakePercent <= 0)
            {
                if (throttleChanged)
                {
                    if (AutoResetFieldWeakening) FieldWeakeningController.SetPercent(0);
                }
                CurrentMotorArrangement = ElectricMotorArrangement.None;
            }
            else if (AutomaticControl)
            {
                // TODO: Adjust field weakening and resistances depending on demanded throttle percent
            }
            else
            {
                var lead = Locomotive.RemoteControlGroup == 0 ? Locomotive.Train.LeadLocomotive : null;
                if (ThrottleToStartingResistance.TryGetValue(ThrottlePercent / 100, out float resist))
                {
                    if (throttleChanged) StartingResistanceController.SetValue(resist);
                }
                else if (AutoNotch)
                {
                    if (throttleChanged)
                    {
                        StartingResistanceController.StepSize = AutoNotchStepSize;
                        StartingResistanceController.SetPercent(100);
                        StartingResistanceController.StartDecrease(0);
                    }
                }
                else if (lead != null && lead != Locomotive)
                {

                }

                if (ThrottleToFieldWeakening.TryGetValue(ThrottlePercent / 100, out float fieldWeak))
                {
                    if (throttleChanged) FieldWeakeningController.SetValue(fieldWeak);
                }
                else if (AutoResetFieldWeakening)
                {
                    if (throttleChanged) FieldWeakeningController.SetPercent(0);
                }
                else if (lead != null && lead != Locomotive)
                {

                }

                if (MotorArrangementThrottleThreshold.TryGetValue(ElectricMotorArrangement.Parallel, out float thr) && thr >= ThrottlePercent) CurrentMotorArrangement = ElectricMotorArrangement.Parallel;
                else if (MotorArrangementThrottleThreshold.TryGetValue(ElectricMotorArrangement.SeriesParallel, out thr) && thr >= ThrottlePercent) CurrentMotorArrangement = ElectricMotorArrangement.SeriesParallel;
                else if (MotorArrangementThrottleThreshold.TryGetValue(ElectricMotorArrangement.Series, out thr) && thr >= ThrottlePercent) CurrentMotorArrangement = ElectricMotorArrangement.Series;
                else CurrentMotorArrangement = ElectricMotorArrangement.None;
            }

            StartingResistanceController.Update(elapsedClockSeconds);
            FieldWeakeningController.Update(elapsedClockSeconds);

            StartingResistorOhms = StartingResistanceController.CurrentValue * MaxStartingResistorOhms;
            FieldWeakeningPercent = FieldWeakeningController.CurrentValue * 100;

            previousThrottlePercent = ThrottlePercent;

            float volts = ElectricLocomotive.ElectricPowerSupply.FilterVoltageV;
            if (CurrentMotorArrangement == ElectricMotorArrangement.None) volts = 0;
            else if (CurrentMotorArrangement == ElectricMotorArrangement.Series) volts /= ElectricLocomotive.TractionMotors.Count;
            else if (CurrentMotorArrangement == ElectricMotorArrangement.SeriesParallel) volts /= 2;
            foreach (var axle in LocomotiveAxles)
            {
                if (axle.Motor is SeriesMotor motor)
                {
                    motor.TerminalVoltageV = volts;
                    motor.ShuntPercent = FieldWeakeningPercent;
                    motor.StartingResistorOhms = StartingResistorOhms;
                }
            }
        }
        public override void Save(BinaryWriter outf)
        {
            outf.Write(previousThrottlePercent);
            StartingResistanceController.Save(outf);
            FieldWeakeningController.Save(outf);
        }
        public override void Restore(BinaryReader inf)
        {
            previousThrottlePercent = inf.ReadSingle();
            StartingResistanceController.Restore(inf);
            FieldWeakeningController.Restore(inf);
        }
    }
}
