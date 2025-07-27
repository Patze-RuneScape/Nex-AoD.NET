using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexAoD.Extensions;

namespace NexAoD.Services;

public class UtilityService
{
	private readonly InteractionService _interactionService;
	private readonly DiscordSocketClient _client;
	private readonly IServiceProvider _serviceProvider;

	public UtilityService(IServiceProvider services)
	{
		_interactionService = services.GetRequiredService<InteractionService>();
		_client = services.GetRequiredService<DiscordSocketClient>();
		_serviceProvider = services;
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

		DebugServerGuildId = (config.GetRequiredSection("Discord")?.GetRequiredSection("DebugServerGuildId")?.Value)!.ToUlong();

		await Task.CompletedTask;
	}

	#region Properties

	public ulong DebugServerGuildId { get; private set; }

	/// <summary>
	/// Modules only available to specific guilds
	/// </summary>
	public List<Tuple<string, ulong>> GuildModules =>
			[
				new Tuple<string, ulong>("NexAoD", 315710189762248705) //Nex, Angel of Death
			];
	#endregion

	#region Discord

	/// <summary>
	/// Determines, if a role has permissions to do moderation-like actions
	/// </summary>
	/// <param name="role"></param>
	/// <returns>true if a role is able to do moderation actions</returns>
	public static bool IsElevatedRole(IRole role)
	{
		return role.Permissions.Administrator
				|| role.Permissions.BanMembers
				|| role.Permissions.CreateEvents
				|| role.Permissions.CreateGuildExpressions
				|| role.Permissions.DeafenMembers
				|| role.Permissions.KickMembers
				|| role.Permissions.ManageChannels
				|| role.Permissions.ManageEmojisAndStickers
				|| role.Permissions.ManageEvents
				|| role.Permissions.ManageGuild
				|| role.Permissions.ManageMessages
				|| role.Permissions.ManageNicknames
				|| role.Permissions.ManageRoles
				|| role.Permissions.ManageWebhooks
				|| role.Permissions.MentionEveryone
				|| role.Permissions.ModerateMembers
				|| role.Permissions.MuteMembers
				|| role.Permissions.ViewAuditLog
				|| role.Permissions.ViewGuildInsights
				|| role.Permissions.ViewMonetizationAnalytics;
	}

	#endregion

	#region Embeds

	/// <summary>
	/// Returns an EmbedBuilder prepared for a standard response
	/// </summary>
	/// <param name="success" cref="bool"></param>
	/// <param name="user" cref="IUser"></param>
	/// <returns cref="EmbedBuilder"></returns>
	public static EmbedBuilder GetResultEmbedBuilder(bool success, IUser user)
	{
		return new EmbedBuilder()
			.WithColor(success ? Color.Green : Color.Red)
			.WithAuthor(new EmbedAuthorBuilder().WithName(user.Username).WithIconUrl(user.GetAvatarUrl()))
			.WithCurrentTimestamp();
	}

	/// <summary>
	/// Returns an EmbedBuilder prepared for an role specific response
	/// </summary>
	/// <param name="role" cref="IRole"></param>
	/// <param name="user" cref="IUser"></param>
	/// <returns cref="EmbedBuilder"></returns>
	public static EmbedBuilder GetRoleResultEmbedBuilder(IRole role, IUser user)
	{
		return new EmbedBuilder()
			.WithColor(role.Color)
			.WithAuthor(new EmbedAuthorBuilder().WithName(user.Username).WithIconUrl(user.GetAvatarUrl()))
			.WithCurrentTimestamp();
	}

	#endregion
}
