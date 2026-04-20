using Microsoft.AspNetCore.Identity;

namespace Ambev.DeveloperEvaluation.Infrastructure.Identity;

public sealed class AppRole : IdentityRole<Guid>
{
    public AppRole() { }
    public AppRole(string roleName) : base(roleName) { }
}
