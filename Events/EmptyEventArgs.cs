using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities.Events
{
    public abstract class EmptyEventArgs<T> : EventArgs where T : EmptyEventArgs<T>
    {
        private static T empty;
        private static bool isInitialised = false;

        public new static T Empty
        {
            get
            {
                if (!isInitialised)
                {
                    empty = default(T);
                    isInitialised = true;
                }
                return empty;
            }
        }
    }
}
