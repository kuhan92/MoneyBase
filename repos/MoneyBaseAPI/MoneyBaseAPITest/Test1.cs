using System;
using Xunit;
using MoneyBaseAPI;
using MoneyBaseAPI.Services;
using MoneyBaseAPI.Models;

namespace MoneyBaseAPI.Tests
{
    public class QueueServiceTests
    {
        [Fact]
        public void EnqueueChat_ShouldReturnTrue_WhenUnderCapacity()
        {
            var manager = new QueueService();
            manager.SimulateShiftChange(0);
            var session = new ChatSessionModel();

            bool result = manager.EnqueueChat(session, true);

            Assert.IsTrue(result);
        }

        [Fact]
        public void EnqueueChat_ShouldReturnFalse_WhenQueueFullAndNotOfficeHours()
        {
            var manager = new QueueService();
            manager.SimulateShiftChange(0);

            for (int i = 0; i < 100; i++)
            {
                var session = new ChatSessionModel();
                manager.EnqueueChat(session, false);
            }

            var newSession = new ChatSessionModel();
            bool result = manager.EnqueueChat(newSession, false);

            Assert.IsFalse(result);
        }

        [Fact]
        public void Poll_ShouldIncrementPollCount()
        {
            var manager = new QueueService();
            var session = new ChatSessionModel();
            manager.EnqueueChat(session, true);

            manager.Poll(session.Id);
            manager.Poll(session.Id);
            manager.Poll(session.Id);

            var status = manager.GetChatStatus(session.Id);

            Assert.Equals("Inactive", status);
        }

        [Fact]
        public void GetChatStatus_ShouldReturnQueuedInitially()
        {
            var manager = new QueueService();
            var session = new ChatSessionModel();
            manager.EnqueueChat(session, true);

            var status = manager.GetChatStatus(session.Id);

            Assert.Equals("Queued", status);
        }

        [Fact]
        public void GetChatStatus_ShouldReturnNotFound_WhenInvalidId()
        {
            var manager = new QueueService();
            var result = manager.GetChatStatus(Guid.NewGuid());

            Assert.Equals("Not Found", result);
        }
    }
}
