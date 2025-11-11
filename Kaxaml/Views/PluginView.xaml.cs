using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Kaxaml.Plugins;
using Kaxaml.Plugins.Default;
using KaxamlPlugins;
using KaxamlPlugins.DependencyInjection;
using KaxamlPlugins.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kaxaml.Views;

public partial class PluginView
{
    public const string PluginSubDir = "\\plugins";

    public static readonly DependencyProperty PluginsProperty = DependencyProperty.Register(
        nameof(Plugins),
        typeof(List<Plugin>),
        typeof(PluginView),
        new UIPropertyMetadata(new List<Plugin>()));

    private readonly ILogger<PluginView> _logger;
    private readonly References _references;

    private Plugin? _findPlugin;

    public PluginView()
    {
        _logger = ApplicationDiServiceProvider.Services.GetRequiredService<ILogger<PluginView>>();
        _references = ApplicationDiServiceProvider.Services.GetRequiredService<References>();
        _logger.LogInformation("Initializing Plugin View...");
        InitializeComponent();
        LoadPlugins();
        _logger.LogInformation("Initializing Plugin View complete.");
    }

    public List<Plugin> Plugins
    {
        get => (List<Plugin>)GetValue(PluginsProperty);
        set => SetValue(PluginsProperty, value);
    }

    public Plugin? SelectedPlugin
    {
        get => (Plugin?)PluginTabControl.SelectedItem;
        set => PluginTabControl.SelectedItem = value;
    }

    private void LoadPlugins()
    {
        // load the snippets plugin
        _logger.LogInformation("Loading Snippets...");
        var snippets = new Plugin
        {
            Root = new Snippets(),
            Name = "Snippets",
            Description = "Manage a set of commonly used snippets (Ctrl+N)",
            Key = Key.N,
            ModifierKeys = ModifierKeys.Control,
            Icon = LoadIcon(typeof(Plugin), "Images\\emb_tag.png")
        };

        Plugins.Add(snippets);
        ((App)Application.Current).Snippets = (Snippets)snippets.Root;

        //// add the find plugin 
        _logger.LogInformation("Loading Find...");
        var find = new Plugin
        {
            Root = new Find(),
            Name = "Find",
            Description = "Find and replace text in the editor (Ctrl+F, F3)",
            Key = Key.F,
            ModifierKeys = ModifierKeys.Control,
            Icon = LoadIcon(typeof(Plugin), "Images\\find.png")
        };

        Plugins.Add(find);
        _findPlugin = find;
        var pluginDir = ApplicationDiServiceProvider.StartupPath + PluginSubDir;

        // if the plugin directory doesn't exist, then we're done
        _logger.LogInformation("Loading plugin folder: {Folder}", pluginDir);
        if (!Directory.Exists(pluginDir))
        {
            _logger.LogInformation("Plugin folder does not exist.");
            return;
        }

        // get a pointer to the plugin directory
        var d = new DirectoryInfo(pluginDir);

        // load each of the plugins in the directory
        var pluginFiles = d.GetFiles("*.dll");
        _logger.LogInformation("Processing {Count} discovered files...", pluginFiles.Length);

        foreach (var f in pluginFiles)
        {
            _logger.LogInformation("Assembly: {File}", f.FullName);
            var asm = Assembly.LoadFile(f.FullName);
            var types = asm.GetExportedTypes();
            foreach (var typ in types)
            {
                _logger.LogInformation("Type: {Type}", typ.FullName);
                if (!typeof(UserControl).IsAssignableFrom(typ))
                {
                    _logger.LogWarning("Skipping non UserControl in Plugin folder...");
                    continue;
                }

                var a = typ
                    .GetCustomAttributes(typeof(PluginAttribute), false)
                    .Cast<PluginAttribute>()
                    .SingleOrDefault();

                if (a is null)
                {
                    _logger.LogInformation("Skipping unattributed type...");
                    continue;
                }

                _logger.LogInformation("Loading plugin: {Attribute}", a.ToString());
                var userControl = (UserControl?)Activator.CreateInstance(typ);
                if (userControl is not null)
                    Plugins.Add(new Plugin
                    {
                        Root = userControl,
                        Name = a.Name,
                        Description = a.Description,
                        Key = a.Key,
                        ModifierKeys = a.ModifierKeys,
                        Icon = LoadIcon(typ, a.Icon)
                    });
            }
        }

        var references = new Plugin
        {
            Root = _references,
            Name = "References",
            Description = "Add Assembly References to Kaxaml",
            Key = Key.N,
            ModifierKeys = ModifierKeys.Control,
            Icon = LoadIcon(typeof(Plugin), "Images\\package_link.png")
        };

        Plugins.Add(references);

        _logger.LogInformation("Plugin load complete with {Count} plugins added.", Plugins.Count);

        // add the settings plugin (we always want this to be at the end)
        var settings = new Plugin
        {
            Root = new Settings(),
            Name = "Settings",
            Description = "Modify program settings and options (Ctrl+E)",
            Key = Key.E,
            ModifierKeys = ModifierKeys.Control,
            Icon = LoadIcon(typeof(Plugin), "Images\\cog.png")
        };

        Plugins.Add(settings);

        // add the about plugin 
        var about = new Plugin
        {
            Root = new About(),
            Name = "About",
            Description = "All about Kaxaml",
            Icon = LoadIcon(typeof(Plugin), "Images\\kaxaml.png")
        };

        Plugins.Add(about);
    }

    private ImageSource? LoadIcon(Type typ, string icon)
    {
        var asm = Assembly.GetAssembly(typ);
        var iconString = typ.Namespace + '.' + icon.Replace('\\', '.');
        var myStream = asm?.GetManifestResourceStream(iconString);

        if (myStream == null)
        {
            iconString = typ.Name + '.' + icon.Replace('\\', '.');
            myStream = asm?.GetManifestResourceStream(iconString);
        }

        if (myStream == null)
        {
            iconString = "Kaxaml.Images.package.png";
            myStream = asm?.GetManifestResourceStream(iconString);
        }

        if (myStream != null)
        {
            var bitmapDecoder = new PngBitmapDecoder(myStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            return bitmapDecoder.Frames[0];
        }

        return null;
    }

    public void OpenPlugin(Key key, ModifierKeys modifierKeys)
    {
        var plugins = Plugins
            .Where(p => modifierKeys == p.ModifierKeys && key == p.Key)
            .ToList();

        foreach (var p in plugins)
            try
            {
                var t = (TabItem)p.Root.Parent;
                t.IsSelected = true;
                t.Focus();

                UpdateLayout();

                if (t.Content is FrameworkElement element) element.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            }
            catch (Exception ex)
            {
                if (ex.IsCriticalException()) throw;
            }
    }

    internal Plugin? GetFindPlugin() => _findPlugin;
}