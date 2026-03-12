using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class Admin_UserManage
    {
        private string searchTerm = "";

        public class UserViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Email { get; set; } = "";
            public string Role { get; set; } = "Customer";
            public string Status { get; set; } = "Active";
            public string JoinDate { get; set; } = "";
        }

        private List<UserViewModel> dummyUsers = new List<UserViewModel>
        {
            new UserViewModel { Id = 1, Name = "Ali Khan", Email = "ali.khan@gmail.com", Role = "Customer", Status = "Active", JoinDate = "10 Feb 2024" },
            new UserViewModel { Id = 2, Name = "Admin User", Email = "admin@system.com", Role = "Admin", Status = "Active", JoinDate = "01 Jan 2024" },
            new UserViewModel { Id = 3, Name = "Sara Ahmed", Email = "sara123@hotmail.com", Role = "Customer", Status = "Banned", JoinDate = "15 Mar 2024" },
            new UserViewModel { Id = 4, Name = "Bilal Raza", Email = "bilal.raza@yahoo.com", Role = "Customer", Status = "Active", JoinDate = "22 Mar 2024" },
            new UserViewModel { Id = 5, Name = "Zain Malik", Email = "zain.malik@gmail.com", Role = "Customer", Status = "Active", JoinDate = "05 Apr 2024" },
        };

        private IEnumerable<UserViewModel> FilteredUsers =>
            string.IsNullOrWhiteSpace(searchTerm)
                ? dummyUsers
                : dummyUsers.Where(u => u.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                        u.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
    }
}
