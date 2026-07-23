using System;
using System.Collections.Generic;

namespace EmployeeTraining.Localization;

[Serializable]
public class Localizations : Dictionary<string, Texts>
{
	private string _language;

	public new string this[string id]
	{
		set
		{
			Add(id, new Texts(value));
		}
	}

	public string Language => _language;

	public Localizations(string Language)
	{
		_language = Language;
	}

	public Texts GetText(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return new Texts(string.Empty);
		}
		if (TryGetValue(text, out Texts value))
		{
			return value;
		}
		Plugin.LogWarn("Translation not found: " + text);
		return new Texts(text);
	}
}
