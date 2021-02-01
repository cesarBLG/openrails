// COPYRIGHT 2013 - 2021 by the Open Rails project.
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

using Orts.Parsers.Msts;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.IO;

namespace Orts.Simulation.RollingStocks.SubSystems
{
    public class CruiseControl

    {
        public CruiseControl(MSTSLocomotive locomotive)
        {
            Locomotive = locomotive;
        }
        MSTSLocomotive Locomotive;
        Simulator Simulator;

        public bool Equipped = false;
        public bool SpeedRegulatorMaxForcePercentUnits = false;
        public float SpeedRegulatorMaxForceSteps = 0;
        public bool MaxForceSetSingleStep = false;
        public bool MaxForceKeepSelectedStepWhenManualModeSet = false;
        public List<string> SpeedRegulatorOptions = new List<string>();
        public SpeedRegulatorMode SpeedRegMode = SpeedRegulatorMode.Manual;
        public SpeedSelectorMode SpeedSelMode = SpeedSelectorMode.Neutral;
        public float SelectedMaxAccelerationPercent = 0;
        public float SelectedMaxAccelerationStep = 0;
        public float SelectedSpeedMpS = 0;
        public int SelectedNumberOfAxles = 0;
        public float SpeedRegulatorNominalSpeedStepMpS = 0;
        public float MaxAccelerationMpSS = 0;
        public float MaxDecelerationMpSS = 0;
        public bool UseThrottle = false;
        public bool AntiWheelSpinEquipped = false;
        public float DynamicBrakeMaxForceAtSelectorStep = 0;
        public float DynamicBrakeDescentCoefficient = 0;
        public float DeltaCoefficient = 0;
        public float ForceThrottleAndDynamicBrake = 0;
        protected float maxForceN = 0;
        protected float trainBrakePercent = 0;
        protected float trainLength = 0;
        public int TrainLengthMeters = 0;
        public int RemainingTrainLengthToPassRestrictedZone = 0;
        public bool RestrictedSpeedActive = false;
        public float CurrentSelectedSpeedMpS = 0;
        protected float nextSelectedSpeedMps = 0;
        protected float restrictedRegionTravelledDistance = 0;
        protected float currentThrottlePercent = 0;
        protected double clockTime = 0;
        protected bool dynamicBrakeSetToZero = false;
        public float StartReducingSpeedDelta = 0.5f;
        public float ThrottleIncreaseSpeed = 0.1f;
        public float ThrottleDecreaseSpeed = 0.2f;
        public bool Battery = false;
        public bool DynamicBrakePriority = false;
        public List<int> ForceStepsThrottleTable = new List<int>();
        public List<float> AccelerationTable = new List<float>();
        public enum SpeedRegulatorMode { Manual, Auto, Testing, AVV }
        public enum SpeedSelectorMode { Parking, Neutral, On, Start }
        protected float absMaxForceN = 0;
        protected float brakePercent = 0;
        public float DynamicBrakeIncreaseSpeed = 0;
        public float DynamicBrakeDecreaseSpeed = 0;
        public uint MinimumMetersToPass = 19;
        protected float relativeAcceleration;
        public float AccelerationRampMaxMpSSS = 0.7f;
        public float AccelerationDemandMpSS;
        public float AccelerationRampMinMpSSS = 0.01f;
        public float ThrottleFullRangeIncreaseTimeSeconds = 0;
        public float ThrottleFullRangeDecreaseTimeSeconds = 0;
        public float DynamicBrakeFullRangeIncreaseTimeSeconds;
        public float DynamicBrakeFullRangeDecreaseTimeSeconds;
        public float ParkingBrakeEngageSpeed = 0;
        public float ParkingBrakePercent = 0;
        public bool SkipThrottleDisplay = false;
        public bool DisableZeroForceStep = false;
        public bool UseThrottleAsSpeedSelector = false;
        public float Ampers = 0;
        public bool ContinuousSpeedIncreasing = false;
        public bool ContinuousSpeedDecreasing = false;
        public float PowerBreakoutAmpers = 0;
        public float PowerBreakoutSpeedDelta = 0;
        public float PowerResumeSpeedDelta = 0;
        public float PowerReductionDelayPaxTrain = 0;
        public float PowerReductionDelayCargoTrain = 0;
        public float PowerReductionValue = 100;
        public float MaxPowerThreshold = 0;
        public float SafeSpeedForAutomaticOperationMpS = 0;

        public float AccelerationRampMpSSS
        {
            get
            {
                if ((Locomotive.Train.MassKg > 0f) && (Locomotive.MassKG > 0))
                {
                    float accelerationRampMpSS = AccelerationRampMaxMpSSS / (Locomotive.Train.MassKg / Locomotive.MassKG);
                    accelerationRampMpSS = accelerationRampMpSS > AccelerationRampMaxMpSSS ? AccelerationRampMaxMpSSS : accelerationRampMpSS;
                    accelerationRampMpSS = accelerationRampMpSS < AccelerationRampMinMpSSS ? AccelerationRampMinMpSSS : accelerationRampMpSS;
                    return accelerationRampMpSS;
                }
                else
                    return AccelerationRampMaxMpSSS;


            }
        }

        public void Initialize()
        {
            Simulator = Locomotive.Simulator;
            clockTime = Simulator.ClockTime * 100;
        }

        public void Update(float elapsedClockSeconds, float AbsWheelSpeedMpS)
        {
            if (maxForceIncreasing) SpeedRegulatorMaxForceIncrease();
            if (maxForceDecreasing) SpeedRegulatorMaxForceDecrease();
            if (SpeedRegMode == SpeedRegulatorMode.Manual)
                return;

            if (absMaxForceN == 0) absMaxForceN = Locomotive.MaxForceN;

            if (selectedSpeedIncreasing) SpeedRegulatorSelectedSpeedIncrease();
            if (selectedSpeedDecreasing) SpeedRegulatorSelectedSpeedDecrease();

            if (Locomotive.DynamicBrakePercent > 0)
                if (Locomotive.DynamicBrakePercent > 100)
                    Locomotive.DynamicBrakePercent = 100;
                ForceThrottleAndDynamicBrake = Locomotive.DynamicBrakePercent;

            UpdateMotiveForce(elapsedClockSeconds, AbsWheelSpeedMpS);
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write(this.AntiWheelSpinEquipped);
            outf.Write(this.applyingPneumaticBrake);
            outf.Write(this.Battery);
            outf.Write(this.brakeIncreasing);
            outf.Write(this.clockTime);
            outf.Write(this.controllerTime);
            outf.Write(this.CurrentSelectedSpeedMpS);
            outf.Write(this.currentThrottlePercent);
            outf.Write(this.DeltaCoefficient);
            outf.Write(this.DynamicBrakeDescentCoefficient);
            outf.Write(this.DynamicBrakeMaxForceAtSelectorStep);
            outf.Write(this.dynamicBrakeSetToZero);
            outf.Write(this.fromAcceleration);
            outf.Write(this.MaxAccelerationMpSS);
            outf.Write(this.MaxDecelerationMpSS);
            outf.Write(this.maxForceDecreasing);
            outf.Write(this.maxForceIncreasing);
            outf.Write(this.maxForceN);
            outf.Write(this.nextSelectedSpeedMps);
            outf.Write(this.restrictedRegionTravelledDistance);
            outf.Write(this.RestrictedSpeedActive);
            outf.Write(this.SelectedMaxAccelerationPercent);
            outf.Write(this.SelectedMaxAccelerationStep);
            outf.Write(this.SelectedMaxAccelerationStep);
            outf.Write(this.SelectedNumberOfAxles);
            outf.Write(this.SelectedSpeedMpS);
            outf.Write((int)this.SpeedRegMode);
            outf.Write(this.SpeedRegulatorMaxForcePercentUnits);
            outf.Write(this.SpeedRegulatorMaxForceSteps);
            outf.Write(this.SpeedRegulatorNominalSpeedStepMpS);
            outf.Write((int)this.SpeedSelMode);
            outf.Write(this.StartReducingSpeedDelta);
            outf.Write(this.ThrottleDecreaseSpeed);
            outf.Write(this.ThrottleIncreaseSpeed);
            outf.Write(this.throttleIsZero);
            outf.Write(this.trainBrakePercent);
            outf.Write(this.trainLength);
            outf.Write(this.UseThrottle);
            outf.Write(this._AccelerationMpSS);
            outf.Write(this.TrainLengthMeters);
        }

