using System.Threading.Tasks;
using Services.Database.Models;

namespace Services.Database
{
    public interface IUserRoleContext
    {
        Task<UserRole> GetByUserId(ulong userId);
        Task<UserRole> GetByRoleId(ulong roleId);
        Task Add(ulong userId, ulong roleId);
        Task RemoveByUserId(ulong userId);
        Task RemoveByRoleId(ulong roleId);
    }
}