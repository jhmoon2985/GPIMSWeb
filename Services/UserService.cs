using GPIMSWeb.Data;
using GPIMSWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace GPIMSWeb.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User> AuthenticateAsync(string username, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive == true);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;

            user.LastLoginAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<List<UserViewModel>> GetAllUsersAsync()
        {
            return await _context.Users
                .Where(u => u.IsActive == true)
                .Select(u => new UserViewModel
                {
                    Id = u.Id,
                    Username = u.Username,
                    Name = u.Name,
                    Department = u.Department,
                    Level = u.Level,
                    LastLoginAt = u.LastLoginAt,
                    IsActive = u.IsActive ?? false
                })
                .ToListAsync();
        }

        public async Task<UserViewModel> GetUserByIdAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return null;

            return new UserViewModel
            {
                Id = user.Id,
                Username = user.Username,
                Name = user.Name,
                Department = user.Department,
                Level = user.Level,
                LastLoginAt = user.LastLoginAt,
                IsActive = user.IsActive ?? false
            };
        }

        public async Task<bool> CreateUserAsync(CreateUserViewModel model)
        {
            try
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == model.Username);

                if (existingUser != null)
                    return false;

                var user = new User
                {
                    Username = model.Username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Name = model.Name,
                    Department = model.Department,
                    Level = model.Level,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateUserAsync(int id, CreateUserViewModel model)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null) return false;

                user.Name = model.Name;
                user.Department = model.Department;
                user.Level = model.Level;

                if (!string.IsNullOrEmpty(model.Password))
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null) return false;

                user.IsActive = false;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task LogUserActionAsync(int userId, string action, string details, string ipAddress)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return;

            var history = new UserHistory
            {
                UserId = userId,
                Username = user.Username,
                Action = action,
                Details = details,
                IpAddress = ipAddress,
                CreatedAt = DateTime.Now
            };

            _context.UserHistories.Add(history);
            await _context.SaveChangesAsync();
        }

        public async Task<List<UserHistoryViewModel>> GetUserHistoryAsync(int? userId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.UserHistories.AsQueryable();

            if (userId.HasValue)
                query = query.Where(h => h.UserId == userId.Value);

            if (fromDate.HasValue)
                query = query.Where(h => h.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(h => h.CreatedAt <= toDate.Value);

            return await query
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => new UserHistoryViewModel
                {
                    Username = h.Username,
                    Action = h.Action,
                    Details = h.Details,
                    IpAddress = h.IpAddress,
                    CreatedAt = h.CreatedAt
                })
                .Take(1000)
                .ToListAsync();
        }
    }
}
