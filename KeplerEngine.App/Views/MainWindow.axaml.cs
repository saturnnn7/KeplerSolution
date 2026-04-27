using Avalonia.Controls;
using Avalonia.Threading;
using KeplerEngine.App.Rendering;
using KeplerEngine.App.ViewModels;
using KeplerEngine.App.Views.Panels;
using KeplerEngine.Simulation;

namespace KeplerEngine.App.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();

        var sim = OrbitSimulation.KerbinExample();
        _vm = new MainViewModel(sim);

        var renderer = this.FindControl<OrbitRenderer>("Renderer")!;
        renderer.SetSimulation(_vm, OnTick);

        var editor = this.FindControl<OrbitEditorPanel>("OrbitEditor")!;
        if (_vm.SelectedBody != null)
            editor.SetViewModel(_vm.SelectedBody);

        this.FindControl<Button>("BtnPause")!.Click    += (_, _) => TogglePause();
        this.FindControl<Button>("BtnWarpUp")!.Click   += (_, _) => { _vm.WarpUp();   Refresh(); };
        this.FindControl<Button>("BtnWarpDown")!.Click += (_, _) => { _vm.WarpDown(); Refresh(); };
    }

    private void OnTick(double realDt)
    {
        _vm.Tick(realDt);

        Dispatcher.UIThread.Post(() =>
        {
            this.FindControl<TextBlock>("TxtUT")!.Text   = _vm.ClockText;
            this.FindControl<TextBlock>("TxtWarp")!.Text = _vm.WarpText;

            this.FindControl<TelemetryPanel>("Telemetry")!.Update(_vm);
            this.FindControl<OrbitEditorPanel>("OrbitEditor")!.SyncFromVm();
        });
    }

    private void TogglePause()
    {
        _vm.TogglePause();
        this.FindControl<Button>("BtnPause")!.Content = _vm.PauseLabel;
    }

    private void Refresh()
    {
        this.FindControl<TextBlock>("TxtWarp")!.Text = _vm.WarpText;
    }
}