using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.Linq;

namespace Avalonia3DCanvas.Demo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void GenerateCubeButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "sample_cube.3ds");
            Avalonia3DCanvas.Model3DSWriter.WriteCube(tempPath, 2.0f);
            Canvas3DControl.LoadModel(tempPath);
            Canvas3DControl.StartAnimation();
            StatusText.Text = $"Generated .3DS cube with 8 vertices, 12 faces: {tempPath}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error generating .3DS cube: {ex.Message}";
        }
    }

    private void GenerateFBXButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "sample_cube.fbx");
            Avalonia3DCanvas.ModelFBXWriter.WriteCube(tempPath, 2.0f);
            Canvas3DControl.LoadModel(tempPath);
            Canvas3DControl.StartAnimation();
            StatusText.Text = $"Generated .FBX cube with 8 vertices, 12 faces: {tempPath}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error generating .FBX cube: {ex.Message}";
        }
    }

    private async void LoadModelButton_Click(object? sender, RoutedEventArgs e)
    {
        var storageProvider = StorageProvider;

        var fileTypes = new FilePickerFileType[]
        {
            new("All 3D Models") { Patterns = new[] { "*.3ds", "*.obj", "*.stl", "*.ply", "*.fbx" } },
            new("3D Studio Files") { Patterns = new[] { "*.3ds" } },
            new("Wavefront OBJ Files") { Patterns = new[] { "*.obj" } },
            new("STL Files") { Patterns = new[] { "*.stl" } },
            new("PLY Files") { Patterns = new[] { "*.ply" } },
            new("FBX Files") { Patterns = new[] { "*.fbx" } },
            new("All Files") { Patterns = new[] { "*.*" } }
        };

        var options = new FilePickerOpenOptions
        {
            Title = "Open 3D Model File",
            AllowMultiple = false,
            FileTypeFilter = fileTypes
        };

        var result = await storageProvider.OpenFilePickerAsync(options);

        if (result.Count > 0)
        {
            var file = result[0];
            var path = file.Path.LocalPath;

            try
            {
                Canvas3DControl.LoadModel(path);
                StatusText.Text = $"Loaded: {file.Name}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error loading {file.Name}: {ex.Message}";
            }
        }
    }

    private void StartAnimationButton_Click(object? sender, RoutedEventArgs e)
    {
        Canvas3DControl.StartAnimation();
        StatusText.Text = "Animation started";
    }

    private void StopAnimationButton_Click(object? sender, RoutedEventArgs e)
    {
        Canvas3DControl.StopAnimation();
        StatusText.Text = "Animation stopped";
    }

    private void ResetRotationButton_Click(object? sender, RoutedEventArgs e)
    {
        Canvas3DControl.ResetRotation();
        StatusText.Text = "Rotation reset";
    }
}