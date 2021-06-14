using System;
using System.Collections.Generic;
using System.Text;

namespace MassTransit.Scheduling.Models
{
    public interface IScheduleNotification
    {
        DateTime DeliveryTime { get; }
        string EmailAddress { get; }
        string Body { get; }
    }
}
