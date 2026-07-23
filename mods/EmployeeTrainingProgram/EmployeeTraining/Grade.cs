using UnityEngine;

namespace EmployeeTraining;

public class Grade
{
	public static readonly Grade Rookie = new Grade(0, 1, 9, 100f, "Rookie", new Color32((byte)0, (byte)175, (byte)240, byte.MaxValue));

	public static readonly Grade Middle = new Grade(1, 10, 24, 500f, "Middle", new Color32((byte)0, (byte)200, (byte)0, byte.MaxValue));

	public static readonly Grade Adv = new Grade(2, 25, 44, 2000f, "Advance", new Color32(byte.MaxValue, (byte)160, (byte)0, byte.MaxValue));

	public static readonly Grade Pro = new Grade(3, 45, 74, 5000f, "Pro", new Color32((byte)224, (byte)0, (byte)128, byte.MaxValue));

	public static readonly Grade Ninja = new Grade(4, 75, 100, null, "Ninja", new Color32((byte)196, (byte)128, byte.MaxValue, byte.MaxValue));

	public static readonly Grade[] List = new Grade[5] { Rookie, Middle, Adv, Pro, Ninja };

	public readonly int Order;

	public readonly int LvlMin;

	public readonly int LvlMax;

	public readonly float? Cost;

	public readonly string Name;

	public readonly Color32 Color;

	private Grade(int order, int lvlMin, int lvlMax, float? cost, string name, Color32 color)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		Order = order;
		LvlMin = lvlMin;
		LvlMax = lvlMax;
		Cost = cost;
		Name = name;
		Color = color;
	}

	public static bool operator <(Grade a, Grade b)
	{
		return a.Order < b.Order;
	}

	public static bool operator >(Grade a, Grade b)
	{
		return a.Order > b.Order;
	}

	public static bool operator <=(Grade a, Grade b)
	{
		return a.Order <= b.Order;
	}

	public static bool operator >=(Grade a, Grade b)
	{
		return a.Order >= b.Order;
	}
}
