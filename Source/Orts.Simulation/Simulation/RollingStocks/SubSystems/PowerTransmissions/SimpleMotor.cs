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

namespace Orts.Simulation.RollingStocks.SubSystems.PowerTransmissions
{
    public class SimpleMotor : ElectricMotor
    {
        public float TargetForceN;
        /// <summary>
        /// Maximum torque, as determined by throttle setting and force curves
        /// </summary>
        float requiredTorqueNm;

        public SimpleMotor(Axle axle, MSTSLocomotive locomotive) : base(axle, locomotive)
        {
        }
        public override double GetDevelopedTorqueNm(double motorSpeedRadpS)
        {
            return requiredTorqueNm;
        }
        public override void Update(float timeSpan)
        {
            requiredTorqueNm = Math.Abs(TargetForceN) * AxleConnected.WheelRadiusM / AxleConnected.TransmissionRatio;
            base.Update(timeSpan);
        }
    }
}
