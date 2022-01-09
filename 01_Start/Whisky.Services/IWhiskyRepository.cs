using System;
using System.Collections.Generic;

public interface IWhiskyRepository
{
    IEnumerable<Whisky> GetAll(int skip = 0, int take = 100);
    Whisky? GetById(Guid id);
    Whisky Add(Whisky whisky);
    void Update(Whisky whisky);
    void Delete(Guid id);

    void AddRating(Guid id, short stars, string message);
}
