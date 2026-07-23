using System;
using System.Collections.Generic;
using EmployeeTraining.Employee;
using MyBox;

namespace EmployeeTraining.EmployeeCsHelper;

public class CsHelperSkill : EmployeeSkill<CsHelperSkill, CsHelperSkillTier, EmplCsHelper, CustomerHelper>
{
	private static readonly Dictionary<Grade, float> WAGE = new Dictionary<Grade, float>
	{
		{
			Grade.Rookie,
			80f
		},
		{
			Grade.Middle,
			100f
		},
		{
			Grade.Adv,
			140f
		},
		{
			Grade.Pro,
			200f
		},
		{
			Grade.Ninja,
			280f
		}
	};

	private static readonly Dictionary<Grade, float> HIRING_COST = new Dictionary<Grade, float>
	{
		{
			Grade.Rookie,
			150f
		},
		{
			Grade.Middle,
			200f
		},
		{
			Grade.Adv,
			270f
		},
		{
			Grade.Pro,
			350f
		},
		{
			Grade.Ninja,
			450f
		}
	};

	private static readonly CsHelperSkillTier[] SKILL_TABLE = new CsHelperSkillTier[100]
	{
		new CsHelperSkillTier
		{
			Lvl = 1,
			Exp = 0,
			IntervalMax = 2.4f,
			IntervalMin = 2f,
			Rapidity = 1.388889f
		},
		new CsHelperSkillTier
		{
			Lvl = 2,
			Exp = 50,
			IntervalMax = 1.818182f,
			IntervalMin = 1.5f,
			Rapidity = 1.444444f
		},
		new CsHelperSkillTier
		{
			Lvl = 3,
			Exp = 110,
			IntervalMax = 1.666667f,
			IntervalMin = 1.333333f,
			Rapidity = 1.5f
		},
		new CsHelperSkillTier
		{
			Lvl = 4,
			Exp = 180,
			IntervalMax = 1.578947f,
			IntervalMin = 1.2f,
			Rapidity = 1.555556f
		},
		new CsHelperSkillTier
		{
			Lvl = 5,
			Exp = 260,
			IntervalMax = 1.5f,
			IntervalMin = 1.153846f,
			Rapidity = 1.638889f
		},
		new CsHelperSkillTier
		{
			Lvl = 6,
			Exp = 360,
			IntervalMax = 1.363636f,
			IntervalMin = 1.111111f,
			Rapidity = 1.722222f
		},
		new CsHelperSkillTier
		{
			Lvl = 7,
			Exp = 480,
			IntervalMax = 1.276596f,
			IntervalMin = 1.071429f,
			Rapidity = 1.805556f
		},
		new CsHelperSkillTier
		{
			Lvl = 8,
			Exp = 620,
			IntervalMax = 1.2f,
			IntervalMin = 1.034483f,
			Rapidity = 1.888889f
		},
		new CsHelperSkillTier
		{
			Lvl = 9,
			Exp = 780,
			IntervalMax = 1.132075f,
			IntervalMin = 1f,
			Rapidity = 2f
		},
		new CsHelperSkillTier
		{
			Lvl = 10,
			Exp = 1080,
			IntervalMax = 1.090909f,
			IntervalMin = 0.857143f,
			Rapidity = 2.222222f
		},
		new CsHelperSkillTier
		{
			Lvl = 11,
			Exp = 1280,
			IntervalMax = 1.052632f,
			IntervalMin = 0.833333f,
			Rapidity = 2.277778f
		},
		new CsHelperSkillTier
		{
			Lvl = 12,
			Exp = 1500,
			IntervalMax = 0.983607f,
			IntervalMin = 0.810811f,
			Rapidity = 2.333333f
		},
		new CsHelperSkillTier
		{
			Lvl = 13,
			Exp = 1745,
			IntervalMax = 0.952381f,
			IntervalMin = 0.789474f,
			Rapidity = 2.388889f
		},
		new CsHelperSkillTier
		{
			Lvl = 14,
			Exp = 2015,
			IntervalMax = 0.902256f,
			IntervalMin = 0.764331f,
			Rapidity = 2.444444f
		},
		new CsHelperSkillTier
		{
			Lvl = 15,
			Exp = 2315,
			IntervalMax = 0.869565f,
			IntervalMin = 0.740741f,
			Rapidity = 2.5f
		},
		new CsHelperSkillTier
		{
			Lvl = 16,
			Exp = 2645,
			IntervalMax = 0.816327f,
			IntervalMin = 0.718563f,
			Rapidity = 2.583333f
		},
		new CsHelperSkillTier
		{
			Lvl = 17,
			Exp = 3010,
			IntervalMax = 0.774194f,
			IntervalMin = 0.693642f,
			Rapidity = 2.666667f
		},
		new CsHelperSkillTier
		{
			Lvl = 18,
			Exp = 3415,
			IntervalMax = 0.727273f,
			IntervalMin = 0.670391f,
			Rapidity = 2.75f
		},
		new CsHelperSkillTier
		{
			Lvl = 19,
			Exp = 3865,
			IntervalMax = 0.701754f,
			IntervalMin = 0.648649f,
			Rapidity = 2.833333f
		},
		new CsHelperSkillTier
		{
			Lvl = 20,
			Exp = 4370,
			IntervalMax = 0.674157f,
			IntervalMin = 0.625f,
			Rapidity = 2.916667f
		},
		new CsHelperSkillTier
		{
			Lvl = 21,
			Exp = 4940,
			IntervalMax = 0.648649f,
			IntervalMin = 0.603015f,
			Rapidity = 3f
		},
		new CsHelperSkillTier
		{
			Lvl = 22,
			Exp = 5585,
			IntervalMax = 0.625f,
			IntervalMin = 0.582524f,
			Rapidity = 3.111111f
		},
		new CsHelperSkillTier
		{
			Lvl = 23,
			Exp = 6320,
			IntervalMax = 0.6f,
			IntervalMin = 0.560748f,
			Rapidity = 3.222222f
		},
		new CsHelperSkillTier
		{
			Lvl = 24,
			Exp = 7160,
			IntervalMax = 0.576923f,
			IntervalMin = 0.540541f,
			Rapidity = 3.333333f
		},
		new CsHelperSkillTier
		{
			Lvl = 25,
			Exp = 9160,
			IntervalMax = 0.6f,
			IntervalMin = 0.5f,
			Rapidity = 3.472222f
		},
		new CsHelperSkillTier
		{
			Lvl = 26,
			Exp = 10005,
			IntervalMax = 0.591133f,
			IntervalMin = 0.493827f,
			Rapidity = 3.527778f
		},
		new CsHelperSkillTier
		{
			Lvl = 27,
			Exp = 10895,
			IntervalMax = 0.576923f,
			IntervalMin = 0.487805f,
			Rapidity = 3.583333f
		},
		new CsHelperSkillTier
		{
			Lvl = 28,
			Exp = 11855,
			IntervalMax = 0.566038f,
			IntervalMin = 0.48f,
			Rapidity = 3.638889f
		},
		new CsHelperSkillTier
		{
			Lvl = 29,
			Exp = 12910,
			IntervalMax = 0.550459f,
			IntervalMin = 0.472441f,
			Rapidity = 3.694444f
		},
		new CsHelperSkillTier
		{
			Lvl = 30,
			Exp = 14090,
			IntervalMax = 0.540541f,
			IntervalMin = 0.465116f,
			Rapidity = 3.777778f
		},
		new CsHelperSkillTier
		{
			Lvl = 31,
			Exp = 15425,
			IntervalMax = 0.528634f,
			IntervalMin = 0.456274f,
			Rapidity = 3.861111f
		},
		new CsHelperSkillTier
		{
			Lvl = 32,
			Exp = 16950,
			IntervalMax = 0.512821f,
			IntervalMin = 0.447761f,
			Rapidity = 3.944444f
		},
		new CsHelperSkillTier
		{
			Lvl = 33,
			Exp = 18700,
			IntervalMax = 0.502092f,
			IntervalMin = 0.43956f,
			Rapidity = 4.027778f
		},
		new CsHelperSkillTier
		{
			Lvl = 34,
			Exp = 20715,
			IntervalMax = 0.48583f,
			IntervalMin = 0.430108f,
			Rapidity = 4.111111f
		},
		new CsHelperSkillTier
		{
			Lvl = 35,
			Exp = 23035,
			IntervalMax = 0.474308f,
			IntervalMin = 0.421053f,
			Rapidity = 4.222222f
		},
		new CsHelperSkillTier
		{
			Lvl = 36,
			Exp = 25705,
			IntervalMax = 0.458015f,
			IntervalMin = 0.410959f,
			Rapidity = 4.333333f
		},
		new CsHelperSkillTier
		{
			Lvl = 37,
			Exp = 28770,
			IntervalMax = 0.446097f,
			IntervalMin = 0.401338f,
			Rapidity = 4.444444f
		},
		new CsHelperSkillTier
		{
			Lvl = 38,
			Exp = 32280,
			IntervalMax = 0.431655f,
			IntervalMin = 0.392157f,
			Rapidity = 4.555556f
		},
		new CsHelperSkillTier
		{
			Lvl = 39,
			Exp = 36285,
			IntervalMax = 0.41958f,
			IntervalMin = 0.382166f,
			Rapidity = 4.666667f
		},
		new CsHelperSkillTier
		{
			Lvl = 40,
			Exp = 40845,
			IntervalMax = 0.405405f,
			IntervalMin = 0.372671f,
			Rapidity = 4.805556f
		},
		new CsHelperSkillTier
		{
			Lvl = 41,
			Exp = 46020,
			IntervalMax = 0.394737f,
			IntervalMin = 0.363636f,
			Rapidity = 4.944444f
		},
		new CsHelperSkillTier
		{
			Lvl = 42,
			Exp = 51880,
			IntervalMax = 0.379747f,
			IntervalMin = 0.352941f,
			Rapidity = 5.083333f
		},
		new CsHelperSkillTier
		{
			Lvl = 43,
			Exp = 58495,
			IntervalMax = 0.365854f,
			IntervalMin = 0.342857f,
			Rapidity = 5.222222f
		},
		new CsHelperSkillTier
		{
			Lvl = 44,
			Exp = 65945,
			IntervalMax = 0.352941f,
			IntervalMin = 0.333333f,
			Rapidity = 5.361111f
		},
		new CsHelperSkillTier
		{
			Lvl = 45,
			Exp = 85945,
			IntervalMax = 0.352941f,
			IntervalMin = 0.3f,
			Rapidity = 5.555556f
		},
		new CsHelperSkillTier
		{
			Lvl = 46,
			Exp = 88960,
			IntervalMax = 0.346821f,
			IntervalMin = 0.295567f,
			Rapidity = 5.611111f
		},
		new CsHelperSkillTier
		{
			Lvl = 47,
			Exp = 92390,
			IntervalMax = 0.338983f,
			IntervalMin = 0.291262f,
			Rapidity = 5.666667f
		},
		new CsHelperSkillTier
		{
			Lvl = 48,
			Exp = 96285,
			IntervalMax = 0.333333f,
			IntervalMin = 0.287081f,
			Rapidity = 5.722222f
		},
		new CsHelperSkillTier
		{
			Lvl = 49,
			Exp = 100695,
			IntervalMax = 0.326087f,
			IntervalMin = 0.283019f,
			Rapidity = 5.777778f
		},
		new CsHelperSkillTier
		{
			Lvl = 50,
			Exp = 105680,
			IntervalMax = 0.319149f,
			IntervalMin = 0.277778f,
			Rapidity = 5.833333f
		},
		new CsHelperSkillTier
		{
			Lvl = 51,
			Exp = 111300,
			IntervalMax = 0.310881f,
			IntervalMin = 0.272727f,
			Rapidity = 5.916667f
		},
		new CsHelperSkillTier
		{
			Lvl = 52,
			Exp = 117625,
			IntervalMax = 0.304569f,
			IntervalMin = 0.267857f,
			Rapidity = 6f
		},
		new CsHelperSkillTier
		{
			Lvl = 53,
			Exp = 124725,
			IntervalMax = 0.29703f,
			IntervalMin = 0.263158f,
			Rapidity = 6.083333f
		},
		new CsHelperSkillTier
		{
			Lvl = 54,
			Exp = 132680,
			IntervalMax = 0.289855f,
			IntervalMin = 0.257511f,
			Rapidity = 6.166667f
		},
		new CsHelperSkillTier
		{
			Lvl = 55,
			Exp = 141570,
			IntervalMax = 0.28169f,
			IntervalMin = 0.252101f,
			Rapidity = 6.25f
		},
		new CsHelperSkillTier
		{
			Lvl = 56,
			Exp = 151485,
			IntervalMax = 0.275229f,
			IntervalMin = 0.246914f,
			Rapidity = 6.333333f
		},
		new CsHelperSkillTier
		{
			Lvl = 57,
			Exp = 162515,
			IntervalMax = 0.267857f,
			IntervalMin = 0.241935f,
			Rapidity = 6.444444f
		},
		new CsHelperSkillTier
		{
			Lvl = 58,
			Exp = 174760,
			IntervalMax = 0.26087f,
			IntervalMin = 0.23622f,
			Rapidity = 6.555556f
		},
		new CsHelperSkillTier
		{
			Lvl = 59,
			Exp = 188320,
			IntervalMax = 0.253165f,
			IntervalMin = 0.230769f,
			Rapidity = 6.666667f
		},
		new CsHelperSkillTier
		{
			Lvl = 60,
			Exp = 203315,
			IntervalMax = 0.246914f,
			IntervalMin = 0.225564f,
			Rapidity = 6.777778f
		},
		new CsHelperSkillTier
		{
			Lvl = 61,
			Exp = 219865,
			IntervalMax = 0.24f,
			IntervalMin = 0.220588f,
			Rapidity = 6.888889f
		},
		new CsHelperSkillTier
		{
			Lvl = 62,
			Exp = 238110,
			IntervalMax = 0.233463f,
			IntervalMin = 0.215054f,
			Rapidity = 7f
		},
		new CsHelperSkillTier
		{
			Lvl = 63,
			Exp = 258190,
			IntervalMax = 0.226415f,
			IntervalMin = 0.20979f,
			Rapidity = 7.111111f
		},
		new CsHelperSkillTier
		{
			Lvl = 64,
			Exp = 280265,
			IntervalMax = 0.220588f,
			IntervalMin = 0.204778f,
			Rapidity = 7.222222f
		},
		new CsHelperSkillTier
		{
			Lvl = 65,
			Exp = 304495,
			IntervalMax = 0.214286f,
			IntervalMin = 0.2f,
			Rapidity = 7.361111f
		},
		new CsHelperSkillTier
		{
			Lvl = 66,
			Exp = 331060,
			IntervalMax = 0.208333f,
			IntervalMin = 0.194805f,
			Rapidity = 7.5f
		},
		new CsHelperSkillTier
		{
			Lvl = 67,
			Exp = 360140,
			IntervalMax = 0.20202f,
			IntervalMin = 0.189873f,
			Rapidity = 7.638889f
		},
		new CsHelperSkillTier
		{
			Lvl = 68,
			Exp = 391935,
			IntervalMax = 0.196721f,
			IntervalMin = 0.185185f,
			Rapidity = 7.777778f
		},
		new CsHelperSkillTier
		{
			Lvl = 69,
			Exp = 426645,
			IntervalMax = 0.190476f,
			IntervalMin = 0.18018f,
			Rapidity = 7.916667f
		},
		new CsHelperSkillTier
		{
			Lvl = 70,
			Exp = 464500,
			IntervalMax = 0.185185f,
			IntervalMin = 0.175439f,
			Rapidity = 8.055556f
		},
		new CsHelperSkillTier
		{
			Lvl = 71,
			Exp = 505730,
			IntervalMax = 0.179641f,
			IntervalMin = 0.17094f,
			Rapidity = 8.194444f
		},
		new CsHelperSkillTier
		{
			Lvl = 72,
			Exp = 550585,
			IntervalMax = 0.174419f,
			IntervalMin = 0.166205f,
			Rapidity = 8.333333f
		},
		new CsHelperSkillTier
		{
			Lvl = 73,
			Exp = 599315,
			IntervalMax = 0.169014f,
			IntervalMin = 0.161725f,
			Rapidity = 8.472222f
		},
		new CsHelperSkillTier
		{
			Lvl = 74,
			Exp = 652190,
			IntervalMax = 0.164384f,
			IntervalMin = 0.15748f,
			Rapidity = 8.611111f
		},
		new CsHelperSkillTier
		{
			Lvl = 75,
			Exp = 852190,
			IntervalMax = 0.166667f,
			IntervalMin = 0.142857f,
			Rapidity = 10f
		},
		new CsHelperSkillTier
		{
			Lvl = 76,
			Exp = 952295,
			IntervalMax = 0.15894f,
			IntervalMin = 0.137931f,
			Rapidity = 10.111111f
		},
		new CsHelperSkillTier
		{
			Lvl = 77,
			Exp = 1055105,
			IntervalMax = 0.151899f,
			IntervalMin = 0.133333f,
			Rapidity = 10.222222f
		},
		new CsHelperSkillTier
		{
			Lvl = 78,
			Exp = 1161620,
			IntervalMax = 0.145455f,
			IntervalMin = 0.129032f,
			Rapidity = 10.333333f
		},
		new CsHelperSkillTier
		{
			Lvl = 79,
			Exp = 1273840,
			IntervalMax = 0.140515f,
			IntervalMin = 0.125786f,
			Rapidity = 10.444444f
		},
		new CsHelperSkillTier
		{
			Lvl = 80,
			Exp = 1394765,
			IntervalMax = 0.1359f,
			IntervalMin = 0.122699f,
			Rapidity = 10.555556f
		},
		new CsHelperSkillTier
		{
			Lvl = 81,
			Exp = 1528395,
			IntervalMax = 0.131579f,
			IntervalMin = 0.11976f,
			Rapidity = 10.666667f
		},
		new CsHelperSkillTier
		{
			Lvl = 82,
			Exp = 1679730,
			IntervalMax = 0.128068f,
			IntervalMin = 0.117417f,
			Rapidity = 10.777778f
		},
		new CsHelperSkillTier
		{
			Lvl = 83,
			Exp = 1856270,
			IntervalMax = 0.12474f,
			IntervalMin = 0.115163f,
			Rapidity = 10.888889f
		},
		new CsHelperSkillTier
		{
			Lvl = 84,
			Exp = 2068015,
			IntervalMax = 0.1222f,
			IntervalMin = 0.112994f,
			Rapidity = 11f
		},
		new CsHelperSkillTier
		{
			Lvl = 85,
			Exp = 2327465,
			IntervalMax = 0.118812f,
			IntervalMin = 0.110497f,
			Rapidity = 11.111111f
		},
		new CsHelperSkillTier
		{
			Lvl = 86,
			Exp = 2649620,
			IntervalMax = 0.116054f,
			IntervalMin = 0.108108f,
			Rapidity = 11.277778f
		},
		new CsHelperSkillTier
		{
			Lvl = 87,
			Exp = 3051980,
			IntervalMax = 0.113422f,
			IntervalMin = 0.10582f,
			Rapidity = 11.444444f
		},
		new CsHelperSkillTier
		{
			Lvl = 88,
			Exp = 3554545,
			IntervalMax = 0.111111f,
			IntervalMin = 0.104167f,
			Rapidity = 11.611111f
		},
		new CsHelperSkillTier
		{
			Lvl = 89,
			Exp = 4197315,
			IntervalMax = 0.10929f,
			IntervalMin = 0.102564f,
			Rapidity = 11.777778f
		},
		new CsHelperSkillTier
		{
			Lvl = 90,
			Exp = 5040290,
			IntervalMax = 0.107527f,
			IntervalMin = 0.10101f,
			Rapidity = 11.944444f
		},
		new CsHelperSkillTier
		{
			Lvl = 91,
			Exp = 6163470,
			IntervalMax = 0.10582f,
			IntervalMin = 0.099834f,
			Rapidity = 12.166667f
		},
		new CsHelperSkillTier
		{
			Lvl = 92,
			Exp = 7666855,
			IntervalMax = 0.10453f,
			IntervalMin = 0.098684f,
			Rapidity = 12.388889f
		},
		new CsHelperSkillTier
		{
			Lvl = 93,
			Exp = 9670445,
			IntervalMax = 0.10327f,
			IntervalMin = 0.097561f,
			Rapidity = 12.611111f
		},
		new CsHelperSkillTier
		{
			Lvl = 94,
			Exp = 12324240,
			IntervalMax = 0.102041f,
			IntervalMin = 0.096774f,
			Rapidity = 12.833333f
		},
		new CsHelperSkillTier
		{
			Lvl = 95,
			Exp = 15808240,
			IntervalMax = 0.10118f,
			IntervalMin = 0.096f,
			Rapidity = 13.055556f
		},
		new CsHelperSkillTier
		{
			Lvl = 96,
			Exp = 20342445,
			IntervalMax = 0.100334f,
			IntervalMin = 0.095238f,
			Rapidity = 13.333333f
		},
		new CsHelperSkillTier
		{
			Lvl = 97,
			Exp = 26196855,
			IntervalMax = 0.099502f,
			IntervalMin = 0.094787f,
			Rapidity = 13.611111f
		},
		new CsHelperSkillTier
		{
			Lvl = 98,
			Exp = 33701470,
			IntervalMax = 0.09901f,
			IntervalMin = 0.09434f,
			Rapidity = 13.888889f
		},
		new CsHelperSkillTier
		{
			Lvl = 99,
			Exp = 43256290,
			IntervalMax = 0.098684f,
			IntervalMin = 0.094044f,
			Rapidity = 14.166667f
		},
		new CsHelperSkillTier
		{
			Lvl = 100,
			Exp = 55361315,
			IntervalMax = 0.097561f,
			IntervalMin = 3f / 32f,
			Rapidity = 14.444444f
		}
	};

