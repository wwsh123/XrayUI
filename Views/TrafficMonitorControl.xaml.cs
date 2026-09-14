using System;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using XrayUI.Helpers;

namespace XrayUI.Views;

public sealed partial class TrafficMonitorControl : UserControl
{
    private TrafficMonitorViewModel? _viewModel;
    public TrafficMonitorViewModel ViewModel
    {
        get => _viewModel ??= new TrafficMonitorViewModel();
        set
        {
            if (ReferenceEquals(_viewModel, value)) return;
            _viewModel = value ?? new TrafficMonitorViewModel();
            if (IsLoaded)
                StartObserving();
        }
    }

    private DispatcherQueueTimer? _timer;
    public TrafficMonitorControl()
    {
        InitializeComponent();
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(SearchBox, Loc.GetString("Traffic_SearchAutomationName"));
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(FilterBox, Loc.GetString("Traffic_FilterAutomationName"));
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(SortBox, Loc.GetString("Traffic_SortAutomationName"));
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(ChartWindow, Loc.GetString("Traffic_WindowAutomationName"));
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        StartObserving();
    }
    private void StartObserving()
    {
        if (_viewModel is null || _timer is not null) return;
        try
        {
            _viewModel.PropertyChanged += OnViewModelChanged;
            _viewModel.Refresh();
            Chart.SetSamples(_viewModel.Samples, _viewModel.WindowMinutes);
            _timer = DispatcherQueue.CreateTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(250);
            _timer.Tick += OnTick;
            _timer.Start();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TrafficMonitorControl] StartObserving failed: {ex}");
            _timer = null;
        }
    }
    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_timer == null) return;
        _timer.Stop(); _timer.Tick -= OnTick; _timer = null;
        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelChanged;
    }
    private void OnTick(DispatcherQueueTimer sender, object args)
    {
        if (_viewModel is null) return;
        if (_viewModel.HasSource && Visibility == Visibility.Visible) _viewModel.Refresh();
    }
    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_viewModel is null) return;
        if (e.PropertyName is nameof(TrafficMonitorViewModel.Samples) or nameof(TrafficMonitorViewModel.WindowMinutes))
            Chart.SetSamples(_viewModel.Samples, _viewModel.WindowMinutes);
    }
    private void Clear_Click(object sender, RoutedEventArgs e) => ViewModel.Clear();
    private async void Details_Click(object sender, RoutedEventArgs e) => await ShowDetailsAsync();
    private async void AccessList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) => await ShowDetailsAsync();
    private bool _detailsOpen;
    private async System.Threading.Tasks.Task ShowDetailsAsync()
    {
        if (_detailsOpen || ViewModel.SelectedRow is not { } row) return;
        _detailsOpen = true;
        try
        {
            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot, RequestedTheme = ActualTheme,
                Title = Loc.GetString("Traffic_DetailsTitle"),
                Content = new TextBlock { Text = row.Details, TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true },
                PrimaryButtonText = Loc.GetString("Traffic_CopyAddress"), CloseButtonText = Loc.GetString("Traffic_Close"),
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var package = new DataPackage(); package.SetText(row.Destination); Clipboard.SetContent(package);
            }
        }
        finally { _detailsOpen = false; }
    }
}
