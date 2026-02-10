using FileTransferWeb.Storage.Application.DependencyInjection;
using FileTransferWeb.Storage.Infrastructure.DependencyInjection;
using FileTransferWeb.Transfer.Application.DependencyInjection;
using FileTransferWeb.Transfer.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddStorageApplication();
builder.Services.AddTransferApplication();
builder.Services.AddStorageInfrastructure(builder.Configuration);
builder.Services.AddTransferInfrastructure();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
