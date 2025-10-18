namespace EvidenceSupportTool.Services
{
    /// <summary>
    /// ユーザーへの通知（ダイアログ表示など）を行うためのインターフェースです。
    /// </summary>
    public interface IUserInteractionService
    {
        /// <summary>
        /// 情報メッセージを表示します。
        /// </summary>
        /// <param name="message">表示するメッセージ。</param>
        void ShowMessage(string message);

        /// <summary>
        /// エラーメッセージを表示します。
        /// </summary>
        /// <param name="message">表示するエラーメッセージ。</param>
        void ShowError(string message);
    }
}
