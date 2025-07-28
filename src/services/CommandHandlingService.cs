using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace NexAoD.Services;

public class CommandHandlingService
{
	private readonly InteractionService _interactionService;
	private readonly DiscordSocketClient _client;
	private readonly IServiceProvider _serviceProvider;
	private readonly UtilityService _utilityService;
	private readonly LoggingService _loggingService;

	public int CommandsRun { get; private set; }

	public CommandHandlingService(IServiceProvider services)
	{
		_serviceProvider = services;
		_interactionService = services.GetRequiredService<InteractionService>();
		_client = services.GetRequiredService<DiscordSocketClient>();
		_utilityService = services.GetRequiredService<UtilityService>();
		_loggingService = services.GetRequiredService<LoggingService>();

		// Hook CommandExecuted to handle post-command-execution logic.
		_interactionService.SlashCommandExecuted += SlashCommandExecuted;
		_interactionService.ComponentCommandExecuted += ComponentCommandExecuted;
	}

	/// <summary>
	/// Adding and registering modules to guilds
	/// </summary>
	/// <returns></returns>
	public async Task InitializeAsync()
	{
		//Register modules that are public and inherit InteractionModuleBase<T>
		await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);

#if DEBUG
		var globalCommands = await _interactionService.RegisterCommandsToGuildAsync(_utilityService.DebugServerGuildId);
		List<string> commands = globalCommands.Select(x => x.Name).ToList();
		await _loggingService.LogAsync(new LogMessage(LogSeverity.Info, $"{nameof(CommandHandlingService)}.{nameof(InitializeAsync)}", $"Registered the following Commands to Debug-Guild: \n- {string.Join("\n- ", commands)}"));
#else
		var globalCommands = await _interactionService.RegisterCommandsGloballyAsync(true);
		List<string> commands = globalCommands.Select(x => x.Name).ToList();
		await _loggingService.LogAsync(new LogMessage(LogSeverity.Info, $"{nameof(CommandHandlingService)}.{nameof(InitializeAsync)}", $"Registered the following Commands to all Guilds: \n- {string.Join("\n- ", commands)}"));
#endif

		//Register guild specific commands with the [DontAutoRegister]-Attribute
		foreach (Tuple<string, ulong> guildSpecificConfig in _utilityService.GuildModules)
		{
			if (!_interactionService.Modules.Any(x => x.Name == guildSpecificConfig.Item1))
			{
				continue;
			}
#if DEBUG
			var guildCommands = await _interactionService.AddModulesToGuildAsync(_utilityService.DebugServerGuildId, false, _interactionService.Modules.First(x => x.Name == guildSpecificConfig.Item1));
			commands = guildCommands.Select(x => x.Name).ToList();
			await _loggingService.LogAsync(new LogMessage(LogSeverity.Info, $"{nameof(CommandHandlingService)}.{nameof(InitializeAsync)}", $"Registered the following Commands to Debug-Guild {guildSpecificConfig.Item1}: \n- {string.Join("\n- ", commands)}"));
#else
			var guildCommands = await _interactionService.AddModulesToGuildAsync(guildSpecificConfig.Item2, false, _interactionService.Modules.First(x => x.Name == guildSpecificConfig.Item1));
			commands = guildCommands.Select(x => x.Name).ToList();
			await _loggingService.LogAsync(new LogMessage(LogSeverity.Info, $"{nameof(CommandHandlingService)}.{nameof(InitializeAsync)}", $"Registered the following Commands to Guild {guildSpecificConfig.Item1}: \n- {string.Join("\n- ", commands)}"));
#endif
		}
	}

	#region PostExecution-Handlers

	/// <summary>
	/// Post-Command execution for slash-commands
	/// </summary>
	/// <param name="arg1" cref="SlashCommandInfo"></param>
	/// <param name="arg2" cref="IInteractionContext"></param>
	/// <param name="arg3" cref="IResult"></param>
	/// <returns></returns>
	private async Task SlashCommandExecuted(SlashCommandInfo arg1, IInteractionContext arg2, Discord.Interactions.IResult arg3)
	{
		CommandsRun++;
		await CommandExecuted(arg1.Name, arg2, arg3);
	}

	/// <summary>
	/// Post-Command execution for component interactions
	/// </summary>
	/// <param name="arg1" cref="ComponentCommandInfo"></param>
	/// <param name="arg2" cref="IInteractionContext"></param>
	/// <param name="arg3" cref="IResult"></param>
	/// <returns></returns>
	private async Task ComponentCommandExecuted(ComponentCommandInfo arg1, IInteractionContext arg2, Discord.Interactions.IResult arg3)
	{
		CommandsRun++;
		await CommandExecuted(arg1.Name, arg2, arg3);
	}

	/// <summary>
	/// Post-Command execution for commands
	/// </summary>
	/// <param name="commandName" cref="string"></param>
	/// <param name="arg2" cref="IInteractionContext"></param>
	/// <param name="arg3" cref="IResult"></param>
	/// <returns></returns>
	private async Task CommandExecuted(string commandName, IInteractionContext arg2, Discord.Interactions.IResult arg3)
	{
		//check if commandExecution needs to be logged
		bool log = true;
		EmbedBuilder responseBuilder;

		if (!arg3.IsSuccess)
		{
			responseBuilder = UtilityService.GetResultEmbedBuilder(false, arg2.User);
			switch (arg3.Error)
			{
				case InteractionCommandError.UnmetPrecondition:
					string reason = arg3.ErrorReason;

					if (arg3 is Discord.Interactions.PreconditionGroupResult pgr)
					{
						reason += $"\r\n{string.Join("\r\n", pgr.Results.Select<Discord.Interactions.PreconditionResult, string>(x => x.ErrorReason))}";
					}

					responseBuilder.WithDescription($"Unmet Precondition: {reason}");
					break;
				case InteractionCommandError.UnknownCommand:
					responseBuilder.WithDescription("Unknown command");
					break;
				case InteractionCommandError.BadArgs:
					responseBuilder.WithDescription("Invalid number or arguments");
					break;
				case InteractionCommandError.Exception:
					responseBuilder.WithDescription($"Command exception: {arg3.ErrorReason}");
					break;
				case InteractionCommandError.Unsuccessful:
					responseBuilder.WithDescription("Command could not be executed");
					break;
				default:
					responseBuilder.WithDescription($"Unknown Error:\r\n{arg3.Error} = {arg3.ErrorReason}");
					break;
			}

			if (arg2.Interaction.HasResponded)
			{
				await arg2.Interaction.FollowupAsync(embed: responseBuilder.Build());
			}
			else
			{
				await arg2.Interaction.RespondAsync(embed: responseBuilder.Build());
			}
		}

		if (log)
		{
			string logMessage = $"{arg2.User.Mention} executed command `{commandName}` in guild `{arg2.Guild.Name}` {(arg3.IsSuccess ? "successfully" : "with error")}";
			LogMessage msg = new LogMessage(arg3.IsSuccess ? LogSeverity.Info : LogSeverity.Warning, nameof(CommandHandlingService), logMessage);
			await _loggingService.LogAsync(msg, arg2.User);
		}
	}

	#endregion
}
