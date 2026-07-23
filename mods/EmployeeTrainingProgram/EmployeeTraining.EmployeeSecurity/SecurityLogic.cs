using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using DG.Tweening;
using EmployeeTraining.Employee;
using MyBox;
using SupermarketSimulator.Clerk;
using UnityEngine;
using UnityEngine.AI;

namespace EmployeeTraining.EmployeeSecurity;

public class SecurityLogic
{
	private static readonly PrivateMtdStatic<bool> mtdIsDisplayAvailable = new PrivateMtdStatic<bool>(typeof(RestockingState), "IsDisplayAvailable", typeof(int));

	private static readonly System.Collections.Generic.Dictionary<int, float> lastDetectXpAt = new System.Collections.Generic.Dictionary<int, float>();

	private static readonly System.Collections.Generic.Dictionary<int, float> lastCatchXpAt = new System.Collections.Generic.Dictionary<int, float>();

	public static void SetSpeed(SecurityGuardAnimationController controller, int speedLevel, NavMeshAgent agent)
	{
		SecurityGuard component = ((Component)controller).GetComponent<SecurityGuard>();
		SecuritySkill skill = SecuritySkillManager.Instance.GetOrAssignSkill(component);
		Il2CppSystem.Collections.Generic.List<float> speeds = controller.m_RunnigSpeeds;
		if (speeds == null || speeds.Count == 0 || agent == null || skill == null)
		{
			return;
		}
		speedLevel = Mathf.Clamp(speedLevel, 0, speeds.Count - 1);
		float bootMult = Employee.BoostStacking.SpeedMultiplier(speeds, speedLevel);
		agent.speed = skill.AgentSpeed * bootMult;
		agent.angularSpeed = Mathf.Max(120f, skill.AgentAngularSpeed * bootMult);
		agent.acceleration = Mathf.Max(8f, skill.AgentAcceleration * bootMult);
	}

	public static IEnumerator Move(SecurityGuardAnimationController controller, Vector3 target, int speedLevel, NavMeshAgent agent)
	{
		SecurityGuard security = ((Component)controller).GetComponent<SecurityGuard>();
		SecuritySkill skill = SecuritySkillManager.Instance.GetOrAssignSkill(security);
		Il2CppSystem.Collections.Generic.List<float> speeds = controller.m_RunnigSpeeds;
		if (speeds == null || speeds.Count == 0)
		{
			yield break;
		}
		speedLevel = Mathf.Clamp(speedLevel, 0, speeds.Count - 1);
		controller.SetSpeed(speedLevel);
		float boost = Employee.BoostStacking.SpeedMultiplier(speeds, speedLevel);
		yield return controller.StartCoroutine(EmployeeLogicHelper.MoveTo((MonoBehaviour)(object)security, target, agent, boost, skill.TurningSpeed, 5f).WrapToIl2Cpp());
	}

