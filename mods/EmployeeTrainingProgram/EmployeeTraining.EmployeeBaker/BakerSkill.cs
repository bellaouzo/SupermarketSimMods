using System;
using System.Collections.Generic;
using EmployeeTraining.Employee;
using MyBox;
using UnityEngine;

namespace EmployeeTraining.EmployeeBaker;

public class BakerSkill : EmployeeSkill<BakerSkill, BakerSkillTier, EmplBaker, Baker>
{
	private static readonly Dictionary<Grade, float> WAGE = new Dictionary<Grade, float>
	{
		{ Grade.Rookie, 95f },
		{ Grade.Middle, 120f },
		{ Grade.Adv, 165f },
		{ Grade.Pro, 230f },
		{ Grade.Ninja, 310f }
	};

	private static readonly Dictionary<Grade, float> HIRING_COST = new Dictionary<Grade, float>
	{
		{ Grade.Rookie, 110f },
		{ Grade.Middle, 165f },
		{ Grade.Adv, 240f },
		{ Grade.Pro, 320f },
		{ Grade.Ninja, 420f }
	};

	private static readonly BakerSkillTier[] SKILL_TABLE = BuildTable();

	private static BakerSkillTier[] BuildTable()
	{
		int[] exp =
		{
			0, 50, 110, 180, 260, 360, 480, 620, 780, 1080,
			1280, 1500, 1745, 2015, 2315, 2645, 3010, 3415, 3865, 4370,
			4940, 5585, 6320, 7160, 9160, 10005, 10895, 11855, 12910, 14090,
			15425, 16950, 18700, 20715, 23035, 25705, 28770, 32280, 36285, 40845,
			46020, 51880, 58495, 65945, 85945, 88960, 92390, 96285, 100695, 105680,
			111300, 117625, 124725, 132680, 141570, 151485, 162515, 174760, 188320, 203315,
			219865, 238110, 258190, 280265, 304495, 331060, 360140, 391935, 426645, 464500,
			505730, 550585, 599315, 652190, 852190, 952295, 1055105, 1161620, 1273840, 1394765,
			1528395, 1679730, 1856270, 2068015, 2327465, 2649620, 3051980, 3554545, 4197315, 5040290,
			6163470, 7666855, 9670445, 12324240, 15808240, 20342445, 26196855, 33701470, 43256290, 55361315
		};
		float[] rapiditySrc =
		{
			1.388889f, 1.444444f, 1.5f, 1.555556f, 1.638889f, 1.722222f, 1.805556f, 1.888889f, 2f, 2.222222f,
			2.277778f, 2.333333f, 2.388889f, 2.444444f, 2.5f, 2.583333f, 2.666667f, 2.75f, 2.833333f, 2.916667f,
			3f, 3.111111f, 3.222222f, 3.333333f, 3.472222f, 3.527778f, 3.583333f, 3.638889f, 3.694444f, 3.777778f,
			3.861111f, 3.944444f, 4.027778f, 4.111111f, 4.222222f, 4.333333f, 4.444444f, 4.555556f, 4.666667f, 4.805556f,
			4.944444f, 5.083333f, 5.222222f, 5.361111f, 5.555556f, 5.611111f, 5.666667f, 5.722222f, 5.777778f, 5.833333f,
			5.916667f, 6f, 6.083333f, 6.166667f, 6.25f, 6.333333f, 6.444444f, 6.555556f, 6.666667f, 6.777778f,
			6.888889f, 7f, 7.111111f, 7.222222f, 7.361111f, 7.5f, 7.638889f, 7.777778f, 7.916667f, 8.055556f,
			8.194444f, 8.333333f, 8.472222f, 8.611111f, 10f, 10.111111f, 10.222222f, 10.333333f, 10.444444f, 10.555556f,
			10.666667f, 10.777778f, 10.888889f, 11f, 11.111111f, 11.277778f, 11.444444f, 11.611111f, 11.777778f, 11.944444f,
			12.166667f, 12.388889f, 12.611111f, 12.833333f, 13.055556f, 13.333333f, 13.611111f, 13.888889f, 14.166667f, 14.444444f
		};
		int[] dexterity =
		{
			70, 73, 76, 79, 82, 85, 88, 91, 100, 100,
			100, 100, 100, 100, 100, 100, 100, 100, 100, 100,
			100, 100, 100, 100, 100, 103, 106, 109, 112, 117,
			122, 127, 132, 137, 144, 151, 158, 165, 172, 181,
			190, 199, 208, 217, 250, 260, 270, 280, 290, 300,
			310, 320, 330, 340, 355, 370, 385, 400, 415, 430,
			445, 460, 475, 490, 510, 530, 550, 570, 590, 610,
			630, 650, 670, 690, 800, 820, 840, 860, 880, 905,
			930, 955, 980, 1005, 1030, 1055, 1080, 1105, 1130, 1160,
			1190, 1220, 1250, 1280, 1320, 1360, 1400, 1450, 1500, 1600
		};
		float srcMin = rapiditySrc[0];
		float srcMax = rapiditySrc[rapiditySrc.Length - 1];
		float dstMin = 1.388889f;
		float dstMax = 5f;
		BakerSkillTier[] table = new BakerSkillTier[100];
		for (int i = 0; i < 100; i++)
		{
			float t = (rapiditySrc[i] - srcMin) / (srcMax - srcMin);
			table[i] = new BakerSkillTier
			{
				Lvl = i + 1,
				Exp = exp[i],
				Rapidity = Mathf.Lerp(dstMin, dstMax, t),
				Dexterity = dexterity[i]
			};
		}
		return table;
	}

