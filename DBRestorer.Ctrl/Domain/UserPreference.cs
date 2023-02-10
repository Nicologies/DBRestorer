using System;

namespace DBRestorer.Ctrl.Domain;

[Serializable]
public class UserPreference
{
    public string LastUsedDbInst { get; set; }
    public string LastUsedDbName { get; set; }
    public DateTime? LastUpdateCheckTime { get; set; }
}