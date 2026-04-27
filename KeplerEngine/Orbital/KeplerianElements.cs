using System;
using KeplerEngine.Core;
 
namespace KeplerEngine.Orbital
{
    public class KeplerianElements
    {
        // -- Elements ----------
        private double _a;
        private double _e;
        private double _i;
        private double _omega;   // Ω RAAN
        private double _argPe;   // ω arg of periapsis
        private double _nu;      // ν true anomaly

        /// <summary>Semi-major axis [m]. Must be &gt; 0 for bound orbits.</summary>
        public double SemiMajorAxis
        {
            get => _a;
            set { ValidatePositive(value, "SemiMajorAxis"); _a = value; }
        }

        /// <summary>Eccentricity [dimensionless]. 0 = circular.</summary>
        public double Eccentricity
        {
            get => _e;
            set { if (value < 0) throw new ArgumentException("Eccentricity cannot be negative."); _e = value; }
        }

        /// <summary>Inclination [radians].</summary>
        public double Inclination
        {
            get => _i;
            set => _i = OrbitalMath.WrapAngle(value);
        }
        public double InclinationDeg
        {
            get => OrbitalMath.RadToDeg(_i);
            set => Inclination = OrbitalMath.DegToRad(value);
        }
        
        /// <summary>Longitude of ascending node Ω [radians].</summary>
        public double LAN
        {
            get => _omega;
            set => _omega = OrbitalMath.WrapAngle(value);
        }
        public double LANDeg
        {
            get => OrbitalMath.RadToDeg(_omega);
            set => LAN = OrbitalMath.DegToRad(value);
        }

        /// <summary>Argument of periapsis ω [radians].</summary>
        public double ArgumentOfPeriapsis
        {
            get => _argPe;
            set => _argPe = OrbitalMath.WrapAngle(value);
        }
        public double ArgumentOfPeriapsisDeg
        {
            get => OrbitalMath.RadToDeg(_argPe);
            set => ArgumentOfPeriapsis = OrbitalMath.DegToRad(value);
        }

        /// <summary>True anomaly ν [radians]. This is the "where on the orbit" element.</summary>
        public double TrueAnomaly
        {
            get => _nu;
            set => _nu = OrbitalMath.WrapAngle(value);
        }
        public double TrueAnomalyDeg
        {
            get => OrbitalMath.RadToDeg(_nu);
            set => TrueAnomaly = OrbitalMath.DegToRad(value);
        }

        // -- Derived anomalies (read-only, depend on current ν) ----------

        public double EccentricAnomaly  => OrbitalMath.EccentricAnomalyFromTrue(_nu, _e);
        public double MeanAnomaly       => OrbitalMath.MeanAnomalyFromEccentric(EccentricAnomaly, _e);

        // -- Constructors ----------

        /// <summary>All-parameters constructor (angles in radians).</summary>
        public KeplerianElements(double a, double e, double i, double lan, double argPe, double nu)
        {
            SemiMajorAxis       = a;
            Eccentricity        = e;
            Inclination         = i;
            LAN                 = lan;
            ArgumentOfPeriapsis = argPe;
            TrueAnomaly         = nu;
        }

        /// <summary>Convenience: circular equatorial orbit at given altitude above a body.</summary>
        public static KeplerianElements CircularOrbit(
            double altitude, double bodyRadius, double inclinationRad = 0, 
            double lanRad = 0, double argPeRad = 0, double nuRad = 0)
        {
            return new KeplerianElements(
                a: bodyRadius + altitude,
                e: 0,
                i: inclinationRad,
                lan: lanRad,
                argPe: argPeRad,
                nu: nuRad);
        }

        // -- Orbital shape queries ----------

        public double Periapsis  => OrbitalMath.Periapsis(_a, _e);
        public double Apoapsis   => OrbitalMath.Apoapsis(_a, _e);
        public double SemiMinorAxis => _a * Math.Sqrt(1.0 - _e * _e);

        /// <summary>Current orbital radius r at current true anomaly.</summary>
        public double CurrentRadius => OrbitalMath.RadiusAtTrueAnomaly(_a, _e, _nu);
 
        public bool IsCircular   => _e < 1e-6;
        public bool IsElliptic   => _e < 1.0;
        public bool IsParabolic  => Math.Abs(_e - 1.0) < 1e-6;
        public bool IsHyperbolic => _e > 1.0;

        // -- Helpers ----------
        private static void ValidatePositive(double v, string name)
        {
            if (v <= 0) throw new ArgumentException($"{name} must be > 0, got {v}.");
        }

        public KeplerianElements Clone() => new(_a, _e, _i, _omega, _argPe, _nu);
 
        public override string ToString() =>
            $"a={_a:E3}m  e={_e:F6}  i={InclinationDeg:F2}°  " +
            $"Ω={LANDeg:F2}°  ω={ArgumentOfPeriapsisDeg:F2}°  ν={TrueAnomalyDeg:F2}°";
    }
}