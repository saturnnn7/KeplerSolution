using System;
using KeplerEngine.Core;
using KeplerEngine.Orbital;
using KeplerEngine.Physics;

namespace KeplerEngine.Physics
{
    /// <summary>
    /// A point-mass body orbiting a CelestialBody.
    /// Holds Keplerian elements and exposes current Cartesian state.
    /// All element mutations are possible at any time — useful for maneuver planning.
    /// </summary>
    public class OrbitalBody
    {
        // ── Identity ───────────────────────────────────────────────────────────
        public string        Name     { get; set; }
        public CelestialBody Primary  { get; private set; }

        // ── Orbital elements ───────────────────────────────────────────────────
        public KeplerianElements Elements { get; private set; }

        // ── Constructor ────────────────────────────────────────────────────────
        public OrbitalBody(string name, CelestialBody primary, KeplerianElements elements)
        {
            Name     = name;
            Primary  = primary;
            Elements = elements;
        }

        // ── Keplerian element shortcuts (delegates to Elements) ────────────────

        public double SemiMajorAxis
        {
            get => Elements.SemiMajorAxis;
            set => Elements.SemiMajorAxis = value;
        }
        public double Eccentricity
        {
            get => Elements.Eccentricity;
            set => Elements.Eccentricity = value;
        }
        public double Inclination
        {
            get => Elements.Inclination;
            set => Elements.Inclination = value;
        }
        public double InclinationDeg
        {
            get => Elements.InclinationDeg;
            set => Elements.InclinationDeg = value;
        }
        public double LAN
        {
            get => Elements.LAN;
            set => Elements.LAN = value;
        }
        public double LANDeg
        {
            get => Elements.LANDeg;
            set => Elements.LANDeg = value;
        }
        public double ArgumentOfPeriapsis
        {
            get => Elements.ArgumentOfPeriapsis;
            set => Elements.ArgumentOfPeriapsis = value;
        }
        public double ArgumentOfPeriapsisDeg
        {
            get => Elements.ArgumentOfPeriapsisDeg;
            set => Elements.ArgumentOfPeriapsisDeg = value;
        }
        public double TrueAnomaly
        {
            get => Elements.TrueAnomaly;
            set => Elements.TrueAnomaly = value;
        }
        public double TrueAnomalyDeg
        {
            get => Elements.TrueAnomalyDeg;
            set => Elements.TrueAnomalyDeg = value;
        }

        // ── Current state ──────────────────────────────────────────────────────

        /// <summary>Cartesian position and velocity in the inertial frame.</summary>
        public StateVector CurrentState =>
            StateVector.FromKeplerian(Elements, Primary.Mu);

        /// <summary>Position relative to Primary center.</summary>
        public Vector3d Position => CurrentState.Position + Primary.Position;

        /// <summary>Current orbital radius from body center.</summary>
        public double OrbitalRadius => Elements.CurrentRadius;

        /// <summary>Altitude above body surface.</summary>
        public double Altitude => OrbitalRadius - Primary.Radius;

        /// <summary>Current orbital speed (vis-viva).</summary>
        public double Speed => OrbitalMath.VisViva(Primary.Mu, OrbitalRadius, Elements.SemiMajorAxis);

        /// <summary>Orbital period. NaN for hyperbolic / parabolic.</summary>
        public double Period => Elements.IsElliptic
            ? OrbitalMath.OrbitalPeriod(Elements.SemiMajorAxis, Primary.Mu)
            : double.NaN;

        // ── Maneuver helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Set orbit from a Cartesian state vector.
        /// Useful for applying Δv: compute new velocity, then call this.
        /// </summary>
        public void SetFromStateVector(StateVector sv)
        {
            Elements = StateVector.ToKeplerian(sv, Primary.Mu);
        }

        /// <summary>
        /// Apply an instantaneous Δv in the velocity frame (prograde/normal/radial).
        ///   deltaPrograde: along velocity vector
        ///   deltaNormal:   perpendicular to orbital plane (out of plane)
        ///   deltaRadial:   toward the central body (radial, inward)
        /// </summary>
        public void ApplyDeltaV(double deltaPrograde, double deltaNormal, double deltaRadial)
        {
            StateVector sv = CurrentState;

            Vector3d prograde = sv.Velocity.Normalized;
            Vector3d h        = Vector3d.Cross(sv.Position, sv.Velocity).Normalized; // normal to plane
            Vector3d radial   = sv.Position.Normalized;

            Vector3d dv = prograde * deltaPrograde
                        + h       * deltaNormal
                        + radial  * deltaRadial;

            var newSv = new StateVector(sv.Position, sv.Velocity + dv);
            SetFromStateVector(newSv);
        }

        // ── Orbit plotting ─────────────────────────────────────────────────────

        /// <summary>
        /// Return N evenly-spaced points along the full orbit in world space.
        /// Useful for rendering the orbit path.
        /// </summary>
        public Vector3d[] GetOrbitPoints(int count = 360)
        {
            var pts  = new Vector3d[count];
            var snap = Elements.Clone();
            double step = OrbitalMath.TwoPi / count;

            for (int idx = 0; idx < count; idx++)
            {
                snap.TrueAnomaly = idx * step;
                var sv = StateVector.FromKeplerian(snap, Primary.Mu);
                pts[idx] = sv.Position + Primary.Position;
            }
            return pts;
        }

        public override string ToString() =>
            $"{Name} @ {Primary.Name}  alt={Altitude / 1000:F1}km  v={Speed:F1}m/s  T={Period:F0}s\n  {Elements}";
    }
}