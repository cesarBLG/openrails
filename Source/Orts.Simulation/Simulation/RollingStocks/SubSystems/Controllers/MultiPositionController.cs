// COPYRIGHT 2010 - 2021 by the Open Rails project.
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
using ORTS.Common;

namespace Orts.Simulation.RollingStocks.SubSystems.Controllers
{ 
    public class MultiPositionController
    {
        public MultiPositionController(MSTSLocomotive locomotive)
        {
            Locomotive = locomotive;
            Simulator = Locomotive.Simulator;
            controllerPosition = ControllerPosition.Neutral;
        }
        MSTSLocomotive Locomotive;
        Simulator Simulator;

        public Dictionary<String, String> PositionsList = new Dictionary<string, string>();

        public bool Equipped = false;
        public bool StateChanged = false;

        public ControllerPosition controllerPosition = new ControllerPosition();
        public ControllerCruiseControlLogic cruiseControlLogic = new ControllerCruiseControlLogic();
        protected float elapsedSecondsFromLastChange = 0;
        protected bool checkNeutral = false;
        protected bool noKeyPressed = true;
        protected string currentPosition = "";
        protected bool emergencyBrake = false;
        protected bool previousDriveModeWasAddPower = false;
        protected bool isBraking = false;
        protected bool needPowerUpAfterBrake = false;
        public bool CanControlTrainBrake = false;
        protected bool initialized = false;
        protected bool movedForward = false;
        protected bool movedAft = false;

        public void Save(BinaryWriter outf)
        {
            outf.Write(this.checkNeutral);
            outf.Write((int)this.controllerPosition);
            outf.Write(this.currentPosition);
            outf.Write(this.elapsedSecondsFromLastChange);
            outf.Write(this.emergencyBrake);
            outf.Write(this.Equipped);
            outf.Write(this.isBraking);
            outf.Write(this.needPowerUpAfterBrake);
            outf.Write(this.noKeyPressed);
            outf.Write(this.previousDriveModeWasAddPower);
            outf.Write(this.StateChanged);
        }

        public void Restore(BinaryReader inf)
        {
            initialized = true;
            checkNeutral = inf.ReadBoolean();
            int fControllerPosition = inf.ReadInt32();
            controllerPosition = (ControllerPosition)fControllerPosition;
            currentPosition = inf.ReadString();
            elapsedSecondsFromLastChange = inf.ReadSingle();
            emergencyBrake = inf.ReadBoolean();
            Equipped = inf.ReadBoolean();
            isBraking = inf.ReadBoolean();
            needPowerUpAfterBrake = inf.ReadBoolean();
            noKeyPressed = inf.ReadBoolean();
            previousDriveModeWasAddPower = inf.ReadBoolean();
            StateChanged = inf.ReadBoolean();
        }

