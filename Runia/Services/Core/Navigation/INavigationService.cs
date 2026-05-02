// INavigationService.cs
using System;

namespace Runia.Services.Core.Navigation
{
    public interface INavigationService
    {
        // Добавляем свойство, которого не хватало (Ошибка CS0535)
        object? CurrentViewModel { get; }

        void SetHost(object? host);
        void RegisterMapping<TViewModel, TView>() where TView : System.Windows.FrameworkElement;

        // Оставляем только три четких метода
        void NavigateTo(object? viewModel); // Базовый по объекту
        void NavigateTo<TViewModel>() where TViewModel : class; // Без параметров
        void NavigateTo<T>(object? parameter) where T : class; // С параметром
    }
}