	public static void GiveDetectExp(SecurityGuard security)
	{
		if ((Object)(object)security == (Object)null)
		{
			return;
		}
		try
		{
			int id = security.ID;
			float now = Time.unscaledTime;
			if (lastDetectXpAt.TryGetValue(id, out float previous) && now - previous < 1f)
			{
				return;
			}
			lastDetectXpAt[id] = now;
			SecuritySkill skill = SecuritySkillManager.Instance.GetOrAssignSkill(security);
			skill.AddExp(2);
			Plugin.LogInfo($"SecurityGuard[{id}] +2 XP (detect) totalExp={skill.TotalExp}");
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"Security detect XP failed: {ex.Message}");
		}
	}

	public static void OnShoplifterDetected(ChaseState state)
	{
		GiveDetectExp(((SecurityGuardState)state).securityGuard);
	}

	public static void OnShoplifterBeaten(Customer customer, bool isHitByGuard, SecurityGuard security)
	{
		if (!isHitByGuard || (Object)(object)security == (Object)null)
		{
			return;
		}
		try
		{
			int id = security.ID;
			float now = Time.unscaledTime;
			if (lastCatchXpAt.TryGetValue(id, out float previous) && now - previous < 1f)
			{
				return;
			}
			lastCatchXpAt[id] = now;
			SecuritySkill skill = SecuritySkillManager.Instance.GetOrAssignSkill(security);
			skill.AddExp(4);
			Plugin.LogInfo($"SecurityGuard[{id}] +4 XP (catch) totalExp={skill.TotalExp}");
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"Security catch XP failed: {ex.Message}");
		}
	}

	public static void FindNearbyProducts(CollectingState state, float m_DetectRadius)
	{
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		SecuritySkill skill = SecuritySkillManager.Instance.GetSkill(((SecurityGuardState)state).securityGuard);
		SecurityGuard securityGuard = ((SecurityGuardState)state).securityGuard;
		SecurityGuardStateController sc = ((SecurityGuardState)state).sc;
		float crateOpeningTime = skill.CrateOpeningTime;
		float collectingIntv = skill.CollectingIntv;
		List<Product> _SpreadedProducts = new List<Product>();
		foreach (Product p in securityGuard.ProductsToCollect)
		{
			_SpreadedProducts.Add(p);
		}
		if (_SpreadedProducts.Count == 0)
		{
			Collider[] array = Physics.OverlapSphere(((Component)sc).transform.position, m_DetectRadius);
			Product item = default(Product);
			foreach (Collider val in array)
			{
				if (((Component)((Component)val).transform).gameObject.layer == 27 && !(((Component)val).transform.position.y > 0.5f) && ((Component)val).TryGetComponent<Product>(out item))
				{
					_SpreadedProducts.Add(item);
				}
			}
		}
		IEnumerator OpenCrate()
		{
			securityGuard.GuardCrate.IsEnabled = true;
			yield return new WaitForSeconds(crateOpeningTime);
		}
		IEnumerator Delay()
		{
			yield return OpenCrate();
			foreach (Product item2 in _SpreadedProducts)
			{
				securityGuard.AddProductIntoCrate(item2);
				yield return new WaitForSeconds(collectingIntv);
			}
			securityGuard.ProductsToCollect.Clear();
			yield return null;
			securityGuard.StateRestocking();
		}
		securityGuard.StartCoroutine(Delay().WrapToIl2Cpp());
	}

	public static IEnumerator ProductRestockLoop(RestockingState state, Clerk m_Restocker, Crate m_Crate, Il2CppSystem.Collections.Generic.List<DisplaySlot> slots)
	{
		SecuritySkill skill = SecuritySkillManager.Instance.GetSkill(((SecurityGuardState)state).securityGuard);
		SecurityGuardStateController sc = ((SecurityGuardState)state).sc;
		SecurityGuard m_SecurityGuard = ((SecurityGuardState)state).securityGuard;
		float productPlacingIntv = skill.ProductPlacingIntv;
		float rotationTime = skill.RotationTime;
		List<Product> list = new List<Product>();
		foreach (Product p in m_Crate.UnlimitedProducts)
		{
			list.Add(p);
		}
		foreach (Product p in m_Crate.Products)
		{
			list.Add(p);
		}
		foreach (Product item in list)
		{
			if ((Object)(object)item == (Object)null)
			{
				Debug.LogWarning("Guard: Product was null");
				continue;
			}
			int productID = item.ProductSO.ID;
			if (Singleton<DisplayManager>.Instance.GetDisplaySlots(productID, false, slots) <= 0)
			{
				Debug.LogWarning("Guard: No display slot!");
				continue;
			}
			DisplaySlot targetDisplaySlot = null;
			foreach (DisplaySlot x in slots)
			{
				if (!x.Full)
				{
					targetDisplaySlot = x;
					break;
				}
			}
			if (!((targetDisplaySlot) != null))
			{
				Debug.LogWarning("Guard: No empty display slot!");
				continue;
			}
			targetDisplaySlot.m_OccupiedRestocker = m_Restocker;
			if (Vector3.Distance(targetDisplaySlot.InteractionPosition, ((Component)m_SecurityGuard).transform.position) > 0.4f)
			{
				var routine = sc.SecurityGuard.Controller.Move(targetDisplaySlot.InteractionPosition - targetDisplaySlot.InteractionPositionForward * 0.3f, 0);
				yield return ((MonoBehaviour)sc.SecurityGuard).StartCoroutine(routine);
			}
			ShortcutExtensions.DOKill((Component)(object)((Component)sc.SecurityGuard).transform, false);
			ShortcutExtensions.DORotateQuaternion(((Component)sc.SecurityGuard).transform, targetDisplaySlot.InteractionRotation, 0.4f);
			yield return (object)new WaitForSeconds(rotationTime);
			if (!mtdIsDisplayAvailable.Invoke(state, productID))
			{
				Debug.LogWarning("Guard: No available display slot!");
				continue;
			}
			Product product = m_Crate.RemoveProduct(productID);
			if (!((product) != null))
			{
				Debug.LogWarning("Guard: Remove product failed!");
				continue;
			}
			((Component)product).gameObject.SetActive(true);
			targetDisplaySlot.AddProduct(productID, product);
			skill.AddExp(1);
			yield return (object)new WaitForSeconds(productPlacingIntv);
			targetDisplaySlot.m_OccupiedRestocker = null;
		}
		if (m_SecurityGuard.ShouldChase)
		{
			m_SecurityGuard.StateChase();
		}
		else
		{
			m_SecurityGuard.StateIdle();
		}
	}
}
