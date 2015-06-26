using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MRobot.Windows.Data;

namespace MRobot.Windows.Extensions
{
    public static class DataExtensions
    {
        public static bool WasSubmittedByWebOrClient(this SubmitType submitType)
        {
            return submitType == SubmitType.MacSubmitted
                   || submitType == SubmitType.WebSubmitted
                   || submitType == SubmitType.WindowsSubmitted;
        }

        public static bool WasSkipped(this SubmitType submitType)
        {
            return submitType == SubmitType.ManualSkipped
                   || submitType == SubmitType.TimerSkipped;
        }
    }
}
