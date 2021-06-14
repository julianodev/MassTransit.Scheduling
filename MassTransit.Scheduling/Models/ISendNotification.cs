using System;
using System.Collections.Generic;
using System.Text;

namespace MassTransit.Scheduling.Models
{
    public interface ISendNotification
    {
        string EmailAddress { get; }
        string Body { get; }
        DateTime DateRequest { get; set; }
        bool CancelMessage { get; set; }
    }
}