	public override Baker Employee
	{
		get => base.Employee;
		set => base.Employee = value;
	}

	public override string JobName => "Baker";

	public float Rapidity => base.Tier.Rapidity * 3.6f;

	public int Dexterity => base.Tier.Dexterity;

	public float AgentSpeed => base.Tier.Rapidity;

	public float AgentAngularSpeed => Math.Max(0f, base.Tier.Rapidity - 2f) * 240f;

	public float AgentAcceleration => (base.Tier.Rapidity < 2f) ? (base.Tier.Rapidity * 4f) : (8f + (base.Tier.Rapidity - 2f) * 6f);

	public float UnpackingTime => 0.85f / ((float)base.Tier.Dexterity / 100f);

	public float ProductPlacingIntv => 0.55f / ((float)base.Tier.Dexterity / 100f);

	public float TurningSpeed => 5f * base.Tier.Rapidity;

	internal override BakerSkillTier[] SkillTable => SKILL_TABLE;

	protected override float CostRateToLevelUp => 2f;

	public override float Wage => WAGE[base.Grade];

	public override float HiringCost => HIRING_COST[base.Grade];

	public BakerSkill(BakerSkillData data)
		: base(data)
	{
	}

	public override float GetWage(Grade g)
	{
		return WAGE[g];
	}

	internal override void ApplyWageToGame(float dailyWage, float hiringCost)
	{
		Singleton<IDManager>.Instance.BakerSO(base.Id).DailyWage = dailyWage;
		Singleton<IDManager>.Instance.BakerSO(base.Id).HiringCost = hiringCost;
	}

	public override void Setup()
	{
		base.InitialWage = Singleton<IDManager>.Instance.BakerSO(base.Id).DailyWage;
		base.InitialHiringCost = Singleton<IDManager>.Instance.BakerSO(base.Id).HiringCost;
		UpdateStatus(init: true);
		base.OnLevelChanged = (Action<bool>)Delegate.Combine(base.OnLevelChanged, (Action<bool>)delegate
		{
			BakerLogic.ApplyRapidity(Employee);
		});
	}

	public override void Despawn()
	{
		if ((Object)(object)ExpGaugeObj != (Object)null)
		{
			Object.Destroy((Object)(object)ExpGaugeObj);
			ExpGaugeObj = null;
		}
		Employee = null;
	}
}
