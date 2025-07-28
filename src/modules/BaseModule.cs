using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using NexAoD.Extensions;
using NexAoD.Services;
using System.Diagnostics;

namespace NexAoD.Modules;

public class BaseModule : InteractionModuleBase<SocketInteractionContext>
{
	private readonly IServiceProvider _serviceProvider;
	private readonly UtilityService _utilityService;
	private readonly CommandHandlingService _commandHandlingService;

	public BaseModule(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
		_utilityService = _serviceProvider.GetRequiredService<UtilityService>();
		_commandHandlingService = serviceProvider.GetRequiredService<CommandHandlingService>();
	}

	[SlashCommand("stats", "My current info!")]
	public async Task StatsAsync()
	{
		DateTime ping = DateTime.UtcNow;
		await DeferAsync();

		ContainerBuilder builder = new ContainerBuilder();

		builder.WithAccentColor(0x7e686c);

		SectionBuilder sectionBuilder = new SectionBuilder()
			.WithTextDisplay(Context.Client.CurrentUser.GlobalName ?? Context.Client.CurrentUser.Username)
			.WithAccessory(new ThumbnailBuilder().WithMedia(new UnfurledMediaItemProperties(Context.Client.CurrentUser.GetDisplayAvatarUrl())));
		builder.WithSection(sectionBuilder).WithSeparator();

		builder.WithTextDisplay("Bot Information:");
		builder.WithTextDisplay(string.Join("\n",
			new string[]
			{
				$"Guilds: {Context.Client.Guilds.Count}",
				$"User Count: {Context.Client.Guilds.Sum(x => x.MemberCount)}",
				$"Categories: {Context.Client.Guilds.Sum(x => x.CategoryChannels.Count)}",
				$"Channels: {Context.Client.Guilds.Sum(x => x.Channels.Count)}",
				$"Voice Channels: {Context.Client.Guilds.Sum(x => x.VoiceChannels.Count)}",
				$"Commands Run: {_commandHandlingService.CommandsRun}"
			}).ToCodeBlock(true, "ml"));
		builder.WithSeparator();

		builder.WithTextDisplay("Host Information:");
		builder.WithTextDisplay(string.Join("\n",
			new string[]
			{
				$"Ping: {(DateTime.UtcNow - ping).TotalMilliseconds} MS",
				$"Uptime: {Process.GetCurrentProcess().StartTime}",
				$"Memory: {_utilityService.GetMemoryInformation()}"
			}).ToCodeBlock(true, "ml"));

		await FollowupAsync(components: new ComponentBuilderV2().WithContainer(builder).Build(), flags: MessageFlags.ComponentsV2);
	}


}
