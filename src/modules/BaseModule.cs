using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using NexAod.Services;

namespace NexAod.Modules;

public class BaseModule : InteractionModuleBase<SocketInteractionContext>
{
	private readonly IServiceProvider _serviceProvider;
	private readonly UtilityService _utilityService;

	public BaseModule(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
		_utilityService = _serviceProvider.GetRequiredService<UtilityService>();
	}

	[SlashCommand("hello", "Hello World")]
	public async Task HelloWorldAsync()
	{
		await DeferAsync();
		await FollowupAsync("Hello World! from C#.");
	}
}
