using Discord;
using Discord.Interactions;
using Discord.Webhook;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexAod.Services;

public class LoggingService
{
	private DiscordWebhookClient _webhookClient;

	private readonly IServiceProvider _serviceProvider;
	private readonly InteractionService _interactionService;
	private readonly DiscordSocketClient _client;

	public LoggingService(IServiceProvider services)
	{
		_serviceProvider = services;
		_client = services.GetRequiredService<DiscordSocketClient>();
		_interactionService = services.GetRequiredService<InteractionService>();
	}

	public async Task InitializeAsync()
	{
		IConfigurationRoot config = new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
#if DEBUG
			.AddJsonFile("appsettings.Development.json", false)
#else
			.AddJsonFile("appsettings.json", false)
#endif
			.AddEnvironmentVariables()
			.Build();

		WebhookUrl = config.GetRequiredSection("Discord")?.GetRequiredSection("WebhookUrl")?.Value!;
		_webhookClient = new DiscordWebhookClient(WebhookUrl);

		await Task.CompletedTask;
	}

	#region Properties

	public string? WebhookUrl { get; private set; }

	#endregion

	public async Task LogAsync(LogMessage message)
	{
		await LogAsync(message, null);
	}

	public async Task LogAsync(LogMessage message, IUser? user = null)
	{
		Console.WriteLine(message);

		EmbedBuilder builder = new EmbedBuilder()
			.WithTitle(message.Source)
			.WithDescription(message.Message)
			.WithCurrentTimestamp();

		switch (message.Severity)
		{
			case LogSeverity.Critical:
				builder.WithColor(Color.DarkRed);
				break;
			case LogSeverity.Error:
				builder.WithColor(Color.Red);
				break;
			case LogSeverity.Warning:
				builder.WithColor(Color.Orange);
				break;
			case LogSeverity.Info:
				builder.WithColor(Color.Blue);
				break;
			case LogSeverity.Verbose:
				builder.WithColor(Color.LightGrey);
				break;
			case LogSeverity.Debug:
				builder.WithColor(Color.Green);
				break;
			default:
				break;
		}

		if (user != null)
		{
			builder.WithAuthor(user);
		}

		await _webhookClient.SendMessageAsync(embeds: [builder.Build()]);
	}
}
