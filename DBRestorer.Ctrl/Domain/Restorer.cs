using System;
using ExtendedCL;

namespace DBRestorer.Ctrl.Domain
{
    public class Restorer
    {
        private readonly SqlServerUtilBase _sqlUtil;

        public Restorer(SqlServerUtilBase sqlUtil)
        {
            _sqlUtil = sqlUtil;
        }

        public void Restore(SqlServerUtilBase.DbRestoreOptions opt, IProgressBarProvider progressBarProvider,
            Action additionalCallbackOnCompleted)
        {
            _sqlUtil.Restore(opt, progressBarProvider, additionalCallbackOnCompleted);
        }
    }
}