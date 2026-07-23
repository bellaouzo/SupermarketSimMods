namespace MultiBoxCarry;

internal static class OnThrowMessanger
{
	public static bool hasThrowMessage;

	public static int OpenBoxBlockDepth;

	public static void ClearMessage(string type)
	{
		if (type == "throw")
		{
			hasThrowMessage = false;
		}
		if (type == "box")
		{
			OpenBoxBlockDepth = 0;
		}
	}

	public static void GaveMessage(string type)
	{
		if (type == "throw")
		{
			hasThrowMessage = false;
		}
		if (type == "box")
		{
			OpenBoxBlockDepth--;
			if (OpenBoxBlockDepth < 0)
			{
				OpenBoxBlockDepth = 0;
			}
		}
	}

	public static void WriteMessage(string type)
	{
		if (type == "throw")
		{
			hasThrowMessage = true;
		}
		if (type == "box")
		{
			OpenBoxBlockDepth = 3;
		}
	}
}
