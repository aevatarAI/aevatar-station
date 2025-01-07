using MongoDB.Driver;
using Aevatar.Authors;
using Volo.Abp.MongoDB;

namespace Aevatar.Books;

public class BookStoreMongoDbContext : AbpMongoDbContext
{
    public IMongoCollection<Book> Books => Collection<Book>();
    public IMongoCollection<Author> Authors => Collection<Author>();

}
