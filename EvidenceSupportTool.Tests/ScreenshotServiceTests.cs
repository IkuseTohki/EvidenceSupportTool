using EvidenceSupportTool.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System;

namespace EvidenceSupportTool.Tests
{
    [TestClass]
    public class ScreenshotServiceTests
    {
        private string _testSavePath = null!;

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
        public void ExecuteWithRetry_WhenActionSucceedsFirstTime_ShouldReturnResult()
        {
            // 観点: 1回目で正常に終了した場合、リトライせずに結果を返すこと
            int callCount = 0;
            var result = ScreenshotService.ExecuteWithRetry(() =>
            {
                callCount++;
                return "success";
            }, maxRetries: 3, delayMs: 1);

            Assert.AreEqual("success", result);
            Assert.AreEqual(1, callCount, "1回のみ呼び出されること。");
        }

        [TestMethod]
        public void ExecuteWithRetry_WhenActionFailsThenSucceeds_ShouldRetryAndReturnResult()
        {
            // 観点: 一時的な例外が発生しても、規定回数内に成功すればリトライして結果を返すこと
            int callCount = 0;
            var result = ScreenshotService.ExecuteWithRetry(() =>
            {
                callCount++;
                if (callCount < 3)
                {
                    throw new System.Runtime.InteropServices.COMException("Clipboard locked");
                }
                return "recovered";
            }, maxRetries: 5, delayMs: 1);

            Assert.AreEqual("recovered", result);
            Assert.AreEqual(3, callCount, "3回呼び出されて成功すること。");
        }

        [TestMethod]
        public void ExecuteWithRetry_WhenAllRetriesFail_ShouldReturnDefault()
        {
            // 観点: すべてのリトライが失敗した場合、例外をスローせずdefault(null)を返すこと
            int callCount = 0;
            var result = ScreenshotService.ExecuteWithRetry<string?>(() =>
            {
                callCount++;
                throw new InvalidOperationException("Persistent error");
            }, maxRetries: 3, delayMs: 1);

            Assert.IsNull(result);
            Assert.AreEqual(3, callCount, "最大リトライ回数まで試行されること。");
        }

        [TestMethod]
        public void ComputeImageHash_ShouldReturnSameHash_ForIdenticalImages()
        {
            // 観点: 同じピクセルデータを持つ画像からは同一のハッシュ値が返されること
            var img1 = CreateImage(10, 10, 10, 20, 30, 255);
            var img2 = CreateImage(10, 10, 10, 20, 30, 255);

            var hash1 = ScreenshotService.ComputeImageHash(img1);
            var hash2 = ScreenshotService.ComputeImageHash(img2);

            Assert.IsFalse(string.IsNullOrEmpty(hash1));
            Assert.AreEqual(hash1, hash2, "同一画像のハッシュ値が一致すること。");
        }

        [TestMethod]
        public void ComputeImageHash_ShouldReturnDifferentHash_ForDifferentImages()
        {
            // 観点: 異なるピクセルデータを持つ画像からは異なるハッシュ値が返されること
            var img1 = CreateImage(10, 10, 10, 20, 30, 255);
            var img2 = CreateImage(10, 10, 10, 20, 31, 255);

            var hash1 = ScreenshotService.ComputeImageHash(img1);
            var hash2 = ScreenshotService.ComputeImageHash(img2);

            Assert.AreNotEqual(hash1, hash2, "異なる画像のハッシュ値が異なること。");
        }

        [TestMethod]
        public void ProcessClipboardInternal_ShouldSaveConsecutiveDifferentImages_EvenWithinShortInterval()
        {
            // 観点: 短時間（100ms）での連続キャプチャであっても、内容が異なる画像であれば取りこぼさずに保存されること（ヌケモレ防止）
            using var service = new ScreenshotService();
            service.Start(_testSavePath);

            var image1 = CreateImage(10, 10, 10, 20, 30, 255);
            var image2 = CreateImage(10, 10, 40, 50, 60, 255);
            var baseTime = DateTime.Now;

            // 1回目の画像保存
            service.ProcessClipboardInternal(() => true, () => image1, baseTime);
            // 100ms後に別の画像を保存
            service.ProcessClipboardInternal(() => true, () => image2, baseTime.AddMilliseconds(100));

            var files = Directory.GetFiles(_testSavePath, "screenshot_*.png");
            Assert.AreEqual(2, files.Length, "異なる2枚の画像が両方とも保存されていること。");

            service.Stop();
        }

