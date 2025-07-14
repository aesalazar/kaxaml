using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Kaxaml;

public class Plugin
{
    #region Properties

    public required UserControl Root { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public Key Key { get; set; }

    public ModifierKeys ModifierKeys { get; set; }

    public required ImageSource? Icon { get; set; }

    #endregion
}