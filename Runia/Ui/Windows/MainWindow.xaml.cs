using Runia.Services.Core.Navigation;
using Runia.Ui.PagesMain;
using Runia.ViewModels.Main;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace Runia.Ui.Windows
{
    public partial class MainWindow : Window
    {
        private SideBarPage? _sidebarPage;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
            Activated += OnWindowActivated;
        }

        private void OnWindowActivated(object? sender, EventArgs e)
        {
            Debug.WriteLine("[MainWindow] OnWindowActivated - ensuring navigation host is set");
            if (App.Services?.GetService(typeof(INavigationService)) is NavigationService navService)
            {
                navService.SetHost(MainContentHost);
            }
        }
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && this.WindowState == WindowState.Normal)
            {
                this.DragMove();
            }
        }

        #region Инициализация UI и Навигации

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] OnWindowLoaded - Start");
            if (App.Services?.GetService(typeof(INavigationService)) is NavigationService navService)
            {
                navService.SetHost(MainContentHost);
                RegisterNavigationMaps(navService);
                if (navService.CurrentViewModel == null)
                {
                    Debug.WriteLine("[MainWindow] Navigating to Home...");
                    navService.NavigateTo<HomeViewModel>();
                }
            }

            InitializeSidebar();
        }
        protected override void OnClosed(EventArgs e)
        {
            Debug.WriteLine("[MainWindow] OnClosed - Cleaning up to prevent leaks");
            if (App.Services?.GetService(typeof(INavigationService)) is NavigationService navService)
            {
                if (navService.Host == MainContentHost)
                {
                    navService.SetHost(null);
                }
            }

            SidebarHost.Content = null;
            _sidebarPage = null;

            base.OnClosed(e);
        }
        private static void RegisterNavigationMaps(NavigationService nav)
        {
            nav.RegisterMapping<HomeViewModel, HomePage>();
            nav.RegisterMapping<SettingsViewModel, SettingsPage>();
            nav.RegisterMapping<UserViewModel, UserPage>();
            nav.RegisterMapping<ServerViewModel, ServerPage>();
            nav.RegisterMapping<NewsDetailsViewModel, NewsDetailsPage>();
          
        }

        private void InitializeSidebar()
        {
            var services = App.Services;
            if (services == null) return;
            _sidebarPage = services.GetService(typeof(SideBarPage)) as SideBarPage ?? new SideBarPage();
            if (services.GetService(typeof(SidebarViewModel)) is SidebarViewModel sbVm)
            {
                _sidebarPage.DataContext = sbVm;
            }

            SidebarHost.Content = _sidebarPage;
        }

        #endregion

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}