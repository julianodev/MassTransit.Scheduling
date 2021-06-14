using MassTransit.Scheduling.Models;
using System;

namespace MassTransit.Scheduling.Commands
{
    public class SendNotificationCommand : ISendNotification
    {
        public string EmailAddress { get; set; }
        public string Body { get; set; }
        public DateTime DateRequest { get; set; }
        public bool CancelMessage { get; set; }
    }
}
