using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using EvidenceSupportTool.ViewModels;
using EvidenceSupportTool.Services;

namespace EvidenceSupportTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // 依存関係の解決とViewModelのインスタンス化
            var userInteractionService = new UserInteractionService();
            var iniParser = new IniParser();
            var configService = new ConfigService(iniParser); // IniParserを渡す
            var evidenceExtractionService = new EvidenceExtractionService(userInteractionService);
            var monitoringService = new MonitoringService(configService, userInteractionService, evidenceExtractionService);

            this.DataContext = new MainViewModel(monitoringService, configService, userInteractionService);
        }
    }
}
