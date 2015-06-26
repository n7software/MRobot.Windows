using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRobot.Windows.Models
{
    public class ActionLink : Link
    {
        public Action ActionToPerform { get; protected set; }
    }
}
