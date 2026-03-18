using System;

namespace EvidenceSupportTool.Services
{
    /// <summary>
    /// クリップボード監視によるスクリーンショット保存の契約を定義します。
    /// </summary>
    public interface IScreenshotService
    {
        /// <summary>
        /// クリップボードの監視を開始します。
        /// </summary>
        /// <param name="savePath">画像を保存するフォルダのパス</param>
        void Start(string savePath);

        /// <summary>
        /// クリップボードの監視を停止します。
        /// </summary>
        void Stop();
    }
}
