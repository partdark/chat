using chat.Hubs;
using chat.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy
                .WithOrigins("http://localhost:3000")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    }
);

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "ChatApp_";
});

builder.Services.AddSingleton<IChatMessageService, RedisChatMessageService>();

builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 102400; 
    options.EnableDetailedErrors = true;
    options.StreamBufferCapacity = 20;
});

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseCors();

app.MapHub<ChatHub>("/chat");

app.Run();