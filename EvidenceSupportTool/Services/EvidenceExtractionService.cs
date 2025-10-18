using EvidenceSupportTool.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EvidenceSupportTool.Services
{
    /// <summary>
    /// IEvidenceExtractionServiceの実装クラスです。
    /// </summary>
    public class EvidenceExtractionService : IEvidenceExtractionService
    {
        private readonly IUserInteractionService _userInteractionService;

        public EvidenceExtractionService(IUserInteractionService userInteractionService)
        {
            _userInteractionService = userInteractionService;
        }

        /// <summary>
        /// 監視対象のログファイルのスナップショットを作成します。
        /// </summary>
        /// <param name="snapshotPath">スナップショットを保存するディレクトリのパス。</param>
        /// <param name="targets">監視対象のリスト。</param>
        public void CreateSnapshot(string snapshotPath, IEnumerable<MonitoringTarget> targets)
        {
            try
            {
                // スナップショットの親ディレクトリを作成
                Directory.CreateDirectory(snapshotPath);

                foreach (var target in targets)
                {
                    // パスパターンを解決 (日付フォーマット)
                    string resolvedPathPattern = ResolveDateFormats(target.PathPattern);

                    IEnumerable<string> filesToCopy;

                    if (resolvedPathPattern.Contains("*"))
                    {
                        // ワイルドカードを含む場合
                        string directory = Path.GetDirectoryName(resolvedPathPattern);
                        string searchPattern = Path.GetFileName(resolvedPathPattern);

                        if (Directory.Exists(directory))
                        {
                            filesToCopy = Directory.GetFiles(directory, searchPattern);
                        }
                        else
                        {
                            filesToCopy = Enumerable.Empty<string>(); // ディレクトリが存在しない場合は空のリストを返す
                        }
                    }
                    else
                    {
                        // ワイルドカードを含まない場合 (単一ファイル)
                        if (File.Exists(resolvedPathPattern))
                        {
                            filesToCopy = new List<string> { resolvedPathPattern };
                        }
                        else
                        {
                            filesToCopy = Enumerable.Empty<string>(); // ファイルが存在しない場合は空のリストを返す
                        }
                    }

                    foreach (string sourcePath in filesToCopy)
                    {
                        // ターゲットごとのサブディレクトリを作成
                        string targetDir = Path.Combine(snapshotPath, target.Name);
                        Directory.CreateDirectory(targetDir);

                        // ファイルをコピー
                        string destPath = Path.Combine(targetDir, Path.GetFileName(sourcePath));
                        File.Copy(sourcePath, destPath, true); // trueで上書きを許可
                    }
                }
            }
            catch (Exception ex)
            {
                _userInteractionService.ShowError($"スナップショットの作成中にエラーが発生しました: {ex.Message}");
            }
        }

        /// <summary>
        /// 2つのスナップショットを比較し、差分（新規追加されたログエントリ）を抽出してevidencePathに保存します。
        /// </summary>
        /// <param name="snapshot1Path">監視開始時のスナップショットのパス。</param>
        /// <param name="snapshot2Path">監視停止時のスナップショットのパス。</param>
        /// <param name="evidencePath">エビデンスを保存するパス。</param>
        /// <param name="keepSnapshot">スナップショットを保持するかどうか。</param>
        /// <returns>差分が検出された場合はtrue、それ以外はfalse。</returns>
        public bool ExtractEvidence(string snapshot1Path, string snapshot2Path, string evidencePath, bool keepSnapshot)
        {
            bool hasDifference = false;
            try
            {
                // snapshot1とsnapshot2のファイルリストを相対パスで取得
                var snapshot1Files = GetRelativeFilePaths(snapshot1Path);
                var snapshot2Files = GetRelativeFilePaths(snapshot2Path);

                // 変更されたファイルと新規追加されたファイルを検出
                foreach (var file2 in snapshot2Files)
                {
                    string fullPath2 = Path.Combine(snapshot2Path, file2.Key);
                    string fullPath1 = Path.Combine(snapshot1Path, file2.Key);

                    if (snapshot1Files.ContainsKey(file2.Key))
                    {
                        // 両方に存在するファイル: snapshot1のファイルサイズ以降のデータを抽出
                        long snapshot1FileSize = new FileInfo(fullPath1).Length;

                        using (FileStream fs2 = new FileStream(fullPath2, FileMode.Open, FileAccess.Read))
                        {
                            if (fs2.Length > snapshot1FileSize)
                            {
                                fs2.Seek(snapshot1FileSize, SeekOrigin.Begin);
                                byte[] buffer = new byte[fs2.Length - snapshot1FileSize];
                                fs2.Read(buffer, 0, buffer.Length);

                                // 抽出したデータをevidenceディレクトリに保存
                                CopyBytesToEvidence(buffer, evidencePath, file2.Key);
                                hasDifference = true;
                            }
                        }
                    }
                    else
                    {
                        // snapshot1に存在せずsnapshot2にのみ存在するファイル: 新規追加
                        CopyFileToEvidence(fullPath2, evidencePath, file2.Key);
                        hasDifference = true;
                    }
                }

                // snapshot1にのみ存在するファイル (削除されたファイル) はevidenceにはコピーしない

                // スナップショットの削除
                if (!keepSnapshot)
                {
                    try
                    {
                        if (Directory.Exists(snapshot1Path))
                        {
                            Directory.Delete(snapshot1Path, true);
                        }
                        if (Directory.Exists(snapshot2Path))
                        {
                            Directory.Delete(snapshot2Path, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        _userInteractionService.ShowError($"スナップショットの削除中にエラーが発生しました: {ex.Message}");
                    }
                }

                return hasDifference;
            }
            catch (Exception ex)
            {
                _userInteractionService.ShowError($"エビデンスの抽出中にエラーが発生しました: {ex.Message}");
                return false; // エラー発生時は差分なしとして扱うか、別途エラー状態を返すか検討
            }
        }

        /// <summary>
        /// パスパターン内の日付フォーマット（{YYYY}, {MM}, {DD}）を現在の日付で解決します。
        /// </summary>
        /// <param name="pathPattern">日付フォーマットを含むパスパターン。</param>
        /// <returns>日付フォーマットが解決されたパス。</returns>
        private string ResolveDateFormats(string pathPattern)
        {
            var today = DateTime.Now;
            return pathPattern.Replace("{YYYY}", today.ToString("yyyy"))
                                .Replace("{MM}", today.ToString("MM"))
                                .Replace("{DD}", today.ToString("dd"));
        }

        /// <summary>
        /// 指定されたディレクトリ内の全ファイルを相対パスとフルパスの辞書として取得します。
        /// </summary>
        private Dictionary<string, string> GetRelativeFilePaths(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"指定されたスナップショットディレクトリが見つかりません: {directoryPath}");
            }

            return Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories)
                            .ToDictionary(
                                fullPath => Path.GetRelativePath(directoryPath, fullPath),
                                fullPath => fullPath
                            );
        }

        

        /// <summary>
        /// ファイルをevidenceディレクトリにコピーします。
        /// </summary>
        /// <param name="sourceFullPath">コピー元ファイルのフルパス。</param>
        /// <param name="evidenceRootPath">エビデンスルートディレクトリのパス。</param>
        /// <param name="relativePath">エビデンスルートからの相対パス。</param>
        private void CopyFileToEvidence(string sourceFullPath, string evidenceRootPath, string relativePath)
        {
            // 差分が検出された場合にのみevidencePath ディレクトリを作成
            if (!Directory.Exists(evidenceRootPath))
            {
                Directory.CreateDirectory(evidenceRootPath);
            }

            string destFullPath = Path.Combine(evidenceRootPath, relativePath);
            string destDir = Path.GetDirectoryName(destFullPath);

            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }
            File.Copy(sourceFullPath, destFullPath, true);
        }

        /// <summary>
        /// バイト配列をevidenceディレクトリ内のファイルに追記または新規作成します。
        /// </summary>
        /// <param name="bytes">追記するバイトデータ。</param>
        /// <param name="evidenceRootPath">エビデンスルートディレクトリのパス。</param>
        /// <param name="relativePath">エビデンスルートからの相対パス。</param>
        private void CopyBytesToEvidence(byte[] bytes, string evidenceRootPath, string relativePath)
        {
            if (bytes == null || bytes.Length == 0) return;

            // 差分が検出された場合にのみevidencePath ディレクトリを作成
            if (!Directory.Exists(evidenceRootPath))
            {
                Directory.CreateDirectory(evidenceRootPath);
            }

            string destFullPath = Path.Combine(evidenceRootPath, relativePath);
            string destDir = Path.GetDirectoryName(destFullPath);

            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            // ファイルが存在する場合は追記、存在しない場合は新規作成
            using (FileStream fs = new FileStream(destFullPath, FileMode.Append, FileAccess.Write))
            {
                fs.Write(bytes, 0, bytes.Length);
            }
        }
    }
}