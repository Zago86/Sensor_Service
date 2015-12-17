using System;

namespace Sensor_Service
{
    public abstract class ParametersMonitored
    {
        protected double v1, v2, v3, i1, i2, i3, p, q, s, u1, u2, u3, fr, pf, spf, etr, psign, qsign, pm, pmax;
        protected uint et;

        public double V1 { get { return v1; } }
        public double V2 { get { return v2; } }
        public double V3 { get { return v3; } }
        public double I1 { get { return i1; } }
        public double I2 { get { return i2; } }
        public double I3 { get { return i3; } }
        public double P { get { return p; } }
        public double Q { get { return q; } }
        public double S { get { return s; } }
        public uint ET { get { return et; } }
        public double U1 { get { return u1; } }
        public double U2 { get { return u2; } }
        public double U3 { get { return u3; } }
        public double FR { get { return fr; } }
        public double PF { get { return pf; } }
        public double SPF { get { return spf; } }
        public double ETR { get { return etr; } }
        public double PSIGN { get { return psign; } }
        public double QSIGN { get { return qsign; } }
        public double PM { get { return pm; } }
        public double PMAX { get { return pmax; } }

        internal abstract void Read(byte[] buffer);
        internal abstract byte[] Write();

        ///
        public struct DataMonitored
        {
            public double V1;
            public double V2;
            public double V3;
            public double I1;
            public double I2;
            public double I3;
            public double P ;
            public double Q ;
            public double S ;
            public uint ET  ;
            public double U1;
            public double U2;
            public double U3;
            public double FR;
            public double PF;
            public double SPF;
            public double ETR;
            public double PSIGN;
            public double QSIGN;
            public double PM;
            public double PMAX;
            public string DatetimeReadData;
        }
    }
}
