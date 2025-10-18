using EvidenceSupportTool.Models;
using System;
using System.Collections.Generic;

namespace EvidenceSupportTool.Services
{
    /// <summary>
    /// IEvidenceExtractionServiceの実装クラスです。
    /// </summary>
    public class EvidenceExtractionService : IEvidenceExtractionService
    {
        public void CreateSnapshot(string snapshotPath, IEnumerable<MonitoringTarget> targets)
        {
            throw new NotImplementedException();
        }

        public void ExtractEvidence(string snapshot1Path, string snapshot2Path, string evidencePath)
        {
            throw new NotImplementedException();
        }
    }
}
