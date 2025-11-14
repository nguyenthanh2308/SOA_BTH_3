using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ProductService.Data;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ============= EF Core + MySQL (Pomelo) =============
var cs = builder.Configuration.GetConnectionString("ProductDb");
builder.Services.AddDbContext<ProductDbContext>(opt =>
    opt.UseMySql(cs, ServerVersion.AutoDetect(cs)));

// ============= Controllers + JSON =============
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// ============= Swagger =============
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    // BẮT BUỘC: đăng ký tài liệu "v1"
    o.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ProductService",
        Version = "v1",
        Description = "E-Commerce Product Service API"
    });

    // Cho phép nhập Bearer token trên Swagger
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập: Bearer {token}"
    });
    o.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    // ĐỪNG gọi IncludeXmlComments nếu chưa cấu hình file XML
});

// ============= JWT Bearer =============
var jwt = builder.Configuration.GetSection("Jwt");
var keyBytes = Encoding.UTF8.GetBytes(jwt["Key"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ============= CORS (dev) =============
builder.Services.AddCors(p =>
{
    p.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// ============= Pipeline =============
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();           // hiển thị stacktrace nếu /swagger/v1/swagger.json lỗi
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProductService v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();   // phải trước UseAuthorization
app.UseAuthorization();

app.MapControllers();

app.Run();
