using Microsoft.EntityFrameworkCore;
using PazarYeriOtomasyon.API.Data;
using PazarYeriOtomasyon.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// EF Core DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services Registration
builder.Services.AddHttpClient<TrendyolService>();
builder.Services.AddHttpClient<HepsiburadaService>();
builder.Services.AddTransient<IMarketplaceService>(sp => sp.GetRequiredService<TrendyolService>());
builder.Services.AddTransient<IMarketplaceService>(sp => sp.GetRequiredService<HepsiburadaService>());
builder.Services.AddHttpClient<InvoiceService>();
builder.Services.AddHostedService<PazarYeriOtomasyon.API.Workers.OrderSyncWorker>();

// CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate(); // Veritabanı yoksa veya tablolar eksikse anında oluşturur.
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