        public void Restore(BinaryReader inf)
        {
            AntiWheelSpinEquipped = inf.ReadBoolean();
            applyingPneumaticBrake = inf.ReadBoolean();
            Battery = inf.ReadBoolean();
            brakeIncreasing = inf.ReadBoolean();
            clockTime = inf.ReadDouble();
            controllerTime = inf.ReadSingle();
            CurrentSelectedSpeedMpS = inf.ReadSingle();
            currentThrottlePercent = inf.ReadSingle();
            float deltaCoefficient = inf.ReadSingle();
            float dynamicBrakeDescentCoefficient = inf.ReadSingle();
            float dynamicBrakeMaxForceAtSelectorStep = inf.ReadSingle();
            dynamicBrakeSetToZero = inf.ReadBoolean();
            fromAcceleration = inf.ReadSingle();
            float maxAccelerationMpSS = inf.ReadSingle();
            float maxDecelerationMpSS = inf.ReadSingle();
            maxForceDecreasing = inf.ReadBoolean();
            maxForceIncreasing = inf.ReadBoolean();
            maxForceN = inf.ReadSingle();
            nextSelectedSpeedMps = inf.ReadSingle();
            restrictedRegionTravelledDistance = inf.ReadSingle();
            RestrictedSpeedActive = inf.ReadBoolean();
            SelectedMaxAccelerationPercent = inf.ReadSingle();
            SelectedMaxAccelerationStep = inf.ReadSingle();
            SelectedMaxAccelerationStep = inf.ReadSingle();
            SelectedNumberOfAxles = inf.ReadInt32();
            SelectedSpeedMpS = inf.ReadSingle();
            int fSpeedRegMode = inf.ReadInt32();
            SpeedRegMode = (SpeedRegulatorMode)fSpeedRegMode;
            SpeedRegulatorMaxForcePercentUnits = inf.ReadBoolean();
            SpeedRegulatorMaxForceSteps = inf.ReadSingle();
            SpeedRegulatorNominalSpeedStepMpS = inf.ReadSingle();
            int fSpeedSelMode = inf.ReadInt32();
            SpeedSelMode = (SpeedSelectorMode)fSpeedSelMode;
            float nill = inf.ReadSingle();
            ThrottleDecreaseSpeed = inf.ReadSingle();
            ThrottleIncreaseSpeed = inf.ReadSingle();
            throttleIsZero = inf.ReadBoolean();
            trainBrakePercent = inf.ReadSingle();
            trainLength = inf.ReadSingle();
            UseThrottle = inf.ReadBoolean();
            _AccelerationMpSS = inf.ReadSingle();
            TrainLengthMeters = inf.ReadInt32();
        }

        public void SpeedRegulatorModeIncrease()
        {
            if (!Locomotive.IsPlayerTrain) return;
            Locomotive.SignalEvent(Common.Event.CruiseControlSpeedRegulator);
            SpeedRegulatorMode previousMode = SpeedRegMode;
            if (!Equipped) return;
            if (SpeedRegMode == SpeedRegulatorMode.Testing) return;
            bool test = false;
            while (!test)
            {
                SpeedRegMode++;
                switch (SpeedRegMode)
                {
                    case SpeedRegulatorMode.Auto:
                        {
                            if (SpeedRegulatorOptions.Contains("regulatorauto")) test = true;
                            SelectedSpeedMpS = Locomotive.AbsSpeedMpS;
                            break;
                        }
                    case SpeedRegulatorMode.Testing: if (SpeedRegulatorOptions.Contains("regulatortest")) test = true; break;
                }
                if (!test && SpeedRegMode == SpeedRegulatorMode.Testing) // if we're here, then it means no higher option, return to previous state and get out
                {
                    SpeedRegMode = previousMode;
                    return;
                }
            }
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Speed regulator mode changed to") + " " + Simulator.Catalog.GetString(SpeedRegMode.ToString()));
        }
        public void SpeedRegulatorModeDecrease()
        {
            Locomotive.SignalEvent(Common.Event.CruiseControlSpeedRegulator);
            if (!Equipped) return;
            if (SpeedRegMode == SpeedRegulatorMode.Manual) return;
            bool test = false;
            while (!test)
            {
                SpeedRegMode--;
                switch (SpeedRegMode)
                {
                    case SpeedRegulatorMode.Auto: if (SpeedRegulatorOptions.Contains("regulatorauto")) test = true; break;
                    case SpeedRegulatorMode.Manual:
                        {
                            Locomotive.SetThrottlePercent(0);
                            currentThrottlePercent = 0;
                            if (SpeedRegulatorOptions.Contains("regulatormanual")) test = true;
                            SelectedSpeedMpS = 0;
                            break;
                        }
                }
                if (!test && SpeedRegMode == SpeedRegulatorMode.Manual)
                    return;
            }
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Speed regulator mode changed to") + " " + Simulator.Catalog.GetString(SpeedRegMode.ToString()));
        }
        public void SpeedSelectorModeStartIncrease()
        {
            Locomotive.SignalEvent(Common.Event.CruiseControlSpeedSelector);
            if (!Equipped) return;
            if (SpeedSelMode == SpeedSelectorMode.Start) return;
            bool test = false;
            while (!test)
            {
                SpeedSelMode++;
                if (SpeedSelMode != SpeedSelectorMode.Parking && !Locomotive.EngineBrakePriority) Locomotive.SetEngineBrakePercent(0);
                switch (SpeedSelMode)
                {
                    case SpeedSelectorMode.Neutral: if (SpeedRegulatorOptions.Contains("selectorneutral")) test = true; break;
                    case SpeedSelectorMode.On: if (SpeedRegulatorOptions.Contains("selectoron")) test = true; break;
                    case SpeedSelectorMode.Start: if (SpeedRegulatorOptions.Contains("selectorstart")) test = true; break;
                }
                if (!test && SpeedSelMode == SpeedSelectorMode.Start)
                    return;
            }
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Speed selector mode changed to") + " " + Simulator.Catalog.GetString(SpeedSelMode.ToString()));
        }
        public void SpeedSelectorModeStopIncrease()
        {
            Locomotive.SignalEvent(Common.Event.CruiseControlSpeedSelector);
            //Locomotive.Mirel.ResetVigilance();
            if (!Equipped) return;
            if (SpeedSelMode == SpeedSelectorMode.Start)
            {
                bool test = false;
                while (!test)
                {
                    SpeedSelMode--;
                    switch (SpeedSelMode)
                    {
                        case SpeedSelectorMode.On: if (SpeedRegulatorOptions.Contains("selectoron")) test = true; break;
                        case SpeedSelectorMode.Neutral: if (SpeedRegulatorOptions.Contains("selectorneutral")) test = true; break;
                        case SpeedSelectorMode.Parking: if (SpeedRegulatorOptions.Contains("selectorparking")) test = true; break;
                    }
                    if (!test && SpeedSelMode == SpeedSelectorMode.Parking && !Locomotive.EngineBrakePriority)
                        return;
                }
            }
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Speed selector mode changed to") + " " + Simulator.Catalog.GetString(SpeedSelMode.ToString()));
        }
        public void SpeedSelectorModeDecrease()
        {
            Locomotive.SignalEvent(Common.Event.CruiseControlSpeedSelector);
            SpeedSelectorMode previousMode = SpeedSelMode;
            if (!Equipped) return;
            if (SpeedSelMode == SpeedSelectorMode.Parking && !Locomotive.EngineBrakePriority) return;
            bool test = false;
            while (!test)
            {
                SpeedSelMode--;
                switch (SpeedSelMode)
                {
                    case SpeedSelectorMode.On: if (SpeedRegulatorOptions.Contains("selectoron")) test = true; break;
                    case SpeedSelectorMode.Neutral: if (SpeedRegulatorOptions.Contains("selectorneutral")) test = true; break;
                    case SpeedSelectorMode.Parking: if (SpeedRegulatorOptions.Contains("selectorparking")) test = true; break;
                }
                if (!test && SpeedSelMode == SpeedSelectorMode.Parking && !Locomotive.EngineBrakePriority)
                {
                    SpeedSelMode = previousMode;
                    return;
                }
            }
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Speed selector mode changed to") + " " + Simulator.Catalog.GetString(SpeedSelMode.ToString()));
        }

