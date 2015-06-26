using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRobot.Windows.Data
{
    public class Turn
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public int SlotId { get; set; }
        public long? UserId { get; set; }
        public int Number { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public int Points { get; set; }
        public int? SaveFileBytes { get; set; }
        public int? ErrorLogId { get; set; }
        public SubmitType SubmitType { get; set; }
        public Guid? SaveId { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime? EstimatedCompletionLow { get; set; }
        public DateTime? EstimatedCompletionHigh { get; set; }
        public DateTime? LastModifiedSave { get; set; }
        public string ErrorBody { get; set; }
    }

    public enum SubmitType : int
    {
        NotSubmitted = 0,
        WebSubmitted = 1,
        WindowsSubmitted = 2,
        MacSubmitted = 3,
        LinuxSubmitted = 4,
        TimerSkipped = 10,
        ManualSkipped = 11,
        VacationSkipped = 12,
        CreateGameSubmitted = 20,
        Reverted = 30
    }
}
