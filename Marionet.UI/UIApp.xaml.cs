using Avalonia;
using Avalonia.Markup.Xaml;

namespace Marionet.UI
{
    public class UIApp : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
