# AGENTS.md

Notes for anyone, human or AI agent, changing this code. `README.md` covers the features, the control's properties
and methods and the file formats; this file covers how the code is put together and the rules that keep it working.

## What this is

`Canvas3D`, an Avalonia 11 (11.3.9) control in C# on .NET 9 that draws a 3D model as a spinning wireframe. It loads
.3ds, .obj, .stl, .ply, .fbx (ASCII) and .lwo files, can build an extruded 3D mesh from text, and can write meshes
back out. A demo app exercises it. No GPU or 3D library is used: the maths and the loaders are all in this repo.

## Commands

```bash
dotnet build Avalonia3DCanvas.sln                      # needs the .NET 9 SDK or later
dotnet run --project Avalonia3DCanvas.Demo             # the demo window
```

There are no tests yet.

## Layout

- `Avalonia3DCanvas/`: the control library (namespace `Avalonia3DCanvas`).
  - `Canvas3D.cs`: the control. Styled properties `ModelPath`, `RotationSpeed`, `LineColor` and `LineThickness`.
    - `LoadModel` picks a loader by file extension; `SetMesh`, `CreateTextMesh`, `ExportToOBJ` and `ExportToLWO`
      are the rest of the mesh API.
    - `StartAnimation` runs a 16 ms `DispatcherTimer` that adds `RotationSpeed` to all three angles.
    - `Render` centres and scales the mesh to 80% of the smaller side, rotates it, projects it orthographically
      (no perspective) and draws every triangle edge into one `StreamGeometry`, clipped by `LineClipper`.
  - `Mesh3D.cs`: vertices plus triangle faces as `(int, int, int)` index triples, with bounds, centre and size.
  - `Vector3D.cs` and `Matrix4x4.cs`: the repo's own small vector and matrix types (not `System.Numerics`).
  - `LineClipper.cs`: Cohen-Sutherland clipping of a line to the control's bounds.
  - `Model*Loader.cs`: one static `Load(path)` per format, each returning a `Mesh3D`.
  - `Model*Writer.cs`: writers for OBJ, LWO, 3DS and FBX. The 3DS and FBX writers also have `WriteCube`, which the
    demo uses to make sample files.
  - `TextMeshGenerator.cs`: gets the text's outline from SkiaSharp (`SKPaint.GetTextPath`, the 2.88 API), then
    triangulates the faces and extrudes them. That
    text API is marked obsolete, so check it when upgrading SkiaSharp.
- `Avalonia3DCanvas.Demo/`: the demo window, with buttons to make sample cubes, load a file, start and stop the
  rotation, make text meshes and export to OBJ or LWO.
- `Objects/`: sample models for trying the loaders (.stl, .fbx, .obj, .lwo, plus a 3ds Max `.max` file, which no
  loader reads).
- `Documents/Images/`: the README's screenshot.

## Rules that keep it working

- **Everything is triangles.** `Mesh3D.Faces` holds triangles only, and `Render` draws three edges per face. A
  loader for a format with quads or polygons must split them into triangle fans, as `ModelOBJLoader` does.
- **Loaders return a mesh, the control does the rest.** Centring and scaling happen in `Render`, so a loader should
  keep the file's own coordinates.
- **A new format needs three touches:** a `Model<Format>Loader` class, a case in the `LoadModel` switch (and the
  `NotSupportedException` message), and the README's format list.
- **Screen Y points down.** `Render` scales Y by a negative factor so models appear upright; keep that if you change
  the projection.
- **Use the repo's `Matrix4x4`.** It shares its name with `System.Numerics.Matrix4x4`, so don't add a
  `using System.Numerics` to files that use it.

## Making a change

- Check changes by running the demo and loading the files in `Objects/`; there are no automated tests.
- Keep `README.md` (formats, properties, methods) in step with public changes.
- `.claude/settings.local.json` and parts of `.idea/` are committed. They are editor settings, not part of the build.

## CI and releases

There is no CI workflow and there are no releases.
