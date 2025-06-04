using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MoneyBaseAPI.Models;
using MoneyBaseAPI.Services;

namespace SupportChatQueueSystem
{
    public enum Seniority
    {
        Junior,
        MidLevel,
        Senior,
        TeamLead
    }

    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private static readonly QueueService service = new QueueService();

        [HttpPost("start")]
        public IActionResult StartChat([FromQuery] bool isOfficeHours)
        {
            var session = new ChatSessionModel();
            bool accepted = service.EnqueueChat(session, isOfficeHours);
            if (accepted)
                return Ok(new { sessionId = session.Id });
            return StatusCode(429, "Queue full, try again later");
        }

        [HttpPost("poll/{id}")]
        public IActionResult Poll(Guid id)
        {
            service.Poll(id);
            return Ok();
        }

        [HttpGet("status/{id}")]
        public IActionResult GetStatus(Guid id)
        {
            var status = service.GetChatStatus(id);
            if (status == "Not Found") return NotFound();
            return Ok(new { sessionId = id, status });
        }

        [HttpPost("shift/{shift}")]
        public IActionResult ChangeShift(int shift)
        {
            service.SimulateShiftChange(shift);
            return Ok();
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddControllers();
                    });
                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                        });
                    });
                })
                .Build()
                .Run();
        }
    }
}
