using FileTransferWeb.Endpoints;
using FileTransferWeb.Storage.Application.DependencyInjection;
using FileTransferWeb.Storage.Infrastructure.DependencyInjection;
using FileTransferWeb.Transfer.Application.DependencyInjection;
using FileTransferWeb.Transfer.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
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

app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    appBuilder => appBuilder.UseExceptionHandler());

app.UseRouting();

app.UseAuthorization();

app.MapStorageEndpoints();
app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

public partial class Program;
