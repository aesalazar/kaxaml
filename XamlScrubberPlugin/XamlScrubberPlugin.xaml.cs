using System.Windows;
using System.Windows.Input;
using Kaxaml.Plugins.XamlScrubber.Properties;
using Kaxaml.Plugins.XamlScrubber.XamlPrettyPrint;
using KaxamlPlugins;
using KaxamlPlugins.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kaxaml.Plugins.XamlScrubber;

[Plugin(
    Name = "Xaml Scrubber",
    Icon = "Images\\page_lightning.png",
    Description = "Reformat and cleanup up your XAML (Ctrl+K)",
    ModifierKeys = ModifierKeys.Control,
    Key = Key.K
)]
public partial class XamlScrubberPlugin
{
    private readonly ILogger<XamlScrubberPlugin> _logger;

    public XamlScrubberPlugin()
    {
        InitializeComponent();

        _logger = ApplicationDiServiceProvider.Services.GetRequiredService<ILogger<XamlScrubberPlugin>>();
        var binding = new CommandBinding(GoCommand);
        binding.Executed += Go_Executed;
        binding.CanExecute += Go_CanExecute;
        InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.D, ModifierKeys.Control, "Ctrl+D")));
        CommandBindings.Add(binding);
    }

    private void Go_Click(object sender, RoutedEventArgs e)
    {
        Go();
    }

    private void Go()
    {
        if (KaxamlInfo.Editor is null) return;
        var s = KaxamlInfo.Editor.Text;
        var length = s.Length;

        var config = new XamlPrettyPrintConfig(
            Settings.Default.AttributeCounteTolerance,
            Settings.Default.ReorderAttributes,
            Settings.Default.ReducePrecision,
            Settings.Default.Precision,
            Settings.Default.RemoveCommonDefaultValues,
            Settings.Default.SpaceCount,
            Settings.Default.ConvertTabsToSpaces
        );

        var prettyPrinter = new XamlPrettyPrinter(config);
        s = prettyPrinter.Indent(s);
        s = prettyPrinter.ReducePrecision(s);

        KaxamlInfo.Editor.ReplaceAllText(s);
        _logger.LogDebug("Text length changed from {Original} to {Updated}", length, s.Length);
    }

    #region GoCommand

    public static readonly RoutedUICommand GoCommand = new("_Go", "GoCommand", typeof(XamlScrubberPlugin));

    private void Go_Executed(object sender, ExecutedRoutedEventArgs args)
    {
        if (Equals(sender, this)) Go();
    }

    private void Go_CanExecute(object sender, CanExecuteRoutedEventArgs args)
    {
        if (Equals(sender, this)) args.CanExecute = true;
    }

    #endregion
}