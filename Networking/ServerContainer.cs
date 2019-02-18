using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities.Networking
{
    public class ServerContainer : System.Windows.Forms.Form
    {
        public virtual void ProcessRequest(Dictionary<string, string> valuePairs) { }
    }
}
