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
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Orts.Common;
using Orts.Parsers.Msts;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks.SubSystems.Controllers;
using static Orts.Simulation.RollingStocks.SubSystems.CruiseControl;

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
        public class ControllerNotch
        {
            /// <summary>
            /// Wiring scheme of the traction motors
            /// </summary>
            public ElectricMotorArrangement MotorArrangement;
            /// <summary>
            /// Resistance in series with the motor
            /// </summary>
            public float StartingResistorOhms;
            /// <summary>
            /// Reduction in the inductive field of the stator
            /// </summary>
            public float FieldWeakeningPercent;
            /// <summary>
            /// Relative position of next notch for advance
            /// </summary>
            public int DefaultAdvanceStep = 1;
            /// <summary>
            /// Relative position of next notch for regression
            /// </summary>
            public int DefaulRegressionStep = -1;
            /// <summary>
            /// Following notch when a specific target notch is requested
            /// </summary>
            public Dictionary<int, int> NextNotch;
            /// <summary>
            /// Stable notch
            /// </summary>
            public bool Stable = true;
            public bool LimitedTransition;
            public ControllerNotch(ElectricMotorArrangement arrangement, float startResistor, float fieldWeak, bool stable, bool limited)
            {
                MotorArrangement = arrangement;
                StartingResistorOhms = startResistor;
                FieldWeakeningPercent = fieldWeak;
                NextNotch = new Dictionary<int, int>();
                Stable = stable;
                LimitedTransition = limited;
            }
            public ControllerNotch(STFReader stf)
            {
                stf.MustMatch("(");
                stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("motorarrangement", () => Enum.TryParse(stf.ReadStringBlock(null), true, out MotorArrangement)),
                        new STFReader.TokenProcessor("resistance", () => { StartingResistorOhms = stf.ReadFloatBlock(STFReader.UNITS.Resistance, null); }),
                        new STFReader.TokenProcessor("fieldweakening", ()=>{ FieldWeakeningPercent = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                        new STFReader.TokenProcessor("stable", ()=>{ Stable = stf.ReadBoolBlock(true); }),
                        new STFReader.TokenProcessor("advancestep", ()=>{ DefaultAdvanceStep = stf.ReadIntBlock(1); }),
                        new STFReader.TokenProcessor("regressionstep", ()=>{ DefaulRegressionStep = stf.ReadIntBlock(0); }),
                        new STFReader.TokenProcessor("nextnotch", ()=>
                        {
                            stf.MustMatch("(");
                            while (!stf.EndOfBlock())
                            {
                                int target = stf.ReadIntBlock(0);
                                int next = stf.ReadIntBlock(0);
                                NextNotch[target] = next;
                            }
                        }),
                });
            }
            public override bool Equals(object obj)
            {
                var step = obj as ControllerNotch;
                if (step == null) return false;
                return (MotorArrangement, StartingResistorOhms, FieldWeakeningPercent).Equals((step.MotorArrangement, step.StartingResistorOhms, step.FieldWeakeningPercent));
            }
            public override int GetHashCode()
            {
                return (MotorArrangement, StartingResistorOhms, FieldWeakeningPercent).GetHashCode();
            }
        }
        List<ControllerNotch> ControllerNotches = new List<ControllerNotch>();
        int CurrentControllerStepIndex;
        ControllerNotch CurrentControllerStep
        {
            get
            {
                return ControllerNotches[CurrentControllerStepIndex];
            }
            set
            {
                CurrentControllerStepIndex = ControllerNotches.IndexOf(value);
            }
        }
        int TargetControllerStepIndex;
        ControllerNotch TargetControllerStep
        {
            get
            {
                return ControllerNotches[TargetControllerStepIndex];
            }
            set
            {
                TargetControllerStepIndex = ControllerNotches.IndexOf(value);
            }
        }
        float StepUnchangedTimeS;
        public readonly float MinimumStepTimeS = 1;
        SortedDictionary<float,float> ThrottleToFieldWeakening = new SortedDictionary<float, float>();
        SortedDictionary<float,float> ThrottleToStartingResistance = new SortedDictionary<float, float>();
        SortedDictionary<float,ElectricMotorArrangement> ThrottleToMotorArrangement = new SortedDictionary<float, ElectricMotorArrangement>();
        MSTSNotchController FieldWeakeningController;
        MSTSNotchController CurrentLimitationController;
        bool AutomaticControl;
        public RheostaticMotorController(MSTSLocomotive locomotive) : base(locomotive)
        {
            float[] sh = new float[] { 0, 0.25f, 0.4f, 0.525f };
            FieldWeakeningController = new MSTSNotchController(new MSTSNotch[] {
                new MSTSNotch(0, false, 0),
                new MSTSNotch(0.25f, false, 0),
                new MSTSNotch(0.4f, false, 0),
                new MSTSNotch(0.525f, false, 0),
            }.ToList())
            {
                MinimumValue = 0,
                MaximumValue = 0.525f,
                StepSize = 0.1f,
            };
            ControllerNotches.Add(new ControllerNotch(ElectricMotorArrangement.None, 0, 0, true, false));
            for (int i = 0; i < 4; i++)
            {
                ControllerNotches.Add(new ControllerNotch(ElectricMotorArrangement.Series, 100 - i * 10, 0, true, true));
            }
            for (int i = 0; i < 6; i++)
            {
                ControllerNotches.Add(new ControllerNotch(ElectricMotorArrangement.Series, 50 - i * 10, 0, i == 5, true));
            }
            for (int i = 0; i < 3; i++)
            {
                ControllerNotches.Add(new ControllerNotch(ElectricMotorArrangement.Series, 0, sh[i + 1], true, true));
            }
            for (int i = 0; i < 8; i++)
            {
                ControllerNotches.Add(new ControllerNotch(ElectricMotorArrangement.Parallel, 70 - i * 10, 0, i == 7, true));
            }
            for (int i = 0; i < 3; i++)
            {
                ControllerNotches.Add(new ControllerNotch(ElectricMotorArrangement.Parallel, 0, sh[i + 1], true, true));
            }
            ControllerNotches[10].DefaultAdvanceStep = 4;
            ControllerNotches[10].NextNotch[11] = 11;
            ControllerNotches[10].NextNotch[12] = 11;
            ControllerNotches[10].NextNotch[13] = 11;
            ControllerNotches[14].DefaulRegressionStep = -4;
            /*float[] sh = new float[] { 0, 0.24f, 0.4f, 0.5f, 0.6f };
            ControllerNotches.Add(new ControllerNotch(ElectricMotorArrangement.None, 0, 0, true, false));
            for (int i = 0; i < 5; i++)
            {
                ControllerNotches.Add(new ControllerNotch(ElectricMotorArrangement.Series, 100 - i * 5, 0, i == 4, true));
            }
            for (int i = 0; i < 8; i++)
            {
                ControllerNotches.Add(new ControllerNotch(ElectricMotorArrangement.Series, 70 - i * 10, 0, i == 7, true));
            }
            for (int i = 0; i < 4; i++)
            {
                ControllerNotches.Add(new ControllerNotch(ElectricMotorArrangement.Series, 0, sh[i + 1], i == 1 || i == 3, true));
            }
            for (int i = 0; i < 8; i++)
            {
                ControllerNotches.Add(new ControllerNotch(ElectricMotorArrangement.Parallel, 70 - i * 10, 0, i == 7, true));
            }
            for (int i = 0; i < 4; i++)
            {
                ControllerNotches.Add(new ControllerNotch(ElectricMotorArrangement.Parallel, 0, sh[i + 1], true, true));
            }
            foreach (var step in ControllerNotches)
            {
                step.DefaulRegressionStep = 0;
            }
            for (int i = 0; i < 4; i++)
            {
                ControllerNotches[26 + i].NextNotch[25] = 25;
            }*/
        }
        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(enginecontrollers(field_weakening": FieldWeakeningController = new MSTSNotchController(stf); break;
                case "engine(ortstractionmotorcontroller(camshaftpositions": break;
            }
        }
        public override void Copy(ElectricMotorController other)
        {
            if (other is RheostaticMotorController o)
            {
                FieldWeakeningController = new MSTSNotchController(o.FieldWeakeningController);
                ControllerNotches = o.ControllerNotches;
            }
        }
        public override void Initialize()
        {
            // Map throttle positions to stable camshaft positions
            if (ThrottleToMotorArrangement.Count == 0)
            {
                var throttleNotches = new List<ControllerNotch>();
                for (int i = 0; i < ControllerNotches.Count; i++)
                {
                    var notch = ControllerNotches[i];
                    if (notch.Stable)
                    {
                        if (FieldWeakeningController != null && throttleNotches.Count > 0)
                        {
                            var lastNotch = throttleNotches.Last();
                            if (lastNotch.MotorArrangement == notch.MotorArrangement && lastNotch.StartingResistorOhms == notch.StartingResistorOhms) continue;
                        }
                        throttleNotches.Add(notch);
                    }
                }
                for (int i=0; i<throttleNotches.Count; i++)
                {
                    float t = Locomotive.ThrottleController.GetNotchByIndex(i)?.Value ?? (float)i / (throttleNotches.Count - 1);
                    ThrottleToStartingResistance[t] = throttleNotches[i].StartingResistorOhms;
                    ThrottleToFieldWeakening[t] = throttleNotches[i].FieldWeakeningPercent;
                    ThrottleToMotorArrangement[t] = throttleNotches[i].MotorArrangement;
                }
            }
        }
        public override void InitializeMoving()
        {
            
        }
        protected int FindStableStepIndex(ElectricMotorArrangement motorArrangement, float fieldWeakeningPercent, float startingResistor)
        {
            for (int i=0; i < ControllerNotches.Count; i++)
            {
                var step = ControllerNotches[i];
                if (step.Stable && step.MotorArrangement == motorArrangement && step.FieldWeakeningPercent == fieldWeakeningPercent && step.StartingResistorOhms == startingResistor)
                {
                    return i;
                }
            }
            return -1;
        }
        private float previousThrottlePercent;
        public override void Update(float elapsedClockSeconds)
        {
            FieldWeakeningController?.Update(elapsedClockSeconds);
            CurrentLimitationController?.Update(elapsedClockSeconds);
            if (FieldWeakeningController != null && FieldWeakeningController.UpdateValue != 0.0)
            {
                Simulator.Confirmer.UpdateWithPerCent(
                    CabControl.FieldWeakening,
                    FieldWeakeningController.UpdateValue > 0 ? CabSetting.Increase : CabSetting.Decrease,
                    FieldWeakeningController.CurrentValue * 100);
            }

            bool power = Locomotive.LocomotivePowerSupply.MainPowerSupplyOn && Locomotive.MaxThrottlePercent > 0;
            if (!power)
            {
                CurrentControllerStepIndex = TargetControllerStepIndex = 0;
            }
            else if (AutomaticControl || (Locomotive.CruiseControl != null && Locomotive.CruiseControl.SpeedRegMode == CruiseControl.SpeedRegulatorMode.Auto))
            {
                int numPositions = 0;
                for (int i=0; i < ControllerNotches.Count; i++)
                {
                    if (ControllerNotches[i].Stable && ControllerNotches[i].StartingResistorOhms == 0) numPositions++;
                }
                int currPosition = 0;
                for (int i=0; i < ControllerNotches.Count; i++)
                {
                    if (ControllerNotches[i].Stable && ControllerNotches[i].StartingResistorOhms == 0)
                    {
                        if (currPosition > previousThrottlePercent / 100 * numPositions) break;
                        TargetControllerStepIndex = i;
                        currPosition++;
                    }
                }
            }
            else if (ThrottlePercent > 0)
            {
                var lead = Locomotive.RemoteControlGroup == 0 ? Locomotive.Train.LeadLocomotive : null;
                float t = ThrottlePercent / 100;

                ElectricMotorArrangement targetArrangement = ElectricMotorArrangement.None;
                foreach (var kvp in ThrottleToMotorArrangement)
                {
                    if (kvp.Key <= t + 1e-5f) targetArrangement = kvp.Value;
                }

                float targetResistor = 0;
                foreach (var kvp in ThrottleToStartingResistance)
                {
                    if (kvp.Key <= t + 1e-5f) targetResistor = kvp.Value;
                }

                float targetShunt = 0;
                foreach (var kvp in ThrottleToFieldWeakening)
                {
                    if (kvp.Key <= t + 1e-5f) targetShunt = kvp.Value;
                }
                if (FieldWeakeningController != null)
                {
                    if (lead != null && lead != Locomotive)
                    {
                    }
                    else
                    {
                        targetShunt = FieldWeakeningController.CurrentValue;
                    }
                }

                TargetControllerStepIndex = FindStableStepIndex(targetArrangement, targetShunt, targetResistor);
                if (TargetControllerStepIndex < 0 && FieldWeakeningController != null) TargetControllerStepIndex = FindStableStepIndex(targetArrangement, 0, targetResistor);
                if (TargetControllerStepIndex < 0) TargetControllerStepIndex = 0;

                previousThrottlePercent = ThrottlePercent;
            }
            else
            {
                TargetControllerStepIndex = 0;
            }

            StepUnchangedTimeS += elapsedClockSeconds;
            if (!TargetControllerStep.Stable || TargetControllerStepIndex >= ControllerNotches.Count || TargetControllerStepIndex < 0) TargetControllerStepIndex = 0;

            if (TargetControllerStepIndex == 0) CurrentControllerStepIndex = 0;
            else if (CurrentControllerStepIndex != TargetControllerStepIndex)
            {
                int next;
                if (CurrentControllerStep.NextNotch.TryGetValue(TargetControllerStepIndex, out int index)) next = index;
                else if (TargetControllerStepIndex < CurrentControllerStepIndex && CurrentControllerStep.DefaulRegressionStep != 0) next = CurrentControllerStepIndex + CurrentControllerStep.DefaulRegressionStep;
                else if (!CurrentControllerStep.Stable || TargetControllerStepIndex > CurrentControllerStepIndex) next = CurrentControllerStepIndex + CurrentControllerStep.DefaultAdvanceStep;
                else next = CurrentControllerStepIndex;
                if (next != CurrentControllerStepIndex && next >= 0 && next < ControllerNotches.Count)
                {
                    if (((!CurrentControllerStep.LimitedTransition || true) || next < CurrentControllerStepIndex) && StepUnchangedTimeS > MinimumStepTimeS)
                    {
                        CurrentControllerStepIndex = next;
                        StepUnchangedTimeS = 0;
                        //Locomotive.SignalEvent();
                    }
                }
            }

            /*float volts = ElectricLocomotive.ElectricPowerSupply.FilterVoltageV;
            if (CurrentControllerStepIndex.MotorArrangement == ElectricMotorArrangement.None) volts = 0;
            else if (CurrentControllerStepIndex.MotorArrangement == ElectricMotorArrangement.Series) volts /= ElectricLocomotive.TractionMotors.Count;
            else if (CurrentControllerStepIndex.MotorArrangement == ElectricMotorArrangement.SeriesParallel) volts /= 2;
            foreach (var axle in LocomotiveAxles)
            {
                if (axle.Motor is SeriesMotor motor)
                {
                    motor.TerminalVoltageV = volts;
                    motor.ShuntPercent = CurrentControllerStepIndex.FieldWeakeningPercent;
                    motor.StartingResistorOhms = CurrentControllerStepIndex.StartingResistorOhms;
                }
            }*/

            float vt = (float)CurrentControllerStepIndex / (ControllerNotches.Count - 1);
            float f = ElectricLocomotive.GetAvailableTractionForceN(vt);
            Locomotive.TractionForceN = vt;
            foreach (var motor in ElectricLocomotive.TractionMotors)
            {
                if (motor is SimpleMotor dc)
                {
                    dc.TargetForceN = f / ElectricLocomotive.TractionMotors.Count;
                }
            }
            Locomotive.Simulator.Confirmer.Message(ConfirmLevel.None, string.Format("{0} {1}", CurrentControllerStepIndex, TargetControllerStepIndex));
        }
        public override void Save(BinaryWriter outf)
        {
            outf.Write(previousThrottlePercent);
            FieldWeakeningController?.Save(outf);
        }
        public override void Restore(BinaryReader inf)
        {
            previousThrottlePercent = inf.ReadSingle();
            FieldWeakeningController?.Restore(inf);
        }

        public void StartFieldWeakeningIncrease(float? target)
        {
            if (FieldWeakeningController == null) return;
            FieldWeakeningController.CommandStartTime = Simulator.ClockTime;
            FieldWeakeningController.StartIncrease(target);
        }

        public void StopFieldWeakeningIncrease()
        {
            FieldWeakeningController?.StopIncrease();
        }

        public void StartFieldWeakeningDecrease(float? target)
        {
            FieldWeakeningController?.StartDecrease(target);
        }

        public void StopFieldWeakeningDecrease()
        {
            FieldWeakeningController?.StopDecrease();
        }
    }
}
