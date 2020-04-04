using System;
using System.Collections.Generic;
using System.Text;

namespace Marionet.Core.LocalState
{
    internal class Controlled : State
    {
        public Controlled(HashSet<string> by)
        {
            By = by;
        }

        public HashSet<string> By { get; }
    }
}
