using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Runia.Services.Core.Navigation;

namespace Runia.ViewModels.Main
{
    public partial class SecurityUserViewModel : ObservableObject
    {
        private readonly INavigationService _nav;
        private readonly ILogger<SecurityUserViewModel> _logger;

        public SecurityUserViewModel(
            INavigationService nav,
            ILogger<SecurityUserViewModel> logger)
        {
            _nav = nav;
            _logger = logger;

            _logger.LogInformation("[SecurityUserViewModel] Initializing...");
        }
    }
}
