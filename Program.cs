using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexAoD.Services;

namespace NexAoD;
class Program
{
#pragma warning disable CS8618
	private static DiscordSocketClient _client;
	private readonly IServiceProvider _serviceProvider;
#pragma warning restore CS8618

	/// <summary>
	/// Wrapper for Main
	/// </summary>
	/// <returns>Task</returns>
	private static async Task Main()
	{
		await MainAsync();
	}

	/// <summary>
	/// Main Task
	/// </summary>
	/// <returns>Task</returns>
	static async Task MainAsync()
	{
		_client = new DiscordSocketClient(new DiscordSocketConfig
		{
			GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
		});

		_client.Ready += OnReady;

		IConfigurationRoot config = new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
#if DEBUG
			.AddJsonFile("appsettings.Development.json", false)
#else
			.AddJsonFile("appsettings.json", false)
#endif
			.AddEnvironmentVariables()
			.Build();

		string? token = config.GetRequiredSection("Discord")?.GetRequiredSection("BotToken")?.Value;

		await _client.LoginAsync(TokenType.Bot, token);
		await _client.StartAsync();

		await Task.Delay(Timeout.Infinite);
	}

	/// <summary>
	/// Ready-Event Handler
	/// </summary>
	/// <returns>Task</returns>
	private static async Task OnReady()
	{
		//init services
		ServiceProvider services = ConfigureServices();

		await services.GetRequiredService<UtilityService>().InitializeAsync();

		LoggingService loggingService = services.GetRequiredService<LoggingService>();
		await loggingService.InitializeAsync();
		_client.Log += loggingService.LogAsync;
		services.GetRequiredService<InteractionService>().Log += loggingService.LogAsync;

		//await services.GetRequiredService<DbContextService>().InitializeAsync();

		// Here we initialize the logic required to register our commands.		
		await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

		// Logs the bot name and all the servers that it's connected to
		string message = $"Connected to these servers as '{_client.CurrentUser.Username}':";
		foreach (var guild in _client.Guilds)
		{
			message += $"\n- {guild.Name}";
		}
		await loggingService.LogAsync(new LogMessage(LogSeverity.Info, nameof(OnReady), message));

		// Set the activity from the environment variable or fallback to 'I'm alive!'
		await _client.SetGameAsync(Environment.GetEnvironmentVariable("DISCORD_BOT_ACTIVITY") ?? "I'm alive!", type: ActivityType.CustomStatus);
		await loggingService.LogAsync(new LogMessage(LogSeverity.Info, nameof(OnReady), $"Activity set to '{_client.Activity.Name}'"));

		//Add event listener for InteractionCreated
		_client.InteractionCreated += async interaction =>
		{
			var scope = services.CreateScope();
			var context = new SocketInteractionContext(_client, interaction);
			await services.GetRequiredService<InteractionService>().ExecuteCommandAsync(context, scope.ServiceProvider);
		};

		await Task.CompletedTask;
	}

	/// <summary>
	/// Configures the ServiceProvider, add all Services you need to use somewhere here
	/// </summary>
	/// <returns>ServiceProvider</returns>
	private static ServiceProvider ConfigureServices()
	{
		return new ServiceCollection()
			.AddSingleton(_client)
			.AddSingleton<CommandHandlingService>()
			.AddSingleton<UtilityService>()
			.AddSingleton<LoggingService>()
			.AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>(), new InteractionServiceConfig { DefaultRunMode = RunMode.Async }))
			//.AddDbContext<DbContextService>()
			.BuildServiceProvider();
	}
}
