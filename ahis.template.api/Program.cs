using ahis.template.api.Filters;
using ahis.template.application.Interfaces.Services;
using ahis.template.application.Services;
using ahis.template.identity;
using ahis.template.identity.Contexts;
using ahis.template.identity.Models.Entities;
using ahis.template.infrastructure;
using ahis.template.infrastructure.Contexts;
using ahis.template.infrastructure.SharedKernel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

namespace ahis.template.api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var jwt = builder.Configuration.GetSection("Jwt");
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            // Dependencies Injection
            builder.Services.AddScoped<IEmailSender, EmailSender>();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            builder.Services.AddScoped<UnitOfWork>();


            // Add assemblies service extentions
            builder.Services.AddInfrastructure();
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
            builder.Services.AddIdentityServices(connectionString);
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            ConfigureSwagger(builder.Services, builder.Configuration);
            ConfigureCors(builder.Services, builder.Configuration);
            ConfigureAuthentication(builder.Services, builder.Configuration, connectionString);
            ConfigureRateLimiting(builder.Services, builder.Configuration);

            builder.Services.AddAuthorization();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // IMPORTANT: apply CORS before authorization and endpoint mapping
            app.UseCors("DefaultCorsPolicy");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseRateLimiter();

            app.MapControllers();


            app.Run();

        }

        private static void ConfigureCors(IServiceCollection services, IConfiguration configuration)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("DefaultCorsPolicy", policy =>
                {
                    policy
                        .WithOrigins("https://localhost:7270", "https://localhost:7280") // <-- allowed origins
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                    //.AllowCredentials(); // remove if do not need cookies/credentials
                });

                // DEV: Can add an open policy for development only (optional)
                options.AddPolicy("AllowAllDev", policy =>
                {
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                });
            });
        }

        private static void ConfigureSwagger(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSwaggerGen(options =>
            {
                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

                // to show enum names + values
                options.SupportNonNullableReferenceTypes();
                options.SchemaFilter<EnumSchemaFilter>();

                // swagger main title and version
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "AHIS API Template", Version = "v1" });

                // Add JWT Bearer Authorization to Swagger
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter 'Bearer' [space] and then your token in the text input below.\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR...\""
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });
        }

        private static void ConfigureRateLimiting(IServiceCollection services, IConfiguration configuration)
        {
            services.AddRateLimiter(options =>
            {
                // Strict: login endpoint
                options.AddFixedWindowLimiter("AuthPolicy", limiter =>
                {
                    limiter.PermitLimit = 5; // 5 requests
                    limiter.Window = TimeSpan.FromMinutes(1);
                    limiter.QueueLimit = 0; // no queue for login
                });

                // Normal API endpoint
                options.AddSlidingWindowLimiter("ApiPolicy", limiter =>
                {
                    limiter.PermitLimit = 10; // 100 requests
                    limiter.Window = TimeSpan.FromMinutes(1);
                    limiter.SegmentsPerWindow = 4; // Sliding window segments
                    limiter.QueueLimit = 2; // small queue for extra requests
                    limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                });

                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = 429;
                    context.HttpContext.Response.ContentType = "application/json";
                    await context.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        Message = "Too many requests. Please try again later."
                    }, cancellationToken: token);
                };
            });
        }

        private static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration, string connectionString)
        {
            var jwt = configuration.GetSection("Jwt");
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Bearer";
                options.DefaultChallengeScheme = "Bearer";
            })
            .AddJwtBearer("Bearer", options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt["Issuer"],
                    ValidAudience = jwt["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SigningKey"])),
                    ClockSkew = TimeSpan.Zero
                };
                // Handle CORS for preflight requests
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Allow tokens in Authorization header
                        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                        if (!string.IsNullOrEmpty(token))
                        {
                            context.Token = token;
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context => {

                        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();

                        var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

                        if (string.IsNullOrWhiteSpace(userId))
                        {
                            context.Fail("Invalid token: missing user id");
                            return;
                        }

                        var user = await userManager.FindByIdAsync(userId);

                        // CRITICAL CHECK
                        if (user == null)
                        {
                            context.Fail("User no longer exists");
                            return;
                        }

                        // Optional hardening
                        if (user.LockoutEnabled && user.LockoutEnd > DateTimeOffset.UtcNow)
                        {
                            context.Fail("User is locked out");
                            return;
                        }
                    }
                };
            }).AddIdentityCookies();

            var authSettings = configuration.GetSection("Authentication");
           

            services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;

                // User settings
                options.User.RequireUniqueEmail = authSettings.GetValue<bool>("User:RequireUniqueEmail");


                // Sign-In settings
                options.SignIn.RequireConfirmedAccount = authSettings.GetValue<bool>("SignIn:RequireConfirmedAccount");
                options.SignIn.RequireConfirmedPhoneNumber = authSettings.GetValue<bool>("SignIn:RequireConfirmedPhoneNumber");
                options.SignIn.RequireConfirmedEmail = authSettings.GetValue<bool>("SignIn:RequireConfirmedEmail");


                // Password settings
                options.Password.RequireDigit = authSettings.GetValue<bool>("Password:RequireDigit");
                options.Password.RequiredLength = authSettings.GetValue<int>("Password:RequiredLength");
                options.Password.RequireNonAlphanumeric = authSettings.GetValue<bool>("Password:RequireNonAlphanumeric");
                options.Password.RequireUppercase = authSettings.GetValue<bool>("Password:RequireUppercase");
                options.Password.RequireLowercase = authSettings.GetValue<bool>("Password:RequireLowercase");


                // Lockout settings
                var lockoutMinutes = authSettings.GetValue<string>("Lockout:DefaultLockoutTimeSpan");
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.Parse(lockoutMinutes);
                options.Lockout.MaxFailedAccessAttempts = authSettings.GetValue<int>("Lockout:MaxFailedAccessAttempts");
                options.Lockout.AllowedForNewUsers = authSettings.GetValue<bool>("Lockout:AllowedForNewUsers");
            })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<IdentityContext>()
                .AddSignInManager()
                .AddDefaultTokenProviders();

            services.ConfigureApplicationCookie(options => { options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; }; });
        }

    } 

}

            