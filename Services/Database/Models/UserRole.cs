using System.ComponentModel.DataAnnotations;

namespace Services.Database.Models
{
    public class UserRole
    {
        [Required]
        public ulong UserId { get; set; }
        [Required]
        public ulong RoleId { get; set; }
    }
}