namespace MoneyBaseAPI.Models
{
    public class ChatSessionModel
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime LastPolled { get; set; } = DateTime.Now;
        public bool IsAssigned { get; set; } = false;
    }
}
