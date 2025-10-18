using System;
using System.ComponentModel;
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

        public MainViewModel(IMonitoringService monitoringService, IConfigService configService, IUserInteractionService userInteractionService)
        {
            _monitoringService = monitoringService;
            _configService = configService;
            _userInteractionService = userInteractionService;

            StartMonitoringCommand = new RelayCommand(StartMonitoring, CanStartMonitoring);
            StopMonitoringCommand = new RelayCommand(StopMonitoring, CanStopMonitoring);
        }

        private void StartMonitoring(object? parameter)
        {
            _monitoringService.Start();
            StatusText = "監視中";
            ((RelayCommand)StartMonitoringCommand).RaiseCanExecuteChanged();
            ((RelayCommand)StopMonitoringCommand).RaiseCanExecuteChanged();
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
        }

        private bool CanStopMonitoring(object? parameter)
        {
            return StatusText == "監視中";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    }
