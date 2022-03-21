using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace GeekShopping.IdentityServer.Configuration
{
    public static class IdentityConfiguration
    {
        // perfis de usuario que vao existir na aplicação
        public const string Admin = "Admin";
        public const string Client = "Client";

        // Identity Resources - recursos a serem protegidos pelo Identity Server - podemos atribuir claims
        public static IEnumerable<IdentityResource> IdentityResources =>
            new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Email(),
                new IdentityResources.Profile(),
            };

        // API Scopes - recursos que o client pode acessar
        // ** identity scope (nome, username, email, etc) e resource scope **
        public static IEnumerable<ApiScope> ApiScopes =>
            new List<ApiScope>
            {
                new ApiScope("geek_shopping", "Geek Shopping Server"),  // Geek Shopping FrontEnd
                new ApiScope(name:"read", "Read data"),
                new ApiScope(name:"write", "Write data"),
                new ApiScope(name:"delete", "Delete data"),
            };

        // um Scope é usado por um Client:
        // Client - permite ou não a um usuário o acesso ao recurso
        // - Client é um componente que solicita um token ao Identity Server - a aplicação web, no nosso caso
        public static IEnumerable<Client> Clients =>
            new List<Client>
            {
                // client "genérico"
                new Client
                {
                    ClientId = "client",
                    // secret deve ser complexa, vai ser usada para encriptar o seu token
                    ClientSecrets = { new Secret("my_super_secret".Sha256()) }, 
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    AllowedScopes = {"read", "write", "profile"}
                },
                // client "mais específico"
                new Client
                {
                    ClientId = "geek_shopping",
                    // secret deve ser complexa, vai ser usada para encriptar o seu token
                    ClientSecrets = { new Secret("my_super_secret".Sha256()) },
                    AllowedGrantTypes = GrantTypes.Code,
                    RedirectUris = {"https://localhost:4430/signin-oidc"},
                    PostLogoutRedirectUris = {"https://localhost:4430/signout-callback-oidc"},
                    AllowedScopes = new List<string>
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Email,
                        IdentityServerConstants.StandardScopes.Profile,
                        "geek_shopping"
                    }
                }
            };
    }
}
