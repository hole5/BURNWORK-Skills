using System.Windows;

namespace MochangStudio;

public partial class StudioDialog : Window
{
    public StudioDialog()
    {
        InitializeComponent();
    }

    public static void Alert(Window? owner, string title, string body, string ok = "确定")
    {
        var dialog = Create(owner, title, body, ok, cancel: null);
        dialog.ShowDialog();
    }

    public static bool Confirm(
        Window? owner,
        string title,
        string body,
        string ok = "确定",
        string cancel = "取消")
    {
        var dialog = Create(owner, title, body, ok, cancel);
        return dialog.ShowDialog() == true;
    }

    private static StudioDialog Create(Window? owner, string title, string body, string ok, string? cancel)
    {
        var dialog = new StudioDialog
        {
            Title = title,
            Owner = owner,
            WindowStartupLocation = owner is null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner,
            ShowInTaskbar = owner is null,
        };
        dialog.Caption.Title = title;
        dialog.BodyText.Text = body;
        dialog.OkBtn.Content = ok;
        if (string.IsNullOrEmpty(cancel))
        {
            dialog.CancelBtn.Visibility = Visibility.Collapsed;
        }
        else
        {
            dialog.CancelBtn.Content = cancel;
            dialog.CancelBtn.IsCancel = true;
        }
        return dialog;
    }

    private void OnOk(object sender, RoutedEventArgs e) => DialogResult = true;

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
