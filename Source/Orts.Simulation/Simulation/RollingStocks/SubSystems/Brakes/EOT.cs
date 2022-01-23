// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
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


using Microsoft.Xna.Framework;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using Orts.Simulation.AIs;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using Orts.Simulation.Signalling;
using Orts.Simulation.Timetables;
using ORTS.Common;
using ORTS.Scripting.Api;
using ORTS.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Event = Orts.Common.Event;

namespace Orts.Simulation.RollingStocks.SubSystems.Brakes
{
    public class EOT
    {
        public enum EOTstate
        {
            Disarmed,
            CommTestOn,
            Armed,
            LocalTestOn,
            ArmNow,
            ArmedTwoWay
        }

        public float CommTestDelayS { get; protected set; } = 5f;
        public float LocalTestDelayS { get; protected set; } = 25f;

        public static Random IDRandom = new Random();
        public int ID;
        public EOTstate EOTState;
        public bool EOTEmergencyBrakingOn = false;
        public Train Train;
        public MSTSLocomotive.EOTenabled EOTType;

        protected Timer DelayTimer;

        public EOT(MSTSLocomotive.EOTenabled eotEnabled, bool armed, Train train)
        {
            Train = train;
            EOTState = EOTstate.Disarmed;
            EOTType = eotEnabled;
            ID = IDRandom.Next(0, 99999);
            if (armed)
                EOTState = EOTstate.Armed;
        }

        public EOT(BinaryReader inf, Train train)
        {
            Train = train;
            ID = inf.ReadInt32();
            EOTState = (EOTstate)(inf.ReadInt32());
        }

        public void Initialize()
        {
        }

        public void Update()
        {
            UpdateState();
            if (Train.Simulator.PlayerLocomotive.Train == Train && EOTState == EOTstate.ArmedTwoWay &&
                (EOTEmergencyBrakingOn ||
                (Train.Simulator.PlayerLocomotive as MSTSLocomotive).TrainBrakeController.GetStatus().ToLower().StartsWith("emergency")))
                Train.Cars.Last().BrakeSystem.AngleCockBOpen = true;
            else
                Train.Cars.Last().BrakeSystem.AngleCockBOpen = false;

        }

        private void UpdateState()
        {
            switch (EOTState)
            {
                case EOTstate.Disarmed:
                    break;
                case EOTstate.CommTestOn:
                    if (DelayTimer.Triggered)
                    {
                        DelayTimer.Stop();
                        EOTState = EOTstate.Armed;
                    }
                    break;
                case EOTstate.Armed:
                    if (EOTType == MSTSLocomotive.EOTenabled.twoway)
                    {
                        if (DelayTimer == null)
                            DelayTimer = new Timer(this);
                        DelayTimer.Setup(LocalTestDelayS);
                        EOTState = EOTstate.LocalTestOn;
                        DelayTimer.Start();
                    }
                    break;
                case EOTstate.LocalTestOn:
                    if (DelayTimer.Triggered)
                    {
                        DelayTimer.Stop();
                        EOTState = EOTstate.ArmNow;
                    }
                    break;
                case EOTstate.ArmNow:
                    break;
                case EOTstate.ArmedTwoWay:
                    break;
            }
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write(ID);
            outf.Write((int)EOTState);
        }

        public float GetDataOf(CabViewControl cvc)
        {
            float data = 0;
            switch (cvc.ControlType)
            {
                case CABViewControlTypes.ORTS_EOT_ID:
                    data = ID;
                    break;
                case CABViewControlTypes.ORTS_EOT_STATE_DISPLAY:
                    data = (float)(int)EOTState;
                    break;
                case CABViewControlTypes.ORTS_EOT_EMERGENCY_BRAKE:
                    data = EOTEmergencyBrakingOn ? 1 : 0;
                    break;
            }
            return data;
        }

        public void CommTest()
        {
            if (EOTState == EOTstate.Disarmed &&
                (EOTType == MSTSLocomotive.EOTenabled.oneway || EOTType == MSTSLocomotive.EOTenabled.twoway))
            {
                if (DelayTimer == null)
                    DelayTimer = new Timer(this);
                DelayTimer.Setup(CommTestDelayS);
                EOTState = EOTstate.CommTestOn;
                DelayTimer.Start();
            }
        }

        public void Disarm()
        {
            EOTState = EOTstate.Disarmed;
        }

        public void ArmTwoWay()
        {
            if (EOTState == EOTstate.ArmNow)
                EOTState = EOTstate.ArmedTwoWay;
        }

        public void EmergencyBrake (bool toState)
        {
            if (EOTState == EOTstate.ArmedTwoWay)
            {
                EOTEmergencyBrakingOn = toState;
            }
        }

    }
}