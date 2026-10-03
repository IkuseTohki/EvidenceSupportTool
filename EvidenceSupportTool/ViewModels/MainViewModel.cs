using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using EvidenceSupportTool.Services;
using EvidenceSupportTool.Commands;

namespace EvidenceSupportTool.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IMonitoringService _monitoringService;
        private readonly IConfigService _configService;
        private readonly IUserInteractionService _userInteractionService;

        private string _statusText = "待機中";
        public string StatusText
        {
            get => _statusText;
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        public ICommand StartMonitoringCommand { get; }
        public ICommand StopMonitoringCommand { get; }
        public ICommand DropSettingFileCommand { get; }

        public MainViewModel(IMonitoringService monitoringService, IConfigService configService, IUserInteractionService userInteractionService)
        {
            _monitoringService = monitoringService;
            _configService = configService;
            _userInteractionService = userInteractionService;

            StartMonitoringCommand = new RelayCommand(StartMonitoring, CanStartMonitoring);
            StopMonitoringCommand = new RelayCommand(StopMonitoring, CanStopMonitoring);
            DropSettingFileCommand = new RelayCommand(DropSettingFile, CanDropSettingFile);
        }

        private void StartMonitoring(object? parameter)
        {
            _monitoringService.Start();
            StatusText = "監視中";
            ((RelayCommand)StartMonitoringCommand).RaiseCanExecuteChanged();
            ((RelayCommand)StopMonitoringCommand).RaiseCanExecuteChanged();
            ((RelayCommand)DropSettingFileCommand).RaiseCanExecuteChanged();
        }

        private bool CanStartMonitoring(object? parameter)
        {
            return StatusText == "待機中";
        }

        private void StopMonitoring(object? parameter)
        {
            _monitoringService.Stop();
            StatusText = "待機中";
            ((RelayCommand)StartMonitoringCommand).RaiseCanExecuteChanged();
            ((RelayCommand)StopMonitoringCommand).RaiseCanExecuteChanged();
            ((RelayCommand)DropSettingFileCommand).RaiseCanExecuteChanged();
        }

        private bool CanStopMonitoring(object? parameter)
        {
            return StatusText == "監視中";
        }

        /// <summary>
        /// 設定ファイルのドラッグ＆ドロップ時に実行される処理です。
        /// </summary>
        /// <param name="parameter">ドロップされたファイルパス（string または string[]）</param>
        private void DropSettingFile(object? parameter)
        {
            string? filePath = parameter switch
            {
                string s => s,
                string[] arr when arr.Length > 0 => arr[0],
                IEnumerable<string> enumerable => enumerable.FirstOrDefault(),
                _ => null
            };

            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            var extension = Path.GetExtension(filePath);
            if (!string.Equals(extension, ".ini", StringComparison.OrdinalIgnoreCase))
            {
                _userInteractionService.ShowError("設定ファイルには .ini ファイルを指定してください。");
                return;
            }

            try
            {
                _configService.LoadSettings(filePath);
                var fileName = Path.GetFileName(filePath);
                StatusText = $"設定ファイルを読み込みました: {fileName}";
            }
            catch (Exception ex)
            {
                _userInteractionService.ShowError($"設定ファイルの読み込みに失敗しました: {ex.Message}");
            }
        }

        private bool CanDropSettingFile(object? parameter)
        {
            return StatusText == "待機中";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    }
