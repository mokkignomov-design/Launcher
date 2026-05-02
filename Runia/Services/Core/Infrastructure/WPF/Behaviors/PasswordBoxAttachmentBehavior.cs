using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Diagnostics;

namespace Runia.Services.Core.Infrastructure.WPF.Behaviors
{
    /// <summary>
    /// Attached behavior для двусторонней привязки PasswordBox к ViewModel свойству.
    /// Использование: local:PasswordBoxAttachmentBehavior.BoundPassword="{Binding Password, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
    /// </summary>
    public static class PasswordBoxAttachmentBehavior
    {
        private static bool _isUpdating = false;

        static PasswordBoxAttachmentBehavior()
        {
            // Регистрируем класс-обработчик на все PasswordBox'ы — гарантируем, что наше поведение поймает ввод
            EventManager.RegisterClassHandler(typeof(PasswordBox), PasswordBox.PasswordChangedEvent, new RoutedEventHandler(PasswordBox_PasswordChanged));
            Debug.WriteLine("[PasswordBoxBehavior] Class handler registered");
        }

        public static readonly DependencyProperty BoundPasswordProperty =
            DependencyProperty.RegisterAttached(
                "BoundPassword",
                typeof(string),
                typeof(PasswordBoxAttachmentBehavior),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, BoundPasswordChanged));

        public static string GetBoundPassword(DependencyObject obj)
        {
            return (string)obj.GetValue(BoundPasswordProperty) ?? string.Empty;
        }

        public static void SetBoundPassword(DependencyObject obj, string value)
        {
            // Use SetCurrentValue to avoid replacing an existing Binding on the attached property
            if (obj is DependencyObject d)
            {
                d.SetCurrentValue(BoundPasswordProperty, value);
            }
            else
            {
                obj.SetValue(BoundPasswordProperty, value);
            }
        }

        private static void BoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBox passwordBox && !_isUpdating)
            {
                _isUpdating = true;
                try
                {
                    string newPassword = (string)e.NewValue ?? string.Empty;
                    Debug.WriteLine($"[PasswordBoxBehavior.BoundPasswordChanged] Setting password: {newPassword.Length} chars");

                    // Устанавливаем пароль в PasswordBox если он отличается
                    if (passwordBox.Password != newPassword)
                    {
                        passwordBox.Password = newPassword;
                    }

                    // Подписываемся на PasswordChanged для обратного потока данных (в первый раз)
                    passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;
                    passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
                }
                finally
                {
                    _isUpdating = false;
                }
            }
        }

        private static void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!(sender is PasswordBox passwordBox)) return;

            if (_isUpdating) return;

            _isUpdating = true;
            try
            {
                string password = passwordBox.Password ?? string.Empty;
                Debug.WriteLine($"[PasswordBoxBehavior.PasswordChanged] User typed: '{password}' ({password.Length} chars)");

                // Сначала пытаемся обновить привязку к нашему AttachedProperty
                var binding = BindingOperations.GetBindingExpression(passwordBox, BoundPasswordProperty);
                if (binding != null)
                {
                    SetBoundPassword(passwordBox, password);
                    Debug.WriteLine("[PasswordBoxBehavior] Attached property binding found - updating source");
                    binding.UpdateSource();
                    return;
                }

                // Если привязки на AttachedProperty нет — пробуем напрямую обновить свойство Password в DataContext (fallback)
                var vm = passwordBox.DataContext;
                if (vm != null)
                {
                    var prop = vm.GetType().GetProperty("Password");
                    if (prop != null && prop.CanWrite)
                    {
                        prop.SetValue(vm, password);
                        Debug.WriteLine("[PasswordBoxBehavior] Fallback: updated ViewModel.Password via reflection");
                    }
                    else
                    {
                        Debug.WriteLine("[PasswordBoxBehavior] Fallback: ViewModel has no settable Password property");
                    }
                }
            }
            finally
            {
                _isUpdating = false;
            }
        }
    }
}