	public int CurrentBoostLevel
	{
		get
		{
			CustomerHelper employee = Employee;
			return employee == null ? 0 : employee.m_CurrentBoostLevel;
		}
		set
		{
			CustomerHelper employee = Employee;
			if (employee != null)
			{
				employee.m_CurrentBoostLevel = value;
			}
		}
	}

	public Il2CppSystem.Collections.Generic.List<float> CustomerHelperScanIntervals => Employee?.m_CustomerHelperScanIntervals;

	public Il2CppSystem.Collections.Generic.List<float> CustomerHelperWalkingSpeeds => Employee?.m_CustomerHelperWalkingSpeeds;

	public override CustomerHelper Employee
	{
		get => base.Employee;
		set => base.Employee = value;
	}

	public override string JobName => "Customer Helper";

	public float IntervalMin => base.Tier.IntervalMin;

	public float IntervalMax => base.Tier.IntervalMax;

	public float Rapidity => base.Tier.Rapidity * 3.6f;

	public float AgentSpeed => base.Tier.Rapidity;

	public float AgentAngularSpeed => Math.Max(0f, base.Tier.Rapidity - 2f) * 240f;

	public float AgentAcceleration => (base.Tier.Rapidity < 2f) ? (base.Tier.Rapidity * 4f) : (8f + (base.Tier.Rapidity - 2f) * 6f);

