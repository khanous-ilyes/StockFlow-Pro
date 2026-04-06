using Microsoft.EntityFrameworkCore;
using StockFlowPro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Configure PostgreSQL Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<StockFlowPro.Application.Interfaces.IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173","https://0d53-154-121-89-67.ngrok-free.app","https://neon-fiber-322314-f44e9.web.app")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings.GetValue<string>("Secret");

// Register AuthService
builder.Services.AddScoped<StockFlowPro.Application.Services.AuthService>(provider => 
{
    var context = provider.GetRequiredService<StockFlowPro.Application.Interfaces.IAppDbContext>();
    return new StockFlowPro.Application.Services.AuthService(
        context, 
        secretKey ?? "", 
        jwtSettings.GetValue<string>("Issuer") ?? "", 
        jwtSettings.GetValue<string>("Audience") ?? "", 
        jwtSettings.GetValue<int>("ExpiryMinutes")
    );
});

// Register Domain Services
builder.Services.AddScoped<StockFlowPro.Application.Services.ICategoryService, StockFlowPro.Application.Services.CategoryService>();
builder.Services.AddScoped<StockFlowPro.Application.Services.IProductService, StockFlowPro.Application.Services.ProductService>();
builder.Services.AddScoped<StockFlowPro.Application.Services.IClientService, StockFlowPro.Application.Services.ClientService>();
builder.Services.AddScoped<StockFlowPro.Application.Services.IOrderService, StockFlowPro.Application.Services.OrderService>();
builder.Services.AddScoped<StockFlowPro.Application.Services.ICreditsService, StockFlowPro.Application.Services.CreditsService>();
builder.Services.AddScoped<StockFlowPro.Application.Services.IInvoiceService, StockFlowPro.Application.Services.InvoiceService>();

// Register License Services
builder.Services.AddSingleton<StockFlowPro.API.Services.License.IMachineIdProvider, StockFlowPro.API.Services.License.WindowsMachineIdProvider>();
builder.Services.AddSingleton<StockFlowPro.API.Services.License.ILicenseCryptographyService, StockFlowPro.API.Services.License.LicenseCryptographyService>();
builder.Services.AddScoped<StockFlowPro.Application.Interfaces.IFileStorageService, StockFlowPro.Infrastructure.Services.SupabaseStorageService>();

if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("JWT Secret is not configured.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.GetValue<string>("Issuer"),
        ValidateAudience = true,
        ValidAudience = jwtSettings.GetValue<string>("Audience"),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Setup Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "StockFlow Pro API", Version = "v1" });
    
    // Configure JWT in Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseStaticFiles();

app.UseHttpsRedirection();

// License Verification Middleware
app.UseMiddleware<StockFlowPro.API.Services.License.LicenseVerificationMiddleware>();

// Use authentication and authorization
app.UseAuthentication();
// Custom Tenant Middleware (simplified for now, later we will create a dedicated class)
app.Use(async (context, next) =>
{
    var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;
    if (Guid.TryParse(tenantIdClaim, out var tenantId))
    {
        var dbContext = context.RequestServices.GetRequiredService<AppDbContext>();
        dbContext.SetCurrentTenant(tenantId);
    }
    await next();
});
app.UseAuthorization();

app.MapControllers();

app.Run();
