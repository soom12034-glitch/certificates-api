using System.Windows;

namespace BlueMax.Presentation.Wpf.Views;

public partial class TextInputWindow : Window
{
    public string? Answer { get; private set; }

    public TextInputWindow(string prompt)
    {
        InitializeComponent();
        PromptText.Text = prompt;
        Loaded += (_, __) =>
        {
            AnswerBox.Focus();
            AnswerBox.SelectAll();
        };
    }

    void OnOkClick(object sender, RoutedEventArgs e)
    {
        Answer = AnswerBox.Text;
        DialogResult = true;
    }

    void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
