public class NotificationRequest
{
    public string EmailAddress { get; set; }
    public NotificationType NotificationType { get; set; }
    public string Region { get; set; } = "All"; // use "all" for all regions
    public string WhiskyId { get; set; }
}

public enum NotificationType
{
    NEW_WHISKY,
    NEW_WHISKY_IN_REGION,
    NEW_RATING
}