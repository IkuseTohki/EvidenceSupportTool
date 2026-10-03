using EvidenceSupportTool.Services;
using System.IO;
using System.Linq; // Add this using

namespace EvidenceSupportTool.Tests
{
    [TestClass]
    public class ConfigServiceTests
    {
        [TestMethod]
        public void GetAppSettings_ShouldReadSettingsCorrectly()
        {
            // Arrange
            var configService = new ConfigService(new IniParser(), "test_setting.ini");

            // Act
            var settings = configService.GetAppSettings();

            // Assert
            Assert.IsNotNull(settings);
            Assert.AreEqual("C:\\TestEvidence", settings.EvidenceSavePath);
            Assert.IsTrue(settings.KeepSnapshot);
            Assert.IsTrue(settings.AutoSaveScreenshots);
        }

        [TestMethod]
        public void GetMonitoringTargets_ShouldReadTargetsCorrectly()
        {
            // Arrange
            var configService = new ConfigService(new IniParser(), "test_setting.ini");

            // Act
            var targets = configService.GetMonitoringTargets().ToList(); // ToList() to materialize the IEnumerable

            // Assert
            Assert.IsNotNull(targets);
            Assert.AreEqual(2, targets.Count);

            var appLogTarget = targets.FirstOrDefault(t => t.Name == "AppLog");
            Assert.IsNotNull(appLogTarget);
            Assert.AreEqual("C:\\Logs\\App\\{YYYY}{MM}{DD}\\application_*.log", appLogTarget.PathPattern);

            var systemLogTarget = targets.FirstOrDefault(t => t.Name == "SystemLog");
            Assert.IsNotNull(systemLogTarget);
            Assert.AreEqual("C:\\Logs\\System\\system.log", systemLogTarget.PathPattern);
        }

        /// <summary>
        /// テスト観点: [CollectTargets]が存在する場合、エントリが正しく読み込まれること
        /// </summary>
        [TestMethod]
        public void GetCollectTargets_ShouldReadCollectTargetsCorrectly()
        {
            // Arrange
            var configService = new ConfigService(new IniParser(), "test_setting.ini");

            // Act
            var targets = configService.GetCollectTargets().ToList();

            // Assert
            Assert.IsNotNull(targets);
            Assert.AreEqual(2, targets.Count);

            var iisLogTarget = targets.FirstOrDefault(t => t.Name == "IisLog");
            Assert.IsNotNull(iisLogTarget);
            Assert.AreEqual("C:\\inetpub\\logs\\{YYYY}{MM}{DD}\\*.log", iisLogTarget.PathPattern);

            var eventLogTarget = targets.FirstOrDefault(t => t.Name == "EventLog");
            Assert.IsNotNull(eventLogTarget);
            Assert.AreEqual("C:\\Windows\\System32\\winevt\\Logs\\Application.evtx", eventLogTarget.PathPattern);
        }

        /// <summary>
        /// テスト観点: [CollectTargets]が存在しない場合、空のリストが返ること
        /// </summary>
        [TestMethod]
        public void GetCollectTargets_WhenSectionMissing_ShouldReturnEmpty()
        {
            // Arrange: [CollectTargets]セクションを持たないINIを使用
            var tempIni = Path.GetTempFileName();
            File.WriteAllText(tempIni,
                "[Targets]\nAppLog = C:\\Logs\\app.log\n\n[Settings]\nEvidenceSavePath = C:\\Out\nKeepSnapshot = false\nAutoSaveScreenshots = false\n");
            var configService = new ConfigService(new IniParser(), tempIni);

            // Act
            var targets = configService.GetCollectTargets().ToList();

            // Assert
            Assert.IsNotNull(targets);
            Assert.AreEqual(0, targets.Count);

            // Cleanup
            File.Delete(tempIni);
        }

        /// <summary>
        /// テスト観点: 有効な設定ファイルを指定してLoadSettingsを呼び出した場合、設定ファイルパスが更新され新しい設定値が反映されること
        /// </summary>
        [TestMethod]
        public void LoadSettings_WhenValidFile_ShouldUpdateSettingsFilePath()
        {
            // Arrange
            var configService = new ConfigService(new IniParser(), "test_setting.ini");
            var tempIni = Path.GetTempFileName();
            File.WriteAllText(tempIni,
                "[Targets]\nNewAppLog = D:\\Logs\\app.log\n\n[Settings]\nEvidenceSavePath = D:\\NewEvidence\nKeepSnapshot = false\nAutoSaveScreenshots = false\n");

            try
            {
                // Act
                configService.LoadSettings(tempIni);
                var settings = configService.GetAppSettings();
                var targets = configService.GetMonitoringTargets().ToList();

                // Assert
                Assert.AreEqual("D:\\NewEvidence", settings.EvidenceSavePath);
                Assert.AreEqual(1, targets.Count);
                Assert.AreEqual("NewAppLog", targets[0].Name);
            }
            finally
            {
                File.Delete(tempIni);
            }
        }

        /// <summary>
        /// テスト観点: 存在しないファイルを指定してLoadSettingsを呼び出した場合、FileNotFoundExceptionがスローされること
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void LoadSettings_WhenFileDoesNotExist_ShouldThrowFileNotFoundException()
        {
            // Arrange
            var configService = new ConfigService(new IniParser(), "test_setting.ini");

            // Act
            configService.LoadSettings("non_existent_file.ini");
        }
    }
}