        bool maxForceIncreasing = false;
        public void SpeedRegulatorMaxForceStartIncrease()
        {
            maxForceIncreasing = true;
        }
        public void SpeedRegulatorMaxForceStopIncrease()
        {
            maxForceIncreasing = false;
        }
        protected void SpeedRegulatorMaxForceIncrease()
        {
            Locomotive.SignalEvent(Common.Event.CruiseControlMaxForce);
            if (MaxForceSetSingleStep) maxForceIncreasing = false;
            if (SelectedMaxAccelerationStep == 0.5f) SelectedMaxAccelerationStep = 0;
            if (!Equipped) return;
            if (SpeedRegulatorMaxForcePercentUnits)
            {
                if (SelectedMaxAccelerationPercent == 100)
                    return;
                SelectedMaxAccelerationPercent += 1f;
                Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Speed regulator max acceleration percent changed to") + " " + Simulator.Catalog.GetString(SelectedMaxAccelerationPercent.ToString()) + "%");
            }
            else
            {
                if (SelectedMaxAccelerationStep == SpeedRegulatorMaxForceSteps)
                    return;
                SelectedMaxAccelerationStep++;
                Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Speed regulator max acceleration changed to") + " " + Simulator.Catalog.GetString(SelectedMaxAccelerationStep.ToString()));
            }
        }

        protected bool maxForceDecreasing = false;
        public void SpeedRegulatorMaxForceStartDecrease()
        {
            maxForceDecreasing = true;
        }
        public void SpeedRegulatorMaxForceStopDecrease()
        {
            maxForceDecreasing = false;
        }
        protected void SpeedRegulatorMaxForceDecrease()
        {
            Locomotive.SignalEvent(Common.Event.CruiseControlMaxForce);
            if (MaxForceSetSingleStep) maxForceDecreasing = false;
            if (!Equipped) return;
            if (DisableZeroForceStep)
            {
                if (SelectedMaxAccelerationStep <= 1) return;
            }
            else
            {
                if (SelectedMaxAccelerationStep <= 0) return;
            }
            SelectedMaxAccelerationStep--;
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Speed regulator max acceleration changed to") + " " + Simulator.Catalog.GetString(SelectedMaxAccelerationStep.ToString()));
        }

        protected bool selectedSpeedIncreasing = false;
        public void SpeedRegulatorSelectedSpeedStartIncrease()
        {
            if (!UseThrottleAsSpeedSelector)
                selectedSpeedIncreasing = true;
            else
                SpeedSelectorModeStartIncrease();
        }
        public void SpeedRegulatorSelectedSpeedStopIncrease()
        {
            if (!UseThrottleAsSpeedSelector)
                selectedSpeedIncreasing = false;
            else
                SpeedSelectorModeStopIncrease();
        }
        public void SpeedRegulatorSelectedSpeedIncrease()
        {
            if (!Equipped) return;
            SelectedSpeedMpS += SpeedRegulatorNominalSpeedStepMpS;
            if (SelectedSpeedMpS > Locomotive.MaxSpeedMpS)
                SelectedSpeedMpS = Locomotive.MaxSpeedMpS;
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Selected speed changed to ") + Math.Round(MpS.FromMpS(SelectedSpeedMpS, true), 0, MidpointRounding.AwayFromZero).ToString() + " km/h");
        }

        protected bool selectedSpeedDecreasing = false;
        public void SpeedRegulatorSelectedSpeedStartDecrease()
        {
            if (!UseThrottleAsSpeedSelector)
                selectedSpeedDecreasing = true;
            else
                SpeedSelectorModeDecrease();
        }
        public void SpeedRegulatorSelectedSpeedStopDecrease()
        {
            selectedSpeedDecreasing = false;
        }
        public void SpeedRegulatorSelectedSpeedDecrease()
        {
            if (!Equipped) return;
            SelectedSpeedMpS -= SpeedRegulatorNominalSpeedStepMpS;
            if (SelectedSpeedMpS < 0)
                SelectedSpeedMpS = 0f;
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Selected speed changed to ") + Math.Round(MpS.FromMpS(SelectedSpeedMpS, true), 0, MidpointRounding.AwayFromZero).ToString() + " km/h");
        }
        public void NumerOfAxlesIncrease()
        {
            NumerOfAxlesIncrease(1);
        }
        public void NumerOfAxlesIncrease(int ByAmount)
        {
            SelectedNumberOfAxles += ByAmount;
            trainLength = SelectedNumberOfAxles * 6.6f;
            TrainLengthMeters = (int)Math.Round(trainLength + 0.5, 0);
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Number of axles increased to ") + SelectedNumberOfAxles.ToString());
        }
        public void NumberOfAxlesDecrease()
        {
            NumberOfAxlesDecrease(1);
        }
        public void NumberOfAxlesDecrease(int ByAmount)
        {
            if ((SelectedNumberOfAxles - ByAmount) < 1) return;
            SelectedNumberOfAxles -= ByAmount;
            trainLength = SelectedNumberOfAxles * 6.6f;
            TrainLengthMeters = (int)Math.Round(trainLength + 0.5, 0);
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Number of axles decreased to ") + SelectedNumberOfAxles.ToString());
        }
        public void ActivateRestrictedSpeedZone()
        {
            RemainingTrainLengthToPassRestrictedZone = TrainLengthMeters;
            if (!RestrictedSpeedActive)
            {
                restrictedRegionTravelledDistance = Simulator.PlayerLocomotive.Train.DistanceTravelledM;
                CurrentSelectedSpeedMpS = SelectedSpeedMpS;
                RestrictedSpeedActive = true;
            }
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Speed restricted zone active."));
        }

