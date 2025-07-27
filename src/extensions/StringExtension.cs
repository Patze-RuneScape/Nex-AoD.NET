namespace NexAoD.Extensions;
public static class StringExtension
{
	public static ulong ToUlong(this string str)
	{
		return ulong.Parse(str);
	}

	public static string ToCodeBlock(this string str, bool multiLine = false)
	{
		return multiLine ? $"```{str}```" : $"`{str}`";
	}
}
