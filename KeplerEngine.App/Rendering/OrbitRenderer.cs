using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;
using KeplerEngine.Core;
using KeplerEngine.Physics;
using KeplerEngine.Simulation;
using KeplerEngine.App.ViewModels;

namespace KeplerEngine.App.Rendering;

public class OrbitRenderer : Control
{
    // -- Simulation ----------
    private OrbitSimulation? _sim;
    private MainViewModel?   _vm;
    private Action<double>?  _onTick;

    // -- Camera ----------
    private double  _metersPerPixel = 15_000;
    private SKPoint _cameraCenter   = SKPoint.Empty;

    // -- Drag ----------
    private bool    _isDragging;
    private Point   _dragStart;
    private SKPoint _cameraCenterAtDragStart;

    // -- Timer ----------
    private readonly System.Timers.Timer _timer;
    private DateTime _lastTick = DateTime.UtcNow;

    // -- Colors ----------
    private static readonly SKColor ColBackground = SKColor.Parse("#0d0d1a");
    private static readonly SKColor ColGrid       = SKColor.Parse("#1a1a2e");
    private static readonly SKColor ColPlanet     = SKColor.Parse("#4a90d9");
    private static readonly SKColor ColOrbit      = SKColor.Parse("#2ecc71");
    private static readonly SKColor ColSatellite  = SKColor.Parse("#e74c3c");
    private static readonly SKColor ColText       = SKColor.Parse("#ecf0f1");
    private static readonly SKColor ColPeriapsis  = SKColor.Parse("#f39c12");

