using SupportChatQueueSystem;

namespace MoneyBaseAPI.Models
{
    public class AgentModel
   {
        public string Name { get; set; }
        public Seniority Level { get; set; }
        public int Shift { get; set; } // 0, 1, 2 for three shifts
        public int ActiveChats { get; set; } = 0;

        public double Efficiency => Level switch
        {
            Seniority.Junior => 0.4,
            Seniority.MidLevel => 0.6,
            Seniority.Senior => 0.8,
            Seniority.TeamLead => 0.5,
            _ => 0.4
        };

        public int MaxChats => (int)(10 * Efficiency);

        public bool IsAvailable => ActiveChats < MaxChats;
    }
}
