using SpareParts.Domain.BusinessAssistant;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf.Interfaces
{
    public interface IBusinessAssistantApiClient
    {
        Task<BusinessAssistantResponseDto> AskAsync(BusinessAssistantQueryRequest request);
        Task<BusinessAssistantResponseDto> RunActionAsync(BusinessAssistantActionRequest request);
    }
}
