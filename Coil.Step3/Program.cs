using Coil.Step3.Hubs;
using Coil.Step3.Interfaces;
using Coil.Step3.Models.Database;
using Coil.Step3.Models.Settings;
using Coil.Step3.Services;
using Dapper;
using Microsoft.AspNetCore.RateLimiting;
using MySql.Data.MySqlClient;
using System.Data;
using System.Net;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Any, 5000); // Or use specific ports like 5001 for HTTPS
});
// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddHttpClient("CoilApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("PIDSG:Base_URL")!);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(1);
}).AddStandardResilienceHandler(options =>
{
    options.Retry.MaxDelay = TimeSpan.FromMilliseconds(100);
    options.Retry.Delay = TimeSpan.FromMilliseconds(70);
    options.Retry.MaxRetryAttempts = 3;
    options.AttemptTimeout.Timeout = TimeSpan.FromMilliseconds(900);
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(4);
});
builder.Services.AddHttpClient("Step2Api", client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(1);
}).AddStandardResilienceHandler(options =>
{
    options.Retry.MaxDelay = TimeSpan.FromMilliseconds(100);
    options.Retry.Delay = TimeSpan.FromMilliseconds(70);
    options.Retry.MaxRetryAttempts = 3;
    options.AttemptTimeout.Timeout = TimeSpan.FromMilliseconds(900);
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(4);
});

builder.Services.AddTransient<IDbConnection>(sp =>
{
    var con = new MySqlConnection(builder.Configuration.GetConnectionString("M_DB"));
    con.Open();
    string? timezone = builder.Configuration.GetSection("Timezone").Value;
    con.ExecuteAsync($@"SET time_zone = '{timezone}';");
    return con;
});
builder.Services.AddScoped<TransactionService>();   
builder.Services.AddScoped<RollingDoorService>();
builder.Services.AddScoped<EmployeeService>();
builder.Services.Configure<ModbusConfig>(builder.Configuration.GetSection("ModbusConfig"));
builder.Services.AddSingleton<ModbusPLCService>();

builder.Services.AddSingleton<TransactionSyncService>();
builder.Services.AddSingleton<RollingDoorSyncService>();
builder.Services.AddSingleton<ContainerSyncService>();
builder.Services.AddSingleton<EmployeeSyncService>();

builder.Services.AddSingleton<ISyncService<TransactionModel>>(sp => sp.GetRequiredService<TransactionSyncService>());  
builder.Services.AddSingleton<ISyncService<RollingDoorModel>>(sp => sp.GetRequiredService<RollingDoorSyncService>());
builder.Services.AddSingleton<ISyncService<ContainerModel>>(sp => sp.GetRequiredService<ContainerSyncService>()); 
builder.Services.AddSingleton<ISyncService<EmployeeModel>>(sp => sp.GetRequiredService<EmployeeSyncService>());

builder.Services.AddSingleton<IStep4CheckService>(sp=>sp.GetRequiredService<TransactionSyncService>());

builder.Services.AddHostedService(sp => sp.GetRequiredService<TransactionSyncService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<RollingDoorSyncService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<ContainerSyncService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<EmployeeSyncService>());
builder.Services.AddHostedService<RackSyncPendingService>();

builder.Services.AddHostedService<ScaleSerialService>();
builder.Services.AddSignalR();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("FixedPolicy", opt =>
    {
        opt.PermitLimit = 1;
        opt.Window = TimeSpan.FromSeconds(5);
        opt.QueueLimit = 0;
        opt.AutoReplenishment = true;
    });
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync("Rate limit exceeded!", token);
    };
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowCredentials()
              .AllowAnyMethod()
              .AllowAnyHeader();
        policy.WithOrigins("http://localhost:3000","http://localhost","http://127.0.0.1","http://10.89.1.226","http://10.89.1.99");
    });
});

var app = builder.Build();

app.UseRouting();
app.UseRateLimiter();

app.MapHub<ScalesHub>("/scales-hub");
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapControllers();

app.Run();
