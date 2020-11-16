using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Marionet.UI.ViewModels;
using System;

namespace Marionet.UI
{
    public class ViewLocator : IDataTemplate
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Probably needed for Avalonia")]
        public bool SupportsRecycling => false;

        public IControl Build(object data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var name = data!.GetType().FullName!.Replace("ViewModel", "View", StringComparison.InvariantCulture);
            var type = Type.GetType(name);

            if (type != null)
            {
                return (Control)Activator.CreateInstance(type)!;
            }
            else
            {
                return new TextBlock { Text = "Not Found: " + name };
            }
        }

        public bool Match(object data)
        {
            return data is ViewModelBase;
        }
    }
}