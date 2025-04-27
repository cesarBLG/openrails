// COPYRIGHT 2011 by the Open Rails project.
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


namespace Orts.Simulation.RollingStocks.SubSystems.PowerTransmissions
{
    public class SeriesMotor : ElectricMotor
    {
        float armatureResistanceOhms;
        public float ArmatureResistanceOhms 
        {
            set
            {
                armatureResistanceOhms = value;
            }
            get
            {
                return armatureResistanceOhms * (235.0f + temperatureK) / (235.0f + 20.0f);
            }
        }

        float fieldResistanceOhms;
        public float FieldResistanceOhms
        {
            set
            {
                fieldResistanceOhms = value;
            }
            get
            {
                return fieldResistanceOhms * (235.0f + temperatureK) / (235.0f + 20.0f);
            }
        }
        public float FieldInductance { set; get; }

        public float ArmatureCurrentA { get; private set; }

        public float FieldCurrentA { get; private set; }


        public float TerminalVoltageV { set; get; }

        public float ArmatureVoltageV
        {
            get
            {
                return ArmatureCurrentA * ArmatureResistanceOhms + BackEMFvoltageV;
            }
        }

        public float StartingResistorOhms { set; get; }
        public float ShuntPercent;
        public float BackEMFvoltageV { get; private set; }

        float FieldWb;

        public float NominalRevolutionsRad;
        public float NominalVoltageV;
        public float NominalCurrentA;

        public void UpdateField()
        {
            FieldWb = (NominalVoltageV - (ArmatureResistanceOhms + FieldResistanceOhms) * NominalCurrentA) / NominalRevolutionsRad;
            if (FieldCurrentA <= NominalCurrentA)
                FieldWb *= FieldCurrentA / NominalCurrentA;
            FieldWb *= 1.0f - ShuntPercent / 100;
        }

        public SeriesMotor(float nomCurrentA, float nomVoltageV, float nomRevolutionsRad, Axle axle, MSTSLocomotive locomotive) : base(axle, locomotive)
        {
            NominalCurrentA = nomCurrentA;
            NominalVoltageV = nomVoltageV;
            NominalRevolutionsRad = nomRevolutionsRad;
        }

        public override double GetDevelopedTorqueNm(double revolutionsRad)
        {
            return FieldWb * ArmatureCurrentA/* - (frictionTorqueNm * revolutionsRad / NominalRevolutionsRad * revolutionsRad / NominalRevolutionsRad)*/;
        }

        public override void Update(float timeSpan)
        {
            float revolutionsRad = (float)AxleConnected.AxleSpeedMpS * AxleConnected.TransmissionRatio / AxleConnected.WheelRadiusM;
            BackEMFvoltageV = revolutionsRad * FieldWb;
            ArmatureCurrentA = FieldCurrentA / (1.0f - ShuntPercent / 100);
            if ((BackEMFvoltageV * FieldCurrentA) >= 0.0f)
            {
                FieldCurrentA += timeSpan / FieldInductance *
                    (TerminalVoltageV
                        - BackEMFvoltageV
                        - ArmatureCurrentA * (ArmatureResistanceOhms + StartingResistorOhms)
                        - FieldCurrentA * FieldResistanceOhms * (1.0f - ShuntPercent / 100)
                    //- ((fieldCurrentA == 0.0) ? 0.0 : 2.0)            //voltage drop on brushes
                    );
            }
            else
            {
                FieldCurrentA = 0.0f;
            }          

            UpdateField();

            powerLossesW = ArmatureResistanceOhms * ArmatureCurrentA * ArmatureCurrentA +
                           FieldResistanceOhms * FieldCurrentA * FieldCurrentA;

            //temperatureK += timeSpan * ThermalCoeffJ_m2sC * SurfaceM / (SpecificHeatCapacityJ_kg_C * WeightKg)
            //    * ((powerLossesW - CoolingPowerKW) / (SpecificHeatCapacityJ_kg_C * WeightKg) - temperatureK);

            base.Update(timeSpan);
        }
        public override void Initialize()
        {
            FieldCurrentA = 0.0f;
            ArmatureCurrentA = 0.0f;
            FieldWb = 0.0f;
            base.Initialize();
        }
    }
}
