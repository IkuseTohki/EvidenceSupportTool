using EvidenceSupportTool.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace EvidenceSupportTool.Services
{
    /// <summary>
    /// ILogCollectionServiceの実装クラスです。
    /// 指定された収集対象のログファイルを、そのままコピーして指定フォルダへ収集します。
    /// </summary>
    public class LogCollectionService : ILogCollectionService
    {
        private readonly IUserInteractionService _userInteractionService;

        /// <summary>
        /// LogCollectionServiceの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="userInteractionService">エラー通知に使用するサービス。</param>
        public LogCollectionService(IUserInteractionService userInteractionService)
        {
            _userInteractionService = userInteractionService;
        }

        /// <summary>
        /// 指定された収集対象のファイルを collectPath へコピーします。
        /// パスパターン内の日付フォーマット（{YYYY}, {MM}, {DD}）は実行時の日付で解決されます。
        /// ワイルドカード（*）はファイル名検索に使用されます。
        /// </summary>
        /// <param name="collectPath">収集先のルートフォルダパス。</param>
        /// <param name="targets">収集対象のリスト。</param>
        public void Collect(string collectPath, IEnumerable<CollectTarget> targets)
        {
            try
            {
                foreach (var target in targets)
                {
                    string resolvedPattern = ResolveDateFormats(target.PathPattern);

                    IEnumerable<string> filesToCopy;

                    if (resolvedPattern.Contains("*"))
                    {
                        // ワイルドカードを含む場合
                        string? directory = Path.GetDirectoryName(resolvedPattern);
                        string? searchPattern = Path.GetFileName(resolvedPattern);

                        if (directory != null && searchPattern != null && Directory.Exists(directory))
                        {
                            filesToCopy = Directory.GetFiles(directory, searchPattern);
                        }
                        else
                        {
                            // ディレクトリが存在しない場合はスキップ
                            continue;
                        }
                    }
                    else
                    {
                        // ワイルドカードを含まない場合（単一ファイル）
                        if (File.Exists(resolvedPattern))
                        {
                            filesToCopy = new[] { resolvedPattern };
                        }
                        else
                        {
                            // ファイルが存在しない場合はスキップ
                            continue;
                        }
                    }

                    // ファイルをコピー
                    foreach (string sourcePath in filesToCopy)
                    {
                        // ターゲットごとのサブディレクトリを作成
                        string targetDir = Path.Combine(collectPath, target.Name);
                        Directory.CreateDirectory(targetDir);

                        string destPath = Path.Combine(targetDir, Path.GetFileName(sourcePath));
                        File.Copy(sourcePath, destPath, overwrite: true);
                    }
                }
            }
            catch (Exception ex)
            {
                _userInteractionService.ShowError($"ログファイルの収集中にエラーが発生しました: {ex.Message}");
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
            return pathPattern
                .Replace("{YYYY}", today.ToString("yyyy"))
                .Replace("{MM}", today.ToString("MM"))
                .Replace("{DD}", today.ToString("dd"));
        }
    }
}
