using XrayUI.Helpers;
using XrayUI.ViewModels;
using WinUIEx;

namespace XrayUI.Views;

public sealed partial class TrafficMonitorWindow
{
    public TrafficMonitorWindow(TrafficMonitorViewModel viewModel)
    {
        InitializeComponent();
        Monitor.ViewModel = viewModel;
        Title = Loc.GetString("Traffic_Title");
        this.SetWindowSize(1080, 720);
        AppWindow.Title = Title;
        ThemeHelper.FollowAppTheme(this, WindowRoot);
    }
}