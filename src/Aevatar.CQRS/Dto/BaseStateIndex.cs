using System;
using Nest;

namespace Aevatar.CQRS.Dto;

public class BaseStateIndex
{
    [Keyword]public string Id { get; set; }
    public DateTime Ctime { get; set; }
    public string State{ get; set; }

}