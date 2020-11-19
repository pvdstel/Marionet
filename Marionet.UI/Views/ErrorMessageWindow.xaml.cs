using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;

namespace Marionet.UI.Views
{
    public class ErrorMessageWindow : Window
    {
        public ErrorMessageWindow() : this("(unknown error)") { }

        public ErrorMessageWindow(string details)
        {
            this.InitializeComponent();

            this.FindControl<TextBlock>("detailsText").Text = details;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        void OnCloseButtonClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        void OnExitButtonClick(object? sender, RoutedEventArgs e)
        {
            Environment.Exit(1);
        }
    }
}
