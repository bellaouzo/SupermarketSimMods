using System.Collections.Generic;
using UnityEngine;

namespace EmployeeTraining.Employee;

internal static class BoostStacking
{
	public static int ClampIndex(int index, int count)
	{
		if (count <= 0)
		{
			return 0;
		}
		if (index < 0)
		{
			return 0;
		}
		if (index >= count)
		{
			return count - 1;
		}
		return index;
	}

	public static float SpeedMultiplier(Il2CppSystem.Collections.Generic.List<float> speeds, int boostLevel)
	{
		if (speeds == null || speeds.Count == 0)
		{
			return 1f;
		}
		int index = ClampIndex(boostLevel, speeds.Count);
		float baseline = Mathf.Max(speeds[0], 0.01f);
		return Mathf.Max(speeds[index] / baseline, 0.01f);
	}

	public static float SpeedMultiplier(List<float> speeds, int boostLevel)
	{
		if (speeds == null || speeds.Count == 0)
		{
			return 1f;
		}
		int index = ClampIndex(boostLevel, speeds.Count);
		float baseline = Mathf.Max(speeds[0], 0.01f);
		return Mathf.Max(speeds[index] / baseline, 0.01f);
	}

	public static float IntervalMultiplier(Il2CppSystem.Collections.Generic.List<float> intervals, int boostLevel)
	{
		if (intervals == null || intervals.Count == 0)
		{
			return 1f;
		}
		int index = ClampIndex(boostLevel, intervals.Count);
		float baseline = Mathf.Max(intervals[0], 0.01f);
		return Mathf.Max(intervals[index] / baseline, 0.05f);
	}

	public static float IntervalMultiplier(List<float> intervals, int boostLevel)
	{
		if (intervals == null || intervals.Count == 0)
		{
			return 1f;
		}
		int index = ClampIndex(boostLevel, intervals.Count);
		float baseline = Mathf.Max(intervals[0], 0.01f);
		return Mathf.Max(intervals[index] / baseline, 0.05f);
	}
}
