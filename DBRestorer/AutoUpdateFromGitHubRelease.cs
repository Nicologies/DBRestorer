using System;
using System.Threading.Tasks;
using DBRestorer.Ctrl.Domain;
using Squirrel;

namespace DBRestorer
{
    internal class AutoUpdateFromGitHubRelease : IAutoUpdateSource
    {
        public async Task<bool> Update(Action<int> progressReport)
        {
            using (var mgr = await UpdateManager.GitHubUpdateManager("https://github.com/Nicologies/DbRestorer"))
            {
                var updated = await mgr.UpdateApp(progressReport) != null;
                if (updated)
                {
                    UpdateManager.RestartApp();
                }

                return updated;
            }
        }
    }
}
