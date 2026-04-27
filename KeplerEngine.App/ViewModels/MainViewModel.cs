using System.ComponentModel;
using System.Runtime.CompilerServices;
using KeplerEngine.Simulation;

namespace KeplerEngine.App.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    public OrbitSimulation Simulation { get; }
    public OrbitalBodyViewModel? SelectedBody { get; private set; }

    public string ClockText     => Simulation.Clock.FormatUT();
    public string WarpText      => Simulation.Clock.WarpLabel;
    public bool   IsPaused      => Simulation.Clock.Paused;
    public string PauseLabel    => IsPaused ? "▶ Resume" : "⏸ Pause";

    public MainViewModel(OrbitSimulation sim)
    {
        Simulation = sim;

        if (sim.Orbitals.Count > 0)
            SelectedBody = new OrbitalBodyViewModel(sim.Orbitals[0]);
    }

    // Called on every tick from the renderer
    public void Tick(double realDt)
    {
        Simulation.Tick(realDt);
        SelectedBody?.RefreshTelemetry();

        OnPropertyChanged(nameof(ClockText));
        OnPropertyChanged(nameof(WarpText));
    }

    public void TogglePause()
    {
        Simulation.Clock.Toggle();
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(PauseLabel));
    }

    public void WarpUp()   { Simulation.Clock.WarpUp();   OnPropertyChanged(nameof(WarpText)); }
    public void WarpDown() { Simulation.Clock.WarpDown(); OnPropertyChanged(nameof(WarpText)); }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}