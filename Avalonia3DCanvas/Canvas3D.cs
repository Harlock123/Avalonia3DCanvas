using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;

namespace Avalonia3DCanvas;

public class Canvas3D : Control
{
    private Mesh3D? _mesh;
    private float _rotationX = 0;
    private float _rotationY = 0;
    private float _rotationZ = 0;
    private DispatcherTimer? _animationTimer;
    private bool _isAnimating = false;

    public static readonly StyledProperty<string?> ModelPathProperty =
        AvaloniaProperty.Register<Canvas3D, string?>(nameof(ModelPath));

    public static readonly StyledProperty<double> RotationSpeedProperty =
        AvaloniaProperty.Register<Canvas3D, double>(nameof(RotationSpeed), 0.02);

    public static readonly StyledProperty<IBrush?> LineColorProperty =
        AvaloniaProperty.Register<Canvas3D, IBrush?>(nameof(LineColor), Brushes.White);

    public static readonly StyledProperty<double> LineThicknessProperty =
        AvaloniaProperty.Register<Canvas3D, double>(nameof(LineThickness), 1.0);

    public string? ModelPath
    {
        get => GetValue(ModelPathProperty);
        set => SetValue(ModelPathProperty, value);
    }

    public double RotationSpeed
    {
        get => GetValue(RotationSpeedProperty);
        set => SetValue(RotationSpeedProperty, value);
    }

    public IBrush? LineColor
    {
        get => GetValue(LineColorProperty);
        set => SetValue(LineColorProperty, value);
    }

    public double LineThickness
    {
        get => GetValue(LineThicknessProperty);
        set => SetValue(LineThicknessProperty, value);
    }

    public Canvas3D()
    {
        ClipToBounds = true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ModelPathProperty)
        {
            LoadModel(ModelPath);
        }
        else if (change.Property == BoundsProperty)
        {
            InvalidateVisual();
        }
    }

    public void LoadModel(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
        {
            _mesh = null;
            InvalidateVisual();
            return;
        }

        var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();

        _mesh = extension switch
        {
            ".3ds" => Model3DSLoader.Load(filePath),
            ".obj" => ModelOBJLoader.Load(filePath),
            ".stl" => ModelSTLLoader.Load(filePath),
            ".ply" => ModelPLYLoader.Load(filePath),
            ".fbx" => ModelFBXLoader.Load(filePath),
            _ => throw new NotSupportedException($"File format '{extension}' is not supported. Supported formats: .3ds, .obj, .stl, .ply, .fbx")
        };

        InvalidateVisual();
    }

    public void StartAnimation()
    {
        if (_isAnimating) return;

        _isAnimating = true;
        _animationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _animationTimer.Tick += OnAnimationTick;
        _animationTimer.Start();
    }

    public void StopAnimation()
    {
        if (!_isAnimating) return;

        _isAnimating = false;
        _animationTimer?.Stop();
        _animationTimer = null;
    }

    public void ResetRotation()
    {
        _rotationX = 0;
        _rotationY = 0;
        _rotationZ = 0;
        InvalidateVisual();
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        _rotationX += (float)RotationSpeed;
        _rotationY += (float)RotationSpeed;
        _rotationZ += (float)RotationSpeed;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (_mesh == null || _mesh.Vertices.Count == 0)
            return;

        var bounds = Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        var centerX = bounds.Width / 2;
        var centerY = bounds.Height / 2;

        var center = _mesh.GetCenter();
        var maxDim = _mesh.GetMaxDimension();
        if (maxDim == 0) maxDim = 1;

        var scale = Math.Min(bounds.Width, bounds.Height) * 0.8f / maxDim;

        var rotationMatrix = Matrix4x4.CreateRotationX(_rotationX)
            * Matrix4x4.CreateRotationY(_rotationY)
            * Matrix4x4.CreateRotationZ(_rotationZ);

        var scaleMatrix = Matrix4x4.CreateScale((float)scale, -(float)scale, (float)scale);
        var translationMatrix = Matrix4x4.CreateTranslation(
            -(float)center.X,
            -(float)center.Y,
            -(float)center.Z
        );

        var transformedVertices = new List<Point>();
        foreach (var vertex in _mesh.Vertices)
        {
            var v = translationMatrix.Transform(vertex);
            v = rotationMatrix.Transform(v);
            v = scaleMatrix.Transform(v);

            var screenX = centerX + v.X;
            var screenY = centerY + v.Y;
            transformedVertices.Add(new Point(screenX, screenY));
        }

        var pen = new Pen(LineColor, LineThickness);

        foreach (var face in _mesh.Faces)
        {
            if (face.Item1 < transformedVertices.Count &&
                face.Item2 < transformedVertices.Count &&
                face.Item3 < transformedVertices.Count)
            {
                context.DrawLine(pen, transformedVertices[face.Item1], transformedVertices[face.Item2]);
                context.DrawLine(pen, transformedVertices[face.Item2], transformedVertices[face.Item3]);
                context.DrawLine(pen, transformedVertices[face.Item3], transformedVertices[face.Item1]);
            }
        }
    }
}
