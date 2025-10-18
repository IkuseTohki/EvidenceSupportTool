using EvidenceSupportTool.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; // For Enumerable.Empty<T>()

namespace EvidenceSupportTool.Services
{
    /// <summary>
    /// IEvidenceExtractionServiceの実装クラスです。
    /// </summary>
    public class EvidenceExtractionService : IEvidenceExtractionService
    {
        public void CreateSnapshot(string snapshotPath, IEnumerable<MonitoringTarget> targets)
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
                        filesToCopy = Enumerable.Empty<string>(); // ディレクトリが存在しない場合は空
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
                        filesToCopy = Enumerable.Empty<string>(); // ファイルが存在しない場合は空
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

        public bool ExtractEvidence(string snapshot1Path, string snapshot2Path, string evidencePath)
        {
            throw new NotImplementedException();
        }

        private string ResolveDateFormats(string pathPattern)
        {
            var today = DateTime.Now;
            return pathPattern.Replace("{YYYY}", today.ToString("yyyy"))
                                .Replace("{MM}", today.ToString("MM"))
                                .Replace("{DD}", today.ToString("dd"));
        }
    }
}
