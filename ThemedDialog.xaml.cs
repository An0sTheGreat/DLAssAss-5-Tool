using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DLSS5ManAger;

public partial class ThemedDialog : Window
{
    private string? _value;

    private ThemedDialog(Window owner, string title, string message, MessageBoxImage icon)
    {
        InitializeComponent();
        Owner = owner;
        Title = title;
        TitleText.Text = title;
        MessageText.Text = message;
        var (symbol, color) = icon switch
        {
            MessageBoxImage.Error => ("✕", "#FF5F63"),
            MessageBoxImage.Warning => ("!", "#F2B84B"),
            MessageBoxImage.Question => ("?", "#76B900"),
            _ => ("✓", "#76B900")
        };
        IconText.Text = symbol;
        IconText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        IconBorder.BorderBrush = IconText.Foreground;
    }

    public static void Show(Window owner, string title, string message,
        MessageBoxImage icon = MessageBoxImage.Information) =>
        new ThemedDialog(owner, title, message, icon).ShowDialog();

    public static void ShowDetails(Window owner, string title, string message,
        IReadOnlyList<string> updated, IReadOnlyList<string> skipped, IReadOnlyList<string> failed,
        MessageBoxImage icon = MessageBoxImage.Information)
    {
        var dialog = new ThemedDialog(owner, title, message, icon);
        dialog.DetailsExpander.Visibility = Visibility.Visible;
        dialog.AddDetailsGroup("Updated", updated, "#A7E66B");
        dialog.AddDetailsGroup("Skipped", skipped, "#F2B84B");
        dialog.AddDetailsGroup("Failed", failed, "#FF8A8E");
        dialog.ShowDialog();
    }

    public static bool Confirm(Window owner, string title, string message,
        MessageBoxImage icon = MessageBoxImage.Question)
    {
        var dialog = new ThemedDialog(owner, title, message, icon);
        dialog.PrimaryButton.Content = "YES";
        dialog.CancelButton.Content = "NO";
        dialog.CancelButton.Visibility = Visibility.Visible;
        return dialog.ShowDialog() == true;
    }

    public static string? Prompt(Window owner, string title, string message, string initialValue)
    {
        var dialog = new ThemedDialog(owner, title, message, MessageBoxImage.Question);
        dialog.InputTextBox.Text = initialValue;
        dialog.InputTextBox.Visibility = Visibility.Visible;
        dialog.CancelButton.Visibility = Visibility.Visible;
        dialog.Loaded += (_, _) => { dialog.InputTextBox.Focus(); dialog.InputTextBox.SelectAll(); };
        return dialog.ShowDialog() == true ? dialog._value : null;
    }

    public static string? Choose(Window owner, string title, string message, IEnumerable<string> choices)
    {
        var dialog = new ThemedDialog(owner, title, message, MessageBoxImage.Question);
        dialog.PrimaryButton.Visibility = Visibility.Collapsed;
        dialog.CancelButton.Visibility = Visibility.Visible;
        dialog.ChoicePanel.Visibility = Visibility.Visible;
        foreach (var choice in choices)
        {
            var button = new Button
            {
                Content = choice,
                Tag = choice,
                MinWidth = 82,
                Background = (Brush)Application.Current.Resources["AccentBrush"],
                BorderBrush = (Brush)Application.Current.Resources["AccentBrush"],
                Foreground = new SolidColorBrush(Color.FromRgb(0x10, 0x12, 0x16)),
                FontWeight = FontWeights.Bold
            };
            button.Click += (_, _) =>
            {
                dialog._value = (string)button.Tag;
                dialog.DialogResult = true;
            };
            dialog.ChoicePanel.Children.Add(button);
        }
        return dialog.ShowDialog() == true ? dialog._value : null;
    }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
        _value = InputTextBox.Visibility == Visibility.Visible ? InputTextBox.Text : null;
        DialogResult = true;
    }

    private void AddDetailsGroup(string heading, IReadOnlyList<string> items, string color)
    {
        DetailsPanel.Children.Add(new TextBlock
        {
            Text = $"{heading} ({items.Count})",
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, DetailsPanel.Children.Count == 0 ? 0 : 12, 0, 4)
        });
        DetailsPanel.Children.Add(new TextBlock
        {
            Text = items.Count == 0 ? "None" : string.Join(Environment.NewLine, items.Select(item => "• " + item)),
            Foreground = (Brush)Application.Current.Resources["MutedBrush"],
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 18
        });
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) DialogResult = false;
    }
}
