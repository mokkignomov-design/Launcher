using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace Runia.Ui.Pages.Controls;

public class SkinView : Control
{
    private readonly Viewport3D _viewport;
    private readonly Model3DGroup _modelGroup;
    private readonly QuaternionRotation3D _yRotation;
    private readonly QuaternionRotation3D _xRotation;

    private double _textureScale = 1.0;
    private BitmapSource? _workingSkin;

    private DateTime _lastMoveTime;
    private double _currentAngleX = 0;
    private double _currentAngleY = 0;
    private DateTime _lastRenderTime = DateTime.Now;
    private Point _lastMousePos;

    // Свойства зависимостей (Dependency Properties)
    public static readonly DependencyProperty SkinSourceProperty = DependencyProperty.Register(
        nameof(SkinSource), typeof(BitmapSource), typeof(SkinView),
        new PropertyMetadata(null, (d, e) => ((SkinView)d).UpdateModel()));

    public static readonly DependencyProperty SkinPathProperty = DependencyProperty.Register(
        nameof(SkinPath), typeof(string), typeof(SkinView),
        new PropertyMetadata(null, (d, e) => ((SkinView)d).LoadSkinFromPath((string)e.NewValue)));

    public BitmapSource SkinSource { get => (BitmapSource)GetValue(SkinSourceProperty); set => SetValue(SkinSourceProperty, value); }
    public string SkinPath { get => (string)GetValue(SkinPathProperty); set => SetValue(SkinPathProperty, value); }

    public SkinView()
    {
        _viewport = new Viewport3D();

        // Линейная фильтрация для заранее увеличенной текстуры убирает дрожание пикселей
        RenderOptions.SetBitmapScalingMode(_viewport, BitmapScalingMode.Linear);

        _modelGroup = new Model3DGroup();
        _viewport.Camera = new PerspectiveCamera(new Point3D(0, 0, 50), new Vector3D(0, 0, -1), new Vector3D(0, 1, 0), 45);

        var modelVisual = new ModelVisual3D { Content = _modelGroup };
        var transformGroup = new Transform3DGroup();

        _yRotation = new QuaternionRotation3D();
        _xRotation = new QuaternionRotation3D();
        transformGroup.Children.Add(new RotateTransform3D(_yRotation));
        transformGroup.Children.Add(new RotateTransform3D(_xRotation));
        modelVisual.Transform = transformGroup;

        _viewport.Children.Add(modelVisual);
        this.AddVisualChild(_viewport);

        SetValue(ClipToBoundsProperty, true);

        // Безопасная подписка на рендер кадра (защита от утечек памяти)
        Loaded += (s, e) => CompositionTarget.Rendering += OnRendering;
        Unloaded += (s, e) => CompositionTarget.Rendering -= OnRendering;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (IsMouseCaptured) return;

        var now = DateTime.Now;
        var dt = (now - _lastRenderTime).TotalSeconds;
        _lastRenderTime = now;

        // Автоматическое вращение, если мышь не трогали 4 секунды
        if ((now - _lastMoveTime).TotalSeconds > 4)
        {
            _currentAngleY += 60.0 * dt; // 60 градусов в секунду (плавно и без привязки к FPS)
            UpdateRotation();
        }
    }

    private void UpdateRotation()
    {
        _yRotation.Quaternion = new Quaternion(new Vector3D(0, 1, 0), _currentAngleY);
        _xRotation.Quaternion = new Quaternion(new Vector3D(1, 0, 0), _currentAngleX);
    }

