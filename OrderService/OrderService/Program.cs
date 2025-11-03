// Program.cs (OrderService)
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// =======================
// 1) EF Core + MySQL
// =======================
var cs = builder.Configuration.GetConnectionString("OrderDb");
builder.Services.AddDbContext<OrderDbContext>(opt =>
    opt.UseMySql(cs, ServerVersion.AutoDetect(cs)));

// =======================
// 2) Controllers + JSON
// =======================
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        // Tránh lỗi cycle khi trả Order kèm Items (EF navigation)
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// =======================
// 3) Swagger (dev)
// =======================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =======================
// 4) CORS cho UI tĩnh
// =======================
builder.Services.AddCors(p => p.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// =======================
// 5) HttpClient -> ProductService
// =======================
builder.Services.AddHttpClient("products", (sp, client) =>
{
    var baseUrl = builder.Configuration["Services:Product"]; // ví dụ https://localhost:7181
    client.BaseAddress = new Uri(baseUrl!);
});

// =======================
// 6) JWT Auth (đồng bộ với AuthService)
// =======================
var jwt = builder.Configuration.GetSection("Jwt");
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        };

        // Trả JSON lý do 401 để debug nhanh
        o.Events = new JwtBearerEvents
        {
            OnChallenge = ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = 401;
                ctx.Response.ContentType = "application/json";
                return ctx.Response.WriteAsync($$"""
                {"error":"{{ctx.Error ?? "unauthorized"}}","desc":"{{ctx.ErrorDescription}}"}
                """);
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// =======================
// Pipeline
// =======================
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
