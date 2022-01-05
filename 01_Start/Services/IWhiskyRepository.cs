public interface IWhiskyRepository
{
    IEnumerable<Whisky> GetAll(int pageNumber = 0, int pageSize = 100);
    Whisky? GetById(Guid id);
    Whisky Add(Whisky whisky);
    void Update(Whisky whisky);
    void Delete(Guid id);

    void AddRating(Guid id, short stars, string message);
}
