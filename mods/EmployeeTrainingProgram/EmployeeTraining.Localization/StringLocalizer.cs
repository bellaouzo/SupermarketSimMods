using System.Collections.Generic;
using System.Linq;

namespace EmployeeTraining.Localization;

public class StringLocalizer
{
	private readonly List<Localizations> localizedTexts;

	public string Lang { get; set; } = "en";

	public StringLocalizer(List<Localizations> texts)
	{
		localizedTexts = texts;
	}

	public Texts Get(string text)
	{
		return Get(text, Lang);
	}

	private Texts Get(string text, string language)
	{
		Localizations localizations = localizedTexts.Where((Localizations x) => x.Language == language).FirstOrDefault();
		if (localizations == null)
		{
			localizations = localizedTexts.Where((Localizations x) => x.Language == "en").FirstOrDefault();
			return localizations.GetText(text);
		}
		if (localizations.GetText(text) != null)
		{
			return localizations.GetText(text);
		}
		localizations = localizedTexts.Where((Localizations x) => x.Language == "en").FirstOrDefault();
		return localizations.GetText(text);
	}
}
