using Avalonia.Controls;
using KeplerEngine.App.ViewModels;

namespace KeplerEngine.App.Views.Panels;

public partial class TelemetryPanel : UserControl
{
    public TelemetryPanel()
    {
        InitializeComponent();
    }

    public void Update(MainViewModel vm)
    {
        this.FindControl<TextBlock>("TxtUT")!.Text     = vm.ClockText;
        this.FindControl<TextBlock>("TxtWarp")!.Text   = vm.WarpText;

        var body = vm.SelectedBody;
        if (body == null) return;

        this.FindControl<TextBlock>("TxtAlt")!.Text    = $"{body.AltitudeKm:F1} km";
        this.FindControl<TextBlock>("TxtSpeed")!.Text  = $"{body.SpeedMs:F0} m/s";
        this.FindControl<TextBlock>("TxtPeriod")!.Text = $"{body.PeriodMin:F1} min";
        this.FindControl<TextBlock>("TxtPe")!.Text     = $"{body.PeriapsisKm:F1} km";
        this.FindControl<TextBlock>("TxtAp")!.Text     = $"{body.ApoapsisKm:F1} km";
        this.FindControl<TextBlock>("TxtNu")!.Text     = $"{body.TrueAnomalyDeg:F2}°";
    }
}