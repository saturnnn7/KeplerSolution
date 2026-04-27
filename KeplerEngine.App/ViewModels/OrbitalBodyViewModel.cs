using System.ComponentModel;
using System.Runtime.CompilerServices;
using KeplerEngine.Core;
using KeplerEngine.Physics;

namespace KeplerEngine.App.ViewModels;

public class OrbitalBodyViewModel : INotifyPropertyChanged
{
    private readonly OrbitalBody _body;
    private bool _suppressUpdate; // Preventing recursion during updates

    public OrbitalBodyViewModel(OrbitalBody body)
    {
        _body = body;
    }

    // ── Keplerian Elements ----------

    // Semi-major axis [km] — displayed in kilometers in the UI, but in meters in the engine
    public double SemiMajorAxisKm
    {
        get => _body.Elements.SemiMajorAxis / 1000.0;
        set
        {
            if (_suppressUpdate) return;
            _body.Elements.SemiMajorAxis = Math.Max(value, _body.Primary.Radius / 1000.0 + 1) * 1000.0;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PeriapsisKm));
            OnPropertyChanged(nameof(ApoapsisKm));
            OnPropertyChanged(nameof(PeriodMin));
        }
    }

    public double SemiMajorAxisMin => _body.Primary.Radius / 1000.0 + 1;
    public double SemiMajorAxisMax => _body.Primary.Radius / 1000.0 * 20;

    // Eccentricity [0 .. 0.99]
    public double Eccentricity
    {
        get => _body.Elements.Eccentricity;
        set
        {
            if (_suppressUpdate) return;
            _body.Elements.Eccentricity = Math.Clamp(value, 0, 0.99);
            OnPropertyChanged();
            OnPropertyChanged(nameof(PeriapsisKm));
            OnPropertyChanged(nameof(ApoapsisKm));
        }
    }

    // Inclination [deg]
    public double InclinationDeg
    {
        get => _body.Elements.InclinationDeg;
        set
        {
            if (_suppressUpdate) return;
            _body.Elements.InclinationDeg = Math.Clamp(value, 0, 180);
            OnPropertyChanged();
        }
    }

    // LAN [deg]
    public double LANDeg
    {
        get => _body.Elements.LANDeg;
        set
        {
            if (_suppressUpdate) return;
            _body.Elements.LANDeg = value;
            OnPropertyChanged();
        }
    }

    // Argument of Periapsis [deg]
    public double ArgumentOfPeriapsisDeg
    {
        get => _body.Elements.ArgumentOfPeriapsisDeg;
        set
        {
            if (_suppressUpdate) return;
            _body.Elements.ArgumentOfPeriapsisDeg = value;
            OnPropertyChanged();
        }
    }

    // True Anomaly [deg]
    public double TrueAnomalyDeg
    {
        get => _body.Elements.TrueAnomalyDeg;
        set
        {
            if (_suppressUpdate) return;
            _body.Elements.TrueAnomalyDeg = value;
            OnPropertyChanged();
        }
    }

    // -- Derived (read-only, telemetry) ----------

    public double PeriapsisKm  => (_body.Elements.Periapsis  - _body.Primary.Radius) / 1000.0;
    public double ApoapsisKm   => (_body.Elements.Apoapsis   - _body.Primary.Radius) / 1000.0;
    public double AltitudeKm   => _body.Altitude / 1000.0;
    public double SpeedMs      => _body.Speed;
    public double PeriodMin    => _body.Period / 60.0;
    public string BodyName     => _body.Name;
    public string PrimaryName  => _body.Primary.Name;

    // -- Called every simulation tick to refresh telemetry ----------

    public void RefreshTelemetry()
    {
        _suppressUpdate = true;
        OnPropertyChanged(nameof(AltitudeKm));
        OnPropertyChanged(nameof(SpeedMs));
        OnPropertyChanged(nameof(TrueAnomalyDeg));
        OnPropertyChanged(nameof(SemiMajorAxisKm));
        OnPropertyChanged(nameof(Eccentricity));
        OnPropertyChanged(nameof(InclinationDeg));
        OnPropertyChanged(nameof(LANDeg));
        OnPropertyChanged(nameof(ArgumentOfPeriapsisDeg));
        OnPropertyChanged(nameof(PeriapsisKm));
        OnPropertyChanged(nameof(ApoapsisKm));
        OnPropertyChanged(nameof(PeriodMin));
        _suppressUpdate = false;
    }

    // -- INotifyPropertyChanged ----------

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}