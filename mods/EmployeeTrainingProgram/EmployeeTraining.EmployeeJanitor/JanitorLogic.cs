using System;
using System.Collections;
using System.Reflection;
using __Project__.Scripts.Janitor;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using DG.Tweening;
using Lean.Pool;
using MyBox;
using UnityEngine;
using UnityEngine.AI;
using Il2CppListGameObject = Il2CppSystem.Collections.Generic.List<UnityEngine.GameObject>;
using Il2CppListParticle = Il2CppSystem.Collections.Generic.List<UnityEngine.ParticleSystem>;

namespace EmployeeTraining.EmployeeJanitor;

public class JanitorLogic
{
	private static readonly PrivateFldStatic<NavMeshAgent> fldJanitorAgent = new PrivateFldStatic<NavMeshAgent>(typeof(Janitor), "m_Agent");

	private static readonly PrivatePropStatic<Coroutine> propDustCleaningCoroutine = new PrivatePropStatic<Coroutine>(typeof(Dust), "CleaningCoroutine", BindingFlags.Instance | BindingFlags.Public);

	private static readonly PrivateMtdStatic mtdDustPlayCleanEffect = new PrivateMtdStatic(typeof(Dust), "PlayCleanEffect");

	private static readonly PrivateFldStatic<bool> fldDustIsClean = new PrivateFldStatic<bool>(typeof(Dust), "m_IsClean");

	private static readonly PrivateFldStatic<bool> fldDustIsCleaning = new PrivateFldStatic<bool>(typeof(Dust), "m_IsCleaning");

	private static readonly System.Collections.Generic.Dictionary<string, float> lastXpAt = new System.Collections.Generic.Dictionary<string, float>();

	public static void ApplyRapidity(Janitor janitor, int? boostLevel = null)
	{
		if (janitor == null)
		{
			return;
		}
		JanitorSkill skill = JanitorSkillManager.Instance.GetOrAssignSkill(janitor);
		NavMeshAgent agent = janitor.m_Agent ?? fldJanitorAgent.GetValue(janitor);
		if (agent == null || skill == null)
		{
			return;
		}
		int level = boostLevel ?? janitor.m_CurrentBoostLevel;
		float bootMult = Employee.BoostStacking.SpeedMultiplier(janitor.m_JanitorWalkingSpeeds, level);
		agent.speed = skill.AgentSpeed * bootMult;
		agent.angularSpeed = Mathf.Max(120f, skill.AgentAngularSpeed * bootMult);
		agent.acceleration = Mathf.Max(8f, skill.AgentAcceleration * bootMult);
	}

	public static void CleaningForJanitor(Janitor janitor, Dust dust, Il2CppListGameObject m_DustList, Il2CppListParticle m_BubbleParticles, ParticleSystem m_BubbleParticle, float m_DustCleaningMultiplier, int m_DustExp, float m_AlphaMax, float m_AlphaMin, int AlphaClip)
	{
		JanitorSkill orAssignSkill = JanitorSkillManager.Instance.GetOrAssignSkill(janitor);
		if (fldDustIsClean.GetValue(dust))
		{
			janitor.TargetObject = null;
		}
		else
		{
			dust.CleaningCoroutine = janitor.StartCoroutine(CleaningForJanitorCoroutine(janitor, dust, orAssignSkill, m_DustList, m_BubbleParticles, m_BubbleParticle, m_DustCleaningMultiplier, m_DustExp, m_AlphaMax, m_AlphaMin, AlphaClip).WrapToIl2Cpp());
		}
	}

	public static float GetCleanDuration(Janitor janitor)
	{
		JanitorSkill orAssignSkill = JanitorSkillManager.Instance.GetOrAssignSkill(janitor);
		return orAssignSkill.CleaningDuration;
	}

	public static void OnDirtCleaned(Janitor janitor)
	{
		GiveCleaningExp(janitor, 2, "dirt");
	}

