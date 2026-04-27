using System;
using KeplerEngine.Core;
using KeplerEngine.Orbital;
using KeplerEngine.Physics;

namespace KeplerEngine.Orbital
{
    /// <summary>
    /// Propagates an orbit forward by solving Kepler's equation at each time step.
    ///
    /// For elliptic orbits (e &lt; 1):
    ///   1. Compute initial mean anomaly M₀ from current true anomaly ν₀
    ///   2. Advance M = M₀ + n·Δt  (n = mean motion = 2π/T)
    ///   3. Solve M → E (Newton-Raphson)
    ///   4. Compute ν from E
    ///
    /// This is purely analytic — no numerical integration error accumulates.
    /// </summary>
    public static class KeplerPropagator
    {
        /// <summary>
        /// Advance an OrbitalBody by simDeltaSeconds of simulation time.
        /// Modifies body.Elements.TrueAnomaly in place.
        /// </summary>
        public static void Propagate(OrbitalBody body, double simDeltaSeconds)
        {
            if (simDeltaSeconds == 0) return;
            PropagateElements(body.Elements, body.Primary.Mu, simDeltaSeconds);
        }

        /// <summary>
        /// Advance KeplerianElements in place. Lower-level method.
        /// </summary>
        public static void PropagateElements(KeplerianElements el, double mu, double dt)
        {
            double a = el.SemiMajorAxis;
            double e = el.Eccentricity;

            if (el.IsHyperbolic || el.IsParabolic)
            {
                // Hyperbolic/parabolic propagation — universal variable method (future work)
                // For now, skip propagation on non-closed orbits
                return;
            }

            // Mean motion n = √(μ/a³)
            double n = Math.Sqrt(mu / (a * a * a));

            // Current mean anomaly
            double E0 = OrbitalMath.EccentricAnomalyFromTrue(el.TrueAnomaly, e);
            double M0 = OrbitalMath.MeanAnomalyFromEccentric(E0, e);

            // Advance mean anomaly
            double M = M0 + n * dt;

            // Solve for new eccentric anomaly
            double E = OrbitalMath.EccentricAnomalyFromMean(M, e);

            // Convert to true anomaly
            el.TrueAnomaly = OrbitalMath.TrueAnomalyFromEccentric(E, e);
        }

        /// <summary>
        /// Return what the state vector will be after dt seconds, without mutating anything.
        /// </summary>
        public static StateVector PredictState(OrbitalBody body, double dt)
        {
            var snap = body.Elements.Clone();
            PropagateElements(snap, body.Primary.Mu, dt);
            return StateVector.FromKeplerian(snap, body.Primary.Mu);
        }

        /// <summary>
        /// Returns the UT at which the body will next reach the given true anomaly.
        /// </summary>
        public static double TimeToTrueAnomaly(OrbitalBody body, double targetNu, double currentUT)
        {
            if (!body.Elements.IsElliptic) return double.NaN;

            double e  = body.Elements.Eccentricity;
            double mu = body.Primary.Mu;
            double a  = body.Elements.SemiMajorAxis;
            double T  = OrbitalMath.OrbitalPeriod(a, mu);
            double n  = OrbitalMath.TwoPi / T;

            double E0 = OrbitalMath.EccentricAnomalyFromTrue(body.Elements.TrueAnomaly, e);
            double M0 = OrbitalMath.MeanAnomalyFromEccentric(E0, e);

            double Et = OrbitalMath.EccentricAnomalyFromTrue(targetNu, e);
            double Mt = OrbitalMath.MeanAnomalyFromEccentric(Et, e);

            double dt = (Mt - M0) / n;
            if (dt < 0) dt += T;   // wrap to next occurrence

            return currentUT + dt;
        }
    }
}