using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marionet.App.SignalR
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class HubCallableAttribute : Attribute
    {
    }
}
