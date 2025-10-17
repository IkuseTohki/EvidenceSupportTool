using EvidenceSupportTool.Services;
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
            var configService = new ConfigService(new IniParser("test_setting.ini"));

            // Act
            var settings = configService.GetAppSettings();

            // Assert
            Assert.IsNotNull(settings);
            Assert.AreEqual("C:\\TestEvidence", settings.EvidenceSavePath);
            Assert.IsTrue(settings.KeepSnapshot);
        }

        [TestMethod]
        public void GetMonitoringTargets_ShouldReadTargetsCorrectly()
        {
            // Arrange
            var configService = new ConfigService(new IniParser("test_setting.ini"));

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
    }
}
