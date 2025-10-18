using System.Windows;

namespace EvidenceSupportTool.Services
{
    public class UserInteractionService : IUserInteractionService
    {
        public void ShowMessage(string message)
        {
            MessageBox.Show(message, "情報", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ShowError(string message)
        {
            MessageBox.Show(message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
