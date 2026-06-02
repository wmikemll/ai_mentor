using InstantBot.Application.Commands.RegisterUser;
using InstantBot.Application.Interfaces;
using InstantBot.Application.Services;
using InstantBot.Infrastructure.Cache;
using InstantBot.Infrastructure.Notifications;
using InstantBot.Infrastructure.OpenAi;
using InstantBot.Infrastructure.Persistence;
using InstantBot.Infrastructure.Persistence.Repositories;
using InstantBot.TelegramBot;
using InstantBot.TelegramBot.Handlers;
using InstantBot.TelegramBot.StateMachine;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StackExchange.Redis;
using Telegram.Bot;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/bot-.log", rollingInterval: Serilog.RollingInterval.Day)
    .CreateLogger();
builder.Logging.ClearProviders();
builder.Services.AddSerilog();

// Telegram
builder.Services.AddSingleton<ITelegramBotClient>(_ =>
    new TelegramBotClient(builder.Configuration["TelegramBot:Token"]!));

// PostgreSQL
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]!));

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IDailyCardRepository, DailyCardRepository>();

// Cache
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// OpenAI (stubs created in Task 10)
builder.Services.AddScoped<IOpenAiService, OpenAiService>();
builder.Services.AddScoped<IWhisperService, WhisperService>();
builder.Services.AddScoped<ITtsService, TtsService>();

// Notification service
builder.Services.AddScoped<INotificationService, TelegramNotificationService>();

// Background jobs
builder.Services.AddScoped<DailyCardJob>();
builder.Services.AddScoped<SubscriptionExpiryJob>();
builder.Services.AddScoped<InactivityJob>();
builder.Services.AddScoped<ConversationSummaryJob>();
builder.Services.AddHostedService<NotificationScheduler>();
builder.Services.AddScoped<StreakService>();

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(RegisterUserCommand).Assembly));

// Bot handlers
builder.Services.AddScoped<UserStateManager>();
builder.Services.AddScoped<MessageHandler>();
builder.Services.AddScoped<CallbackHandler>();
builder.Services.AddScoped<PaymentHandler>();
builder.Services.AddScoped<VoiceHandler>();
builder.Services.AddScoped<UpdateRouter>();

// Long polling
builder.Services.AddHostedService<BotPollingService>();

var app = builder.Build();

// Auto-migrate on start
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

await app.RunAsync();
