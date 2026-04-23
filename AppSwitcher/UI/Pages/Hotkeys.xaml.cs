using AppSwitcher.Extensions;
using AppSwitcher.UI.Controls;
using AppSwitcher.UI.ViewModels;
using AppSwitcher.UI.ViewModels.Common;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;

namespace AppSwitcher.UI.Pages;

internal partial class Hotkeys : Page
{
    private readonly HotkeysViewModel _viewModel;

    public static readonly DependencyProperty FlyoutViewModelProperty =
        DependencyProperty.Register(
            nameof(FlyoutViewModel),
            typeof(AddApplicationFlyoutViewModel),
            typeof(Hotkeys),
            new PropertyMetadata());

    public AddApplicationFlyoutViewModel FlyoutViewModel
    {
        get => (AddApplicationFlyoutViewModel)GetValue(FlyoutViewModelProperty);
        private init => SetValue(FlyoutViewModelProperty, value);
    }

    public Hotkeys(HotkeysViewModel viewModel, AddApplicationFlyoutViewModel flyoutViewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        FlyoutViewModel = flyoutViewModel;

        Resources["ValidationStripeBrush"] = CreateValidationStripeBrush();

        FlyoutViewModel.ApplicationSelected += OnApplicationSelected;
        _viewModel.ApplicationAdded += OnApplicationAdded;
        AddApplicationFlyout.CloseRequested += () => AddFlyoutPopup.IsOpen = false;
        ApplicationsList.RequestBringIntoView += (_, e) => e.Handled = true;
    }

    private DrawingBrush CreateValidationStripeBrush()
    {
        var color = TryFindResource("SystemFillColorCaution") is Color c
            ? c
            : Color.FromRgb(0xFF, 0xB9, 0x00); // Windows caution amber fallback

        var stripeBrush = new SolidColorBrush(color);

        var stripeDrawing = new GeometryDrawing
        {
            Brush = stripeBrush,
            Geometry = new GeometryGroup
            {
                Children =
                [
                    Geometry.Parse("M 0,4 L 4,0 L 8,0 L 0,8 Z"),
                    Geometry.Parse("M 4,8 L 8,4 L 8,8 Z"),
                ]
            }
        };

        var backgroundDrawing = new GeometryDrawing
        {
            Brush = Brushes.Transparent,
            Geometry = new RectangleGeometry(new Rect(0, 0, 8, 8))
        };

        return new DrawingBrush
        {
            TileMode = TileMode.Tile,
            ViewportUnits = BrushMappingMode.Absolute,
            Viewport = new Rect(0, 0, 8, 8),
            Drawing = new DrawingGroup
            {
                Children = [backgroundDrawing, stripeDrawing]
            }
        };
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var staticAppsProcessNames = _viewModel.State.Applications.Select(a => a.ProcessName);
        FlyoutViewModel.Refresh(staticAppsProcessNames);
        AddFlyoutPopup.IsOpen = true;
    }

    private void AddFlyoutPopup_Opened(object sender, EventArgs e)
    {
        AddApplicationFlyout.FocusSearch();
    }

    private void OnApplicationSelected(ApplicationSelectionArgs args)
    {
        AddFlyoutPopup.IsOpen = false;
        _viewModel.AddApplicationCommand.Execute(args);
    }

    private void OnApplicationAdded(ApplicationShortcutViewModel addedVm)
    {
        Dispatcher.InvokeAsync(() =>
        {
            ApplicationsList.UpdateLayout();
            MainScrollViewer.ScrollToEnd();

            // key is already assigned when pinning one of dynamic apps - skip key assignment activation in this case
            if (addedVm.Key == (Key)(-1))
            {
                ActivateKeyAssignmentFor(addedVm);
            }
        }, DispatcherPriority.Loaded);
    }

    private void ActivateKeyAssignmentFor(ApplicationShortcutViewModel targetVm)
    {
        if (ApplicationsList.ItemContainerGenerator.ContainerFromItem(targetVm) is not FrameworkElement container)
        {
            return;
        }

        var keyButton = container.FindVisualChild<KeyAssignmentButton>();
        keyButton?.StartListening();
    }
}