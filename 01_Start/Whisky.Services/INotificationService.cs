public interface INotificationService
{
    Task WhiskeyAdded(Whisky whisky);
    Task RatingAdded(Whisky whisky, Rating rating);
}
