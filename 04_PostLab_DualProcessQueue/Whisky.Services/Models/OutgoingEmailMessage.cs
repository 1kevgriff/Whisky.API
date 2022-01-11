public class OutgoingEmailMessage : IQueuedItem
{
    public string To { get; set; }
    public string From { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }


    public int Attempts { get; set; }
    public DateTimeOffset ScheduledTimeStamp { get; set; } = DateTimeOffset.UtcNow;
}