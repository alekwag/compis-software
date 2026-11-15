
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Compis.Core.Objects;
using Compis.Core.Parameters;
using Compis.Core.Scene;

namespace Compis.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public BuildingScene Scene { get; } = new();

    public ObservableCollection<BuildingVolume> Volumes { get; } = [];

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
            if (SetProperty(ref _selectedVolume, value))
            {
                if (value is not null)
                {
                    _widthInput = value.Dimensions.Width.ToString(CultureInfo.CurrentCulture);
                    _depthInput = value.Dimensions.Depth.ToString(CultureInfo.CurrentCulture);
                    _heightInput = value.Dimensions.Height.ToString(CultureInfo.CurrentCulture);
                }
                else
                {
                    _widthInput = "";
                    _depthInput = "";
                    _heightInput = "";
                }

                OnPropertyChanged(nameof(WidthInput));
                OnPropertyChanged(nameof(DepthInput));
                OnPropertyChanged(nameof(HeightInput));
            }
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
}
