using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRobot.Windows.Data
{
    public enum AuthenticationResult
    {
        Success = 0,
        TooManySessionCreates = 1,
        InvalidAuthKey = 2
    }
}
