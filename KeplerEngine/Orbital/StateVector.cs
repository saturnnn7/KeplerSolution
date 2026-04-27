using System;
using KeplerEngine.Core;
using KeplerEngine.Orbital;
 
namespace KeplerEngine.Orbital
{
    /// <summary>
    /// Cartesian state vector: position r and velocity v in the inertial frame.
    /// Also provides bidirectional conversion between Keplerian elements and state vectors.
    /// Reference frame: Z-up, X toward vernal equinox.
    /// </summary>
    public readonly struct StateVector
    {
        public readonly Vector3d Position;   // [m]
        public readonly Vector3d Velocity;   // [m/s]
 
        public StateVector(Vector3d position, Vector3d velocity)
        {
            Position = position;
            Velocity = velocity;
        }
 
        // ── Keplerian → Cartesian ─────────────────────────────────────────────
 
        /// <summary>
        /// Convert Keplerian elements to Cartesian state vector.
        /// μ = gravitational parameter of the central body.
        /// </summary>
        public static StateVector FromKeplerian(KeplerianElements el, double mu)
        {
            double a   = el.SemiMajorAxis;
            double e   = el.Eccentricity;
            double i   = el.Inclination;
            double lan = el.LAN;
            double w   = el.ArgumentOfPeriapsis;
            double nu  = el.TrueAnomaly;
 
            // ── Position and velocity in the perifocal (PQW) frame ────────────
            double p   = a * (1.0 - e * e);                            // semi-latus rectum
            double r   = p / (1.0 + e * Math.Cos(nu));                 // orbital radius
            double v_r = Math.Sqrt(mu / p) * e * Math.Sin(nu);         // radial speed
            double v_t = Math.Sqrt(mu / p) * (1.0 + e * Math.Cos(nu)); // tangential speed
 
            Vector3d r_pqw = new(r * Math.Cos(nu), r * Math.Sin(nu), 0);
            Vector3d v_pqw = new(-Math.Sqrt(mu / p) * Math.Sin(nu),
                                  Math.Sqrt(mu / p) * (e + Math.Cos(nu)),
                                  0);
 
            // ── Rotation: PQW → ECI (Earth-Centered Inertial or generic inertial) ─
            // R = Rz(-Ω) · Rx(-i) · Rz(-ω)
            var R = RotationPQWtoIJK(lan, i, w);
 
            Vector3d pos = R.Multiply(r_pqw);
            Vector3d vel = R.Multiply(v_pqw);
 
            return new StateVector(pos, vel);
        }
 
        // ── Cartesian → Keplerian ─────────────────────────────────────────────
 
        /// <summary>
        /// Convert Cartesian state vector to Keplerian elements.
        /// </summary>
        public static KeplerianElements ToKeplerian(StateVector sv, double mu)
        {
            Vector3d r = sv.Position;
            Vector3d v = sv.Velocity;
 
            double rMag = r.Magnitude;
            double vMag = v.Magnitude;
 
            // Angular momentum
            Vector3d h = Vector3d.Cross(r, v);
            double   hMag = h.Magnitude;
 
            // Node vector (points toward ascending node)
            Vector3d n = Vector3d.Cross(Vector3d.UnitZ, h);
            double   nMag = n.Magnitude;
 
            // Eccentricity vector (points toward periapsis)
            Vector3d eVec = (r * (vMag * vMag - mu / rMag) - v * Vector3d.Dot(r, v)) / mu;
            double   e    = eVec.Magnitude;
 
            // Specific orbital energy
            double energy = vMag * vMag / 2.0 - mu / rMag;
 
            // Semi-major axis  (a < 0 for hyperbolic → handled)
            double a = (Math.Abs(energy) < 1e-12) ? double.PositiveInfinity : -mu / (2.0 * energy);
 
            // Inclination
            double inc = Math.Acos(Math.Clamp(h.Z / hMag, -1.0, 1.0));
 
            // LAN (Ω)
            double lan = 0;
            if (nMag > 1e-12)
            {
                lan = Math.Acos(Math.Clamp(n.X / nMag, -1.0, 1.0));
                if (n.Y < 0) lan = OrbitalMath.TwoPi - lan;
            }
 
            // Argument of periapsis (ω)
            double argPe = 0;
            if (nMag > 1e-12 && e > 1e-12)
            {
                argPe = Math.Acos(Math.Clamp(Vector3d.Dot(n, eVec) / (nMag * e), -1.0, 1.0));
                if (eVec.Z < 0) argPe = OrbitalMath.TwoPi - argPe;
            }
 
            // True anomaly (ν)
            double nu = 0;
            if (e > 1e-12)
            {
                nu = Math.Acos(Math.Clamp(Vector3d.Dot(eVec, r) / (e * rMag), -1.0, 1.0));
                if (Vector3d.Dot(r, v) < 0) nu = OrbitalMath.TwoPi - nu;
            }
            else
            {
                // Circular: use argument of latitude
                nu = Math.Acos(Math.Clamp(Vector3d.Dot(n, r) / (nMag * rMag), -1.0, 1.0));
                if (r.Z < 0) nu = OrbitalMath.TwoPi - nu;
            }
 
            return new KeplerianElements(a, e, inc, lan, argPe, nu);
        }
 
        // ── Rotation matrix ───────────────────────────────────────────────────
 
        private static Matrix3x3 RotationPQWtoIJK(double lan, double inc, double argPe)
        {
            double cosO = Math.Cos(lan),  sinO = Math.Sin(lan);
            double cosI = Math.Cos(inc),  sinI = Math.Sin(inc);
            double cosW = Math.Cos(argPe),sinW = Math.Sin(argPe);
 
            // Combined rotation matrix R = Rz(lan) * Rx(inc) * Rz(argPe)
            return new Matrix3x3(
                cosO * cosW - sinO * sinW * cosI,
               -cosO * sinW - sinO * cosW * cosI,
                sinO * sinI,
 
                sinO * cosW + cosO * sinW * cosI,
               -sinO * sinW + cosO * cosW * cosI,
               -cosO * sinI,
 
                sinW * sinI,
                cosW * sinI,
                cosI
            );
        }
 
        public override string ToString() =>
            $"r={Position}  v={Velocity}";
    }
 
    // ── Minimal 3×3 rotation matrix ───────────────────────────────────────────
    internal readonly struct Matrix3x3
    {
        private readonly double _m00, _m01, _m02;
        private readonly double _m10, _m11, _m12;
        private readonly double _m20, _m21, _m22;
 
        public Matrix3x3(
            double m00, double m01, double m02,
            double m10, double m11, double m12,
            double m20, double m21, double m22)
        {
            (_m00, _m01, _m02) = (m00, m01, m02);
            (_m10, _m11, _m12) = (m10, m11, m12);
            (_m20, _m21, _m22) = (m20, m21, m22);
        }
 
        public Vector3d Multiply(Vector3d v) => new(
            _m00 * v.X + _m01 * v.Y + _m02 * v.Z,
            _m10 * v.X + _m11 * v.Y + _m12 * v.Z,
            _m20 * v.X + _m21 * v.Y + _m22 * v.Z);
    }
}