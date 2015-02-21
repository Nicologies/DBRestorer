﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CollectionEx;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;

namespace DBRestorer.Domain
{
    public class SqlInstancesVM : ViewModelBase
    {
        public const string RetrivingInstances = "Retrieving SQL Instances...";
        public const string RetrivingDbNames = "Retrieving Database Names...";
        private readonly ISqlServerUtil _util;

        public SqlInstancesVM(ISqlServerUtil util)
        {
            _util = util;
        }

        public ObservableCollection<string> Instances { get; } = new ObservableCollection<string>();

        private bool _IsProcessing = false;
        public bool IsProcessing {
            get
            {
                return _IsProcessing;
            }
            set
            {
                _IsProcessing = value;
                RaisePropertyChanged(nameof(IsProcessing));
            }
        }

        private string _SelectedInst;
        private string _ProgressDesc = "";

        public string SelectedInst
        {
            get
            {
                return _SelectedInst;
            }
            set
            {
                _SelectedInst = value;
                RaisePropertyChanged(nameof(SelectedInst));
            }
        }

        public async Task RetrieveInstanceAsync(bool clearCache = false)
        {
            if (!clearCache && this.Instances.Count > 0)
            {
                return;
            }
            ProgressDesc = RetrivingInstances;
            IsProcessing = true;
            var insts = await Task.Run(() => _util.GetSqlInstances());
            Instances.Assign(insts);
            SelectedInst = Instances.FirstOrDefault();
            IsProcessing = false;
        }

        public ICommand RefreshCmd
        {
            get
            {
                return new RelayCommand(() => RetrieveInstanceAsync(clearCache:true));
            }
        }


        public string ProgressDesc
        {
            get { return _ProgressDesc; }
            set
            {
                _ProgressDesc = value;
                RaisePropertyChanged(nameof(ProgressDesc));
            }
        }

        public ObservableCollection<string> DbNames { get; set; } = new ObservableCollection<string>();

        public async Task RetrieveDbNamesAsync(string mssqlserver)
        {
            if (string.IsNullOrEmpty(mssqlserver))
            {
                return;
            }
            ProgressDesc = RetrivingDbNames;
            IsProcessing = true;
            var dbNames = await Task.Run(() => _util.GetDatabaseNames(mssqlserver));
            DbNames.Assign(dbNames);
            IsProcessing = false;
        }
    }
}
