using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace Runia.Services.Core.Navigation
{
    public class NavigationService : INavigationService
    {
        private readonly Dictionary<Type, Type> _mappings = [];
        private object? _host;
        private object? _currentViewModel;
        private readonly Dictionary<Type, FrameworkElement> _viewCache = [];

        public object? CurrentViewModel => _currentViewModel;
        // Expose the current host so callers can decide whether to clear it
        public object? Host => _host;

        public void SetHost(object? host)
        {
            _host = host;
            _currentViewModel = null;
        }

        public void RegisterMapping<TViewModel, TView>() where TView : FrameworkElement
        {
            _mappings[typeof(TViewModel)] = typeof(TView);
        }

        public void NavigateTo<T>() where T : class => NavigateTo<T>(null);

        public void NavigateTo<T>(object? parameter) where T : class
        {
            if (App.Services == null) return;
            var vm = parameter == null
                ? App.Services.GetService<T>()
                : ActivatorUtilities.CreateInstance<T>(App.Services, parameter);
            if (vm != null) NavigateTo(vm);
        }

        public void NavigateTo(object? viewModel)
        {
            Debug.WriteLine($"[NavigationService] NavigateTo called. viewModel={viewModel?.GetType().Name ?? "null"}, hostType={_host?.GetType().Name ?? "null"}");
            if (_host == null)
            {
                Debug.WriteLine("[NavigationService] Navigation aborted: host is null. Make sure SetHost was called on the NavigationService with the ContentControl host before navigating.");
                return;
            }

            if (viewModel == null || App.Services == null)
            {
                Debug.WriteLine("[NavigationService] Navigation aborted: viewModel or Services is null");
                return;
            }

            if (_currentViewModel?.GetType() == viewModel.GetType()) return;

            var vmType = viewModel.GetType();
            if (!_mappings.TryGetValue(vmType, out var viewType))
            {
                var pageName = vmType.Name.Replace("ViewModel", "Page");
                viewType = vmType.Assembly.GetTypes().FirstOrDefault(t => t.Name == pageName);
                if (viewType != null) _mappings[vmType] = viewType;
            }

            if (viewType == null) return;


            FrameworkElement? view = null;

            // 0. Check cache first
            try
            {
                if (_viewCache.TryGetValue(vmType, out var cached))
                {
                    view = cached;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NavigationService] View cache lookup failed: {ex}");
            }

            // If not found in cache, resolve/create and then cache
            if (view == null)
            {
                // 1. Пытаемся достать из DI (если страница там есть)
                try { view = App.Services.GetService(viewType) as FrameworkElement; } catch (Exception ex) { Debug.WriteLine($"[NavigationService] DI resolution failed: {ex}"); }

                // 2. Fallback: Создаем вручную, передавая ViewModel в конструктор
                if (view == null)
                {
                    try { view = Activator.CreateInstance(viewType, viewModel) as FrameworkElement; } catch (Exception ex) { Debug.WriteLine($"[NavigationService] Activator with VM failed: {ex}"); }
                }

                // 3. Last Resort: Создаем пустую страницу
                if (view == null)
                {
                    try { view = Activator.CreateInstance(viewType) as FrameworkElement; } catch (Exception ex) { Debug.WriteLine($"[NavigationService] Activator empty failed: {ex}"); }
                }

                if (view != null)
                {
                    try
                    {
                        // cache the created/resolved view for future navigations
                        _viewCache[vmType] = view;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[NavigationService] Caching view failed: {ex}");
                    }
                }
            }

            if (view != null)
            {
                _currentViewModel = viewModel;
                view.DataContext = viewModel;

                if (_host is ContentControl cc)
                {
                    cc.Content = view;
                }
            }
        }
    }
}