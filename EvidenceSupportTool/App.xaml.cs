using System;
using System.IO;
using System.Windows;

namespace EvidenceSupportTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// アプリケーション起動時のイベントハンドラです。
        /// コマンドライン引数に設定ファイルパスが渡された場合（exeへのD&amp;D）は
        /// そのファイルを優先使用し、引数なしの場合はexeと同じフォルダのsetting.iniをデフォルトとします。
        /// </summary>
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            string settingsFilePath;

            if (e.Args.Length > 0)
            {
                // exeへのD&Dで起動：引数を検証する
                string argPath = e.Args[0];

                if (!string.Equals(Path.GetExtension(argPath), ".ini", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(
                        "設定ファイルには .ini ファイルを指定してください。",
                        "エラー",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Shutdown(1);
                    return;
                }

                if (!File.Exists(argPath))
                {
                    MessageBox.Show(
                        $"設定ファイルが見つかりません:\n{argPath}",
                        "エラー",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Shutdown(1);
                    return;
                }

                settingsFilePath = argPath;
            }
            else
            {
                // 引数なし起動：exeと同じフォルダのsetting.iniをデフォルトとする
                string exeDirectory = AppDomain.CurrentDomain.BaseDirectory ?? string.Empty;
                settingsFilePath = Path.Combine(exeDirectory, "setting.ini");
            }

            var mainWindow = new MainWindow(settingsFilePath);
            mainWindow.Show();
        }
    }
}
