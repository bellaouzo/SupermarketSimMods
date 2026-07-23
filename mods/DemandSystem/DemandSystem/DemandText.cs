using System;
using System.Collections.Generic;
using UnityEngine.Localization.Settings;

namespace DemandSystem;

internal readonly struct OverlayText
{
	internal OverlayText(string title, string subtitle, string rowNote)
	{
		Title = title;
		Subtitle = subtitle;
		RowNote = rowNote;
	}

	internal string Title { get; }
	internal string Subtitle { get; }
	internal string RowNote { get; }
}

internal static class DemandText
{
	private static readonly Dictionary<string, OverlayText> Texts = new Dictionary<string, OverlayText>(StringComparer.OrdinalIgnoreCase)
	{
		["en"] = new OverlayText("HIGH DEMAND TODAY", "Customers are likely to buy more of these products", "More demand in shopping carts"),
		["fr"] = new OverlayText("FORTE DEMANDE AUJOURD'HUI", "Les clients auront tendance à acheter plus de ces produits", "Plus de demande dans les paniers"),
		["de"] = new OverlayText("HEUTE HOHE NACHFRAGE", "Kunden kaufen heute wahrscheinlich mehr von diesen Produkten", "Mehr Nachfrage in Einkaufswagen"),
		["it"] = new OverlayText("ALTA DOMANDA OGGI", "I clienti tenderanno a comprare più di questi prodotti", "Più richiesta nei carrelli"),
		["es"] = new OverlayText("ALTA DEMANDA HOY", "Los clientes tenderán a comprar más de estos productos", "Más demanda en los carritos"),
		["pt"] = new OverlayText("DEMANDA ALTA HOJE", "Clientes tendem a comprar mais destes produtos", "Mais procura nos carrinhos"),
		["pt-BR"] = new OverlayText("DEMANDA ALTA HOJE", "Clientes tendem a comprar mais destes produtos", "Mais procura nos carrinhos"),
		["pt-PT"] = new OverlayText("PROCURA ELEVADA HOJE", "Os clientes tendem a comprar mais destes produtos", "Mais procura nos carrinhos"),
		["nl"] = new OverlayText("VANDAAG HOGE VRAAG", "Klanten kopen waarschijnlijk meer van deze producten", "Meer vraag in winkelwagens"),
		["tr"] = new OverlayText("BUGÜN YÜKSEK TALEP", "Müşteriler bu ürünlerden daha fazla satın alma eğiliminde", "Sepetlerde daha fazla talep"),
		["ru"] = new OverlayText("СЕГОДНЯ ВЫСОКИЙ СПРОС", "Покупатели будут чаще покупать эти товары", "Больше спроса в корзинах"),
		["zh"] = new OverlayText("今日高需求", "顾客更可能购买更多这些商品", "购物车需求增加"),
		["zh-Hans"] = new OverlayText("今日高需求", "顾客更可能购买更多这些商品", "购物车需求增加"),
		["ja"] = new OverlayText("本日は需要が高め", "お客様はこれらの商品を多めに購入しやすくなります", "買い物かごで需要増加"),
		["ko"] = new OverlayText("오늘 수요 높음", "고객들이 이 상품들을 더 많이 구매할 가능성이 높습니다", "장바구니 수요 증가"),
		["cs"] = new OverlayText("DNES VYSOKÁ POPTÁVKA", "Zákazníci budou těchto produktů kupovat více", "Větší poptávka v košících"),
		["da"] = new OverlayText("HØJ EFTERSPØRGSEL I DAG", "Kunderne vil sandsynligvis købe flere af disse produkter", "Mere efterspørgsel i kurve"),
		["fi"] = new OverlayText("TÄNÄÄN KORKEA KYSYNTÄ", "Asiakkaat ostavat todennäköisesti enemmän näitä tuotteita", "Enemmän kysyntää ostoskoreissa"),
		["hu"] = new OverlayText("MA NAGY A KERESLET", "A vásárlók valószínűleg többet vesznek ezekből a termékekből", "Nagyobb kereslet a kosarakban"),
		["lt"] = new OverlayText("ŠIANDIEN DIDELĖ PAKLAUSA", "Pirkėjai greičiausiai pirks daugiau šių produktų", "Didesnė paklausa krepšeliuose"),
		["pl"] = new OverlayText("DZIŚ WYSOKI POPYT", "Klienci będą częściej kupować te produkty", "Większy popyt w koszykach"),
		["ro"] = new OverlayText("CERERE MARE ASTĂZI", "Clienții tind să cumpere mai multe dintre aceste produse", "Cerere mai mare în coșuri")
	};

	internal static OverlayText Current()
	{
		string code = CurrentLocaleCode();
		if (Texts.TryGetValue(code, out OverlayText exact))
		{
			return exact;
		}

		int dash = code.IndexOf('-');
		if (dash > 0 && Texts.TryGetValue(code.Substring(0, dash), out OverlayText lang))
		{
			return lang;
		}

		return Texts["en"];
	}

	internal static string CurrentLocaleCode()
	{
		try
		{
			var locale = LocalizationSettings.SelectedLocale;
			if (locale != null && !string.IsNullOrEmpty(locale.Identifier.Code))
			{
				return locale.Identifier.Code;
			}
		}
		catch
		{
		}

		return "en";
	}
}
