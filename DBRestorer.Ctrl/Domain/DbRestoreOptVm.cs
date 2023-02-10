using System.IO;

namespace DBRestorer.Ctrl.Domain;

public class DbRestoreOptVm : ViewModelBaseEx
{
    private string _RelocateLdfTo;
    private string _RelocateMdfTo;
    private string _SrcPath;
    private string _TargetDbName;

    public string SrcPath
    {
        get => _SrcPath;
        set
        {
            RaiseAndSetIfChanged(ref _SrcPath, value);

            if (string.IsNullOrEmpty(_SrcPath))
            {
                return;
            }
            var fileName = Path.GetFileNameWithoutExtension(_SrcPath);
            TargetDbName = fileName;
            GenerateMdfLdfFilePath();
        }
    }

    public string TargetDbName
    {
        get => _TargetDbName;
        set
        {
            _TargetDbName = value;
            RaisePropertyChanged();
            GenerateMdfLdfFilePath();
        }
    }

    private void GenerateMdfLdfFilePath()
    {
        if (!string.IsNullOrWhiteSpace(_TargetDbName)
            && !string.IsNullOrWhiteSpace(_SrcPath))
        {
            var dir = Path.GetDirectoryName(_SrcPath)!;
            RelocateLdfTo = Path.Combine(dir, TargetDbName + "_log.ldf");
            RelocateMdfTo = Path.Combine(dir, TargetDbName + ".mdf");
        }
    }

    public string RelocateMdfTo
    {
        get => _RelocateMdfTo;
        set
        {
            _RelocateMdfTo = value;
            RaisePropertyChanged();
        }
    }

    public string RelocateLdfTo
    {
        get => _RelocateLdfTo;
        set
        {
            _RelocateLdfTo = value;
            RaisePropertyChanged();
        }
    }

    public SqlServerUtilBase.DbRestoreOptions GetDbRestoreOption(string serverInstName)
    {
        return new SqlServerUtilBase.DbRestoreOptions
        {
            SqlServerInstName = serverInstName,
            RelocateMdfTo = RelocateMdfTo,
            RelocateLdfTo = RelocateLdfTo,
            SrcPath = SrcPath,
            TargetDbName = TargetDbName
        };
    }
}