        public void Update(float elapsedClockSeconds)
        {
            if (!initialized)
            {
                foreach (KeyValuePair<String, String> pair in PositionsList)
                {
                    if (pair.Value.ToLower() == "default")
                    {
                        currentPosition = pair.Key;
                        break;
                    }
                }
                initialized = true;
            }
            if (!Locomotive.IsPlayerTrain) return;

            if (Locomotive.CruiseControl.DynamicBrakePriority) return;
            ReloadPositions();
            if (!Locomotive.Battery) return;
            if (Locomotive.AbsSpeedMpS > 0)
            {
                if (emergencyBrake)
                {
                    Locomotive.TrainBrakeController.TCSEmergencyBraking = true;
                    return;
                }
            }
            else
            {
                emergencyBrake = false;
            }
            if (Locomotive.TrainBrakeController.TCSEmergencyBraking)
                Locomotive.TrainBrakeController.TCSEmergencyBraking = false;
            elapsedSecondsFromLastChange += elapsedClockSeconds;
            // Simulator.Confirmer.MSG(currentPosition.ToString());
            if (checkNeutral)
            {
                if (elapsedSecondsFromLastChange > 0.2f)
                {
                    CheckNeutralPosition();
                    checkNeutral = false;
                }
            }
            if (!Locomotive.CruiseControl.Equipped || Locomotive.CruiseControl.SpeedRegMode == CruiseControl.SpeedRegulatorMode.Manual)
            {
                if (controllerPosition == ControllerPosition.ThrottleIncrease)
                {
                    if (Locomotive.DynamicBrakePercent < 1)
                    {
                        if (Locomotive.ThrottlePercent < 100)
                        {
                            float step = 100 / Locomotive.CruiseControl.ThrottleFullRangeIncreaseTimeSeconds;
                            step *= elapsedClockSeconds;
                            Locomotive.SetThrottlePercent(Locomotive.ThrottlePercent + step);
                        }
                    }
                }
                if (controllerPosition == ControllerPosition.ThrottleIncreaseFast)
                {
                    if (Locomotive.DynamicBrakePercent < 1)
                    {
                        if (Locomotive.ThrottlePercent < 100)
                        {
                            float step = 100 / Locomotive.CruiseControl.ThrottleFullRangeIncreaseTimeSeconds * 2;
                            step *= elapsedClockSeconds;
                            Locomotive.SetThrottlePercent(Locomotive.ThrottlePercent + step);
                        }
                    }
                }
                if (controllerPosition == ControllerPosition.ThrottleDecrease)
                {
                    if (Locomotive.ThrottlePercent > 0)
                    {
                        float step = 100 / Locomotive.CruiseControl.ThrottleFullRangeDecreaseTimeSeconds;
                        step *= elapsedClockSeconds;
                        Locomotive.SetThrottlePercent(Locomotive.ThrottlePercent - step);
                    }
                }
                if (controllerPosition == ControllerPosition.ThrottleDecreaseFast)
                {
                    if (Locomotive.ThrottlePercent > 0)
                    {
                        float step = 100 / Locomotive.CruiseControl.ThrottleFullRangeDecreaseTimeSeconds * 2;
                        step *= elapsedClockSeconds;
                        Locomotive.SetThrottlePercent(Locomotive.ThrottlePercent - step);
                    }
                }
                if (controllerPosition == ControllerPosition.Neutral || controllerPosition == ControllerPosition.DynamicBrakeHold)
                {
                    if (CanControlTrainBrake)
                    {
                        if (Locomotive.TrainBrakeController.GetStatus().ToLower() == "apply")
                        {
                            Locomotive.StartTrainBrakeDecrease(null);
                        }
                        if (Locomotive.TrainBrakeController.GetStatus().ToLower() == "neutral")
                        {
                            Locomotive.StopTrainBrakeDecrease();
                        }
                    }
                    if (Locomotive.ThrottlePercent < 2)
                    {
                        if (Locomotive.ThrottlePercent != 0)
                            Locomotive.SetThrottlePercent(0);
                    }
                    if (Locomotive.ThrottlePercent > 1)
                    {
                        Locomotive.SetThrottlePercent(Locomotive.ThrottlePercent - 1f);
                    }
                    if (Locomotive.ThrottlePercent > 100)
                    {
                        Locomotive.ThrottlePercent = 100;
                    }

                }
                if (controllerPosition == ControllerPosition.DynamicBrakeIncrease)
                {
                    if (CanControlTrainBrake)
                    {
                        if (Locomotive.TrainBrakeController.GetStatus().ToLower() == "apply")
                        {
                            Locomotive.StartTrainBrakeDecrease(null);
                        }
                        if (Locomotive.TrainBrakeController.GetStatus().ToLower() == "neutral")
                        {
                            Locomotive.StopTrainBrakeDecrease();
                        }
                    }
                    if (Locomotive.DynamicBrakePercent == -1) Locomotive.SetDynamicBrakePercent(0);
                    if (Locomotive.ThrottlePercent < 1 && Locomotive.DynamicBrakePercent < 100)
                    {
                        Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent + 2f);
                    }
                }
                if (controllerPosition == ControllerPosition.DynamicBrakeIncreaseFast)
                {
                    if (CanControlTrainBrake)
                    {
                        if (Locomotive.TrainBrakeController.GetStatus().ToLower() == "apply")
                        {
                            Locomotive.StartTrainBrakeDecrease(null);
                        }
                        if (Locomotive.TrainBrakeController.GetStatus().ToLower() == "neutral")
                        {
                            Locomotive.StopTrainBrakeDecrease();
                        }
                    }
                    if (Locomotive.DynamicBrakePercent == -1) Locomotive.SetDynamicBrakePercent(0);
                    if (Locomotive.ThrottlePercent < 1 && Locomotive.DynamicBrakePercent < 100)
                    {
                        Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent + 2f);
                    }
                }
                if (controllerPosition == ControllerPosition.DynamicBrakeDecrease)
                {
                    if (Locomotive.DynamicBrakePercent > 0)
                    {
                        Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent - 1);
                    }
                }
                if (controllerPosition == ControllerPosition.Drive || controllerPosition == ControllerPosition.ThrottleHold)
                {
                    if (Locomotive.DynamicBrakePercent < 2)
                    {
                        Locomotive.SetDynamicBrakePercent(-1);
                    }
                    if (Locomotive.DynamicBrakePercent > 1)
                    {
                        Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent - 1);
                    }
                }
                if (controllerPosition == ControllerPosition.TrainBrakeIncrease)
                {
                    if (CanControlTrainBrake)
                    {
                        if (Locomotive.TrainBrakeController.GetStatus().ToLower() != "apply")
                        {
                            String test = Locomotive.TrainBrakeController.GetStatus().ToLower();
                            Locomotive.StartTrainBrakeIncrease(null);
                        }
                        else
                        {
                            Locomotive.StopTrainBrakeIncrease();
                        }
                    }
                }
                else if (controllerPosition == ControllerPosition.Drive)
                {
                    if (CanControlTrainBrake)
                    {
                        if (Locomotive.TrainBrakeController.GetStatus().ToLower() != "release")
                        {
                            String boom = Locomotive.TrainBrakeController.GetStatus().ToString();
                            Locomotive.StartTrainBrakeDecrease(null);
                        }
                        else
                            Locomotive.StopTrainBrakeDecrease();
                    }
                }
                if (controllerPosition == ControllerPosition.TrainBrakeDecrease)
                {
                    if (CanControlTrainBrake)
                    {
                        if (Locomotive.TrainBrakeController.GetStatus().ToLower() != "release")
                        {
                            String boom = Locomotive.TrainBrakeController.GetStatus().ToString();
                            Locomotive.StartTrainBrakeDecrease(null);
                        }
                        else
                            Locomotive.StopTrainBrakeDecrease();
                    }
                }
                if (controllerPosition == ControllerPosition.EmergencyBrake)
                {
                    EmergencyBrakes();
                    emergencyBrake = true;
                }
                if (controllerPosition == ControllerPosition.ThrottleIncreaseOrDynamicBrakeDecrease)
                {
                    if (Locomotive.DynamicBrakePercent > 0)
                    {
                        Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent - 0.2f);
                        if (Locomotive.DynamicBrakePercent < 2)
                        {
                            Locomotive.SetDynamicBrakePercent(0);
                            Locomotive.DynamicBrakeChangeActiveState(false);
                        }
                    }
                    else
                    {
                        if (Locomotive.ThrottlePercent < 100)
                            Locomotive.SetThrottlePercent(Locomotive.ThrottlePercent + 0.2f);
                        if (Locomotive.ThrottlePercent > 100)
                            Locomotive.SetThrottlePercent(100);
                    }
                }
                if (controllerPosition == ControllerPosition.ThrottleIncreaseOrDynamicBrakeDecreaseFast)
                {
                    if (Locomotive.DynamicBrakePercent > 0)
                    {
                        Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent - 1);
                        if (Locomotive.DynamicBrakePercent < 2)
                        {
                            Locomotive.SetDynamicBrakePercent(0);
                            Locomotive.DynamicBrakeChangeActiveState(false);
                        }
                    }
                    else
                    {
                        if (Locomotive.ThrottlePercent < 100)
                            Locomotive.SetThrottlePercent(Locomotive.ThrottlePercent + 1);
                        if (Locomotive.ThrottlePercent > 100)
                            Locomotive.SetThrottlePercent(100);
                    }
                }

                if (controllerPosition == ControllerPosition.DynamicBrakeIncreaseOrThrottleDecrease)
                {
                    if (Locomotive.ThrottlePercent > 0)
                    {
                        Locomotive.SetThrottlePercent(Locomotive.ThrottlePercent - 0.2f);
                        if (Locomotive.ThrottlePercent < 0)
                            Locomotive.ThrottlePercent = 0;
                    }
                    else
                    {
                        if (Locomotive.DynamicBrakePercent < 100)
                        {
                            Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent + 0.2f);
                        }
                        if (Locomotive.DynamicBrakePercent > 100)
                            Locomotive.SetDynamicBrakePercent(100);
                    }
                }
                if (controllerPosition == ControllerPosition.DynamicBrakeIncreaseOrThrottleDecreaseFast)
                {
                    if (Locomotive.ThrottlePercent > 0)
                    {
                        Locomotive.SetThrottlePercent(Locomotive.ThrottlePercent - 1);
                        if (Locomotive.ThrottlePercent < 0)
                            Locomotive.ThrottlePercent = 0;
                    }
                    else
                    {
                        if (Locomotive.DynamicBrakePercent < 100)
                        {
                            Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent + 1);
                        }
                        if (Locomotive.DynamicBrakePercent > 100)
                            Locomotive.SetDynamicBrakePercent(100);
                    }
                }
            }
            else if (Locomotive.CruiseControl.Equipped && Locomotive.CruiseControl.SpeedRegMode == CruiseControl.SpeedRegulatorMode.Auto)
            {
                if (cruiseControlLogic == ControllerCruiseControlLogic.SpeedOnly)
                {
                    if (controllerPosition == ControllerPosition.ThrottleIncrease || controllerPosition == ControllerPosition.ThrottleIncreaseFast)
                    {
                        if (!Locomotive.CruiseControl.ContinuousSpeedIncreasing && movedForward) return;
                        movedForward = true;
                        Locomotive.CruiseControl.SelectedSpeedMpS = Locomotive.CruiseControl.SelectedSpeedMpS + Locomotive.CruiseControl.SpeedRegulatorNominalSpeedStepMpS;
                        if (Locomotive.CruiseControl.SelectedSpeedMpS > Locomotive.MaxSpeedMpS) Locomotive.CruiseControl.SelectedSpeedMpS = Locomotive.MaxSpeedMpS;
                    }
                    if (controllerPosition == ControllerPosition.ThrottleDecrease || controllerPosition == ControllerPosition.ThrottleDecreaseFast)
                    {
                        if (!Locomotive.CruiseControl.ContinuousSpeedDecreasing && movedAft) return;
                        movedAft = true;
                        Locomotive.CruiseControl.SelectedSpeedMpS = Locomotive.CruiseControl.SelectedSpeedMpS - Locomotive.CruiseControl.SpeedRegulatorNominalSpeedStepMpS;
                        if (Locomotive.CruiseControl.SelectedSpeedMpS < 0) Locomotive.CruiseControl.SelectedSpeedMpS = 0;
                    }
                    return;
                }
                if (controllerPosition == ControllerPosition.ThrottleIncrease)
                {
                    isBraking = false;
                    Locomotive.CruiseControl.SpeedSelMode = CruiseControl.SpeedSelectorMode.Start;
                    previousDriveModeWasAddPower = true;
                }
                if (controllerPosition == ControllerPosition.Neutral)
                {
                    Locomotive.CruiseControl.SpeedSelMode = CruiseControl.SpeedSelectorMode.Neutral;
                }
                if (controllerPosition == ControllerPosition.Drive)
                {
                    bool applyPower = true;
                    if (isBraking && needPowerUpAfterBrake)
                    {
                        if (Locomotive.DynamicBrakePercent < 2)
                        {
                            Locomotive.SetDynamicBrakePercent(-1);
                        }
                        if (Locomotive.DynamicBrakePercent > 1)
                        {
                            Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent - 1);
                        }
                        if (CanControlTrainBrake)
                        {
                            if (Locomotive.TrainBrakeController.GetStatus().ToLower() != "release")
                            {
                                Locomotive.StartTrainBrakeDecrease(null);
                            }
                            else
                                Locomotive.StopTrainBrakeDecrease();
                        }
                        applyPower = false;
                    }
                    if (applyPower) Locomotive.CruiseControl.SpeedSelMode = CruiseControl.SpeedSelectorMode.On;
                }
                if (controllerPosition == ControllerPosition.DynamicBrakeIncrease)
                {
                    isBraking = true;
                    previousDriveModeWasAddPower = false;
                    Locomotive.CruiseControl.SpeedSelMode = CruiseControl.SpeedSelectorMode.Neutral;
                    if (CanControlTrainBrake)
                    {
                        if (Locomotive.TrainBrakeController.GetStatus().ToLower() == "apply")
                        {
                            Locomotive.StartTrainBrakeDecrease(null);
                        }
                        if (Locomotive.TrainBrakeController.GetStatus().ToLower() == "neutral")
                        {
                            Locomotive.StopTrainBrakeDecrease();
                        }
                    }
                    if (Locomotive.ThrottlePercent < 1 && Locomotive.DynamicBrakePercent < 100)
                    {
                        if (Locomotive.DynamicBrakePercent < 0)
                            Locomotive.DynamicBrakeChangeActiveState(true);
                        Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent + 1f);
                    }
                }
                if (controllerPosition == ControllerPosition.DynamicBrakeIncreaseFast)
                {
                    isBraking = true;
                    previousDriveModeWasAddPower = false;
                    Locomotive.CruiseControl.SpeedSelMode = CruiseControl.SpeedSelectorMode.Neutral;
                    if (CanControlTrainBrake)
                    {
                        if (Locomotive.TrainBrakeController.GetStatus().ToLower() == "apply")
                        {
                            Locomotive.StartTrainBrakeDecrease(null);
                        }
                        if (Locomotive.TrainBrakeController.GetStatus().ToLower() == "neutral")
                        {
                            Locomotive.StopTrainBrakeDecrease();
                        }
                    }
                    if (Locomotive.ThrottlePercent < 1 && Locomotive.DynamicBrakePercent < 100)
                    {
                        Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent + 2f);
                    }
                }
                if (controllerPosition == ControllerPosition.TrainBrakeIncrease)
                {
                    isBraking = true;
                    previousDriveModeWasAddPower = false;
                    Locomotive.CruiseControl.SpeedSelMode = CruiseControl.SpeedSelectorMode.Neutral;
                    if (CanControlTrainBrake)
                    {
                        if (Locomotive.TrainBrakeController.GetStatus().ToLower() != "apply")
                        {
                            String test = Locomotive.TrainBrakeController.GetStatus().ToLower();
                            Locomotive.StartTrainBrakeIncrease(null);
                        }
                        else
                        {
                            Locomotive.StopTrainBrakeIncrease();
                        }
                    }
                }
                if (controllerPosition == ControllerPosition.EmergencyBrake)
                {
                    isBraking = true;
                    previousDriveModeWasAddPower = false;
                    Locomotive.CruiseControl.SpeedSelMode = CruiseControl.SpeedSelectorMode.Neutral;
                    EmergencyBrakes();
                    emergencyBrake = true;
                }
                if (controllerPosition == ControllerPosition.CruiseControlSpeedIncrease)
                {
                    Locomotive.CruiseControl.SelectedSpeedMpS = MpS.FromKpH((MpS.ToKpH(Locomotive.CruiseControl.SelectedSpeedMpS) + 1));
                }
                if (controllerPosition == ControllerPosition.CruiseControlSpeedDecrease)
                {
                    Locomotive.CruiseControl.SelectedSpeedMpS = MpS.FromKpH((MpS.ToKpH(Locomotive.CruiseControl.SelectedSpeedMpS) + 1));
                }
                if (controllerPosition == ControllerPosition.CruiseControlSpeedSetZero)
                {
                    Locomotive.CruiseControl.SelectedSpeedMpS = 0;
                }
            }
        }

        private bool messageDisplayed = false;
        public void DoMovement(Movement movement)
        {
            if (movement == Movement.Aft) movedForward = false;
            if (movement == Movement.Forward) movedAft = false;
            if (movement == Movement.Neutral) movedForward = movedAft = false;
            messageDisplayed = false;
            if (String.IsNullOrEmpty(currentPosition))
            {
                foreach (KeyValuePair<String, String> pair in PositionsList)
                {
                    if (pair.Value.ToLower() == "default")
                    {
                        currentPosition = pair.Key;
                        break;
                    }
                }
            }
            if (movement == Movement.Forward)
            {
                noKeyPressed = false;
                checkNeutral = false;
                bool isFirst = true;
                string previous = "";
                foreach (KeyValuePair<String, String> pair in PositionsList)
                {
                    if (pair.Key == currentPosition)
                    {
                        if (isFirst)
                            break;
                        currentPosition = previous;
                        break;
                    }
                    isFirst = false;
                    previous = pair.Key;
                }
            }
            if (movement == Movement.Aft)
            {
                noKeyPressed = false;
                checkNeutral = false;
                bool selectNext = false;
                foreach (KeyValuePair<String, String> pair in PositionsList)
                {
                    if (selectNext)
                    {
                        currentPosition = pair.Key;
                        break;
                    }
                    if (pair.Key == currentPosition) selectNext = true;
                }
            }
            if (movement == Movement.Neutral)
            {
                noKeyPressed = true;
                foreach (KeyValuePair<String, String> pair in PositionsList)
                {
                    if (pair.Key == currentPosition)
                    {
                        if (pair.Value.ToLower() == "springloadedbackwards" || pair.Value.ToLower() == "springloadedforwards")
                        {
                            checkNeutral = true;
                            elapsedSecondsFromLastChange = 0;
                        }
                        if (pair.Value.ToLower() == "springloadedbackwardsimmediatelly" || pair.Value.ToLower() == "springloadedforwardsimmediatelly")
                        {
                            CheckNeutralPosition();
                            ReloadPositions();
                        }
                    }
                }
            }

        }

        protected void ReloadPositions()
        {
            if (noKeyPressed)
            {
                foreach (KeyValuePair<String, String> pair in PositionsList)
                {
                    if (pair.Key == currentPosition)
                    {
                        if (pair.Value.ToLower() == "cruisecontrol.needincreaseafteranybrake")
                        {
                            needPowerUpAfterBrake = true;
                        }
                        if (pair.Value.ToLower() == "springloadedforwards" || pair.Value.ToLower() == "springloadedbackwards")
                        {
                            if (elapsedSecondsFromLastChange > 0.2f)
                            {
                                elapsedSecondsFromLastChange = 0;
                                checkNeutral = true;
                            }
                        }
                    }
                }
            }
            switch (currentPosition)
            {
                case "ThrottleIncrease":
                    {
                        if (!messageDisplayed) Simulator.Confirmer.Information("Controller: S");
                        controllerPosition = ControllerPosition.ThrottleIncrease;
                        break;
                    }
                case "ThrottleIncreaseFast":
                    {
                        if (!messageDisplayed) Simulator.Confirmer.Information("Controller: S");
                        controllerPosition = ControllerPosition.ThrottleIncreaseFast;
                        break;
                    }
                case "ThrottleIncreaseOrDynamicBrakeDecrease":
                    {
                        if (!messageDisplayed) Simulator.Confirmer.Information("Controller: +");
                        controllerPosition = ControllerPosition.ThrottleIncreaseOrDynamicBrakeDecrease;
                        break;
                    }
                case "ThrottleIncreaseOrDynamicBrakeDecreaseFast":
                    {
                        if (!messageDisplayed) Simulator.Confirmer.Information("Controller: ++");
                        controllerPosition = ControllerPosition.ThrottleIncreaseOrDynamicBrakeDecreaseFast;
                        break;
                    }
                case "DynamicBrakeIncreaseOrThrottleDecrease":
                    {
                        if (!messageDisplayed) Simulator.Confirmer.Information("Controller: -");
                        controllerPosition = ControllerPosition.DynamicBrakeIncreaseOrThrottleDecrease;
                        break;
                    }
                case "DynamicBrakeIncreaseOrThrottleDecreaseFast":
                    {
                        if (!messageDisplayed) Simulator.Confirmer.Information("Controller: --");
                        controllerPosition = ControllerPosition.DynamicBrakeIncreaseOrThrottleDecreaseFast;
                        break;
                    }
                case "ThrottleDecrease":
                    {
                        if (!messageDisplayed) Simulator.Confirmer.Information("Controller: V");
                        controllerPosition = ControllerPosition.ThrottleDecrease;
                        break;
                    }
                case "ThrottleDecreaseFast":
                    {
                        if (!messageDisplayed) Simulator.Confirmer.Information("Controller: V");
                        controllerPosition = ControllerPosition.ThrottleDecreaseFast;
                        break;
                    }
                case "Drive":
                    {
                        //Locomotive.TrainBrakePriority = false;
                        if (!messageDisplayed) Simulator.Confirmer.Information("Controller: J");
                        controllerPosition = ControllerPosition.Drive;
                        break;
                    }
                case "ThrottleHold":
                    {
                        if (!messageDisplayed) Simulator.Confirmer.Information("Controller: J");
                        controllerPosition = ControllerPosition.ThrottleHold;
                        break;
                    }
                case "Neutral":
                    {
                        if (!messageDisplayed) Simulator.Confirmer.Information("Controller: V");
                        controllerPosition = ControllerPosition.Neutral;
                        break;
                    }
                case "KeepCurrent":
                    {
                        if (!messageDisplayed) Simulator.Confirmer.Information("Controller: 0");
                        controllerPosition = ControllerPosition.KeepCurrent;
                        break;
                    }
                case "DynamicBrakeHold":
                    {
                        if (!messageDisplayed) Simulator.Confirmer.Information("Controller: V");
                        controllerPosition = ControllerPosition.DynamicBrakeHold;
                        break;
                    }
                case "DynamicBrakeIncrease":
                    {
                        if (!messageDisplayed) Simulator.Confirmer.Information("Controller: BE");
                        controllerPosition = ControllerPosition.DynamicBrakeIncrease;
                        break;
                    }
                case "DynamicBrakeIncreaseFast":
                    {
                        if (!messageDisplayed) Simulator.Confirmer.Information("Controller: BE2");
                        controllerPosition = ControllerPosition.DynamicBrakeIncreaseFast;
                        break;
                    }
                case "DynamicBrakeDecrease":
                    {
                        if (!messageDisplayed) Simulator.Confirmer.Information("Controller: V");
                        controllerPosition = ControllerPosition.DynamicBrakeDecrease;
                        break;
                    }
                case "TrainBrakeIncrease":
                    {
                        //Locomotive.TrainBrakePriority = true;
                        if (!messageDisplayed) Simulator.Confirmer.Information("Controller: BP");
                        controllerPosition = ControllerPosition.TrainBrakeIncrease;
                        break;
                    }
                case "TrainBrakeDecrease":
                    {
                        if (!messageDisplayed) Simulator.Confirmer.Information("Controller: V");
                        controllerPosition = ControllerPosition.TrainBrakeDecrease;
                        break;
                    }
                case "EmergencyBrake":
                    {
                        //Locomotive.TrainBrakePriority = true;
                        if (!messageDisplayed) Simulator.Confirmer.Information("Controller: R");
                        controllerPosition = ControllerPosition.EmergencyBrake;
                        break;
                    }
                case "CruiseControlSpeedIncrease":
                    {
                        controllerPosition = ControllerPosition.CruiseControlSpeedIncrease;
                        break;
                    }
                case "CruiseControlSpeedDecrease":
                    {
                        controllerPosition = ControllerPosition.CruiseControlSpeedIncrease;
                        break;
                    }
                case "CruiseControlSpeedSetZero":
                    {
                        controllerPosition = ControllerPosition.CruiseControlSpeedSetZero;
                        break;
                    }
            }
            messageDisplayed = true;
        }

        protected void CheckNeutralPosition()
        {
            bool setNext = false;
            String previous = "";
            foreach (KeyValuePair<String, String> pair in PositionsList)
            {
                if (setNext)
                {
                    currentPosition = pair.Key;
                    break;
                }
                if (pair.Key == currentPosition)
                {
                    if (pair.Value.ToLower() == "springloadedbackwards" || pair.Value.ToLower() == "springloadedbackwardsimmediatelly")
                    {
                        setNext = true;
                    }
                    if (pair.Value.ToLower() == "springloadedforwards" || pair.Value.ToLower() == "springloadedforwardsimmediatelly")
                    {
                        currentPosition = previous;
                        break;
                    }
                }
                previous = pair.Key;
            }
        }

        protected void EmergencyBrakes()
        {
            Locomotive.SetThrottlePercent(0);
            Locomotive.SetDynamicBrakePercent(100);
            Locomotive.TrainBrakeController.TCSEmergencyBraking = true;
        }
        public enum Movement
        {
            Forward,
            Neutral,
            Aft
        };
        public enum ControllerPosition
        {
            Neutral,
            Drive,
            ThrottleIncrease,
            ThrottleDecrease,
            ThrottleIncreaseFast,
            ThrottleDecreaseFast,
            DynamicBrakeIncrease, DynamicBrakeDecrease,
            DynamicBrakeIncreaseFast,
            TrainBrakeIncrease,
            TrainBrakeDecrease,
            EmergencyBrake,
            ThrottleHold,
            DynamicBrakeHold,
            ThrottleIncreaseOrDynamicBrakeDecreaseFast,
            ThrottleIncreaseOrDynamicBrakeDecrease,
            DynamicBrakeIncreaseOrThrottleDecreaseFast,
            DynamicBrakeIncreaseOrThrottleDecrease,
            KeepCurrent,
            CruiseControlSpeedIncrease,
            CruiseControlSpeedDecrease,
            CruiseControlSpeedSetZero
        };
        public enum ControllerCruiseControlLogic
        {
            None,
            Full,
            SpeedOnly
        }
    }
}