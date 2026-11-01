using System.Windows;
using System.Windows.Media;

namespace PWSV.Client.Views;

public partial class MessageDialog : Window
{
    public MessageDialog(string title, string message, MessageDialogKind kind)
    {
        InitializeComponent();
        Title = title;
        HeadingText.Text = title;
        MessageText.Text = message;

        var (glyph, foreground, background) = kind switch
        {
            MessageDialogKind.Error => ("\uE783", "ErrorBrush", "ErrorSoftBrush"),
            MessageDialogKind.Confirmation => ("\uE897", "WarningBrush", "WarningSoftBrush"),
            _ => ("\uE946", "PrimaryBrush", "PrimarySoftBrush")
        };
        IconGlyph.Text = glyph;
        IconGlyph.Foreground = (Brush)FindResource(foreground);
        IconBadge.Background = (Brush)FindResource(background);

        var isConfirmation = kind == MessageDialogKind.Confirmation;
        CancelButton.Visibility = isConfirmation ? Visibility.Visible : Visibility.Collapsed;
        ConfirmButton.Content = isConfirmation ? "Так" : "Гаразд";
        ConfirmButton.IsCancel = !isConfirmation;
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
