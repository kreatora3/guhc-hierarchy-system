using GUHC.HierarchySystem.Core.DTOs;

namespace GUHC.HierarchySystem.Core.Services
{
    public interface IAccountService
    {
        Task<AccountResponseDto> CreateAccountAsync(string name, int? parentId);        
        Task MoveAccountAsync(int accountId, int? newParentId);
        Task DeleteAccountAsync(int id);
        Task<AccountTreeResponseDto> GetSubtreeAsync(int id);
    }
}
