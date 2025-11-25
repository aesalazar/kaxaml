using System.Windows;
using System.Windows.Controls;
using KaxamlPlugins;

namespace Kaxaml.Plugins;

public partial class Find
{
    public Find()
    {
        InitializeComponent();
    }

    private void DoFind(object sender, RoutedEventArgs e)
    {
        KaxamlInfo.Editor?.Find(FindText.Text);
    }

    private void DoReplace(object sender, RoutedEventArgs e)
    {
        KaxamlInfo.Editor?.Replace(FindText.Text, ReplaceText.Text, Selection?.IsChecked is true);
    }

    private void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox t)
            if (!string.IsNullOrEmpty(t.Text))
                t.SelectAll();
    }
}