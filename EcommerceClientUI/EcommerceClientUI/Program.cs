var builder = WebApplication.CreateBuilder(args);

// (tùy chọn cho DEV) bật CORS nếu bạn muốn truy cập từ origin khác
builder.Services.AddCors(p =>
    p.AddDefaultPolicy(b => b.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    // với app FE tĩnh, có thể tắt HSTS để dễ test: // app.UseHsts();
}

app.UseHttpsRedirection();

app.UseCors();              // nếu dùng CORS dev

// Cho phép phục vụ file mặc định: index.html
app.UseDefaultFiles();      // tìm index.html trong wwwroot
app.UseStaticFiles();       // phục vụ file tĩnh từ wwwroot

app.Run();