        public virtual void CheckRestrictedSpeedZone()
        {
            RemainingTrainLengthToPassRestrictedZone = (int)Math.Round((Simulator.PlayerLocomotive.Train.DistanceTravelledM - restrictedRegionTravelledDistance));
            if (RemainingTrainLengthToPassRestrictedZone < 0) RemainingTrainLengthToPassRestrictedZone = 0;
            if ((Simulator.PlayerLocomotive.Train.DistanceTravelledM - restrictedRegionTravelledDistance) >= trainLength)
            {
                RestrictedSpeedActive = false;
                Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Speed restricted zone off."));
                Locomotive.SignalEvent(Common.Event.Alert);
            }
        }

        public void SetSpeed(float SpeedKpH)
        {
            Locomotive.SignalEvent(Common.Event.Alert1);
            if (!Equipped) return;
            SelectedSpeedMpS = MpS.FromKpH(SpeedKpH);
            if (SelectedSpeedMpS > Locomotive.MaxSpeedMpS)
                SelectedSpeedMpS = Locomotive.MaxSpeedMpS;
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Selected speed set to ") + SpeedKpH.ToString());
        }

        protected List<MSTSLocomotive> playerNotDriveableTrainLocomotives = new List<MSTSLocomotive>();
        float _AccelerationMpSS = 0;
        protected bool throttleIsZero = false;
        protected bool brakeIncreasing = false;
        protected float controllerTime = 0;
        protected float fromAcceleration = 0;
        protected bool applyingPneumaticBrake = false;
        protected bool firstIteration = true;
        protected float previousMotiveForce = 0;
        protected float addPowerTimeCount = 0;
        public float controllerVolts = 0;
        protected float throttleChangeTime = 0;
        protected bool breakout = false;
        protected  float timeFromEngineMoved = 0;
        protected bool reducingForce = false;
        protected bool canAddForce = true;
        List<float> concurrentAccelerationList = new List<float>();
        public float TrainElevation = 0;
        protected float skidSpeedDegratation = 0;