	public static void GiveCleaningExp(Janitor janitor, int amount, string source)
	{
		if (janitor == null)
		{
			return;
		}
		try
		{
			int id = janitor.JanitorID;
			string debounceKey = source != null && source.StartsWith("garbage") ? $"{id}:garbage" : $"{id}:clean";
			float now = Time.unscaledTime;
			if (lastXpAt.TryGetValue(debounceKey, out float previous) && now - previous < 0.75f)
			{
				return;
			}
			lastXpAt[debounceKey] = now;
			JanitorSkill skill = JanitorSkillManager.Instance.GetOrAssignSkill(janitor);
			skill.AddExp(amount);
			Plugin.LogInfo($"Janitor[{id}] +{amount} XP ({source}) totalExp={skill.TotalExp}");
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"Janitor XP ({source}) failed: {ex.Message}");
		}
	}

	public static void OnGarbageCleaned(CleanGarbageAction action)
	{
		try
		{
			Janitor value = action?.janitorParam?.value;
			if (value == null)
			{
				return;
			}
			GiveCleaningExp(value, 2, "garbage");
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"Janitor garbage XP failed: {ex.Message}");
		}
	}

	public static void OnGarbagePickedUp(TrashBag trashBag)
	{
		if (trashBag == null)
		{
			return;
		}
		try
		{
			EmployeeManager manager = EmployeeManager.Instance;
			Il2CppSystem.Collections.Generic.List<Janitor> active = manager?.ActiveJanitor ?? manager?.m_ActiveJanitor;
			if (active == null)
			{
				return;
			}
			foreach (Janitor janitor in active)
			{
				if (janitor != null && janitor.TrashBag == trashBag)
				{
					GiveCleaningExp(janitor, 2, "garbage-pickup");
					return;
				}
			}
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"Janitor garbage-pickup XP failed: {ex.Message}");
		}
	}

	private static IEnumerator CleaningForJanitorCoroutine(Janitor janitor, Dust dust, JanitorSkill skill, Il2CppListGameObject m_DustList, Il2CppListParticle m_BubbleParticles, ParticleSystem m_BubbleParticle, float m_DustCleaningMultiplier, int m_DustExp, float m_AlphaMax, float m_AlphaMin, int AlphaClip)
	{
		float cleaningDuration = skill.CleaningDuration;
		float elapsedTime = dust.CurrentDustPercentage * cleaningDuration;
		while (elapsedTime < cleaningDuration && dust.m_CurrentAlpha < 1f)
		{
			elapsedTime = dust.CurrentDustPercentage * cleaningDuration;
			float num = Mathf.Clamp01(elapsedTime / cleaningDuration);
			float num2 = Time.deltaTime * m_DustCleaningMultiplier * Mathf.Min(m_AlphaMax / Mathf.Max(m_AlphaMax - num, 0.05f), 3f);
			num += num2;
			dust.m_CurrentAlpha = num * (m_AlphaMax - m_AlphaMin) + m_AlphaMin;
			foreach (GameObject gameObject in m_DustList)
			{
				ShortcutExtensions.DOKill(((Renderer)gameObject.GetComponent<MeshRenderer>()).material, false);
				((Renderer)gameObject.GetComponent<MeshRenderer>()).material.SetFloat(AlphaClip, dust.m_CurrentAlpha);
			}
			dust.dustingSaveData.DustingAlpha = dust.m_CurrentAlpha;
			Singleton<GarbageGenerator>.Instance.SaveDustingData(dust.dustingSaveData);
			Singleton<GarbageGenerator>.Instance.OnDirtLevelChange?.Invoke();
			if (Mathf.Abs(num - m_AlphaMax) < 0.01f)
			{
				fldDustIsClean.SetValue(dust, value: true);
				if (m_BubbleParticles.Count > 0)
				{
					dust.StopCleaningEffect();
				}
				mtdDustPlayCleanEffect.Invoke(dust);
				Singleton<StoreLevelManager>.Instance.AddPoint(m_DustExp);
				SaveManager.ProgressionContainer progression = Singleton<SaveManager>.Instance.Progression;
				progression.CleanedGlassCount++;
				Singleton<GarbageGenerator>.Instance.OnGlassCleaned?.Invoke(obj: false);
				skill.AddExp(2);
			}
			else if (Mathf.Abs(num - m_AlphaMax) >= 0.01f && m_BubbleParticles.Count < 1)
			{
				foreach (GameObject dust2 in m_DustList)
				{
					ParticleSystem particleSystem = LeanPool.Spawn<ParticleSystem>(m_BubbleParticle, dust2.transform.position, Quaternion.Euler(((Component)m_BubbleParticle).transform.eulerAngles.x, ((Component)m_BubbleParticle).transform.eulerAngles.y, 90f), (Transform)null);
					m_BubbleParticles.Add(particleSystem);
					Bounds bounds = dust2.GetComponent<Renderer>().bounds;
					Vector3 size = bounds.size;
					size = new Vector3(size.y, size.z, size.x);
					ParticleSystem.ShapeModule shape = particleSystem.shape;
					shape.scale = size;
					ParticleSystem.ShapeModule shape2 = ((Component)((Component)particleSystem).transform.GetChild(0)).GetComponent<ParticleSystem>().shape;
					shape2.scale = size;
					ParticleSystem.ShapeModule shape3 = ((Component)((Component)particleSystem).transform.GetChild(1)).GetComponent<ParticleSystem>().shape;
					shape3.scale = size;
					particleSystem.Play();
					shape = default(ParticleSystem.ShapeModule);
					shape2 = default(ParticleSystem.ShapeModule);
					shape3 = default(ParticleSystem.ShapeModule);
				}
				if (m_BubbleParticles.Count < 1)
				{
					Debug.Log("Bubble particle could not be spawned");
					yield break;
				}
			}
			yield return null;
		}
		fldDustIsClean.SetValue(dust, value: true);
		if (m_BubbleParticles.Count > 0)
		{
			dust.StopCleaningEffect();
		}
		mtdDustPlayCleanEffect.Invoke(dust);
		dust.m_CurrentAlpha = m_AlphaMax;
		SaveManager.ProgressionContainer progression2 = Singleton<SaveManager>.Instance.Progression;
		progression2.CleanedGlassCount++;
		Singleton<GarbageGenerator>.Instance.OnGlassCleaned?.Invoke(obj: false);
		fldDustIsCleaning.SetValue(dust, value: false);
		janitor.DustCleaningAnimation(false);
		janitor.Sponge.SetActive(false);
		janitor.TargetObject = null;
	}
}
