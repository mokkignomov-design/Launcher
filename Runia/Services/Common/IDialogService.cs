
namespace Runia.Services.Common
{
    public interface IDialogService
    {
        void ShowMessage(string message, string title = "Сообщение");
        void ShowError(string message, string title = "Ошибка");
        bool ShowConfirmation(string message, string title = "Подтверждение");
        void ShowMainWindow();
        void ShowLoginWindow();
        void CloseLoginWindow();
        void CloseMainWindow();
    }
}