        protected virtual void UpdateMotiveForce(float elapsedClockSeconds, float AbsWheelSpeedMpS)
        {
            if (DynamicBrakeFullRangeIncreaseTimeSeconds == 0)
                DynamicBrakeFullRangeIncreaseTimeSeconds = 3;
            if (DynamicBrakeFullRangeDecreaseTimeSeconds == 0)
                DynamicBrakeFullRangeDecreaseTimeSeconds = 3;
            float speedDiff = AbsWheelSpeedMpS - Locomotive.AbsSpeedMpS;
            float newThrotte = 0;
            // calculate new max force if MaxPowerThreshold is set
            if (MaxPowerThreshold > 0)
            {
                float currentSpeed = MpS.ToKpH(AbsWheelSpeedMpS);
                float percentComplete = (int)Math.Round((double)(100 * currentSpeed) / MaxPowerThreshold);
                if (percentComplete > 100)
                    percentComplete = 100;
                newThrotte = percentComplete;
            }

            int count = 0;
            TrainElevation = 0;
            foreach (TrainCar tc in Locomotive.Train.Cars)
            {
                count++;
                TrainElevation += tc.Flipped ? tc.CurrentElevationPercent : -tc.CurrentElevationPercent;
            }
            TrainElevation = TrainElevation / count;

            if (SpeedSelMode == SpeedSelectorMode.On || SpeedSelMode == SpeedSelectorMode.Start)
            {
                canAddForce = true;
            }
            else
            {
                canAddForce = false;
                timeFromEngineMoved = 0;
                reducingForce = true;
            }
            if (canAddForce)
            {
                if (Locomotive.AbsSpeedMpS == 0)
                {
                    timeFromEngineMoved = 0;
                    reducingForce = true;
                }
                else if (reducingForce)
                {
                    timeFromEngineMoved += elapsedClockSeconds;
                    float timeToReduce = Locomotive.SelectedTrainType == MSTSLocomotive.TrainType.Pax ? PowerReductionDelayPaxTrain : PowerReductionDelayCargoTrain;
                    if (timeFromEngineMoved > timeToReduce)
                        reducingForce = false;
                }
            }
            if (Bar.FromPSI(Locomotive.BrakeSystem.BrakeLine1PressurePSI) < 4.98)
            {
                canAddForce = false;
                reducingForce = true;
                timeFromEngineMoved = 0;
                maxForceN = 0;
                if (controllerVolts > 0)
                    controllerVolts = 0;
                Ampers = 0;
                Locomotive.SetThrottlePercent(0);
                return;
            }
            else if (Bar.FromPSI(Locomotive.BrakeSystem.BrakeLine1PressurePSI) > 4.7)
            {
                canAddForce = true;
            }
            if (SpeedRegulatorOptions.Contains("engageforceonnonzerospeed") && SelectedSpeedMpS > 0)
            {
                SpeedSelMode = SpeedSelectorMode.On;
                SpeedRegMode = SpeedRegulatorMode.Auto;
                SkipThrottleDisplay = true;
            }
            if (SpeedRegulatorOptions.Contains("engageforceonnonzerospeed") && SelectedSpeedMpS == 0) return;

            float t = 0;
            if (SpeedRegMode == SpeedRegulatorMode.Manual) DynamicBrakePriority = false;

            if (RestrictedSpeedActive)
                CheckRestrictedSpeedZone();
            if (DynamicBrakePriority)
            {
                Locomotive.SetThrottlePercent(0);
                ForceThrottleAndDynamicBrake = -Locomotive.DynamicBrakePercent;
                return;
            }

           if (firstIteration) // if this is exetuted the first time, let's check all other than player engines in the consist, and record them for further throttle manipulation
            {
                foreach (TrainCar tc in Locomotive.Train.Cars)
                {
                    if (tc.GetType() == typeof(MSTSLocomotive) || tc.GetType() == typeof(MSTSDieselLocomotive) || tc.GetType() == typeof(MSTSElectricLocomotive))
                    {
                        if (tc != Locomotive)
                        {
                            try
                            {
                                playerNotDriveableTrainLocomotives.Add((MSTSLocomotive)tc);
                            }
                            catch { }
                        }
                    }
                }
                firstIteration = false;
            }

            if (SpeedRegMode == SpeedRegulatorMode.Auto)
            {
                if (SpeedSelMode == SpeedSelectorMode.Parking && !Locomotive.EngineBrakePriority)
                {
                    if (Locomotive.DynamicBrakePercent > 0)
                    {
                        if (AbsWheelSpeedMpS == 0)
                        {
                            Locomotive.SetDynamicBrakePercent(0);
                            Locomotive.DynamicBrakeChangeActiveState(false);
                        }
                    }
                    if (!UseThrottle) Locomotive.SetThrottlePercent(0);
                    throttleIsZero = true;

                    if (AbsWheelSpeedMpS < MpS.FromKpH(ParkingBrakeEngageSpeed))
                        Locomotive.SetEngineBrakePercent(ParkingBrakePercent);
                }
                else if (SpeedSelMode == SpeedSelectorMode.Neutral || SpeedSelMode < SpeedSelectorMode.Start && !SpeedRegulatorOptions.Contains("startfromzero") && AbsWheelSpeedMpS < SafeSpeedForAutomaticOperationMpS)
                {
                    if (controllerVolts > 0)
                    {
                        float step = 100 / ThrottleFullRangeIncreaseTimeSeconds;
                        step *= elapsedClockSeconds;
                        controllerVolts -= step;
                        if (controllerVolts < 0) controllerVolts = 0;
                        if (controllerVolts > 0 && controllerVolts < 0.1) controllerVolts = 0;
                    }

                    float delta = 0;
                    if (!RestrictedSpeedActive)
                        delta = SelectedSpeedMpS - AbsWheelSpeedMpS;
                    else
                        delta = CurrentSelectedSpeedMpS - AbsWheelSpeedMpS;

                    if (delta > 0)
                    {
                        if (controllerVolts < -0.1)
                        {
                            float step = 100 / ThrottleFullRangeDecreaseTimeSeconds;
                            step *= elapsedClockSeconds;
                            controllerVolts += step;
                        }
                        else if (controllerVolts > 0.1)
                        {

                            float step = 100 / ThrottleFullRangeIncreaseTimeSeconds;
                            step *= elapsedClockSeconds;
                            controllerVolts -= step;
                        }
                        else
                        {
                            controllerVolts = 0;
                        }
                    }

                    if (delta < 0) // start braking
                    {
                        if (maxForceN > 0)
                        {
                            if (controllerVolts > 0)
                            {
                                float step = 100 / ThrottleFullRangeDecreaseTimeSeconds;
                                step *= elapsedClockSeconds;
                                controllerVolts -= step;
                            }
                        }
                        else
                        {
                            if (Locomotive.DynamicBrakeAvailable)
                            {
                                delta = 0;
                                if (!RestrictedSpeedActive)
                                    delta = (SelectedSpeedMpS + (TrainElevation < -0.01 ? TrainElevation * (SelectedNumberOfAxles / 40) : 0)) - AbsWheelSpeedMpS;
                                else
                                    delta = (CurrentSelectedSpeedMpS + (TrainElevation < -0.01 ? TrainElevation * (SelectedNumberOfAxles / 40) : 0)) - AbsWheelSpeedMpS;

                                relativeAcceleration = (float)-Math.Sqrt(-StartReducingSpeedDelta * delta);
                                AccelerationDemandMpSS = (float)-Math.Sqrt(-StartReducingSpeedDelta * AccelerationRampMpSSS * delta);
                                if (maxForceN > 0)
                                {
                                    if (controllerVolts > 0)
                                    {
                                        float step = 100 / ThrottleFullRangeDecreaseTimeSeconds;
                                        step *= elapsedClockSeconds;
                                        controllerVolts -= step;
                                    }
                                }
                                if (maxForceN == 0)
                                {
                                    if (!UseThrottle) Locomotive.SetThrottlePercent(0);
                                    if (relativeAcceleration < -1) relativeAcceleration = -1;
                                    if (Locomotive.DynamicBrakePercent < -(AccelerationDemandMpSS * 100) && AccelerationDemandMpSS < -0.05f)
                                    {
                                        if (controllerVolts > -100)
                                        {
                                            float step = 100 / DynamicBrakeFullRangeIncreaseTimeSeconds;
                                            step *= elapsedClockSeconds;
                                            controllerVolts -= step;
                                        }
                                    }
                                    if (Locomotive.DynamicBrakePercent > -((AccelerationDemandMpSS - 0.05f) * 100))
                                    {
                                        if (controllerVolts < 0)
                                        {
                                            float step = 100 / DynamicBrakeFullRangeDecreaseTimeSeconds;
                                            step *= elapsedClockSeconds;
                                            controllerVolts += step;
                                        }
                                    }
                                }
                            }
                            else // use TrainBrake
                            {
                                if (delta > -0.1)
                                {
                                    if (!UseThrottle)
                                        Locomotive.SetThrottlePercent(100);
                                    throttleIsZero = false;
                                    maxForceN = 0;
                                }
                                else if (delta > -1)
                                {
                                    if (!UseThrottle)
                                        Locomotive.SetThrottlePercent(0);
                                    throttleIsZero = true;

                                    brakePercent = 10 + (-delta * 10);
                                }
                                else
                                {
                                    Locomotive.TractiveForceN = 0;
                                    if (!UseThrottle)
                                        Locomotive.SetThrottlePercent(0);
                                    throttleIsZero = true;

                                    if (_AccelerationMpSS > -MaxDecelerationMpSS)
                                        brakePercent += 0.5f;
                                    else if (_AccelerationMpSS < -MaxDecelerationMpSS)
                                        brakePercent -= 1;
                                    if (brakePercent > 100)
                                        brakePercent = 100;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (Locomotive.DynamicBrakeAvailable)
                        {
                            if (Locomotive.DynamicBrakePercent > 0)
                            {
                                if (controllerVolts < 0)
                                {
                                    float step = 100 / DynamicBrakeFullRangeDecreaseTimeSeconds;
                                    step *= elapsedClockSeconds;
                                    controllerVolts += step;
                                }
                            }
                        }
                    }
                }

                if ((AbsWheelSpeedMpS > SafeSpeedForAutomaticOperationMpS || SpeedSelMode == SpeedSelectorMode.Start || SpeedRegulatorOptions.Contains("startfromzero")) && (SpeedSelMode != SpeedSelectorMode.Neutral && SpeedSelMode != SpeedSelectorMode.Parking))
                {
                    float delta = 0;
                    if (!RestrictedSpeedActive)
                        delta = SelectedSpeedMpS - AbsWheelSpeedMpS;
                    else
                        delta = CurrentSelectedSpeedMpS - AbsWheelSpeedMpS;

                    if (delta > 0.0f)
                    {
                        if (Locomotive.DynamicBrakePercent > 0)
                            Locomotive.SetDynamicBrakePercent(0);
                        if (Locomotive.DynamicBrakePercent == 0 && Locomotive.DynamicBrake) Locomotive.DynamicBrakeChangeActiveState(false);
                        relativeAcceleration = (float)Math.Sqrt(AccelerationRampMaxMpSSS * delta);
                        float coeff = 1;
                        float speed = MpS.ToKpH(Locomotive.WheelSpeedMpS);
                        if (speed > 100)
                        {
                            coeff = (speed / 100) * 1.2f;
                        }
                        else
                        {
                            coeff = 1;
                        }
                        float numAxesCoeff = 0;
                        numAxesCoeff = (SelectedNumberOfAxles) / 3;
                        AccelerationDemandMpSS = (float)Math.Sqrt((StartReducingSpeedDelta + numAxesCoeff) * coeff * coeff * AccelerationRampMpSSS * (delta));
                    }
                    else // start braking
                    {
                        if (controllerVolts > 0)
                        {
                            float step = 100 / ThrottleFullRangeDecreaseTimeSeconds;
                            step *= elapsedClockSeconds;
                            controllerVolts -= step;
                            if (controllerVolts < 0) controllerVolts = 0;
                            if (controllerVolts > 0 && controllerVolts < 0.1) controllerVolts = 0;
                        }

                        delta = 0;
                        if (!RestrictedSpeedActive)
                            delta = (SelectedSpeedMpS + (TrainElevation < -0.01 ? TrainElevation * (SelectedNumberOfAxles / 40) : 0)) - AbsWheelSpeedMpS;
                        else
                            delta = (CurrentSelectedSpeedMpS + (TrainElevation < -0.01 ? TrainElevation * (SelectedNumberOfAxles / 40) : 0)) - AbsWheelSpeedMpS;

                        if (delta > 0)
                        {
                            if (controllerVolts < -0.1)
                            {
                                float step = 100 / ThrottleFullRangeDecreaseTimeSeconds;
                                step *= elapsedClockSeconds;
                                controllerVolts += step;
                            }
                            else if (controllerVolts > 0.1)
                            {

                                float step = 100 / ThrottleFullRangeIncreaseTimeSeconds;
                                step *= elapsedClockSeconds;
                                controllerVolts -= step;
                            }
                            else
                            {
                                controllerVolts = 0;
                            }
                        }

                        if (delta < 0) // start braking
                        {
                            if (maxForceN > 0)
                            {
                                if (controllerVolts > 0)
                                {
                                    float step = 100 / ThrottleFullRangeDecreaseTimeSeconds;
                                    step *= elapsedClockSeconds;
                                    controllerVolts -= step;
                                }
                            }
                            else
                            {
                                if (Locomotive.DynamicBrakeAvailable)
                                {
                                    delta = 0;
                                    if (!RestrictedSpeedActive)
                                        delta = (SelectedSpeedMpS + (TrainElevation < -0.01 ? TrainElevation * (SelectedNumberOfAxles / 40) : 0)) - AbsWheelSpeedMpS;
                                    else
                                        delta = (CurrentSelectedSpeedMpS + (TrainElevation < -0.01 ? TrainElevation * (SelectedNumberOfAxles / 40) : 0)) - AbsWheelSpeedMpS;

                                    relativeAcceleration = (float)-Math.Sqrt(-StartReducingSpeedDelta * delta);
                                    AccelerationDemandMpSS = (float)-Math.Sqrt(-StartReducingSpeedDelta * AccelerationRampMpSSS * delta);
                                    if (maxForceN > 0)
                                    {
                                        if (controllerVolts > 0)
                                        {
                                            float step = 100 / ThrottleFullRangeDecreaseTimeSeconds;
                                            step *= elapsedClockSeconds;
                                            controllerVolts -= step;
                                        }
                                    }
                                    if (maxForceN == 0)
                                    {
                                        if (!UseThrottle) Locomotive.SetThrottlePercent(0);
                                        if (relativeAcceleration < -1) relativeAcceleration = -1;
                                        if (Locomotive.DynamicBrakePercent < -(AccelerationDemandMpSS * 100) && AccelerationDemandMpSS < -0.05f)
                                        {
                                            if (controllerVolts > -100)
                                            {
                                                float step = 100 / DynamicBrakeFullRangeIncreaseTimeSeconds;
                                                step *= elapsedClockSeconds;
                                                controllerVolts -= step;
                                            }
                                        }
                                        if (Locomotive.DynamicBrakePercent > -((AccelerationDemandMpSS - 0.05f) * 100))
                                        {
                                            if (controllerVolts < 0)
                                            {
                                                float step = 100 / DynamicBrakeFullRangeDecreaseTimeSeconds;
                                                step *= elapsedClockSeconds;
                                                controllerVolts += step;
                                            }
                                        }
                                    }
                                }
                                else // use TrainBrake
                                {
                                    if (delta > -0.1)
                                    {
                                        if (!UseThrottle)
                                            Locomotive.SetThrottlePercent(100);
                                        throttleIsZero = false;
                                        maxForceN = 0;
                                    }
                                    else if (delta > -1)
                                    {
                                        if (!UseThrottle)
                                            Locomotive.SetThrottlePercent(0);
                                        throttleIsZero = true;

                                        brakePercent = 10 + (-delta * 10);
                                    }
                                    else
                                    {
                                        Locomotive.TractiveForceN = 0;
                                        if (!UseThrottle)
                                            Locomotive.SetThrottlePercent(0);
                                        throttleIsZero = true;

                                        if (_AccelerationMpSS > -MaxDecelerationMpSS)
                                            brakePercent += 0.5f;
                                        else if (_AccelerationMpSS < -MaxDecelerationMpSS)
                                            brakePercent -= 1;
                                        if (brakePercent > 100)
                                            brakePercent = 100;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (Locomotive.DynamicBrakeAvailable)
                            {
                                if (Locomotive.DynamicBrakePercent > 0)
                                {
                                    if (controllerVolts < 0)
                                    {
                                        float step = 100 / DynamicBrakeFullRangeDecreaseTimeSeconds;
                                        step *= elapsedClockSeconds;
                                        controllerVolts += step;
                                    }
                                }
                            }
                        }
                    }
                    if (relativeAcceleration > 1.0f)
                        relativeAcceleration = 1.0f;

                    if ((SpeedSelMode == SpeedSelectorMode.On || SpeedSelMode == SpeedSelectorMode.Start) && delta > 0)
                    {
                        if (Locomotive.DynamicBrakePercent > 0)
                        {
                            if (controllerVolts <= 0)
                            {
                                float step = 100 / DynamicBrakeFullRangeDecreaseTimeSeconds;
                                step *= elapsedClockSeconds;
                                controllerVolts += step;
                            }
                        }
                        else
                        {
                            if (!UseThrottle)
                            {
                                Locomotive.SetThrottlePercent(100);
                            }
                            throttleIsZero = false;
                        }
                    }
                    float a = 0;
                    if (Locomotive.PowerOn && Locomotive.Direction != Direction.N)
                    {
                        if (Locomotive.DynamicBrakePercent < 0)
                        {
                            if (relativeAcceleration > 0)
                            {
                                if (ForceStepsThrottleTable.Count > 0)
                                {
                                    t = ForceStepsThrottleTable[(int)SelectedMaxAccelerationStep - 1];
                                    if (AccelerationTable.Count > 0)
                                        a = AccelerationTable[(int)SelectedMaxAccelerationStep - 1];
                                }
                                else
                                    t = SelectedMaxAccelerationStep;
                                if (t < newThrotte)
                                    t = newThrotte;
                                t /= 100;
                            }
                            else
                            {
                                if (controllerVolts > 0)
                                {
                                    float step = 100 / ThrottleFullRangeIncreaseTimeSeconds;
                                    step *= elapsedClockSeconds;
                                    controllerVolts -= step;
                                }
                            }
                        }
                        if (reducingForce)
                        {
                            if (t > PowerReductionValue / 100)
                                t = PowerReductionValue / 100;
                        }
                        // FIFO from accelerationDemand, 20 iterations
                        float smoothedAccelerationDemand = 0;
                        concurrentAccelerationList.Add(AccelerationDemandMpSS);
                        if (concurrentAccelerationList.Count > 20)
                        {
                            // calculate smooth
                            foreach (float accel in concurrentAccelerationList)
                            {
                                smoothedAccelerationDemand += accel;
                            }
                            smoothedAccelerationDemand = smoothedAccelerationDemand / concurrentAccelerationList.Count;
                            concurrentAccelerationList.RemoveAt(0);
                        }
                        if (t > smoothedAccelerationDemand) t = smoothedAccelerationDemand;
                        float demandedVolts = t * 100;
                        if (demandedVolts < PowerBreakoutAmpers)
                            breakout = true;
                        if (demandedVolts > 13.5f)
                            breakout = false;
                        if (UseThrottle) // not valid for diesel engines.
                            breakout = false;
                        if ((controllerVolts != demandedVolts) && delta > 0)
                        {
                            if (a > 0 && MpS.ToKpH(Locomotive.WheelSpeedMpS) > 5)
                            {
                                if (controllerVolts < demandedVolts && Locomotive.AccelerationMpSS < a - 0.02)
                                {
                                    float step = 100 / ThrottleFullRangeIncreaseTimeSeconds;
                                    step *= elapsedClockSeconds;
                                    controllerVolts += step;
                                }
                            }
                            else
                            {
                                if (controllerVolts < demandedVolts)
                                {
                                    float step = 100 / ThrottleFullRangeIncreaseTimeSeconds;
                                    step *= elapsedClockSeconds;
                                    controllerVolts += step;
                                }
                            }
                            if (a > 0 && MpS.ToKpH(Locomotive.WheelSpeedMpS) > 5)
                            {
                                if (controllerVolts > demandedVolts && Locomotive.AccelerationMpSS > a + 0.02)
                                {
                                    float step = 100 / ThrottleFullRangeIncreaseTimeSeconds;
                                    step *= elapsedClockSeconds;
                                    controllerVolts -= step;
                                }
                            }
                            else
                            {
                                if (controllerVolts > demandedVolts)
                                {
                                    float step = 100 / ThrottleFullRangeIncreaseTimeSeconds;
                                    step *= elapsedClockSeconds;
                                    controllerVolts -= step;
                                }
                            }
                            if (controllerVolts > demandedVolts && delta < 0.8)
                            {
                                float step = 100 / ThrottleFullRangeIncreaseTimeSeconds;
                                step *= elapsedClockSeconds;
                                controllerVolts -= step;
                            }
                        }
                        if (a > 0 && MpS.ToKpH(Locomotive.WheelSpeedMpS) > 5)
                        {
                            if ((a != Locomotive.AccelerationMpSS) && delta > 0.8)
                            {
                                if (Locomotive.AccelerationMpSS < a + 0.02)
                                {
                                    float step = 100 / ThrottleFullRangeIncreaseTimeSeconds;
                                    step *= elapsedClockSeconds;
                                    controllerVolts += step;
                                }
                                if (Locomotive.AccelerationMpSS > a - 0.02)
                                {
                                    float step = 100 / ThrottleFullRangeIncreaseTimeSeconds;
                                    step *= elapsedClockSeconds;
                                    controllerVolts -= step;
                                }
                            }
                        }

                        if (UseThrottle)
                        {
                            //Simulator.Confirmer.Information(Locomotive.Train.AccelerationMpSpS.SmoothedValue.ToString());
                            // 
                            if (SpeedRegMode == SpeedRegulatorMode.AVV)
                            {
                                Physics.Train train = Locomotive.Train;
                                Signalling.ObjectItemInfo firstObject = null;
                                Signalling.ObjectItemInfo nextObject = null;
                                float nextSpeed = -1;
                                if (train.SignalObjectItems.Count > 0)
                                {
                                    int i = 0;
                                    firstObject = train.SignalObjectItems[i];
                                    firstObject.distance_to_train = train.GetObjectDistanceToTrain(firstObject);
                                    if (firstObject.speed_passenger < 0)
                                    {
                                        while (nextSpeed < 0)
                                        {
                                            i++;
                                            if (i + 1 > train.SignalObjectItems.Count)
                                                break;
                                            nextObject = train.SignalObjectItems[i];
                                            nextSpeed = nextObject.speed_passenger;
                                            nextObject.distance_to_train = train.GetObjectDistanceToTrain(nextObject);
                                        }
                                    }
                                    else
                                    {
                                        nextSpeed = firstObject.speed_passenger;
                                    }
                                    if (nextSpeed < 0 && Locomotive.TrainControlSystem.CurrentSpeedLimitMpS > 0)
                                        nextSpeed = Locomotive.TrainControlSystem.CurrentSpeedLimitMpS;
                                }

                                // 160 km/h = 2000m; 1KpH = 12,5m
                                if (firstObject != null)
                                {
                                    float computedMaxSpeed = 0;
                                    float computedMaxSpeed1 = 0;
                                    if (nextObject != null)
                                    {
                                        if (firstObject.speed_passenger > -1 && firstObject.speed_passenger < nextObject.speed_passenger)
                                        {
                                            computedMaxSpeed = (firstObject.distance_to_train - 50) / 35;
                                            computedMaxSpeed = MpS.FromKpH(computedMaxSpeed);
                                            computedMaxSpeed1 = firstObject.distance_to_train / 35;
                                            computedMaxSpeed1 = MpS.FromKpH(computedMaxSpeed1);
                                        }
                                        else
                                        {
                                            computedMaxSpeed = (nextObject.distance_to_train - 50) / 35;
                                            computedMaxSpeed = MpS.FromKpH(computedMaxSpeed);
                                            computedMaxSpeed1 = nextObject.distance_to_train / 35;
                                            computedMaxSpeed1 = MpS.FromKpH(computedMaxSpeed1);
                                        }
                                    }
                                    else
                                    {
                                        computedMaxSpeed = (firstObject.distance_to_train - 50) / 35;
                                        computedMaxSpeed = MpS.FromKpH(computedMaxSpeed);
                                        computedMaxSpeed1 = firstObject.distance_to_train / 35;
                                        computedMaxSpeed1 = MpS.FromKpH(computedMaxSpeed1);
                                    }
                                    float maxSpeedAhead = firstObject.speed_passenger;
                                    computedMaxSpeed = computedMaxSpeed + nextSpeed;
                                    if ((computedMaxSpeed1 + nextSpeed) > Locomotive.TrainControlSystem.CurrentSpeedLimitMpS) computedMaxSpeed = Locomotive.TrainControlSystem.CurrentSpeedLimitMpS;
                                    if ((computedMaxSpeed1 + nextSpeed) > Locomotive.TrainControlSystem.NextSpeedLimitMpS) computedMaxSpeed = Locomotive.TrainControlSystem.NextSpeedLimitMpS;
                                    if (nextObject != null)
                                    {
                                        if (nextObject.speed_passenger < 0 && firstObject.speed_passenger < 0)
                                        {
                                            computedMaxSpeed = Locomotive.TrainControlSystem.CurrentSpeedLimitMpS;
                                        }
                                    }
                                    if (computedMaxSpeed > Locomotive.MaxSpeedMpS) computedMaxSpeed = Locomotive.MaxSpeedMpS;
                                    if (nextObject != null)
                                        Simulator.Confirmer.MSG(MpS.ToKpH(firstObject.speed_passenger).ToString() + ", " + MpS.ToKpH(nextObject.speed_passenger).ToString() + ", " + MpS.ToKpH(Locomotive.TrainControlSystem.NextSpeedLimitMpS) + ", " + MpS.ToKpH(computedMaxSpeed));
                                    else
                                        Simulator.Confirmer.MSG(MpS.ToKpH(firstObject.speed_passenger).ToString() + ", " + MpS.ToKpH(Locomotive.TrainControlSystem.NextSpeedLimitMpS) + ", " + MpS.ToKpH(computedMaxSpeed));
                                    if (SelectedSpeedMpS != computedMaxSpeed)
                                    {
                                        SelectedSpeedMpS = computedMaxSpeed;
                                        Simulator.Confirmer.Information("AVV - Selected max speed changed to " + MpS.ToKpH(SelectedSpeedMpS) + "kph");
                                    }
                                }
                            }
                            if (controllerVolts > 0)
                                Locomotive.SetThrottlePercent(controllerVolts);
                            //Simulator.Confirmer.MSG(controllerVolts.ToString());
                        }
                    }
                }
                else if (UseThrottle)
                {
                    if (Locomotive.ThrottlePercent > 0)
                    {
                        float newValue = (Locomotive.ThrottlePercent - 1) / 100;
                        if (newValue < 0)
                            newValue = 0;
                        Locomotive.StartThrottleDecrease(newValue);
                    }
                }

                if (Locomotive.WheelSpeedMpS == 0 && controllerVolts < 0)
                    controllerVolts = 0;
                ForceThrottleAndDynamicBrake = controllerVolts;

                if (controllerVolts > 0)
                {
                    if (speedDiff > 0.5)
                    {
                        skidSpeedDegratation += 0.05f;
                    }
                    else if (skidSpeedDegratation > 0)
                    {
                        skidSpeedDegratation -= 0.1f;
                    }
                    if (speedDiff < 0.4)
                        skidSpeedDegratation = 0;
                    controllerVolts -= skidSpeedDegratation;
                    if (breakout || Bar.FromPSI(Locomotive.BrakeSystem.BrakeLine1PressurePSI) < 4.98)
                    {
                        maxForceN = 0;
                        controllerVolts = 0;
                        Ampers = 0;
                        if (!UseThrottle) Locomotive.SetThrottlePercent(0);
                    }
                    else
                    {
                        if (Locomotive.ThrottlePercent < 100 && SpeedSelMode != SpeedSelectorMode.Parking && !UseThrottle)
                        {
                            Locomotive.SetThrottlePercent(100);
                            throttleIsZero = false;
                        }
                        if (Locomotive.DynamicBrakePercent > -1)
                        {
                            Locomotive.SetDynamicBrakePercent(0);
                            Locomotive.DynamicBrakeChangeActiveState(false);
                        }

                        if (Locomotive.TractiveForceCurves != null && !UseThrottle)
                        {
                            maxForceN = Locomotive.TractiveForceCurves.Get(controllerVolts / 100, AbsWheelSpeedMpS) * (1 - Locomotive.PowerReduction);
                        }
                        else
                        {
                            if (Locomotive.TractiveForceCurves == null)
                                maxForceN = Locomotive.MaxForceN * (controllerVolts / 100);
                            else
                                maxForceN = Locomotive.TractiveForceCurves.Get(controllerVolts / 100, AbsWheelSpeedMpS) * (1 - Locomotive.PowerReduction);
                        }
                    }
                }
                else if (controllerVolts < 0)
                {
                    if (maxForceN > 0) maxForceN = 0;
                    if (Locomotive.ThrottlePercent > 0) Locomotive.SetThrottlePercent(0);
                    if (Locomotive.DynamicBrakePercent <= 0)
                    {
                        string status = Locomotive.GetDynamicBrakeStatus();
                        Locomotive.DynamicBrakeChangeActiveState(true);
                    }
                    Locomotive.SetDynamicBrakePercent(-controllerVolts);
                    Locomotive.DynamicBrakePercent = -controllerVolts;
                }
                else if (controllerVolts == 0)
                {
                    if (!breakout)
                    {

                        /*if (Locomotive.MultiPositionController.controllerPosition == Controllers.MultiPositionController.ControllerPosition.DynamicBrakeIncrease || Locomotive.MultiPositionController.controllerPosition == Controllers.MultiPositionController.ControllerPosition.DynamicBrakeIncreaseFast)
                        {
                            controllerVolts = -Locomotive.DynamicBrakePercent;
                        }
                        else
                        {*/
                        if (maxForceN > 0) maxForceN = 0;
                        if (Locomotive.ThrottlePercent > 0 && !UseThrottle) Locomotive.SetThrottlePercent(0);
                        if (Locomotive.DynamicBrakePercent > -1)
                        {
                            Locomotive.SetDynamicBrakePercent(0);
                            Locomotive.DynamicBrakeChangeActiveState(false);
                        }
                    }
                }

                if (!Locomotive.PowerOn) // || Locomotive.Mirel.NZ1 || Locomotive.Mirel.NZ2 || Locomotive.Mirel.NZ3 || Locomotive.Mirel.NZ4 || Locomotive.Mirel.NZ5)
                {
                    controllerVolts = 0;
                    Locomotive.SetThrottlePercent(0);
                    if (Locomotive.DynamicBrakePercent > 0)
                        Locomotive.SetDynamicBrakePercent(0);
                    Locomotive.DynamicBrakeIntervention = -1;
                    maxForceN = 0;
                    ForceThrottleAndDynamicBrake = 0;
                    Ampers = 0;
                }
                else
                    ForceThrottleAndDynamicBrake = controllerVolts;

                Locomotive.MotiveForceN = maxForceN;
                Locomotive.TractiveForceN = maxForceN;
            }

            if (playerNotDriveableTrainLocomotives.Count > 0) // update any other than the player's locomotive in the consist throttles to percentage of the current force and the max force
            {
                foreach (MSTSLocomotive lc in playerNotDriveableTrainLocomotives)
                {
                    if (UseThrottle)
                    {
                        lc.SetThrottlePercent(Locomotive.ThrottlePercent);
                    }
                    else
                    {
                        lc.IsAPartOfPlayerTrain = true;
                        float locoPercent = Locomotive.MaxForceN - (Locomotive.MaxForceN - Locomotive.MotiveForceN);
                        lc.ThrottleOverriden = locoPercent / Locomotive.MaxForceN;
                    }
                }
            }
        }

        public enum AvvSignal {
            Stop,
            Restricted,
            Restricting40,
            Clear,
            Restricting60,
            Restricting80,
            Restricting100
        };
        public AvvSignal avvSignal = AvvSignal.Stop;
        public void DrawAvvSignal(AvvSignal ToState)
        {
            avvSignal = ToState;
        }
    }
}