    // --- Обработка мыши (переопределения вместо подписок для большей производительности) ---
    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            _lastMousePos = e.GetPosition(this);
            CaptureMouse();
            e.Handled = true;
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed)
        {
            var pos = e.GetPosition(this);
            _currentAngleY += pos.X - _lastMousePos.X;
            _currentAngleX = Math.Clamp(_currentAngleX + (pos.Y - _lastMousePos.Y), -70, 70);

            UpdateRotation();
            _lastMousePos = pos;
            _lastMoveTime = DateTime.Now;
            e.Handled = true;
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Released)
        {
            ReleaseMouseCapture();
            e.Handled = true;
        }
        base.OnMouseUp(e);
    }

    // --- Загрузка и рендер ---
    private void LoadSkinFromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path);
            bitmap.EndInit();
            bitmap.Freeze();
            SkinSource = bitmap;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SkinView] Failed to load skin from {path}: {ex}");
        }
    }

    private static BitmapSource UpscaleTexture(BitmapSource source, int scale)
    {
        if (scale <= 1) return source;

        var fcb = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        int origWidth = fcb.PixelWidth;
        int origHeight = fcb.PixelHeight;
        int origStride = origWidth * 4;

        byte[] origPixels = new byte[origStride * origHeight];
        fcb.CopyPixels(origPixels, origStride, 0);

        int newWidth = origWidth * scale;
        int newHeight = origHeight * scale;
        int newStride = newWidth * 4;
        byte[] newPixels = new byte[newStride * newHeight];

        // Ручной алгоритм Nearest Neighbor
        for (int y = 0; y < newHeight; y++)
        {
            int origY = y / scale;
            int origRowOffset = origY * origStride;
            int newRowOffset = y * newStride;

            for (int x = 0; x < newWidth; x++)
            {
                int origX = x / scale;
                int origPixelOffset = origRowOffset + (origX * 4);
                int newPixelOffset = newRowOffset + (x * 4);

                newPixels[newPixelOffset] = origPixels[origPixelOffset];
                newPixels[newPixelOffset + 1] = origPixels[origPixelOffset + 1];
                newPixels[newPixelOffset + 2] = origPixels[origPixelOffset + 2];
                newPixels[newPixelOffset + 3] = origPixels[origPixelOffset + 3];
            }
        }

        return BitmapSource.Create(newWidth, newHeight, 96, 96, PixelFormats.Bgra32, null, newPixels, newStride);
    }

    private void UpdateModel()
    {
        if (SkinSource == null) return;
        _modelGroup.Children.Clear();
        _modelGroup.Children.Add(new AmbientLight(Colors.White));

        // Апскейл для защиты от субпиксельного дрожания
        var upscaled = UpscaleTexture(SkinSource, 16);
        _workingSkin = upscaled;

        var img = new Image { Source = _workingSkin };
        RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.Linear);
        var brush = new VisualBrush(img) { ViewportUnits = BrushMappingMode.Absolute };
        var material = new DiffuseMaterial(brush);

        var baseMesh = new MeshGeometry3D();
        var overlayMesh = new MeshGeometry3D();

        bool isSlim = false;

        // Поддержка HD скейлинга
        _textureScale = _workingSkin.PixelWidth / 64.0;
        if (_workingSkin.PixelWidth >= 64)
        {
            try
            {
                var cb = new CroppedBitmap(_workingSkin, new Int32Rect((int)(42 * _textureScale), (int)(48 * _textureScale), 1, 1));
                byte[] pixel = new byte[4];
                cb.CopyPixels(pixel, 4, 0);
                if (pixel[3] < 128) isSlim = true;
            }
            catch { }
        }

        void AddPart(double x, double y, double z, double w, double h, double d, int u, int v, double inf, bool isOverlay, bool isArm = false, bool isLeft = false)
        {
            double actualW = (isArm && isSlim) ? 3 : w;
            double renderX = x + (isArm && isSlim ? (isLeft ? 0.5 : -0.5) : 0);
            AddBoxToMesh(isOverlay ? overlayMesh : baseMesh, renderX, y, z, actualW, h, d, u * _textureScale, v * _textureScale, inf, isOverlay);
        }

        // Голова
        AddPart(0, 10, 0, 8, 8, 8, 8, 8, 1.0, false);
        AddPart(0, 10, 0, 8, 8, 8, 40, 8, 1.12, true);

        // Тело
        AddPart(0, 0, 0, 8, 12, 4, 20, 20, 1.0, false);
        AddPart(0, 0, 0, 8, 12, 4, 20, 36, 1.06, true);

        // Современный скин (высота 64) с отдельными слоями для ног и рук
        if (SkinSource.PixelHeight >= 64)
        {
            // Руки
            AddPart(-6, 0, 0, 4, 12, 4, 44, 20, 1.0, false, true, true);
            AddPart(-6, 0, 0, 4, 12, 4, 44, 36, 1.08, true, true, true);
            AddPart(6, 0, 0, 4, 12, 4, 36, 52, 1.0, false, true, false);
            AddPart(6, 0, 0, 4, 12, 4, 52, 52, 1.08, true, true, false);
            // Ноги
            AddPart(-2, -12, 0, 4, 12, 4, 4, 20, 1.0, false);
            AddPart(-2, -12, 0, 4, 12, 4, 4, 36, 1.08, true);
            AddPart(2, -12, 0, 4, 12, 4, 20, 52, 1.0, false);
            AddPart(2, -12, 0, 4, 12, 4, 4, 52, 1.08, true);
        }

        // Рендерим внутренние полигоны (BackMaterial), чтобы не просвечивали пустые зоны при прозрачности
        var baseModel = new GeometryModel3D(baseMesh, material) { BackMaterial = material };
        var overlayMat = new DiffuseMaterial(brush);
        var overlayModel = new GeometryModel3D(overlayMesh, overlayMat) { BackMaterial = overlayMat };

        _modelGroup.Children.Add(baseModel);
        _modelGroup.Children.Add(overlayModel);
    }

    private void AddBoxToMesh(MeshGeometry3D mesh, double x, double y, double z, double w, double h, double d, double u, double v, double inf, bool isOverlay)
    {
        double hw = w / 2 * inf, hh = h / 2 * inf, hd = d / 2 * inf;
        Point3D[] p = [
            new (x-hw, y+hh, z+hd), new (x+hw, y+hh, z+hd), new (x+hw, y-hh, z+hd), new (x-hw, y-hh, z+hd),
            new (x+hw, y+hh, z-hd), new (x-hw, y+hh, z-hd), new (x-hw, y-hh, z-hd), new (x+hw, y-hh, z-hd)
        ];

        double tw = _workingSkin?.PixelWidth ?? SkinSource.PixelWidth;
        double th = _workingSkin?.PixelHeight ?? SkinSource.PixelHeight;

        // Отступаем внутрь на 1.5 пикселя для предотвращения кровотечения текстур (Texture Bleeding)
        double marginU = 1.5 / tw;
        double marginV = 1.5 / th;

        void AddFace(Point3D p1, Point3D p2, Point3D p3, Point3D p4, double uS, double vS, double uW, double vH, bool flipUV = false)
        {
            Vector3D normal = Vector3D.CrossProduct(p2 - p1, p3 - p1);
            if (normal.LengthSquared > 0.0000001) normal.Normalize();

            double offset = isOverlay ? 0.01 : 0.0;

            var op1 = p1 + normal * offset; var op2 = p2 + normal * offset;
            var op3 = p3 + normal * offset; var op4 = p4 + normal * offset;

            int b = mesh.Positions.Count;
            mesh.Positions.Add(op1); mesh.Positions.Add(op2); mesh.Positions.Add(op3); mesh.Positions.Add(op4);

            double u1 = uS / tw + marginU, v1 = vS / th + marginV;
            double u2 = (uS + uW) / tw - marginU, v2 = (vS + vH) / th - marginV;

            if (flipUV)
            {
                mesh.TextureCoordinates.Add(new Point(u1, v2)); mesh.TextureCoordinates.Add(new Point(u1, v1));
                mesh.TextureCoordinates.Add(new Point(u2, v1)); mesh.TextureCoordinates.Add(new Point(u2, v2));
            }
            else
            {
                mesh.TextureCoordinates.Add(new Point(u1, v1)); mesh.TextureCoordinates.Add(new Point(u2, v1));
                mesh.TextureCoordinates.Add(new Point(u2, v2)); mesh.TextureCoordinates.Add(new Point(u1, v2));
            }

            mesh.TriangleIndices.Add(b); mesh.TriangleIndices.Add(b + 2); mesh.TriangleIndices.Add(b + 1);
            mesh.TriangleIndices.Add(b); mesh.TriangleIndices.Add(b + 3); mesh.TriangleIndices.Add(b + 2);
        }

        double wPix = w * _textureScale; double hPix = h * _textureScale; double dPix = d * _textureScale;

        AddFace(p[0], p[1], p[2], p[3], u, v, wPix, hPix);             // Front
        AddFace(p[1], p[4], p[7], p[2], u + wPix, v, dPix, hPix);         // Right
        AddFace(p[4], p[5], p[6], p[7], u + wPix + dPix, v, wPix, hPix);     // Back
        AddFace(p[5], p[0], p[3], p[6], u - dPix, v, dPix, hPix);         // Left
        AddFace(p[5], p[4], p[1], p[0], u, v - dPix, wPix, dPix);         // Top
        AddFace(p[2], p[7], p[6], p[3], u + wPix, v - dPix, wPix, dPix, true); // Bottom
    }

    protected override Visual GetVisualChild(int index) => _viewport;
    protected override int VisualChildrenCount => 1;
    protected override Size ArrangeOverride(Size sz) { _viewport.Arrange(new Rect(sz)); return sz; }
}