using System;
using Newtonsoft.Json;

namespace EmployeeTraining.Localization;

[Serializable]
public class Texts
{
	[JsonProperty("Text")]
	private string text;

	public Texts(string text)
	{
		this.text = text;
	}

	public string Translate(params object[] args)
	{
		try
		{
			return string.Format(text, args);
		}
		catch (FormatException)
		{
			Plugin.LogWarn("Invalid format: " + text + " with args=[" + string.Join(", ", args) + "]");
			return "*TranslateError*";
		}
	}
}
