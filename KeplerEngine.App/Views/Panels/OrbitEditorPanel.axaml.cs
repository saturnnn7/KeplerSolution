using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using KeplerEngine.App.ViewModels;

namespace KeplerEngine.App.Views.Panels;

public partial class OrbitEditorPanel : UserControl
{
    private OrbitalBodyViewModel? _vm;
    private bool _suspendSync; // не даём слайдеру и TextBox гонять друг друга

    public OrbitEditorPanel()
    {
        InitializeComponent();
    }

    public void SetViewModel(OrbitalBodyViewModel vm)
    {
        _vm = vm;

        // Слайдеры
        BindSlider("SliderA",     v => { vm.SemiMajorAxisKm = v;         SyncFromVm(); });
        BindSlider("SliderE",     v => { vm.Eccentricity    = v;         SyncFromVm(); });
        BindSlider("SliderI",     v => { vm.InclinationDeg  = v;         SyncFromVm(); });
        BindSlider("SliderLAN",   v => { vm.LANDeg          = v;         SyncFromVm(); });
        BindSlider("SliderArgPe", v => { vm.ArgumentOfPeriapsisDeg = v;  SyncFromVm(); });
        BindSlider("SliderNu",    v => { vm.TrueAnomalyDeg  = v;         SyncFromVm(); });

        // TextBox — применяем по Enter или потере фокуса
        BindTextBox("InputA",     v => { vm.SemiMajorAxisKm = v;         SyncFromVm(); });
        BindTextBox("InputE",     v => { vm.Eccentricity    = v;         SyncFromVm(); });
        BindTextBox("InputI",     v => { vm.InclinationDeg  = v;         SyncFromVm(); });
        BindTextBox("InputLAN",   v => { vm.LANDeg          = v;         SyncFromVm(); });
        BindTextBox("InputArgPe", v => { vm.ArgumentOfPeriapsisDeg = v;  SyncFromVm(); });
        BindTextBox("InputNu",    v => { vm.TrueAnomalyDeg  = v;         SyncFromVm(); });

        SyncFromVm();
    }

    // Update all controls from the ViewModel (called on every tick)
    public void SyncFromVm()
    {
        if (_vm == null || _suspendSync) return;
        _suspendSync = true;

        SetSlider("SliderA",     _vm.SemiMajorAxisKm);
        SetSlider("SliderE",     _vm.Eccentricity);
        SetSlider("SliderI",     _vm.InclinationDeg);
        SetSlider("SliderLAN",   _vm.LANDeg);
        SetSlider("SliderArgPe", _vm.ArgumentOfPeriapsisDeg);
        SetSlider("SliderNu",    _vm.TrueAnomalyDeg);

        SetInput("InputA",     $"{_vm.SemiMajorAxisKm:F1}");
        SetInput("InputE",     $"{_vm.Eccentricity:F4}");
        SetInput("InputI",     $"{_vm.InclinationDeg:F1}");
        SetInput("InputLAN",   $"{_vm.LANDeg:F1}");
        SetInput("InputArgPe", $"{_vm.ArgumentOfPeriapsisDeg:F1}");
        SetInput("InputNu",    $"{_vm.TrueAnomalyDeg:F1}");

        SetLabel("LblA",     $"{_vm.SemiMajorAxisKm:F1} km");
        SetLabel("LblE",     $"{_vm.Eccentricity:F4}");
        SetLabel("LblI",     $"{_vm.InclinationDeg:F1}°");
        SetLabel("LblLAN",   $"{_vm.LANDeg:F1}°");
        SetLabel("LblArgPe", $"{_vm.ArgumentOfPeriapsisDeg:F1}°");
        SetLabel("LblNu",    $"{_vm.TrueAnomalyDeg:F1}°");

        SetLabel("TxtPeDerived",     $"{_vm.PeriapsisKm:F1} km");
        SetLabel("TxtApDerived",     $"{_vm.ApoapsisKm:F1} km");
        SetLabel("TxtPeriodDerived", $"{_vm.PeriodMin:F1} min");

        _suspendSync = false;
    }

    // -- Helpers ----------

    private void BindSlider(string name, Action<double> onChange)
    {
        var slider = this.FindControl<Slider>(name)!;
        slider.PropertyChanged += (_, e) =>
        {
            if (_suspendSync) return;
            if (e.Property == RangeBase.ValueProperty)
                onChange(slider.Value);
        };
    }

    private void BindTextBox(string name, Action<double> onChange)
    {
        var box = this.FindControl<TextBox>(name)!;

        box.KeyDown += (_, e) =>
        {
            if (e.Key != Avalonia.Input.Key.Enter) return;
            if (double.TryParse(box.Text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double v))
                onChange(v);
        };

        box.LostFocus += (_, _) =>
        {
            if (double.TryParse(box.Text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double v))
                onChange(v);
        };
    }

    private void SetSlider(string name, double value)
    {
        var s = this.FindControl<Slider>(name)!;
        s.Value = Math.Clamp(value, s.Minimum, s.Maximum);
    }

    private void SetInput(string name, string text)
        => this.FindControl<TextBox>(name)!.Text = text;

    private void SetLabel(string name, string text)
        => this.FindControl<TextBlock>(name)!.Text = text;
}