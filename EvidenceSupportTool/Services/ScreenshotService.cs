using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Threading;

namespace EvidenceSupportTool.Services
{
    /// <summary>
    /// IScreenshotServiceの実装クラスです。
    /// クリップボードを監視し、画像がコピーされた際にファイルとして保存する責務を持ちます。
    /// </summary>
    public class ScreenshotService : IScreenshotService, IDisposable
    {
        private string? _savePath;
        private HwndSource? _hwndSource;
        private Thread? _messageLoopThread;
        private readonly ManualResetEvent _initEvent = new ManualResetEvent(false);
        private DateTime _lastSaveTime = DateTime.MinValue;
        private string? _lastSavedHash;
        private readonly object _saveLock = new object();

        private const int WM_CLIPBOARDUPDATE = 0x031D;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        public void Start(string savePath)
        {
            if (string.IsNullOrEmpty(savePath))
            {
                throw new ArgumentException("Save path cannot be null or empty.", nameof(savePath));
            }

            _savePath = savePath;

            if (!Directory.Exists(_savePath))
            {
                Directory.CreateDirectory(_savePath);
            }

            _lastSavedHash = null;
            _lastSaveTime = DateTime.MinValue;

            // メッセージループ用のスレッドを開始
            _messageLoopThread = new Thread(RunMessageLoop);
            _messageLoopThread.SetApartmentState(ApartmentState.STA);
            _messageLoopThread.IsBackground = true;
            _messageLoopThread.Start();

            // 初期化完了を待機
            _initEvent.WaitOne(5000);
        }

        public void Stop()
        {
            if (_hwndSource != null)
            {
                _hwndSource.Dispatcher.Invoke(() =>
                {
                    RemoveClipboardFormatListener(_hwndSource.Handle);
                    _hwndSource.Dispose();
                    _hwndSource = null;
                });
            }

            if (_messageLoopThread != null && _messageLoopThread.IsAlive)
            {
                _messageLoopThread.Join(1000);
            }

            _lastSavedHash = null;
            _initEvent.Reset();
        }

        private void RunMessageLoop()
        {
            // ウィンドウを完全に非表示（メッセージ専用）に設定
            var parameters = new HwndSourceParameters("ClipboardDetector")
            {
                WindowStyle = 0,
                Width = 0,
                Height = 0,
                PositionX = 0,
                PositionY = 0,
                ParentWindow = new IntPtr(-3) // HWND_MESSAGE: メッセージ専用ウィンドウ
            };

            _hwndSource = new HwndSource(parameters);
            _hwndSource.AddHook(HwndHook);

            AddClipboardFormatListener(_hwndSource.Handle);

            _initEvent.Set();

            // メッセージループを開始
            System.Windows.Threading.Dispatcher.Run();
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_CLIPBOARDUPDATE)
            {
                OnClipboardChanged();
            }
            return IntPtr.Zero;
        }

        private void OnClipboardChanged()
        {
            // クリップボードがビジーの場合に備え、リトライ付きで取得を試みる
            ProcessClipboardInternal(
                () => ExecuteWithRetry(() => Clipboard.ContainsImage()) == true,
                () => ExecuteWithRetry(() => Clipboard.GetImage()),
                DateTime.Now);
        }

        /// <summary>
        /// テスト用にクリップボードの判定と保存ロジックを分離した内部メソッドです。
        /// </summary>
        internal void ProcessClipboardInternal(Func<bool> containsImage, Func<BitmapSource?> getImage, DateTime now)
        {
            try
            {
                if (!containsImage())
                {
                    return;
                }

                var rawImage = getImage();
                if (rawImage == null)
                {
                    return;
                }

                // アルファチャンネル補正と有効性検証（極小画像や無効画像を除外）
                var validImage = NormalizeAndValidateImage(rawImage);
                if (validImage == null)
                {
                    return;
                }

                // 画像ハッシュによる重複判定（短時間の連続別キャプチャは漏らさず、同一画像のみスキップ）
                var imageHash = ComputeImageHash(validImage);

                lock (_saveLock)
                {
                    if (imageHash == _lastSavedHash)
                    {
                        return;
                    }

                    SaveImage(validImage);
                    _lastSavedHash = imageHash;
                    _lastSaveTime = now;
                }
            }
            catch (Exception)
            {
                // 想定外のエラーは無視
            }
        }

        /// <summary>
        /// クリップボードの排他ロック競合に対処するため、指定された操作をリトライ付きで実行します。
        /// </summary>
        internal static T? ExecuteWithRetry<T>(Func<T> action, int maxRetries = 5, int delayMs = 50)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return action();
                }
                catch (Exception)
                {
                    if (attempt == maxRetries)
                    {
                        return default;
                    }
                    Thread.Sleep(delayMs);
                }
            }
            return default;
        }

        /// <summary>
        /// 画像のピクセルデータから SHA256 ハッシュ値を計算します。
        /// </summary>
        internal static string ComputeImageHash(BitmapSource bitmapSource)
        {
            BitmapSource source = bitmapSource;
            if (source.Format != System.Windows.Media.PixelFormats.Bgra32)
            {
                source = new FormatConvertedBitmap(source, System.Windows.Media.PixelFormats.Bgra32, null, 0);
            }

            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            source.CopyPixels(pixels, stride, 0);

            using var sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(pixels);
            return BitConverter.ToString(hash).Replace("-", "");
        }

        private void SaveImage(BitmapSource bitmapSource)
        {
            if (string.IsNullOrEmpty(_savePath)) return;

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            var fileName = $"screenshot_{timestamp}.png";
            var filePath = Path.Combine(_savePath, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(fileStream);
            }
        }

        public void Dispose()
        {
            Stop();
            _initEvent.Dispose();
        }

        /// <summary>
        /// 画像の正規化と有効性検証を行います。
        /// 極小画像の除外や、Snipping Tool等によるアルファ値0（完全透明）の補正を行います。
        /// </summary>
        internal static BitmapSource? NormalizeAndValidateImage(BitmapSource? bitmapSource)
        {
            if (bitmapSource == null) return null;

            // 2px以下の極小画像は除外
            if (bitmapSource.PixelWidth <= 2 || bitmapSource.PixelHeight <= 2)
            {
                return null;
            }

            BitmapSource source = bitmapSource;
            if (source.Format != System.Windows.Media.PixelFormats.Bgra32)
            {
                source = new FormatConvertedBitmap(source, System.Windows.Media.PixelFormats.Bgra32, null, 0);
            }

            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            source.CopyPixels(pixels, stride, 0);

            // アルファチャンネルの検査
            bool allAlphaZero = true;
            for (int i = 3; i < pixels.Length; i += 4)
            {
                if (pixels[i] != 0)
                {
                    allAlphaZero = false;
                    break;
                }
            }

            if (allAlphaZero)
            {
                // 全ピクセルのアルファが0の場合、Snipping Tool等の既知不具合対策として不透明(255)に補正
                for (int i = 3; i < pixels.Length; i += 4)
                {
                    pixels[i] = 255;
                }

                var corrected = BitmapSource.Create(
                    width, height,
                    source.DpiX, source.DpiY,
                    System.Windows.Media.PixelFormats.Bgra32,
                    null, pixels, stride);
                corrected.Freeze();
                return corrected;
            }

            if (source.CanFreeze && !source.IsFrozen)
            {
                source.Freeze();
            }

            return source;
        }
    }
}
