using System;
 
namespace KeplerEngine.Time
{
    /// <summary>
    /// Universal Simulation Clock.
    ///
    /// Tracks Universal Time (UT) in seconds.
    /// Supports time warp (1×, 10×, 100× ... up to any multiplier).
    /// Call Tick(realDeltaSeconds) each simulation update.
    /// </summary>
    public class SimulationClock
    {
        // -- State ----------

        /// <summary>Universal Time in seconds since epoch.</summary>
        public double UT { get; private set; }

        /// <summary>Current time warp multiplier. 1 = real-time, 10 = 10× speed, etc.</summary>
        public double TimeWarp { get; private set; } = 1.0;
        
        /// <summary>Is the simulation paused?</summary>
        public bool Paused { get; private set; }

        // -- Epoch ----------

        /// <summary>Human-readable start time of the simulation (cosmetic).</summary>
        public DateTime Epoch { get; }
        
        public SimulationClock(double startUT = 0, DateTime? epoch = null)
        {
            UT = startUT;
            Epoch = epoch ?? new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc); // J2000
        }

        // -- Warp presets (KSP-style) ----------
        private static readonly double[] WarpPresets = { 1, 5, 10, 50, 100, 1_000, 10_000, 100_000 };
        private int _warpIndex = 0;

        public void WarpUp()
        {
            if (_warpIndex < WarpPresets.Length - 1)
                SetWarp(WarpPresets[++_warpIndex]);
        }

        public void WarpDown()
        {
            if (_warpIndex > 0)
                SetWarp(WarpPresets[--_warpIndex]);
        }

        public void SetWarp(double multiplier)
        {
            if (multiplier < 0) throw new ArgumentException("Warp must be non-negative.");
            TimeWarp = multiplier;
            _warpIndex = 0;
            for (int i = 0; i < WarpPresets.Length; i++)
                if (WarpPresets[i] <= multiplier) _warpIndex = i;
        }

        public void Pause() => Paused = true;
        public void Resume() => Paused = false;
        public void Toggle() => Paused = !Paused;

        // -- Tick ----------

        /// <summary>
        /// Advance the clock by realDeltaSeconds of real time.
        /// The simulation UT advances by realDeltaSeconds × TimeWarp.
        /// </summary>
        public double Tick(double realDeltaSeconds)
        {
            if (Paused || realDeltaSeconds < 0) return 0;
            double simDelta = realDeltaSeconds * TimeWarp;
            UT += simDelta;
            return simDelta;
        }
        
        /// <summary>Jump UT directly (e.g., warp-to-node).</summary>
        public void SetUT(double ut) => UT = ut;

        // --Formatting helpers  ----------
        public string FormatUT()
        {
            double t   = UT;
            int years  = (int)(t / (365.25 * 86400)); t -= years  * 365.25 * 86400;
            int days   = (int)(t / 86400);             t -= days   * 86400;
            int hours  = (int)(t / 3600);              t -= hours  * 3600;
            int mins   = (int)(t / 60);                t -= mins   * 60;
            double sec = t;
            return $"Y{years + 1} D{days + 1:D3} {hours:D2}:{mins:D2}:{sec:F0}";
        }

        public string WarpLabel => TimeWarp >= 1000 ? $"{TimeWarp / 1000:0}k×" : $"{TimeWarp}×";
 
        public override string ToString() => $"UT={FormatUT()}  Warp={WarpLabel}{(Paused ? " [PAUSED]" : "")}";
    }
}