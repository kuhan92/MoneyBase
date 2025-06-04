using MoneyBaseAPI.Models;
using SupportChatQueueSystem;

namespace MoneyBaseAPI.Services
{
    public class QueueService
    {
        private Queue<ChatSessionModel> chatQueue = new();
        private List<ChatSessionModel> overflowQueue = new();
        private List<AgentModel> agents;
        private List<AgentModel> overflowAgents;
        private Dictionary<Guid, int> pollCount = new();
        private readonly object lockObj = new();
        private int currentShift = 0; // simulate 0, 1, 2
        private Dictionary<Guid, ChatSessionModel> allSessions = new();

        public QueueService()
        {
            agents = new List<AgentModel>
            {
                new AgentModel { Name = "A1", Level = Seniority.TeamLead, Shift = 0 },
                new AgentModel { Name = "A2", Level = Seniority.MidLevel, Shift = 0 },
                new AgentModel { Name = "A3", Level = Seniority.MidLevel, Shift = 0 },
                new AgentModel { Name = "A4", Level = Seniority.Junior, Shift = 0 },
                new AgentModel { Name = "B1", Level = Seniority.Senior, Shift = 1 },
                new AgentModel { Name = "B2", Level = Seniority.MidLevel, Shift = 1 },
                new AgentModel { Name = "B3", Level = Seniority.Junior, Shift = 1 },
                new AgentModel { Name = "B4", Level = Seniority.Junior, Shift = 1 },
                new AgentModel { Name = "C1", Level = Seniority.MidLevel, Shift = 2 },
                new AgentModel { Name = "C2", Level = Seniority.MidLevel, Shift = 2 },
            };

            overflowAgents = Enumerable.Range(1, 6)
                .Select(i => new AgentModel { Name = $"Overflow{i}", Level = Seniority.Junior, Shift = -1 })
                .ToList();

            StartMonitoring();
        }

        private double CalculateCapacity(List<AgentModel> team) =>
            team.Where(a => a.Shift == currentShift)
                .Sum(a => Math.Floor(10 * a.Efficiency));

        public bool EnqueueChat(ChatSessionModel session, bool isOfficeHours)
        {
            lock (lockObj)
            {
                var activeAgents = agents.Where(a => a.Shift == currentShift).ToList();
                double capacity = CalculateCapacity(activeAgents);
                int maxQueue = (int)(capacity * 1.5);

                if (chatQueue.Count < maxQueue)
                {
                    chatQueue.Enqueue(session);
                    pollCount[session.Id] = 0;
                    return true;
                }
                else if (isOfficeHours && overflowQueue.Count < overflowAgents.Sum(a => a.MaxChats))
                {
                    overflowQueue.Add(session);
                    pollCount[session.Id] = 0;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public string GetChatStatus(Guid sessionId)
        {
            lock (lockObj)
            {
                if (!allSessions.ContainsKey(sessionId))
                    return "Not Found";
                var session = allSessions[sessionId];
                if (pollCount.ContainsKey(sessionId) && pollCount[sessionId] >= 3)
                    return "Inactive";
                return session.IsAssigned ? "Assigned" : "Queued";
            }
        }

        public void Poll(Guid sessionId)
        {
            lock (lockObj)
            {
                if (pollCount.ContainsKey(sessionId))
                    pollCount[sessionId]++;
            }
        }

        private void StartMonitoring()
        {
            new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(1000);

                    lock (lockObj)
                    {
                        var inactive = pollCount.Where(p => p.Value >= 3).Select(p => p.Key).ToList();
                        foreach (var id in inactive)
                        {
                            chatQueue = new Queue<ChatSessionModel>(chatQueue.Where(c => c.Id != id));
                            overflowQueue = overflowQueue.Where(c => c.Id != id).ToList();
                            pollCount.Remove(id);
                        }

                        AssignChats();
                    }
                }
            })
            { IsBackground = true }.Start();
        }

        private void AssignChats()
        {
            var priorityOrder = new[]
            {
                Seniority.Junior,
                Seniority.MidLevel,
                Seniority.Senior,
                Seniority.TeamLead
            };

            foreach (var queue in new[] { chatQueue, new Queue<ChatSessionModel>(overflowQueue) })
            {
                while (queue.Any())
                {
                    var chat = queue.Dequeue();

                    var availableAgents = (queue == chatQueue ? agents : overflowAgents)
                        .Where(a => a.Shift == currentShift || a.Shift == -1)
                        .Where(a => a.IsAvailable)
                        .OrderBy(a => Array.IndexOf(priorityOrder, a.Level))
                        .ToList();

                    if (availableAgents.Any())
                    {
                        var agent = availableAgents.First();
                        agent.ActiveChats++;
                        chat.IsAssigned = true;
                    }
                    else
                    {
                        if (queue == chatQueue)
                            chatQueue.Enqueue(chat);
                        else
                            overflowQueue.Add(chat);
                        break;
                    }
                }
            }
        }

        public void SimulateShiftChange(int newShift)
        {
            currentShift = newShift;
        }
    }
}
