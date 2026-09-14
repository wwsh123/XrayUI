using System;
using System.Diagnostics;
using XrayUI.Helpers;
using XrayUI.ViewModels;
using WinUIEx;

namespace XrayUI.Views;

public sealed partial class TrafficMonitorWindow
{
    public TrafficMonitorWindow(TrafficMonitorViewModel viewModel)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            InitializeComponent();
            Monitor.ViewModel = viewModel ?? new TrafficMonitorViewModel();
            Title = TryResolveTitle();
            this.SetWindowSize(1080, 720);
            AppWindow.Title = Title;
            ThemeHelper.FollowAppTheme(this, WindowRoot);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TrafficWindow] Construction failed: {ex}");
            Title = "Traffic";
        }
    }

    private static string TryResolveTitle()
    {
        try
        {
            return Loc.GetString("Traffic_Title.Text");
        }
        catch
        {
            return Loc.GetString("Traffic_OpenTooltip");
        }
    }
}