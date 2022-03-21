using GeekShopping.IdentityServer.Configuration;
using GeekShopping.IdentityServer.Model;
using GeekShopping.IdentityServer.Model.Context;
using IdentityModel;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace GeekShopping.IdentityServer.Initializer
{
    public class DbInitializer : IDbInitializer
    {
        private readonly MySqlContext _context;
        private readonly UserManager<ApplicationUser> _user;
        private readonly RoleManager<IdentityRole> _role;

        public DbInitializer(MySqlContext context, UserManager<ApplicationUser> user, RoleManager<IdentityRole> role)
        {
            _context = context;
            _user = user;
            _role = role;
        }

        public void Initialize()
        {
            // se ja tem um Admin, sai
            if (_role.FindByNameAsync(IdentityConfiguration.Admin).Result != null) return;

            // cadastro dos roles
            _role.CreateAsync(new IdentityRole(IdentityConfiguration.Admin))
                .GetAwaiter().GetResult();
            _role.CreateAsync(new IdentityRole(IdentityConfiguration.Client))
                .GetAwaiter().GetResult();

            // cadastro do usuario admin
            ApplicationUser admin = new ApplicationUser()
            {
                UserName = "marcio-admin",
                Email = "marcio-admin@qqqqqqqqq.com",
                EmailConfirmed = true,
                PhoneNumber = "+55 (21) 99999-2323",
                FirstName = "Marcio",
                LastName = "Admin"
            };

            // vinculando o user ao role
            _user.CreateAsync(admin, "Senha1234!").GetAwaiter().GetResult();
            _user.AddToRoleAsync(admin, IdentityConfiguration.Admin).GetAwaiter().GetResult();

            // claims
            var adminClaims = _user.AddClaimsAsync(admin, new Claim[]
            {
                new Claim(JwtClaimTypes.Name, $"{admin.FirstName} {admin.LastName}"),
                new Claim(JwtClaimTypes.GivenName, admin.FirstName),
                new Claim(JwtClaimTypes.FamilyName, admin.LastName),
                new Claim(JwtClaimTypes.Role, IdentityConfiguration.Admin)
            }).Result;



            // cadastro do usuario client
            ApplicationUser client = new ApplicationUser()
            {
                UserName = "marcio-client",
                Email = "marcio-client@qqqqqqqqq.com",
                EmailConfirmed = true,
                PhoneNumber = "+55 (21) 99999-44444",
                FirstName = "Marcio",
                LastName = "Client"
            };

            // vinculando o user ao role
            _user.CreateAsync(client, "Senha1234!").GetAwaiter().GetResult();
            _user.AddToRoleAsync(client, IdentityConfiguration.Client).GetAwaiter().GetResult();

            // claims
            var clientClaims = _user.AddClaimsAsync(client, new Claim[]
            {
                new Claim(JwtClaimTypes.Name, $"{client.FirstName} {client.LastName}"),
                new Claim(JwtClaimTypes.GivenName, client.FirstName),
                new Claim(JwtClaimTypes.FamilyName, client.LastName),
                new Claim(JwtClaimTypes.Role, IdentityConfiguration.Client)
            }).Result;

        }
    }
}
