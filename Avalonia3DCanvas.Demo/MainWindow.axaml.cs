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

    private void Generate3DTextButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            // Try the actual text generation
            var mesh = TextMeshGenerator.GenerateTextMesh("AOWP", "Arial", 100, 25);

            if (mesh.Vertices.Count == 0)
            {
                // Fallback to test mesh
                StatusText.Text = "Text mesh has no vertices! Using test mesh instead.";
                var testMesh = CreateTestMesh();
                Canvas3DControl.SetMesh(testMesh);
            }
            else
            {
                StatusText.Text = $"Generated text mesh: {mesh.Vertices.Count} vertices, {mesh.Faces.Count} faces";

                // Print bounds info
                mesh.GetBounds(out var min, out var max);
                System.Diagnostics.Debug.WriteLine($"Mesh bounds: min=({min.X},{min.Y},{min.Z}) max=({max.X},{max.Y},{max.Z})");
                StatusText.Text += $" | Bounds: ({min.X:F1},{min.Y:F1}) to ({max.X:F1},{max.Y:F1})";

                Canvas3DControl.SetMesh(mesh);
            }

            Canvas3DControl.StartAnimation();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Exception: {ex}");
        }
    }

    private Mesh3D CreateTestMesh()
    {
        var mesh = new Mesh3D();

        // Create a simple square extruded into 3D (like the letter "I")
        float width = 20;
        float height = 40;
        float depth = 10;

        // Front face (4 vertices)
        mesh.Vertices.Add(new Vector3D(0, 0, 0));
        mesh.Vertices.Add(new Vector3D(width, 0, 0));
        mesh.Vertices.Add(new Vector3D(width, height, 0));
        mesh.Vertices.Add(new Vector3D(0, height, 0));

        // Back face (4 vertices)
        mesh.Vertices.Add(new Vector3D(0, 0, depth));
        mesh.Vertices.Add(new Vector3D(width, 0, depth));
        mesh.Vertices.Add(new Vector3D(width, height, depth));
        mesh.Vertices.Add(new Vector3D(0, height, depth));

        // Front face triangles
        mesh.Faces.Add((0, 1, 2));
        mesh.Faces.Add((0, 2, 3));

        // Back face triangles
        mesh.Faces.Add((4, 6, 5));
        mesh.Faces.Add((4, 7, 6));

        // Side faces
        mesh.Faces.Add((0, 4, 1));
        mesh.Faces.Add((1, 4, 5));

        mesh.Faces.Add((1, 5, 2));
        mesh.Faces.Add((2, 5, 6));

        mesh.Faces.Add((2, 6, 3));
        mesh.Faces.Add((3, 6, 7));

        mesh.Faces.Add((3, 7, 0));
        mesh.Faces.Add((0, 7, 4));

        return mesh;
    }

    private async void ExportToOBJButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var currentMesh = Canvas3DControl.GetCurrentMesh();
            if (currentMesh == null || currentMesh.Vertices.Count == 0)
            {
                StatusText.Text = "No mesh to export. Load or generate a model first.";
                return;
            }

            var storageProvider = StorageProvider;

            var options = new FilePickerSaveOptions
            {
                Title = "Export to OBJ File",
                SuggestedFileName = "model.obj",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Wavefront OBJ Files") { Patterns = new[] { "*.obj" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
                }
            };

            var result = await storageProvider.SaveFilePickerAsync(options);

            if (result != null)
            {
                var path = result.Path.LocalPath;
                Canvas3DControl.ExportToOBJ(path, "ExportedModel");
                StatusText.Text = $"Exported mesh to: {result.Name} ({currentMesh.Vertices.Count} vertices, {currentMesh.Faces.Count} faces)";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error exporting mesh: {ex.Message}";
        }
    }
}