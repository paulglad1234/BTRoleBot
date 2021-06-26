using System.Configuration;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Services.Database.Models;

namespace Services.Database
{
    public sealed class UserRoleDbContext : DbContext
    {
        public DbSet<UserRole> UserRoles { get; set; }

        public UserRoleDbContext(DbContextOptions<UserRoleDbContext> options)
        : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserRole>().HasKey(ur => new {ur.UserId, ur.RoleId});
            modelBuilder.Entity<UserRole>().HasIndex(ur => ur.UserId).IsUnique();
            modelBuilder.Entity<UserRole>().HasIndex(ur => ur.RoleId).IsUnique();
        }

        public async Task<UserRole> GetByUserId(ulong userId)
        {
            return await UserRoles.FirstOrDefaultAsync(ur => ur.UserId == userId);
        }

        public async Task<UserRole> GetByRoleId(ulong roleId)
        {
            return await UserRoles.FirstOrDefaultAsync(ur => ur.RoleId == roleId);
        }

        public async Task Add(ulong userId, ulong roleId)
        {
            await UserRoles.AddAsync(new UserRole {UserId = userId, RoleId = roleId});
            await SaveChangesAsync();
        }

        public async Task RemoveByUserId(ulong userId)
        {
            var ur = await GetByUserId(userId);
            if (ur != null)
            {
                UserRoles.Remove(ur);
                await SaveChangesAsync();
            }
        }

        public async Task RemoveByRoleId(ulong roleId)
        {
            var ur = await GetByRoleId(roleId);
            if (ur != null)
            {
                UserRoles.Remove(ur);
                await SaveChangesAsync();
            }
        }
    }
}