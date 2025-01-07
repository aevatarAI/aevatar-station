using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Aevatar.Books;

public class Book : AuditedAggregateRoot<Guid>
{
    public string Name { get; set; } = string.Empty;

    public BookType Type { get; set; }

    public DateTime PublishDate { get; set; }

    public float Price { get; set; }
}
