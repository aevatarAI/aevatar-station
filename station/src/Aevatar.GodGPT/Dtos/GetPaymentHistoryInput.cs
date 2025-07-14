using Volo.Abp.Application.Dtos;

namespace Aevatar.GodGPT.Dtos;

public class GetPaymentHistoryInput
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}