        [TestMethod]
        public void ProcessClipboardInternal_ShouldIgnoreDuplicateImage_WhenSameImageCopied()
        {
            // 観点: 同一の画像が連続して検知された場合、重複保存を防止すること
            using var service = new ScreenshotService();
            service.Start(_testSavePath);

            var image1 = CreateImage(10, 10, 10, 20, 30, 255);
            var baseTime = DateTime.Now;

            // 1回目の画像保存
            service.ProcessClipboardInternal(() => true, () => image1, baseTime);
            // 同一画像を100ms後に再通知
            service.ProcessClipboardInternal(() => true, () => image1, baseTime.AddMilliseconds(100));
            // 同一画像を600ms後に再通知
            service.ProcessClipboardInternal(() => true, () => image1, baseTime.AddMilliseconds(600));

            var files = Directory.GetFiles(_testSavePath, "screenshot_*.png");
            Assert.AreEqual(1, files.Length, "同一の画像は重複して保存されず1枚のみ保存されること。");

            service.Stop();
        }

        [TestMethod]
        public void ProcessClipboardInternal_ShouldIgnoreInvalidImage_WhenImageIsTooSmall()
        {
            // 観点: 極小画像（幅・高さが2px以下）などの無効画像は保存されないこと
            using var service = new ScreenshotService();
            service.Start(_testSavePath);

            var tinyImage = CreateImage(2, 2, 0, 0, 0, 255);
            service.ProcessClipboardInternal(() => true, () => tinyImage, DateTime.Now);

            var files = Directory.GetFiles(_testSavePath, "screenshot_*.png");
            Assert.AreEqual(0, files.Length, "無効な画像は保存されないこと。");

            service.Stop();
        }

        [TestMethod]
        public void NormalizeAndValidateImage_WhenImageTooSmall_ShouldReturnNull()
        {
            // 観点: 幅または高さが2px以下の極小画像は無効としてnullを返すこと
            var tinyImage = CreateImage(2, 2, 0, 0, 0, 255);
            var result = ScreenshotService.NormalizeAndValidateImage(tinyImage);
            Assert.IsNull(result, "2px以下の極小画像はnullが返されること。");
        }

        [TestMethod]
        public void NormalizeAndValidateImage_WhenAllAlphaAreZero_ShouldCorrectAlphaTo255()
        {
            // 観点: 全ピクセルのアルファ値が0（完全透明）の場合、Snipping Tool等の既知仕様対策としてアルファ値を255に補正すること
            // 10x10の画像でRGB=(100, 150, 200), A=0 の画像を作成
            var transparentImage = CreateImage(10, 10, 100, 150, 200, 0);
            var result = ScreenshotService.NormalizeAndValidateImage(transparentImage);

            Assert.IsNotNull(result, "補正された画像が返されること。");
            
            // ピクセルを取り出してアルファ値が255になっているか検証
            byte[] pixels = new byte[result.PixelHeight * result.PixelWidth * 4];
            result.CopyPixels(pixels, result.PixelWidth * 4, 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte b = pixels[i];
                byte g = pixels[i + 1];
                byte r = pixels[i + 2];
                byte a = pixels[i + 3];

                Assert.AreEqual((byte)100, b, "青チャンネルが維持されていること。");
                Assert.AreEqual((byte)150, g, "緑チャンネルが維持されていること。");
                Assert.AreEqual((byte)200, r, "赤チャンネルが維持されていること。");
                Assert.AreEqual((byte)255, a, "アルファ値が255に補正されていること。");
            }
        }

        [TestMethod]
        public void NormalizeAndValidateImage_WhenImageHasValidAlpha_ShouldKeepImage()
        {
            // 観点: 既にアルファ値が不透明(255)の通常の画像はそのまま返されること
            var normalImage = CreateImage(10, 10, 50, 50, 50, 255);
            var result = ScreenshotService.NormalizeAndValidateImage(normalImage);

            Assert.IsNotNull(result, "画像がそのまま返されること。");
            byte[] pixels = new byte[result.PixelHeight * result.PixelWidth * 4];
            result.CopyPixels(pixels, result.PixelWidth * 4, 0);
            Assert.AreEqual((byte)255, pixels[3], "アルファ値が255のままであること。");
        }

        private System.Windows.Media.Imaging.BitmapSource CreateImage(int width, int height, byte b, byte g, byte r, byte a)
        {
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = b;
                pixels[i + 1] = g;
                pixels[i + 2] = r;
                pixels[i + 3] = a;
            }
            return System.Windows.Media.Imaging.BitmapSource.Create(
                width, height, 96, 96,
                System.Windows.Media.PixelFormats.Bgra32,
                null, pixels, stride);
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
