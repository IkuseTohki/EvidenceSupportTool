using System.Windows;
using EvidenceSupportTool.ViewModels;
using EvidenceSupportTool.Services;

namespace EvidenceSupportTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(string settingsFilePath)
        {
            InitializeComponent();

            // 依存関係の解決とViewModelのインスタンス化
            var userInteractionService = new UserInteractionService();
            var iniParser = new IniParser();
            var configService = new ConfigService(iniParser, settingsFilePath);
            var evidenceExtractionService = new EvidenceExtractionService(userInteractionService);
            var screenshotService = new ScreenshotService();
            var logCollectionService = new LogCollectionService(userInteractionService);
            var monitoringService = new MonitoringService(configService, userInteractionService, evidenceExtractionService, screenshotService, logCollectionService);

            this.DataContext = new MainViewModel(monitoringService, userInteractionService);
        }
    }
}
