using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DBRestorer.Ctrl.Domain;
using Squirrel;

namespace DBRestorer
{
    internal class AutoUpdateFromGitHubRelease : IAutoUpdateSource
    {
        public async Task<bool> Update(Action<int> progressReport)
        {
            if (Debugger.IsAttached)
            {
                // app is trying to upgrade itself.
                Debugger.Break();
                return false;
            }
            using (var mgr = await UpdateManager.GitHubUpdateManager("https://github.com/Nicologies/DbRestorer"))
            {
                var updated = await mgr.UpdateApp(progressReport) != null;
                if (updated)
                {
                    RemoveLegacyClickOnceFiles();
                    UpdateManager.RestartApp();
                }

                return updated;
            }
        }

        private void RemoveLegacyClickOnceFiles()
        {
            var fileName = "DbRestorer.appref-ms";
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var desktopShortcut = Path.Combine(desktop, fileName);
            DeleteIfExists(desktopShortcut);

            var startMenu = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
            var startMenuShortcut = Path.Combine(startMenu, "programs", "Nicologies", fileName);
            DeleteIfExists(startMenuShortcut);

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var quickLaunchFolder = Path.Combine(appData, @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar");
            var quickLaunch = Path.Combine(quickLaunchFolder, fileName);
            DeleteIfExists(quickLaunch);
        }

        private static void DeleteIfExists(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
