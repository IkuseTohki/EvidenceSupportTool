using System;
using System.IO;
using System.Runtime.InteropServices;
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
            // 実際のクリップボード操作を伴う呼び出し
            ProcessClipboardInternal(
                () => Clipboard.ContainsImage(),
                () => Clipboard.GetImage(),
                DateTime.Now);
        }

        /// <summary>
        /// テスト用にクリップボードの判定と保存ロジックを分離した内部メソッドです。
        /// </summary>
        internal void ProcessClipboardInternal(Func<bool> containsImage, Func<BitmapSource?> getImage, DateTime now)
        {
            try
            {
                if (containsImage())
                {
                    lock (_saveLock)
                    {
                        // 前回の保存から500ms以内なら無視（二重保存対策）
                        if ((now - _lastSaveTime).TotalMilliseconds < 500)
                        {
                            return;
                        }

                        var bitmapSource = getImage();
                        if (bitmapSource != null)
                        {
                            SaveImage(bitmapSource);
                            _lastSaveTime = now;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // クリップボードアクセスエラーなどは無視
            }
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
    }
}
