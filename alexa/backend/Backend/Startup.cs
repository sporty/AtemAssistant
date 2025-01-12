using Google.Apis.Auth.AspNetCore3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;

namespace LiveStreamAssistance
{
    public class Startup
    {
        // Set the TEST_WEB_CLIENT_SECRET_FILENAME configuration key to point to the client ID json file.
        // This can be set on appsettings.json or as an environment variable.
        // You can read more about configuring ASP.NET Core applications here:
        // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1
        private const string ClientSecretFilenameKey = "TEST_WEB_CLIENT_SECRET_FILENAME";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers(); // Web API コントローラのサポートを追加
            services.AddEndpointsApiExplorer(); // Swagger などのエンドポイント情報をサポート
            services.AddSwaggerGen(); // Swagger (API ドキュメント) の生成

            services.AddControllersWithViews();
            services.AddRazorPages();

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            // This loads the OAuth 2.0 client ID used by this application from a client ID json file.
            // You can use any mechanism you want to store and retrieve your client ID information, as long
            // as it is secured. If your client ID information is leaked any other app can pose as your own.
            var clientSecrets = GoogleClientSecrets.FromFile(Configuration[ClientSecretFilenameKey]).Secrets;

            // This configures Google.Apis.Auth.AspNetCore3 for use in this app.
            services
                .AddAuthentication(o =>
                {
                    // This forces challenge results to be handled by Google OpenID Handler, so there's no
                    // need to add an AccountController that emits challenges for Login.
                    o.DefaultChallengeScheme = GoogleOpenIdConnectDefaults.AuthenticationScheme;
                    // This forces forbid results to be handled by Google OpenID Handler, which checks if
                    // extra scopes are required and does automatic incremental auth.
                    o.DefaultForbidScheme = GoogleOpenIdConnectDefaults.AuthenticationScheme;
                    // Default scheme that will handle everything else.
                    // Once a user is authenticated, the OAuth2 token info is stored in cookies.
                    o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie()
                .AddGoogleOpenIdConnect(options =>
                {
                    options.ClientId = clientSecrets.ClientId;
                    options.ClientSecret = clientSecrets.ClientSecret;

                    options.Scope.Add("https://www.googleapis.com/auth/youtube");

                    // トークンをファイルに保存
                    var fileDataStore = new FileDataStore(Environment.CurrentDirectory, true);
                    options.SaveTokens = true;
                    options.Events = new OpenIdConnectEvents
                    {
                        OnRedirectToIdentityProvider = context =>
                        {
                            context.ProtocolMessage.SetParameter("prompt", "consent");
                            context.ProtocolMessage.SetParameter("access_type", "offline");
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            var refreshToken = context.TokenEndpointResponse.RefreshToken;
                            Console.WriteLine($"Refresh Token (TokenEndpointResponse): {refreshToken}");

                            fileDataStore.StoreAsync("GoogleToken", refreshToken);

                            return Task.CompletedTask;
                        },
                    };
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // This is not a production app so we always use the developer exception page.
            // You should ensure that your app uses the correct error page depending on the environment
            // it runs in.
            app.UseDeveloperExceptionPage();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseForwardedHeaders();

            app.UseAuthentication();
            app.UseAuthorization();

            var envValue = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            Console.WriteLine($"ASPNETCORE_ENVIRONMENT: {envValue}");
            if (envValue == "Development")
            {
                app.UseSwagger(); // Swagger のエンドポイントを有効化
                app.UseSwaggerUI();
            }

            app.UseEndpoints(endpoints =>
            {
                Console.WriteLine("MapControllers");
                endpoints.MapControllers();
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}