using Runia.Services.Core.Infrastructure.Persistence;
using Runia.Services.Core.Launcher;

namespace Runia.Services.Core.Infrastructure.Constants
{
    public static class AppStatic
    {
        public static DataManager? Data { get; set; }
        public static VersionsManager? Versions { get; set; }
    }
}
