using System.IO;

namespace Runia.Services.Core.Launcher
{
    public class VersionsManager
    {
        public const string ProjectName = "Runia";

        public static string Root => !string.IsNullOrEmpty(Properties.Settings.Default.GameRootPath)
            ? Properties.Settings.Default.GameRootPath
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ProjectName);

        public static string GetVersionPath(string packName) => Path.Combine(Root, "versions", packName);

        public static void EnsureDirectoriesExist(string packName)
        {
            var paths = new[] { Root, Path.Combine(Root, "versions"), GetVersionPath(packName) };
            foreach (var p in paths) Directory.CreateDirectory(p);
        }
    }
}