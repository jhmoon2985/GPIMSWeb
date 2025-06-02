using GPIMSWeb.Models;

namespace GPIMSWeb.Services
{
    public interface IUserService
    {
        Task<User> AuthenticateAsync(string username, string password);
        Task<List<UserViewModel>> GetAllUsersAsync();
        Task<UserViewModel> GetUserByIdAsync(int id);
        Task<bool> CreateUserAsync(CreateUserViewModel model);
        Task<bool> UpdateUserAsync(int id, CreateUserViewModel model);
        Task<bool> DeleteUserAsync(int id);
        Task LogUserActionAsync(int userId, string action, string details, string ipAddress);
        Task<List<UserHistoryViewModel>> GetUserHistoryAsync(int? userId = null, DateTime? fromDate = null, DateTime? toDate = null);
    }
}