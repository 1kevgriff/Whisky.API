public interface IWhiskyRepository
{
    IEnumerable<Whisky> GetAll(int pageNumber = 0, int pageSize = 100);
    Whisky? GetById(Guid id);
    void Add(Whisky whisky);
    void Update(Whisky whisky);
    void Delete(Guid id);
}
