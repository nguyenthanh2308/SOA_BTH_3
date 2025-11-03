var builder = WebApplication.CreateBuilder(args);

// (Dev) CORS: cho phép gọi từ mọi origin nếu cần
builder.Services.AddCors(p =>
    p.AddDefaultPolicy(b => b.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors();          // bật CORS khi dev

// phục vụ file tĩnh từ wwwroot, tự nhận index.html
app.UseDefaultFiles();  // tìm index.html
app.UseStaticFiles();

app.Run();