	public float TurningSpeed => 5f * base.Tier.Rapidity;

	internal override CsHelperSkillTier[] SkillTable => SKILL_TABLE;

	protected override float CostRateToLevelUp => 2f;

	public override float Wage => WAGE[base.Grade];

	public override float HiringCost => HIRING_COST[base.Grade];

	public CsHelperSkill(CsHelperSkillData data)
		: base((SkillData<CsHelperSkill>)data)
	{
	}

	public override float GetWage(Grade g)
	{
		return WAGE[g];
	}

	internal override void ApplyWageToGame(float dailyWage, float hiringCost)
	{
		Singleton<IDManager>.Instance.CustomerHelperSO(base.Id).DailyWage = dailyWage;
		Singleton<IDManager>.Instance.CustomerHelperSO(base.Id).HiringCost = hiringCost;
	}

	public override void Setup()
	{
		base.InitialWage = Singleton<IDManager>.Instance.CustomerHelperSO(base.Id).DailyWage;
		base.InitialHiringCost = Singleton<IDManager>.Instance.CustomerHelperSO(base.Id).HiringCost;
		UpdateStatus(init: true);
		base.OnLevelChanged = (Action<bool>)Delegate.Combine(base.OnLevelChanged, (Action<bool>)delegate
		{
			CsHelperLogic.ApplyRapidity(Employee);
		});
	}

	public override void Despawn()
	{
		Employee = null;
	}
}