    public OrbitRenderer()
    {
        ClipToBounds = true;

        PointerWheelChanged += OnWheel;
        PointerPressed      += OnPointerPressed;
        PointerMoved        += OnPointerMoved;
        PointerReleased     += OnPointerReleased;

        _timer = new System.Timers.Timer(16);
        _timer.Elapsed += (_, _) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(InvalidateVisual);
        };
    }

    // -- Public API ----------

    public void SetSimulation(MainViewModel vm, Action<double> onTick)
    {
        _vm     = vm;
        _onTick = onTick;
        _sim    = vm.Simulation;

        if (_sim.Celestials.Count > 0)
        {
            var pos = _sim.Celestials[0].Position;
            _cameraCenter = new SKPoint((float)pos.X, (float)pos.Y);
        }

        _timer.Start();
    }

    public void StopSimulation() => _timer.Stop();

    // -- Render ----------

    public override void Render(DrawingContext context)
    {
        var now = DateTime.UtcNow;
        double dt = (now - _lastTick).TotalSeconds;
        _lastTick = now;

        // Тикаем симуляцию через коллбэк
        _onTick?.Invoke(dt);

        var bounds = new Rect(Bounds.Size);
        context.Custom(new SkiaDrawOperation(bounds, DrawScene));
    }

    private void DrawScene(SKCanvas canvas, SKRect bounds)
    {
        canvas.Clear(ColBackground);
        if (_sim == null) return;

        float cx = bounds.MidX;
        float cy = bounds.MidY;

        DrawGrid(canvas, bounds, cx, cy);

        foreach (var body in _sim.Celestials)
            DrawCelestialBody(canvas, body, cx, cy);

        foreach (var body in _sim.Orbitals)
        {
            DrawOrbitPath(canvas, body, cx, cy);
            DrawOrbitalBody(canvas, body, cx, cy);
            DrawPeriapsisMarker(canvas, body, cx, cy);
        }
    }

    // -- Drawing ----------

    private void DrawGrid(SKCanvas canvas, SKRect bounds, float cx, float cy)
    {
        using var paint = new SKPaint
        {
            Color       = ColGrid,
            StrokeWidth = 1,
            IsAntialias = false
        };

        double gridStepM  = NiceGridStep();
        float  gridStepPx = MetersToPixels(gridStepM);

        float startX = cx - (int)(cx / gridStepPx) * gridStepPx - gridStepPx;
        for (float x = startX; x < bounds.Right; x += gridStepPx)
            canvas.DrawLine(x, bounds.Top, x, bounds.Bottom, paint);

        float startY = cy - (int)(cy / gridStepPx) * gridStepPx - gridStepPx;
        for (float y = startY; y < bounds.Bottom; y += gridStepPx)
            canvas.DrawLine(bounds.Left, y, bounds.Right, y, paint);
    }

    private void DrawCelestialBody(SKCanvas canvas, CelestialBody body, float cx, float cy)
    {
        var (sx, sy) = WorldToScreen(body.Position.X, body.Position.Y, cx, cy);
        float r = Math.Max(4f, MetersToPixels(body.Radius));

        using var glowPaint = new SKPaint
        {
            Color      = ColPlanet.WithAlpha(40),
            IsAntialias = true,
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r * 0.6f)
        };
        canvas.DrawCircle(sx, sy, r * 1.8f, glowPaint);

        using var paint = new SKPaint { Color = ColPlanet, IsAntialias = true };
        canvas.DrawCircle(sx, sy, r, paint);

        using var textPaint = new SKPaint
        {
            Color       = ColText,
            TextSize    = 13,
            IsAntialias = true
        };
        canvas.DrawText(body.Name, sx + r + 4, sy - r - 4, textPaint);
    }

    private void DrawOrbitPath(SKCanvas canvas, KeplerEngine.Physics.OrbitalBody body, float cx, float cy)
    {
        var points = body.GetOrbitPoints(256);

        using var paint = new SKPaint
        {
            Color       = ColOrbit.WithAlpha(180),
            StrokeWidth = 1.5f,
            IsAntialias = true,
            IsStroke    = true
        };

        using var path = new SKPath();
        bool first = true;
        foreach (var p in points)
        {
            var (sx, sy) = WorldToScreen(p.X, p.Y, cx, cy);
            if (first) { path.MoveTo(sx, sy); first = false; }
            else          path.LineTo(sx, sy);
        }
        path.Close();
        canvas.DrawPath(path, paint);
    }

    private void DrawOrbitalBody(SKCanvas canvas, KeplerEngine.Physics.OrbitalBody body, float cx, float cy)
    {
        var pos = body.Position;
        var (sx, sy) = WorldToScreen(pos.X, pos.Y, cx, cy);

        using var paint = new SKPaint { Color = ColSatellite, IsAntialias = true };
        canvas.DrawCircle(sx, sy, 5, paint);

        var sv     = body.CurrentState;
        double vScale = sv.Velocity.Magnitude > 0 ? 30.0 / sv.Velocity.Magnitude : 0;
        float  vx  = sx + (float)(sv.Velocity.X * vScale);
        float  vy  = sy - (float)(sv.Velocity.Y * vScale);

        using var velPaint = new SKPaint
        {
            Color       = SKColors.Yellow.WithAlpha(200),
            StrokeWidth = 1.5f,
            IsAntialias = true
        };
        canvas.DrawLine(sx, sy, vx, vy, velPaint);

        using var textPaint = new SKPaint
        {
            Color       = ColText,
            TextSize    = 12,
            IsAntialias = true
        };
        string label = $"{body.Name}  {body.Speed:F0} m/s  {body.Altitude / 1000:F0} km";
        canvas.DrawText(label, sx + 8, sy - 8, textPaint);
    }

    private void DrawPeriapsisMarker(SKCanvas canvas, KeplerEngine.Physics.OrbitalBody body, float cx, float cy)
    {
        var snap = body.Elements.Clone();
        snap.TrueAnomaly = 0;
        var sv = KeplerEngine.Orbital.StateVector.FromKeplerian(snap, body.Primary.Mu);
        var (px, py) = WorldToScreen(
            sv.Position.X + body.Primary.Position.X,
            sv.Position.Y + body.Primary.Position.Y,
            cx, cy);

        using var paint = new SKPaint { Color = ColPeriapsis, IsAntialias = true };
        canvas.DrawCircle(px, py, 3, paint);
    }

    // -- Coordinate transforms ----------

    private (float x, float y) WorldToScreen(double wx, double wy, float cx, float cy)
    {
        float sx = cx + (float)((wx - _cameraCenter.X) / _metersPerPixel);
        float sy = cy - (float)((wy - _cameraCenter.Y) / _metersPerPixel);
        return (sx, sy);
    }

    private float MetersToPixels(double meters) => (float)(meters / _metersPerPixel);

    // -- Input ----------

    private void OnWheel(object? sender, PointerWheelEventArgs e)
    {
        double factor = e.Delta.Y > 0 ? 0.85 : 1.15;
        _metersPerPixel *= factor;
        _metersPerPixel  = Math.Clamp(_metersPerPixel, 100, 1e9);
        InvalidateVisual();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _isDragging              = true;
        _dragStart               = e.GetPosition(this);
        _cameraCenterAtDragStart = _cameraCenter;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging) return;
        var pos = e.GetPosition(this);
        double dx = (pos.X - _dragStart.X) * _metersPerPixel;
        double dy = (pos.Y - _dragStart.Y) * _metersPerPixel;
        _cameraCenter = new SKPoint(
            _cameraCenterAtDragStart.X - (float)dx,
            _cameraCenterAtDragStart.Y + (float)dy);
        InvalidateVisual();
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e) =>
        _isDragging = false;

    // -- Utilities ----------

    private double NiceGridStep()
    {
        double targetPx = 80.0;
        double raw  = targetPx * _metersPerPixel;
        double mag  = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        double norm = raw / mag;
        double nice = norm < 2 ? 1 : norm < 5 ? 2 : 5;
        return nice * mag;
    }
}

// -- SkiaSharp draw operation ----------

internal class SkiaDrawOperation : ICustomDrawOperation
{
    private readonly Action<SKCanvas, SKRect> _draw;
    public Rect Bounds { get; }

    public SkiaDrawOperation(Rect bounds, Action<SKCanvas, SKRect> draw)
    {
        Bounds = bounds;
        _draw  = draw;
    }

    public void Render(ImmediateDrawingContext context)
    {
        var skia = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
        if (skia == null) return;
        using var lease  = skia.Lease();
        var canvas = lease.SkCanvas;
        var bounds = new SKRect(0, 0, (float)Bounds.Width, (float)Bounds.Height);
        _draw(canvas, bounds);
    }

    public bool Equals(ICustomDrawOperation? other) => false;
    public bool HitTest(Point p) => Bounds.Contains(p);
    public void Dispose() { }
}