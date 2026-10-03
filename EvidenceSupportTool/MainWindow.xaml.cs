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
            var screenshotService = new ScreenshotService();
            var logCollectionService = new LogCollectionService(userInteractionService);
            var monitoringService = new MonitoringService(configService, userInteractionService, evidenceExtractionService, screenshotService, logCollectionService);

            this.DataContext = new MainViewModel(monitoringService, configService, userInteractionService);
        }

        /// <summary>
        /// ドラッグオーバー時のイベントハンドラです。ファイルドロップを受け入れ可能か判定します。
        /// </summary>
        /// <param name="sender">イベント送信元</param>
        /// <param name="e">ドラッグイベント引数</param>
        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        /// <summary>
        /// ファイルドロップ時のイベントハンドラです。ドロップされたファイルパスをViewModelのコマンドに渡します。
        /// </summary>
        /// <param name="sender">イベント送信元</param>
        /// <param name="e">ドラッグイベント引数</param>
        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null && DataContext is MainViewModel vm)
                {
                    if (vm.DropSettingFileCommand.CanExecute(files))
                    {
                        vm.DropSettingFileCommand.Execute(files);
                    }
                }
            }
        }
    }
}
