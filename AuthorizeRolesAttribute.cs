using Microsoft.AspNetCore.Authorization;
using webprogbackend.Models.Enums;

namespace webprogbackend.Attributes
{
    public class AuthorizeRolesAttribute : AuthorizeAttribute
    {
        public AuthorizeRolesAttribute(params UserRole[] roles)
        {
            Roles = string.Join(",", roles.Select(r => r.ToString()));
        }
    }
} 