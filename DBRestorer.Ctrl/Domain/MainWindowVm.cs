using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DBRestorer.Ctrl.Model;
using DBRestorer.Ctrl.PluginManagement;
using DBRestorer.Plugin.Interface;
using ExtendedCL;
using GalaSoft.MvvmLight.Threading;
using Nicologies.WpfCommon.Utils;
using PropertyChanged;

namespace DBRestorer.Ctrl.Domain;

[AddINotifyPropertyChangedInterface]
public class MainWindowVm : ViewModelBaseEx, IProgressBarProvider
{
    private readonly ISqlServerUtil _sqlServerUtil;
    private readonly IUserPreferencePersist _userPreferencePersist;
    private readonly DbRestoreOptVm _dbRestoreOption = new();

    public MainWindowVm(ISqlServerUtil sqlServerUtil, IUserPreferencePersist userPreferencePersist)
    {
        _sqlServerUtil = sqlServerUtil;
        _userPreferencePersist = userPreferencePersist;
        SqlInstancesVm = new SqlInstancesVm(_sqlServerUtil, this, userPreferencePersist);
        _dbRestoreOption.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(DbRestoreOptVm.TargetDbName))
            {
                var pref = _userPreferencePersist.LoadPreference();
                pref.LastUsedDbName = _dbRestoreOption.TargetDbName;
                _userPreferencePersist.SavePreference(pref);
            }
        };
    }

    public string ApplicationTitle
    {
        get
        {
            var ver = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()!.Location).ProductVersion;
            return $"DBRestorer v{ver}";
        }
    }

    public void LoadPlugins()
    {
        var plugins = Plugins.GetPlugins<IPostDbRestore>();
        PostRestorePlugins.AddRange(plugins.Select(r => r.Value.PluginName));

        var utilities = Plugins.GetPlugins<IDbUtility>();
        Utilities.AddRange(utilities.Select(r => r.Value.PluginName));

        var settings = Plugins.GetPlugins<IDbRestorerSettings>();
        PluginSettings.AddRange(settings.Select(r => r.Value.Name));
    }

    public SqlInstancesVm SqlInstancesVm
    {
        get;
        set;
    }

    public DbRestoreOptVm DbRestoreOptVm
    {
        get;
        set;
    }

    public int Percent
    {
        get;
        set;
    }

    public string ProgressDesc
    {
        get;
        set;
    }

    public bool PercentageDisabled
    {
        get;
        set;
    } = true;

    public bool IsProcessing
    {
        get;
        set;
    }

    public async Task AutoUpdate()
    {
        var lastUpdateCheckTime = GetLastUpdateCheckTime();
        var checkedRecently = lastUpdateCheckTime >= DateTime.Now.AddDays(-1);
        if (checkedRecently)
        {
            return;
        }

        Start(true, "Checking new release");
        try
        {
            SaveLastUpdateCheckTime();

            await AutoUpdateSource.Source.Update(i => Percent = i);
        }
        catch (Exception ex)
        {
            MessengerInstance.Send(new ErrorMsg($"Failed to check new release {ex}"));
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private DateTime GetLastUpdateCheckTime()
    {
        var pref = _userPreferencePersist.LoadPreference();

        var lastUpdateCheckTime = (pref.LastUpdateCheckTime ?? DateTime.MinValue);
        return lastUpdateCheckTime;
    }

    private void SaveLastUpdateCheckTime()
    {
        var pref = _userPreferencePersist.LoadPreference();
        pref.LastUpdateCheckTime = DateTime.Now;
        _userPreferencePersist.SavePreference(pref);
    }

    public ObservableCollection<string> PostRestorePlugins
    {
        get; set;
    } = new ObservableCollection<string>();

    public ObservableCollection<string> Utilities { get; set; } = new ObservableCollection<string>(); 
    public ObservableCollection<string> PluginSettings { get; set; } = new ObservableCollection<string>(); 

    public void OnCompleted(string msg)
    {
        DispatcherHelper.CheckBeginInvokeOnUI(() =>
        {
            if (msg == SqlServerUtil.FinishedRestore)
            {
                MessengerInstance.Send(new CallPostRestorePlugins("Call PostRestore Plugins"));
            }
            IsProcessing = false;
            if (!string.IsNullOrWhiteSpace(msg))
            {
                MessengerInstance.Send(new SucceedMsg(msg));
            }
        });
    }

    public void Start(bool willReportProgress, string taskDesc)
    {
        Percent = 0;
        PercentageDisabled = !willReportProgress;
        IsProcessing = true;
        ProgressDesc = taskDesc;
    }

    public void OnError(string err)
    {
        DispatcherHelper.CheckBeginInvokeOnUI(() =>
        {
            IsProcessing = false;
            MessengerInstance.Send(new ErrorMsg(err));
        });
    }

    public void ReportProgress(int percent)
    {
        DispatcherHelper.CheckBeginInvokeOnUI(() => Percent = percent);
    }

    public async Task Restore()
    {
        Start(false, "Initializing...");
        try
        {
            await _sqlServerUtil.Restore(DbRestoreOptVm.GetDbRestoreOption(SqlInstancesVm.SelectedInst),
                this, OnRestored);
        }
        catch
        {
            IsProcessing = false;
            throw;
        }
    }

    private void OnRestored()
    {
        DispatcherHelper.CheckBeginInvokeOnUI(
            async () => await SqlInstancesVm.RetrieveDbNamesAsync(SqlInstancesVm.SelectedInst));
    }

    public void SaveInstSelection()
    {
        SqlInstancesVm.SavePreference();
    }

    public async Task LoadSqlInstanceAndDbs()
    {
        await SqlInstancesVm.RetrieveInstanceAsync();
        await SqlInstancesVm.RetrieveDbNamesAsync(SqlInstancesVm.SelectedInst);
        var pref = _userPreferencePersist.LoadPreference();
        DbRestoreOptVm.TargetDbName = pref.LastUsedDbName;
    }
}