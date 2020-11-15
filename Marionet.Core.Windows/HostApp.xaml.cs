using Avalonia;
using Avalonia.Markup.Xaml;

namespace Marionet.Core.Windows
{
    public class HostApp : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
