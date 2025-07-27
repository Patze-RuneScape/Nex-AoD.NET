using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NexAoD.Extensions;
using NexAoD.Services;

namespace NexAoD.Modules;
public class SelfAssignModule : InteractionModuleBase<SocketInteractionContext>
{
	private readonly IServiceProvider _serviceProvider;
	private readonly LoggingService _loggingService;
	private readonly UtilityService _utilityService;

	public SelfAssignModule(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
		_loggingService = _serviceProvider.GetRequiredService<LoggingService>();
		_utilityService = _serviceProvider.GetRequiredService<UtilityService>();
	}

	/// <summary>
	/// Self-Assign Interaction for Buttons
	/// </summary>
	/// <param name="parameter">Custom-ID of the Button</param>
	/// <returns>Task</returns>
	[ComponentInteraction("b_selfassign_*")]
	public async Task ButtonSelfAssign(string parameter)
	{
		await DeferAsync(true);

		Embed result = await HandleSelfAssign(parameter);

		await FollowupAsync(embed: result);
	}

	/// <summary>
	/// Self-Assign Interaction for StringSelectMenus
	/// </summary>
	/// <param name="parameter">Custom-ID of the String-Select-Menu</param>
	/// <param name="entrySelections">Selected Entries of the String-Select-Menu</param>
	/// <returns>Task</returns>
	[ComponentInteraction("s_selfassign_*")]
	public async Task StringSelectSelfAssign(string parameter, string[] entrySelections)
	{
		await DeferAsync(true);

		foreach (string str in entrySelections)
		{
			Embed result = await HandleSelfAssign(str);
			await FollowupAsync(embed: result);
		}

		// reset the DropDown
		if (((IComponentInteraction)Context.Interaction).Message is IUserMessage msg)
		{
			await msg.ModifyAsync(m => m.Components = msg.Components as MessageComponent);
		}
	}

	/// <summary>
	/// Handles the Self Assign, parses the source and target-RoleIds on its own
	/// </summary>
	/// <param name="parameter"></param>
	/// <returns>Embed with the response for the user</returns>
	private async Task<Embed> HandleSelfAssign(string parameter)
	{
		SocketGuildUser user = (SocketGuildUser)Context.User;

		string[] roleIds = parameter.Split(';', StringSplitOptions.RemoveEmptyEntries);

		RestRole targetRole = await Context.Guild.GetRoleAsync(roleIds[0].ToUlong());

		// safety check, targetRole should never be a role with elevated permissions
		if (UtilityService.IsElevatedRole(targetRole))
		{
			await _loggingService.LogAsync(new LogMessage(LogSeverity.Critical, nameof(HandleSelfAssign), $"Attempted self assign of role {targetRole.Mention} with elevated permissions."), user);
			return UtilityService
				.GetResultEmbedBuilder(false, user)
				.WithDescription($"{targetRole.Mention} can not be assigned by self-assign. This incident has been logged.")
				.Build();
		}

		bool hasTargetRole = !user.Roles.Contains((IRole)targetRole);

		// if user has the role already it's an unassign which should always be possible, even if no longer eligible for the role
		if (hasTargetRole)
		{
			await _loggingService.LogAsync(new LogMessage(LogSeverity.Info, nameof(HandleSelfAssign), $"Removing Role {targetRole.Mention} from User {user.Mention}"), user);
			await user.RemoveRoleAsync(targetRole);
			return UtilityService
					.GetRoleResultEmbedBuilder(targetRole, user)
					.WithDescription($"Successfully removed role {targetRole.Mention}")
					.Build();
		}

		// check if the user has one of the needed roles
		List<RestRole> sourceRoles = (await Task.WhenAll(roleIds.Skip(1).Select(id => Context.Guild.GetRoleAsync(id.ToUlong())))).ToList();
		bool hasAnySourceRole = sourceRoles != null && sourceRoles.Count > 0 ? sourceRoles.Any(x => user.Roles.Contains((IRole)x)) : true;

		// if the user has the needed role, assign it to him
		if (hasAnySourceRole)
		{
			await _loggingService.LogAsync(new LogMessage(LogSeverity.Info, nameof(HandleSelfAssign), $"Assigning Role {targetRole.Mention} to User {user.Mention}"), user);
			await user.AddRoleAsync(targetRole);
			return UtilityService
					.GetRoleResultEmbedBuilder(targetRole, user)
					.WithDescription($"Successfully assigned role {targetRole.Mention}")
					.Build();
		}

		return UtilityService
				.GetResultEmbedBuilder(false, user)
				.WithDescription($"You need any of the following roles to be able to assign this tag: {string.Join(", ", sourceRoles!.Select(x => x.Mention))}")
				.Build();
	}
}
