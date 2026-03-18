using EvidenceSupportTool.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System;

namespace EvidenceSupportTool.Tests
{
    [TestClass]
    public class ScreenshotServiceTests
    {
        private string _testSavePath;

        [TestInitialize]
        public void Initialize()
        {
            // ターゲットや実行ごとにユニークなディレクトリを作成
            _testSavePath = Path.Combine(Path.GetTempPath(), "ScreenshotTest_" + Guid.NewGuid().ToString("N"));
            if (Directory.Exists(_testSavePath))
            {
                Directory.Delete(_testSavePath, true);
            }
            Directory.CreateDirectory(_testSavePath);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testSavePath))
            {
                Directory.Delete(_testSavePath, true);
            }
        }

        [TestMethod]
        public void Start_ShouldCreateDirectoryIfNotExist()
        {
            // Arrange
            using var service = new ScreenshotService();
            var subPath = Path.Combine(_testSavePath, "new_folder");

            // Act
            service.Start(subPath);

            // Assert
            Assert.IsTrue(Directory.Exists(subPath), "保存先ディレクトリが作成されていること。");
            
            service.Stop();
        }

        [TestMethod]
        public void ProcessClipboardInternal_ShouldSaveOnlyOnce_WhenCalledWithinDebounceInterval()
        {
            // Arrange
            using var service = new ScreenshotService();
            service.Start(_testSavePath);
            
            var dummyImage = CreateDummyImage();
            var baseTime = DateTime.Now;

            // Act
            // 1回目の呼び出し
            service.ProcessClipboardInternal(() => true, () => dummyImage, baseTime);
            // 2回目の呼び出し（100ms後、デバウンス範囲内）
            service.ProcessClipboardInternal(() => true, () => dummyImage, baseTime.AddMilliseconds(100));
            // 3回目の呼び出し（600ms後、デバウンス範囲外）
            service.ProcessClipboardInternal(() => true, () => dummyImage, baseTime.AddMilliseconds(600));

            // Assert
            var files = Directory.GetFiles(_testSavePath, "screenshot_*.png");
            Assert.AreEqual(2, files.Length, "デバウンスにより、3回の呼び出しのうち2回だけ保存されていること。");

            service.Stop();
        }

        private System.Windows.Media.Imaging.BitmapSource CreateDummyImage()
        {
            int width = 1;
            int height = 1;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            return System.Windows.Media.Imaging.BitmapSource.Create(
                width, height, 96, 96, 
                System.Windows.Media.PixelFormats.Bgra32, 
                null, pixels, stride);
        }
    }
}
