namespace Aevatar.EventSourcing.MongoDB;

public class MongoDbStorageException : Exception
{
    public MongoDbStorageException()
    {
        
    }
    
    public MongoDbStorageException(string message) : base(message)
    {
        
    }
}