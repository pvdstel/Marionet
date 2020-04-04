using System;
using System.Collections.Generic;
using System.Text;

namespace Marionet.Core.Input
{
    public interface IInputBlocking
    {
        bool IsInputBlocked { get; }
    }
}
