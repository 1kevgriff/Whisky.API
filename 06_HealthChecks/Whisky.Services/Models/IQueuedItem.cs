public interface IQueuedItem
{
    int Attempts { get; set; }
    DateTimeOffset ScheduledTimeStamp { get; set; }
}