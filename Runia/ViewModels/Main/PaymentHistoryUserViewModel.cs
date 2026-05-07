using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Runia.Services.Core.Navigation;

namespace Runia.ViewModels.Main
{
    public partial class PaymentHistoryUserViewModel : ObservableObject
    {
        private readonly INavigationService _nav;
        private readonly ILogger<PaymentHistoryUserViewModel> _logger;

        public PaymentHistoryUserViewModel(
            INavigationService nav,
            ILogger<PaymentHistoryUserViewModel> logger)
        {
            _nav = nav;
            _logger = logger;

            _logger.LogInformation("[PaymentHistoryUserViewModel] Initializing...");
        }
    }
}
