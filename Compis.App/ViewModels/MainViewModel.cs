
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Compis.App.Rendering;
using Compis.Core.Objects;
using Compis.Core.Parameters;
using Compis.Core.Scene;

namespace Compis.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public BuildingScene Scene { get; } = new();

    public ObservableCollection<BuildingVolume> Volumes { get; } = [];

    private TransformMode _currentTransformMode = TransformMode.Translate;

    public TransformMode CurrentTransformMode
    {
        get => _currentTransformMode;
        set
        {
            if (SetProperty(ref _currentTransformMode, value))
            {
                OnPropertyChanged(nameof(IsTranslateMode));
                OnPropertyChanged(nameof(IsRotateMode));
                OnPropertyChanged(nameof(IsScaleMode));
            }
        }
    }

    public bool IsTranslateMode
    {
        get => CurrentTransformMode == TransformMode.Translate;
        set
        {
            if (value)
                CurrentTransformMode = TransformMode.Translate;
            else if (IsTranslateMode)
                OnPropertyChanged(nameof(IsTranslateMode));
        }
    }

    public bool IsRotateMode
    {
        get => CurrentTransformMode == TransformMode.Rotate;
        set
        {
            if (value)
                CurrentTransformMode = TransformMode.Rotate;
            else if (IsRotateMode)
                OnPropertyChanged(nameof(IsRotateMode));
        }
    }

    public bool IsScaleMode
    {
        get => CurrentTransformMode == TransformMode.Scale;
        set
        {
            if (value)
                CurrentTransformMode = TransformMode.Scale;
            else if (IsScaleMode)
                OnPropertyChanged(nameof(IsScaleMode));
        }
    }

    private MeshElementMode _currentMeshElementMode = MeshElementMode.Object;

    public MeshElementMode CurrentMeshElementMode
    {
        get => _currentMeshElementMode;
        set
        {
            if (SetProperty(ref _currentMeshElementMode, value))
            {
                OnPropertyChanged(nameof(IsObjectElementMode));
                OnPropertyChanged(nameof(IsVertexElementMode));
                OnPropertyChanged(nameof(IsEdgeElementMode));
                OnPropertyChanged(nameof(IsFaceElementMode));
            }
        }
    }

    public bool IsObjectElementMode
    {
        get => CurrentMeshElementMode == MeshElementMode.Object;
        set { if (value) CurrentMeshElementMode = MeshElementMode.Object; }
    }

    public bool IsVertexElementMode
    {
        get => CurrentMeshElementMode == MeshElementMode.Vertex;
        set { if (value) CurrentMeshElementMode = MeshElementMode.Vertex; }
    }

    public bool IsEdgeElementMode
    {
        get => CurrentMeshElementMode == MeshElementMode.Edge;
        set { if (value) CurrentMeshElementMode = MeshElementMode.Edge; }
    }

    public bool IsFaceElementMode
    {
        get => CurrentMeshElementMode == MeshElementMode.Face;
        set { if (value) CurrentMeshElementMode = MeshElementMode.Face; }
    }

    private TransformSpace _currentTransformSpace = TransformSpace.Global;

    public TransformSpace CurrentTransformSpace
    {
        get => _currentTransformSpace;
        set
        {
            if (SetProperty(ref _currentTransformSpace, value))
            {
                OnPropertyChanged(nameof(IsGlobalSpace));
                OnPropertyChanged(nameof(IsLocalSpace));
            }
        }
    }

    public bool IsGlobalSpace
    {
        get => CurrentTransformSpace == TransformSpace.Global;
        set
        {
            if (value)
                CurrentTransformSpace = TransformSpace.Global;
            else if (IsGlobalSpace)
                OnPropertyChanged(nameof(IsGlobalSpace));
        }
    }

    public bool IsLocalSpace
    {
        get => CurrentTransformSpace == TransformSpace.Local;
        set
        {
            if (value)
                CurrentTransformSpace = TransformSpace.Local;
            else if (IsLocalSpace)
                OnPropertyChanged(nameof(IsLocalSpace));
        }
    }

    private bool _showVertices = true;

    public bool ShowVertices
    {
        get => _showVertices;
        set => SetProperty(ref _showVertices, value);
    }

    private bool _showEdges = true;

    public bool ShowEdges
    {
        get => _showEdges;
        set => SetProperty(ref _showEdges, value);
    }

    private bool _showFaces = true;

    public bool ShowFaces
    {
        get => _showFaces;
        set => SetProperty(ref _showFaces, value);
    }

    [ObservableProperty]
    public partial string Greeting { get; set; } = "Welcome to Compis!";

    [ObservableProperty]
    public partial int ObjectCount { get; set; }

    private BuildingVolume? _selectedVolume;

    public BuildingVolume? SelectedVolume
    {
        get => _selectedVolume;
        set
        {
            BuildingVolume? previousVolume = _selectedVolume;

            if (SetProperty(ref _selectedVolume, value))
            {
                if (previousVolume is not null)
                    previousVolume.PropertyChanged -= OnSelectedVolumePropertyChanged;

                if (value is not null)
                {
                    value.PropertyChanged += OnSelectedVolumePropertyChanged;
                    _widthInput = value.Dimensions.Width.ToString(CultureInfo.CurrentCulture);
                    _depthInput = value.Dimensions.Depth.ToString(CultureInfo.CurrentCulture);
                    _heightInput = value.Dimensions.Height.ToString(CultureInfo.CurrentCulture);
                    _xInput = value.Position.X.ToString(CultureInfo.CurrentCulture);
                    _yInput = value.Position.Y.ToString(CultureInfo.CurrentCulture);
                    _zInput = value.Position.Z.ToString(CultureInfo.CurrentCulture);
                    _rotationXInput = value.Rotation.X.ToString(CultureInfo.CurrentCulture);
                    _rotationYInput = value.Rotation.Y.ToString(CultureInfo.CurrentCulture);
                    _rotationZInput = value.Rotation.Z.ToString(CultureInfo.CurrentCulture);
                    _scaleXInput = value.Scale.X.ToString(CultureInfo.CurrentCulture);
                    _scaleYInput = value.Scale.Y.ToString(CultureInfo.CurrentCulture);
                    _scaleZInput = value.Scale.Z.ToString(CultureInfo.CurrentCulture);
                    _colorHex = ToHex(value.Color);
                    _colorHex = ToHex(value.Color);
                }
                else
                {
                    _widthInput = "";
                    _depthInput = "";
                    _heightInput = "";
                    _xInput = "";
                    _yInput = "";
                    _zInput = "";
                    _rotationXInput = "";
                    _rotationYInput = "";
                    _rotationZInput = "";
                    _scaleXInput = "";
                    _scaleYInput = "";
                    _scaleZInput = "";
                    _colorHex = "";
                    _colorHex = "";
                }

                OnPropertyChanged(nameof(WidthInput));
                OnPropertyChanged(nameof(DepthInput));
                OnPropertyChanged(nameof(HeightInput));
                OnPropertyChanged(nameof(XInput));
                OnPropertyChanged(nameof(YInput));
                OnPropertyChanged(nameof(ZInput));
                OnPropertyChanged(nameof(RotationXInput));
                OnPropertyChanged(nameof(RotationYInput));
                OnPropertyChanged(nameof(RotationZInput));
                OnPropertyChanged(nameof(ScaleXInput));
                OnPropertyChanged(nameof(ScaleYInput));
                OnPropertyChanged(nameof(ScaleZInput));
                OnPropertyChanged(nameof(ColorHex));
                OnPropertyChanged(nameof(ColorHex));
            }
        }
    }

    private void OnSelectedVolumePropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (sender is not BuildingVolume volume ||
            !ReferenceEquals(volume, SelectedVolume))
            return;

        if (e.PropertyName == nameof(BuildingVolume.Position))
        {
            _xInput = volume.Position.X.ToString(CultureInfo.CurrentCulture);
            _yInput = volume.Position.Y.ToString(CultureInfo.CurrentCulture);
            _zInput = volume.Position.Z.ToString(CultureInfo.CurrentCulture);

            OnPropertyChanged(nameof(XInput));
            OnPropertyChanged(nameof(YInput));
            OnPropertyChanged(nameof(ZInput));
        }
        else if (e.PropertyName == nameof(BuildingVolume.Rotation))
        {
            _rotationXInput = volume.Rotation.X.ToString(CultureInfo.CurrentCulture);
            _rotationYInput = volume.Rotation.Y.ToString(CultureInfo.CurrentCulture);
            _rotationZInput = volume.Rotation.Z.ToString(CultureInfo.CurrentCulture);

            OnPropertyChanged(nameof(RotationXInput));
            OnPropertyChanged(nameof(RotationYInput));
            OnPropertyChanged(nameof(RotationZInput));
        }
        else if (e.PropertyName == nameof(BuildingVolume.Scale))
        {
            _scaleXInput = volume.Scale.X.ToString(CultureInfo.CurrentCulture);
            _scaleYInput = volume.Scale.Y.ToString(CultureInfo.CurrentCulture);
            _scaleZInput = volume.Scale.Z.ToString(CultureInfo.CurrentCulture);

            OnPropertyChanged(nameof(ScaleXInput));
            OnPropertyChanged(nameof(ScaleYInput));
            OnPropertyChanged(nameof(ScaleZInput));
        }
        else if (e.PropertyName == nameof(BuildingVolume.Color))
        {
            _colorHex = ToHex(volume.Color);
            OnPropertyChanged(nameof(ColorHex));
        }
        else if (e.PropertyName == nameof(BuildingVolume.Color))
        {
            _colorHex = ToHex(volume.Color);
            OnPropertyChanged(nameof(ColorHex));
        }
    }

    private string _widthInput = "10";

    public string WidthInput
    {
        get => _widthInput;
        set
        {
            if (SetProperty(ref _widthInput, value))
                UpdateSelectedDimensions();
        }
    }

    private string _depthInput = "8";

    public string DepthInput
    {
        get => _depthInput;
        set
        {
            if (SetProperty(ref _depthInput, value))
                UpdateSelectedDimensions();
        }
    }

    private string _heightInput = "3";

    public string HeightInput
    {
        get => _heightInput;
        set
        {
            if (SetProperty(ref _heightInput, value))
                UpdateSelectedDimensions();
        }
    }

    [RelayCommand]
    private void AddBuildingVolume()
    {
        var volume = new BuildingVolume(
            $"Building Volume {ObjectCount + 1}",
            new Dimensions3D(10, 8, 3),
            new Position3D(0, 0, 0)
        );

        Scene.AddVolume(volume);
        Volumes.Add(volume);

        ObjectCount = Volumes.Count;
        SelectedVolume = volume;
    }

    private void UpdateSelectedDimensions()
    {
        if (SelectedVolume is null)
            return;

        var validWidth = double.TryParse(
            WidthInput, NumberStyles.Float, CultureInfo.CurrentCulture,
            out var width);

        var validDepth = double.TryParse(
            DepthInput, NumberStyles.Float, CultureInfo.CurrentCulture,
            out var depth);

        var validHeight = double.TryParse(
            HeightInput, NumberStyles.Float, CultureInfo.CurrentCulture,
            out var height);

        if (!validWidth || !validDepth || !validHeight)
            return;

        if (width <= 0 || depth <= 0 || height <= 0)
            return;

        SelectedVolume.Resize(new Dimensions3D(width, depth, height));

        // Notify bindings that the selected volume's dimensions changed.
        OnPropertyChanged(nameof(SelectedVolume));
    }

    private string _xInput = "0";
    public string XInput
    {
        get => _xInput;
        set
        {
            if (SetProperty(ref _xInput, value))
                UpdateSelectedPosition();
        }
    }

    private string _yInput = "0";
    public string YInput
    {
        get => _yInput;
        set
        {
            if (SetProperty(ref _yInput, value))
                UpdateSelectedPosition();
        }
    }

    private string _zInput = "0";
    public string ZInput
    {
        get => _zInput;
        set
        {
            if (SetProperty(ref _zInput, value))
                UpdateSelectedPosition();
        }
    }

    
    private void UpdateSelectedPosition()
{
    if (SelectedVolume is null)
        return;

    bool validX = double.TryParse(
        XInput, NumberStyles.Float,
        CultureInfo.CurrentCulture, out var x);

    bool validY = double.TryParse(
        YInput, NumberStyles.Float,
        CultureInfo.CurrentCulture, out var y);

    bool validZ = double.TryParse(
        ZInput, NumberStyles.Float,
        CultureInfo.CurrentCulture, out var z);

    if (!validX || !validY || !validZ)
        return;

    SelectedVolume.MoveTo(new Position3D(x, y, z));

    OnPropertyChanged(nameof(SelectedVolume));
}

    private string _rotationXInput = "0";
    public string RotationXInput
    {
        get => _rotationXInput;
        set
        {
            if (SetProperty(ref _rotationXInput, value))
                UpdateSelectedRotation();
        }
    }

    private string _rotationYInput = "0";
    public string RotationYInput
    {
        get => _rotationYInput;
        set
        {
            if (SetProperty(ref _rotationYInput, value))
                UpdateSelectedRotation();
        }
    }

    private string _rotationZInput = "0";
    public string RotationZInput
    {
        get => _rotationZInput;
        set
        {
            if (SetProperty(ref _rotationZInput, value))
                UpdateSelectedRotation();
        }
    }

    private void UpdateSelectedRotation()
    {
        if (SelectedVolume is null ||
            !double.TryParse(RotationXInput, NumberStyles.Float, CultureInfo.CurrentCulture, out double x) ||
            !double.TryParse(RotationYInput, NumberStyles.Float, CultureInfo.CurrentCulture, out double y) ||
            !double.TryParse(RotationZInput, NumberStyles.Float, CultureInfo.CurrentCulture, out double z))
            return;

        SelectedVolume.RotateTo(new Rotation3D(x, y, z));
    }

    private string _scaleXInput = "1";
    public string ScaleXInput
    {
        get => _scaleXInput;
        set
        {
            if (SetProperty(ref _scaleXInput, value))
                UpdateSelectedScale();
        }
    }

    private string _scaleYInput = "1";
    public string ScaleYInput
    {
        get => _scaleYInput;
        set
        {
            if (SetProperty(ref _scaleYInput, value))
                UpdateSelectedScale();
        }
    }

    private string _scaleZInput = "1";
    public string ScaleZInput
    {
        get => _scaleZInput;
        set
        {
            if (SetProperty(ref _scaleZInput, value))
                UpdateSelectedScale();
        }
    }

    private void UpdateSelectedScale()
    {
        if (SelectedVolume is null ||
            !double.TryParse(ScaleXInput, NumberStyles.Float, CultureInfo.CurrentCulture, out double x) ||
            !double.TryParse(ScaleYInput, NumberStyles.Float, CultureInfo.CurrentCulture, out double y) ||
            !double.TryParse(ScaleZInput, NumberStyles.Float, CultureInfo.CurrentCulture, out double z) ||
            x <= 0 || y <= 0 || z <= 0)
            return;

        SelectedVolume.ScaleTo(new Scale3D(x, y, z));
    }

    private string _colorHex = "#B88757";

    public string ColorHex
    {
        get => _colorHex;
        private set => SetProperty(ref _colorHex, value);
    }

    [RelayCommand]
    private void SetSelectedColor(string? hex)
    {
        if (SelectedVolume is null || string.IsNullOrWhiteSpace(hex))
            return;

        string value = hex.Trim().TrimStart('#');
        if (value.Length != 6 || !uint.TryParse(value, NumberStyles.HexNumber,
                CultureInfo.InvariantCulture, out uint rgb))
            return;

        SelectedVolume.SetColor(new MaterialColor(
            ((rgb >> 16) & 0xFF) / 255f,
            ((rgb >> 8) & 0xFF) / 255f,
            (rgb & 0xFF) / 255f));
    }

    private static string ToHex(MaterialColor color) =>
        $"#{(int)(Math.Clamp(color.Red, 0f, 1f) * 255):X2}" +
        $"{(int)(Math.Clamp(color.Green, 0f, 1f) * 255):X2}" +
        $"{(int)(Math.Clamp(color.Blue, 0f, 1f) * 255):X2}";

}
