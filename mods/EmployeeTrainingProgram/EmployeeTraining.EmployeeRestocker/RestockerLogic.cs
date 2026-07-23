using System;
using BepInEx.Unity.IL2CPP.Utils;
using SupermarketSimulator.Clerk;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DG.Tweening;
using EmployeeTraining.Employee;
using EmployeeTraining.api;
using HarmonyLib;
using Lean.Pool;
using MyBox;
using UnityEngine;
using UnityEngine.AI;

namespace EmployeeTraining.EmployeeRestocker;

public class RestockerLogic
{
	public class Inventory : List<InventorySlot>
	{
		public IEnumerable<Box> Boxes => this.Select((InventorySlot s) => s.Box);

		public IEnumerable<int> ProductIds => (from s in this
			where s.Box.HasProducts
			select s.Box.Data.ProductID).Distinct();

		public void Add(Box box)
		{
			if (!((Object)(object)box == (Object)null))
			{
				base.Add(new InventorySlot(box, ((Component)box).gameObject.layer));
			}
		}

		public void Remove(Box box)
		{
			base.Remove(Find((InventorySlot s) => ((object)s.Box).Equals((object)box)));
		}

		public bool Contains(Box box)
		{
			return this.Any((InventorySlot s) => ((object)s.Box).Equals((object)box));
		}

		public int BoxLayer(Box box)
		{
			return this.FirstOrDefault((InventorySlot s) => ((object)s.Box).Equals((object)box))?.Layer ?? (-1);
		}
	}

	public class InventorySlot
	{
		public Box Box;

		public int Layer;

		public InventorySlot(Box box, int layer)
		{
			Box = box;
			Layer = layer;
		}
	}

	private static readonly BoxSize[] BoxTowerOrder;

	private static readonly Dictionary<BoxSize, int> BoxHeights;

	private readonly PrivateFld<RestockerState> fldState = new PrivateFld<RestockerState>(typeof(Restocker), "m_State");

	private readonly PrivateProp<int> fldTargetProductID = new PrivateProp<int>(typeof(Restocker), "m_TargetProductID");

	private readonly PrivateFld<DisplaySlot> fldTargetDisplaySlot = new PrivateFld<DisplaySlot>(typeof(Restocker), "m_TargetDisplaySlot");

	private readonly PrivateFld<RackSlot> fldTargetRackSlot = new PrivateFld<RackSlot>(typeof(Restocker), "m_TargetRackSlot");

	private readonly PrivateFld<bool> fldCheckTasks = new PrivateFld<bool>(typeof(Restocker), "m_CheckTasks");

	private readonly PrivateFld<LayerMask> fldCurrentBoxLayer = new PrivateFld<LayerMask>(typeof(Restocker), "m_CurrentBoxLayer");

	private readonly PrivateFld<Box> fldBox = new PrivateFld<Box>(typeof(Restocker), "m_Box");

	private readonly PrivateFld<NavMeshAgent> fldAgent = new PrivateFld<NavMeshAgent>(typeof(Restocker), "m_Agent");

	private readonly PrivateFld<float> fldMinFillRateForDisplaySlotsToRestock = new PrivateFld<float>(typeof(Restocker), "m_MinFillRateForDisplaySlotsToRestock");

	private readonly PrivateFld<bool> fldIsCarryBoxToRack = new PrivateFld<bool>(typeof(Restocker), "m_IsCarryBoxToRack");

	private readonly PrivateFld<Box> fldTargetBox = new PrivateFld<Box>(typeof(Restocker), "m_TargetBox");

	private readonly PrivateFld<int> fldCurrentBoostLevel = new PrivateFld<int>(typeof(Restocker), "m_CurrentBoostLevel");

	private readonly PrivateFld<List<float>> fldRestockerWalkingSpeeds = new PrivateFld<List<float>>(typeof(Restocker), "m_RestockerWalkingSpeeds");

	private readonly PrivateFld<List<float>> fldRestockerPlacingSpeeds = new PrivateFld<List<float>>(typeof(Restocker), "m_RestockerPlacingSpeeds");

	private readonly PrivateFld<Il2CppSystem.Collections.Generic.List<DisplaySlot>> fldCashedSlots = new PrivateFld<Il2CppSystem.Collections.Generic.List<DisplaySlot>>(typeof(Restocker), "m_CachedSlots");

	private readonly PrivateFld<bool> fldUsingVehicle = new PrivateFld<bool>(typeof(Restocker), "m_UsingVehicle");

	private readonly PrivateFld<CharacterModelComponent> fldModelComponent = new PrivateFld<CharacterModelComponent>(typeof(Restocker), "m_ModelComponent");

	private readonly PrivateFld<Il2CppSystem.Collections.Generic.Dictionary<int, Il2CppSystem.Collections.Generic.List<RackSlot>>> fldRackSlots = new PrivateFld<Il2CppSystem.Collections.Generic.Dictionary<int, Il2CppSystem.Collections.Generic.List<RackSlot>>>(typeof(RackManager), "m_RackSlots");

	private readonly PrivateFld<Il2CppSystem.Collections.Generic.List<Rack>> fldRacks = new PrivateFld<Il2CppSystem.Collections.Generic.List<Rack>>(typeof(RackManager), "m_Racks");

	private readonly Action ResetTargets;

	private readonly PrivateMtd mtdResetTargets = new PrivateMtd(typeof(Restocker), "ResetTargets");

	private readonly Func<IEnumerator> TryRestocking;

	private readonly PrivateMtd<IEnumerator> mtdTryRestocking = new PrivateMtd<IEnumerator>(typeof(Restocker), "TryRestocking");

	private readonly Func<IEnumerator> PlaceBoxFromStreet;

	private readonly PrivateMtd<IEnumerator> mtdPlaceBoxFromStreet = new PrivateMtd<IEnumerator>(typeof(Restocker), "PlaceBoxFromStreet");

	private readonly Action PlaceBox;

	private readonly PrivateMtd mtdPlaceBox = new PrivateMtd(typeof(Restocker), "PlaceBox");

	private readonly Func<IEnumerator> DropBox;

	private readonly PrivateMtd<IEnumerator> mtdDropBox = new PrivateMtd<IEnumerator>(typeof(Restocker), "DropBox");

	private readonly Func<bool, IEnumerator> PickUpBox;

	private readonly PrivateMtd<IEnumerator> mtdPickUpBox = new PrivateMtd<IEnumerator>(typeof(Restocker), "PickUpBox", typeof(bool));

	private readonly Func<IEnumerator> PerformRestocking;

	private readonly PrivateMtd<IEnumerator> mtdPerformRestocking = new PrivateMtd<IEnumerator>(typeof(Restocker), "PerformRestocking");

	private readonly Func<IEnumerator> PlaceProducts;

	private readonly PrivateMtd<IEnumerator> mtdPlaceProducts = new PrivateMtd<IEnumerator>(typeof(Restocker), "PlaceProducts");

	private readonly Func<IEnumerator> PlaceBoxToRack;

	private readonly PrivateMtd<IEnumerator> mtdPlaceBoxToRack = new PrivateMtd<IEnumerator>(typeof(Restocker), "PlaceBoxToRack");

	private readonly Func<bool> GetAvailableDisplaySlotToRestock;

	private readonly PrivateMtd<bool> mtdGetAvailableDisplaySlotToRestock = new PrivateMtd<bool>(typeof(Restocker), "GetAvailableDisplaySlotToRestock");

	private readonly Func<bool> CheckForAvailableRackSlotToTakeBox;

	private readonly PrivateMtd<bool> mtdCheckForAvailableRackSlotToTakeBox = new PrivateMtd<bool>(typeof(Restocker), "CheckForAvailableRackSlotToTakeBox");

	private readonly Func<bool> CheckForAvailableRackSlotToPlaceBox;

	private readonly PrivateMtd<bool> mtdCheckForAvailableRackSlotToPlaceBox = new PrivateMtd<bool>(typeof(Restocker), "CheckForAvailableRackSlotToPlaceBox");

	private readonly Func<int, int, bool> IsAvailableRackSlotToPlaceBox;

	private readonly PrivateMtd<bool> mtdIsAvailableRackSlotToPlaceBox = new PrivateMtd<bool>(typeof(Restocker), "IsAvailableRackSlotToPlaceBox", typeof(int), typeof(int));

	private readonly Func<Box, bool> HasEmptySpaceForMergeInAnyRack;

	private readonly PrivateMtd<bool> mtdHasEmptySpaceForMergeInAnyRack = new PrivateMtd<bool>(typeof(Restocker), "HasEmptySpaceForMergeInAnyRack", typeof(Box));

	private readonly Func<RackSlot, int, bool> IsRackSlotStillAvailable;

	private readonly PrivateMtd<bool> mtdIsRackSlotStillAvailable = new PrivateMtd<bool>(typeof(Restocker), "IsRackSlotStillAvailable", typeof(RackSlot), typeof(int));

	private readonly Func<IEnumerator> ThrowBoxToTrashBin;

	private readonly PrivateMtd<IEnumerator> mtdThrowBoxToTrashBin = new PrivateMtd<IEnumerator>(typeof(Restocker), "ThrowBoxToTrashBin");

	private readonly Func<Vector3, Quaternion, IEnumerator> GoTo;

	private readonly PrivateMtd<IEnumerator> mtdGoTo = new PrivateMtd<IEnumerator>(typeof(Restocker), "GoTo", typeof(Vector3), typeof(Quaternion));

	private readonly Func<RestockerState, IEnumerator> GoToWaiting;

	private readonly PrivateMtd<IEnumerator> mtdGoToWaiting = new PrivateMtd<IEnumerator>(typeof(Restocker), "GoToWaiting", typeof(RestockerState));

	private readonly Func<Box, RackSlot> HasBoxAtRackForMerge;

	private readonly PrivateMtd<RackSlot> mtdHasBoxAtRackForMerge = new PrivateMtd<RackSlot>(typeof(Restocker), "HasBoxAtRackForMerge", typeof(Box));

	private readonly Func<RackSlot, IEnumerator> MergeBox;

	private readonly PrivateMtd<IEnumerator> mtdMergeBox = new PrivateMtd<IEnumerator>(typeof(Restocker), "MergeBox", typeof(RackSlot));

	private readonly Func<IEnumerator> PlaceBoxFromVehicle;

	private readonly PrivateMtd<IEnumerator> mtdPlaceBoxFromVehicle = new PrivateMtd<IEnumerator>(typeof(Restocker), "PlaceBoxFromVehicle");

	private readonly RestockerSkill skill;

	private readonly Restocker restocker;

	private readonly Inventory inventory = new Inventory();

	private readonly Dictionary<int, int> planList = new Dictionary<int, int>();

	private readonly Dictionary<int, int> carryingBoxes = new Dictionary<int, int>();

	private int productsNeeded;

	private int totalCarryingWeight = 0;

	private int totalCarryingHeight = 0;

	private readonly List<DisplaySlot> occupiedDisplaySlots = new List<DisplaySlot>();

	private readonly Il2CppSystem.Collections.Generic.List<DisplaySlot> labeledEmptySlotsCache = new Il2CppSystem.Collections.Generic.List<DisplaySlot>(250);

	private RestockerState State
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			return fldState.Value;
		}
		set
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			fldState.Value = value;
		}
	}

	private int TargetProductID
	{
		get
		{
			return fldTargetProductID.Value;
		}
		set
		{
			fldTargetProductID.Value = value;
		}
	}

	private DisplaySlot TargetDisplaySlot
	{
		get
		{
			return fldTargetDisplaySlot.Value;
		}
		set
		{
			fldTargetDisplaySlot.Value = value;
		}
	}

	private RackSlot TargetRackSlot
	{
		get
		{
			return fldTargetRackSlot.Value;
		}
		set
		{
			fldTargetRackSlot.Value = value;
		}
	}

	private bool CheckTasks
	{
		get
		{
			return fldCheckTasks.Value;
		}
		set
		{
			fldCheckTasks.Value = value;
		}
	}

	private LayerMask CurrentBoxLayer
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			return fldCurrentBoxLayer.Value;
		}
		set
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			fldCurrentBoxLayer.Value = value;
		}
	}

	private Box Box
	{
		get
		{
			return fldBox.Value;
		}
		set
		{
			fldBox.Value = value;
		}
	}

	private NavMeshAgent Agent
	{
		get
		{
			return fldAgent.Value;
		}
		set
		{
			fldAgent.Value = value;
		}
	}

	private float MinFillRateForDisplaySlotsToRestock
	{
		get
		{
			return fldMinFillRateForDisplaySlotsToRestock.Value;
		}
		set
		{
			fldMinFillRateForDisplaySlotsToRestock.Value = value;
		}
	}

	private bool IsCarryBoxToRack
	{
		get
		{
			return fldIsCarryBoxToRack.Value;
		}
		set
		{
			fldIsCarryBoxToRack.Value = value;
		}
	}

	private Box TargetBox
	{
		get
		{
			return fldTargetBox.Value;
		}
		set
		{
			fldTargetBox.Value = value;
		}
	}

	private int CurrentBoostLevel
	{
		get
		{
			return fldCurrentBoostLevel.Value;
		}
		set
		{
			fldCurrentBoostLevel.Value = value;
		}
	}

	private List<float> RestockerWalkingSpeeds
	{
		get
		{
			return fldRestockerWalkingSpeeds.Value;
		}
		set
		{
			fldRestockerWalkingSpeeds.Value = value;
		}
	}

	private List<float> RestockerPlacingSpeeds
	{
		get
		{
			return fldRestockerPlacingSpeeds.Value;
		}
		set
		{
			fldRestockerPlacingSpeeds.Value = value;
		}
	}

	private Il2CppSystem.Collections.Generic.List<DisplaySlot> CachedSlots
	{
		get
		{
			return fldCashedSlots.Value;
		}
		set
		{
			fldCashedSlots.Value = value;
		}
	}

	private bool UsingVehicle
	{
		get
		{
			return fldUsingVehicle.Value;
		}
		set
		{
			fldUsingVehicle.Value = value;
		}
	}

	private CharacterModelComponent ModelComponent
	{
		get
		{
			return fldModelComponent.Value;
		}
		set
		{
			fldModelComponent.Value = value;
		}
	}

	private Il2CppSystem.Collections.Generic.Dictionary<int, Il2CppSystem.Collections.Generic.List<RackSlot>> RackSlots
	{
		get
		{
			return fldRackSlots.Value;
		}
		set
		{
			fldRackSlots.Value = value;
		}
	}

	private Il2CppSystem.Collections.Generic.List<Rack> Racks
	{
		get
		{
			return fldRacks.Value;
		}
		set
		{
			fldRacks.Value = value;
		}
	}

	private int CarryingCapacity => skill.CarryingCapacity;

	private int CarryingMaxHeight => skill.CarryingMaxHeight;

	private float ProductPlacingInterval => skill.ProductPlacingIntv;

	private float UnpackingTime => skill.UnpackingTime;

	private float TakingBoxTime => skill.TakingBoxTime;

	private float ThrowingBoxTime => skill.ThrowingBoxTime;

	private float MovingSpeed => skill.AgentSpeed;

	private float AngularSpeed => skill.AgentAngularSpeed;

	private float Acceleration => skill.AgentAcceleration;

	private float TurningSpeed => skill.TurningSpeed;

	private float RotationTime => skill.RotationTime;

	private static bool VerboseLog => Plugin.Instance.Settings.RestockerLog;

	public RestockerLogic(RestockerSkill skill, Restocker restocker)
	{
		this.skill = skill;
		this.restocker = restocker;
		fldState.Instance = restocker;
		fldTargetProductID.Instance = restocker;
		fldTargetDisplaySlot.Instance = restocker;
		fldTargetRackSlot.Instance = restocker;
		fldCheckTasks.Instance = restocker;
		fldCurrentBoxLayer.Instance = restocker;
		fldBox.Instance = restocker;
		fldAgent.Instance = restocker;
		fldIsCarryBoxToRack.Instance = restocker;
		fldMinFillRateForDisplaySlotsToRestock.Instance = restocker;
		fldTargetBox.Instance = restocker;
		fldCurrentBoostLevel.Instance = restocker;
		fldRestockerWalkingSpeeds.Instance = restocker;
		fldRestockerPlacingSpeeds.Instance = restocker;
		fldCashedSlots.Instance = restocker;
		fldUsingVehicle.Instance = restocker;
		fldModelComponent.Instance = restocker;
		fldRackSlots.Instance = Singleton<RackManager>.Instance;
		fldRacks.Instance = Singleton<RackManager>.Instance;
		ResetTargets = delegate
		{
			mtdResetTargets.Invoke();
		};
		mtdResetTargets.Instance = restocker;
		TryRestocking = () => mtdTryRestocking.Invoke();
		mtdTryRestocking.Instance = restocker;
		PlaceBoxFromStreet = () => mtdPlaceBoxFromStreet.Invoke();
		mtdPlaceBoxFromStreet.Instance = restocker;
		PlaceBox = delegate
		{
			mtdPlaceBox.Invoke();
		};
		mtdPlaceBox.Instance = restocker;
		DropBox = () => mtdDropBox.Invoke();
		mtdDropBox.Instance = restocker;
		PickUpBox = (bool isFromRack) => mtdPickUpBox.Invoke(isFromRack);
		mtdPickUpBox.Instance = restocker;
		PerformRestocking = () => mtdPerformRestocking.Invoke();
		mtdPerformRestocking.Instance = restocker;
		PlaceProducts = () => mtdPlaceProducts.Invoke();
		mtdPlaceProducts.Instance = restocker;
		PlaceBoxToRack = () => mtdPlaceBoxToRack.Invoke();
		mtdPlaceBoxToRack.Instance = restocker;
		GetAvailableDisplaySlotToRestock = () => mtdGetAvailableDisplaySlotToRestock.Invoke();
		mtdGetAvailableDisplaySlotToRestock.Instance = restocker;
		CheckForAvailableRackSlotToTakeBox = () => mtdCheckForAvailableRackSlotToTakeBox.Invoke();
		mtdCheckForAvailableRackSlotToTakeBox.Instance = restocker;
		CheckForAvailableRackSlotToPlaceBox = () => mtdCheckForAvailableRackSlotToPlaceBox.Invoke();
		mtdCheckForAvailableRackSlotToPlaceBox.Instance = restocker;
		IsAvailableRackSlotToPlaceBox = (int productID, int boxID) => mtdIsAvailableRackSlotToPlaceBox.Invoke(productID, boxID);
		mtdIsAvailableRackSlotToPlaceBox.Instance = restocker;
		HasEmptySpaceForMergeInAnyRack = (Box box) => mtdHasEmptySpaceForMergeInAnyRack.Invoke(box);
		mtdHasEmptySpaceForMergeInAnyRack.Instance = restocker;
		IsRackSlotStillAvailable = (RackSlot rackSlot, int productId) => mtdIsRackSlotStillAvailable.Invoke(rackSlot, productId);
		mtdIsRackSlotStillAvailable.Instance = restocker;
		ThrowBoxToTrashBin = () => mtdThrowBoxToTrashBin.Invoke();
		mtdThrowBoxToTrashBin.Instance = restocker;
		GoTo = (Vector3 position, Quaternion rotation) => mtdGoTo.Invoke(position, rotation);
		mtdGoTo.Instance = restocker;
		GoToWaiting = (RestockerState state) => mtdGoToWaiting.Invoke(state);
		mtdGoToWaiting.Instance = restocker;
		HasBoxAtRackForMerge = (Box box) => mtdHasBoxAtRackForMerge.Invoke(box);
		mtdHasBoxAtRackForMerge.Instance = restocker;
		MergeBox = (RackSlot slot) => mtdMergeBox.Invoke(slot);
		mtdMergeBox.Instance = restocker;
		PlaceBoxFromVehicle = () => mtdPlaceBoxFromVehicle.Invoke();
		mtdPlaceBoxFromVehicle.Instance = restocker;
	}

	public void AfterResetRestocker()
	{
		UnoccupyBoxes();
		inventory.Clear();
		carryingBoxes.Clear();
		planList.Clear();
		UpdateCarryingWeightAndHeight();
	}

	public void UnoccupyBoxes()
	{
		inventory.ForEach(delegate(InventorySlot s)
		{
			s.Box.SetOccupy(false, (Transform)null);
		});
	}

	public void Internal_DropTheBox()
	{
		if ((Object)(object)Box == (Object)null || !restocker.CarryingBox)
		{
			return;
		}
		foreach (InventorySlot item in inventory)
		{
			Box box = item.Box;
			Singleton<InventoryManager>.Instance.RemoveBox(box.Data);
			LeanPool.Despawn(((Component)box).gameObject);
			((Component)box).gameObject.layer = item.Layer;
			box.ResetBox();
		}
		UnoccupyBoxes();
		inventory.Clear();
		carryingBoxes.Clear();
		planList.Clear();
		UpdateCarryingWeightAndHeight();
		Box = null;
		TargetBox = null;
		restocker.CarryingBox = false;
		State = (RestockerState)0;
		CheckTasks = true;
	}

	public void Internal_DropBoxToGround()
	{
		foreach (InventorySlot item in inventory)
		{
			item.Box.DropBox();
			((Component)item.Box).gameObject.layer = item.Layer;
			Singleton<StorageStreet>.Instance.SubscribeBox(item.Box);
		}
		UnoccupyBoxes();
		inventory.Clear();
		carryingBoxes.Clear();
		planList.Clear();
		UpdateCarryingWeightAndHeight();
		Box = null;
		TargetBox = null;
		restocker.CarryingBox = false;
		CheckTasks = true;
	}

	public IEnumerator Internal_TryRestocking()
	{
		if ((int)State > 0)
		{
			yield break;
		}
		State = (RestockerState)1;
		ResetTargets();
		Singleton<InventoryManager>.Instance.UpdateOrderedProducts();
		planList.Clear();
		carryingBoxes.Clear();
		MinFillRateForDisplaySlotsToRestock = 1f;
		UpdateCarryingWeightAndHeight();
		bool doneRestocking = false;
		if (restocker.ManagementData.RestockFromVehicles)
		{
			yield return PlaceBoxFromVehicle();
			if (UsingVehicle)
			{
				yield break;
			}
		}
		List<int> productsToRestock = MakeAPlanToRestock();
		restocker.FreeTargetDisplaySlot();
		yield return null;
		for (int j = 0; j < productsToRestock.Count; j++)
		{
			TargetProductID = productsToRestock[j];
			productsNeeded = GetTotalDisplayCapacity(TargetProductID) - Singleton<DisplayManager>.Instance.GetDisplayedProductCount(TargetProductID);
			LogStat($"Collecting goods for {Singleton<IDManager>.Instance.ProductSO(TargetProductID).name} x {productsNeeded}");
			while (productsNeeded > 0 && GetAvailableDisplaySlotToRestock() && restocker.ManagementData.RestockShelf)
			{
				CheckTasks = false;
				bool isBoxFromRack = true;
				if (restocker.ManagementData.PickUpBoxGround)
				{
					LogSimple("Trying to get a box from street");
					TargetBox = GetBoxFromStreet();
				}
				if ((Object)(object)TargetBox != (Object)null && restocker.ManagementData.PickUpBoxGround)
				{
					LogSimple("Found a box and aiming for TargetBox=" + (TargetBox?.ToBoxInfo() ?? "NULL"));
					TargetBox.SetOccupy(true, ((Component)restocker).transform);
					isBoxFromRack = false;
					Vector3 target = Vector3.MoveTowards(((Component)TargetBox).transform.position, ((Component)restocker).transform.position, 0.35f);
					Vector3 position = ((Component)TargetBox).transform.position;
					position.y = ((Component)restocker).transform.position.y;
					Quaternion rotation = Quaternion.LookRotation(position, Vector3.up);
					yield return ((MonoBehaviour)restocker).StartCoroutine(GoTo(target, rotation));
				}
				else
				{
					TargetBox = null;
					bool foundAvailableRack = false;
					while (!foundAvailableRack && CheckForAvailableRackSlotToTakeBox())
					{
						LogStat("going to the rack " + TargetRackSlot.ToRackInfo());
						yield return ((MonoBehaviour)restocker).StartCoroutine(GoTo(TargetRackSlot.InteractionPosition, TargetRackSlot.InteractionRotation));
						if ((Object)(object)TargetRackSlot != (Object)null)
						{
							bool isRackActive = ((Component)TargetRackSlot).gameObject.activeInHierarchy;
							bool isRackStillAvailable = IsRackSlotStillAvailable(TargetRackSlot, TargetProductID);
							Plugin.LogDebug($"active={isRackActive}, still available={isRackStillAvailable}");
							if (isRackActive && isRackStillAvailable)
							{
								foundAvailableRack = true;
							}
						}
					}
					if (!foundAvailableRack)
					{
						break;
					}
				}
				if (!IsDisplaySlotAvailableToRestock(TargetDisplaySlot))
				{
					TargetDisplaySlot.m_OccupiedRestocker = null;
					occupiedDisplaySlots.Remove(TargetDisplaySlot);
					LogSimple("Trying to get another display slot");
					if (!GetAvailableDisplaySlotToRestock())
					{
						break;
					}
				}
				LogSimple("Trying to pick up TargetBox=" + (TargetBox?.ToBoxInfo() ?? "NULL"));
				if ((Object)(object)TargetBox == (Object)null || !TargetBox.IsBoxOccupied || (Object)(object)TargetBox.OccupyOwner == (Object)(object)((Component)restocker).transform)
				{
					yield return ((MonoBehaviour)restocker).StartCoroutine(PickUpBox(isBoxFromRack));
					if (isBoxFromRack && (Object)(object)TargetRackSlot != (Object)null && restocker.ManagementData.RemoveLabelRack && !TargetRackSlot.HasBox)
					{
						TargetRackSlot.ClearLabel();
					}
				}
			}
		}
		LogStat("has finished collection, will restock");
		List<int> productIds = inventory.ProductIds.ToList();
		foreach (int id in productIds)
		{
			TargetProductID = id;
			Singleton<DisplayManager>.Instance.GetDisplaySlots(TargetProductID, false, CachedSlots);
			Singleton<DisplayManager>.Instance.GetLabeledEmptyDisplaySlots(TargetProductID, labeledEmptySlotsCache);
			foreach (DisplaySlot emptySlot in labeledEmptySlotsCache)
			{
				CachedSlots.Add(emptySlot);
			}
			Box = inventory.Boxes.FirstOrDefault((Box b) => b.HasProducts && b.Data.ProductID == TargetProductID);
			bool foundAvailableDisplaySlot = false;
			foreach (DisplaySlot cachedSlot in CachedSlots)
			{
				DisplaySlot slot = (TargetDisplaySlot = cachedSlot);
				if (IsDisplaySlotAvailableToRestock(slot))
				{
					if (((Object)(object)TargetBox != (Object)null && !TargetBox.HasProducts) || ((Object)(object)TargetBox != (Object)null && (Object)(object)TargetBox.OccupyOwner != (Object)(object)((Component)restocker).transform))
					{
						break;
					}
					LogStat($"going to the display {TargetDisplaySlot}");
					TargetDisplaySlot.m_OccupiedRestocker = restocker != null ? restocker.TryCast<Clerk>() : null;
					occupiedDisplaySlots.Add(TargetDisplaySlot);
					yield return ((MonoBehaviour)restocker).StartCoroutine(GoTo(TargetDisplaySlot.InteractionPosition - TargetDisplaySlot.InteractionPositionForward * 0.3f, TargetDisplaySlot.InteractionRotation));
					if ((Object)(object)TargetDisplaySlot != (Object)null && ((Component)TargetDisplaySlot).gameObject.activeInHierarchy && !TargetDisplaySlot.Full && IsDisplaySlotAvailableToRestock(TargetDisplaySlot) && TargetProductID == TargetDisplaySlot.ProductID)
					{
						foundAvailableDisplaySlot = true;
						break;
					}
					if (((TargetDisplaySlot) != null))
					{
						TargetDisplaySlot.m_OccupiedRestocker = null;
						occupiedDisplaySlots.Remove(TargetDisplaySlot);
					}
				}
			}
			if (!foundAvailableDisplaySlot)
			{
				if (((TargetDisplaySlot) != null))
				{
					TargetDisplaySlot.m_OccupiedRestocker = null;
				}
				occupiedDisplaySlots.Remove(TargetDisplaySlot);
			}
			else
			{
				LogStat($"restocking to {TargetDisplaySlot}");
				yield return ((MonoBehaviour)restocker).StartCoroutine(PerformRestocking());
				LogStat("done restocking");
			}
			doneRestocking = true;
		}
		LogStat("goes dropping box");
		yield return ((MonoBehaviour)restocker).StartCoroutine(DropBox());
		if (doneRestocking)
		{
			skill.AddExp(2);
		}
		else if (!HasBox())
		{
			restocker.FreeTargetDisplaySlot();
			planList.Clear();
			IsCarryBoxToRack = false;
			if (restocker.ManagementData.PickUpBoxGround)
			{
				yield return ((MonoBehaviour)restocker).StartCoroutine(PlaceBoxFromStreet());
			}
			if (!restocker.CarryingBox && !IsCarryBoxToRack && (int)State > 0)
			{
				yield return ((MonoBehaviour)restocker).StartCoroutine(restocker.SoftResetRestocker());
			}
		}
	}

	private Box GetBoxFromStreet()
	{
		Il2CppSystem.Collections.Generic.List<Box> streetBoxes = Singleton<StorageStreet>.Instance.boxes;
		streetBoxes.RemoveAll((Il2CppSystem.Predicate<Box>)((Box x) => x.Racked));
		streetBoxes.RemoveAll((Il2CppSystem.Predicate<Box>)((Box x) => !((Component)x).gameObject.activeInHierarchy));
		Il2CppSystem.Collections.Generic.List<Box> boxesFromStreet = Singleton<StorageStreet>.Instance.GetBoxesFromStreet();
		bool pickUpBoxGround = restocker.ManagementData.PickUpBoxGround;
		List<Box> list = new List<Box>();
		foreach (Box x in boxesFromStreet)
		{
			bool occupiedBySelf = x.IsBoxOccupied && (Object)(object)x.OccupyOwner == (Object)(object)((Component)restocker).transform;
			if (x.HasProducts && !x.Racked && x.Product.ID == TargetProductID && ((Component)x).gameObject.activeInHierarchy && (!x.IsBoxOccupied || occupiedBySelf))
			{
				list.Add(x);
			}
			else if (pickUpBoxGround && !x.HasProducts && (!x.IsBoxOccupied || occupiedBySelf))
			{
				list.Add(x);
			}
		}
		if (list.Count <= 0)
		{
			return null;
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	private List<int> MakeAPlanToRestock()
	{
		//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c3: Unknown result type (might be due to invalid IL or missing references)
		List<Customer> customersInShopping = ShoppingCustomerList.Instance.CustomersInShopping;
		bool flag = customersInShopping.Count > 0 && customersInShopping.Any((Customer c) => c.ShoppingList != null && c.ShoppingList.HasProduct);
		bool canFullyStock = !flag;
		if (VerboseLog)
		{
			Plugin.LogDebug($"Restocker[{skill.Id}] canFullyStock: {canFullyStock}");
			Plugin.LogDebug(string.Format("ActiveCustomers={0}, {1}", customersInShopping.Count, Il2CppHelpers.Join(customersInShopping.Select(delegate(Customer c)
			{
				ItemQuantity shoppingList = c.ShoppingList;
				return "[" + ((shoppingList != null) ? Il2CppHelpers.Join(Il2CppHelpers.KeysToList(shoppingList.Products), ", ") : null) + "]";
			}), ", ")));
		}
		DisplayManager dispMgr = Singleton<DisplayManager>.Instance;
		Dictionary<int, int> carrying = CollectProductsCarrying();
		List<(int ProductID, int Capacity, int Stocking)> demands = new List<(int, int, int)>();
		foreach (Il2CppSystem.Collections.Generic.KeyValuePair<int, Il2CppSystem.Collections.Generic.List<DisplaySlot>> pair in dispMgr.DisplayedProducts)
		{
			if (pair.Value == null || pair.Value.Count <= 0 || pair.Key <= 0)
			{
				continue;
			}
			int capacity = GetTotalDisplayCapacity(pair.Key);
			int displayed = dispMgr.GetDisplayedProductCount(pair.Key);
			carrying.TryGetValue(pair.Key, out int carried);
			int stocking = displayed + carried;
			if ((float)stocking < (float)capacity * (canFullyStock ? 1f : 0.8f))
			{
				demands.Add((pair.Key, capacity, stocking));
			}
		}
		List<KeyValuePair<int, int>> list = (from d in demands
			orderby d.Stocking
			select new KeyValuePair<int, int>(d.ProductID, d.Capacity - d.Stocking)).ToList();
		if (VerboseLog)
		{
			Plugin.LogDebug(string.Format("Restocker[{0}] Demands: {1}", restocker.RestockerID, Il2CppHelpers.Join(list.Select((KeyValuePair<int, int> p) => $"[{Singleton<IDManager>.Instance.ProductSO(p.Key).name} x{p.Value}]"), "")));
		}
		int num = 0;
		int num2 = 0;
		foreach (KeyValuePair<int, int> item in list)
		{
			if (num > CarryingCapacity || num2 > CarryingMaxHeight)
			{
				break;
			}
			int key = item.Key;
			int num3 = item.Value;
			List<List<BoxData>> boxesInRacks = GetBoxesInRacks(key);
			if (boxesInRacks == null)
			{
				continue;
			}
			BoxSize boxSize = Singleton<IDManager>.Instance.ProductSO(key).GridLayoutInBox.boxSize;
			int num4 = BoxHeights[boxSize];
			foreach (List<BoxData> item2 in boxesInRacks)
			{
				Stack<BoxData> stack = new Stack<BoxData>(item2);
				while (stack.Count > 0)
				{
					BoxData val = stack.Pop();
					int num5 = ProductWeight.CalcWeight(val);
					if (num3 > 0 && (num == 0 || num + num5 <= CarryingCapacity) && (num2 == 0 || num2 + num4 <= CarryingMaxHeight))
					{
						if (!planList.TryAdd(key, val.ProductCount))
						{
							planList[key] += val.ProductCount;
						}
						num3 -= val.ProductCount;
						num += num5;
						num2 += num4;
						continue;
					}
					break;
				}
			}
		}
		if (VerboseLog)
		{
			LogSimple("Planned: " + Il2CppHelpers.Join(planList.Select((KeyValuePair<int, int> p) => $"[{Singleton<IDManager>.Instance.ProductSO(p.Key).name} x{p.Value}]"), ""));
			LogSimple($"Total weight: {num}, Capacity: {CarryingCapacity}");
		}
		return planList.Select((KeyValuePair<int, int> p) => p.Key).ToList();
	}

	private int GetTotalDisplayCapacity(int productId)
	{
		Il2CppSystem.Collections.Generic.List<DisplaySlot> list = new Il2CppSystem.Collections.Generic.List<DisplaySlot>();
		Singleton<DisplayManager>.Instance.GetDisplaySlots(productId, false, list);
		int total = 0;
		HashSet<DisplaySlot> seen = new HashSet<DisplaySlot>();
		foreach (DisplaySlot i in list)
		{
			if (seen.Add(i))
			{
				total += GetCapacityInDisplaySlot(i);
			}
		}
		return total;
	}

	private int GetCapacityInDisplaySlot(DisplaySlot displaySlot)
	{
		if (displaySlot.Data == null || displaySlot.Data.FirstItemID <= 0)
		{
			return 0;
		}
		foreach (IModdedDisplayHandler item in ModdedDisplayManager.Registry)
		{
			if (item.IsTargetDisplay(displaySlot))
			{
				return item.GetProductCountOfGridLayout(displaySlot);
			}
		}
		ProductSO val = Singleton<IDManager>.Instance.ProductSO(displaySlot.Data.FirstItemID);
		return val.GridLayoutInStorage.productCount;
	}

	private Dictionary<int, int> CollectProductsCarrying()
	{
		return (from i in RestockerSkillManager.Instance.GetActiveLogics()
			from j in i.planList
			select j).GroupBy(delegate(KeyValuePair<int, int> k)
		{
			KeyValuePair<int, int> keyValuePair = k;
			return keyValuePair.Key;
		}, delegate(KeyValuePair<int, int> k)
		{
			KeyValuePair<int, int> keyValuePair = k;
			return keyValuePair.Value;
		}).ToDictionary((IGrouping<int, int> g) => g.Key, (IGrouping<int, int> g) => g.Sum());
	}

	private List<List<BoxData>> GetBoxesInRacks(int productID)
	{
		List<BoxData> street = new List<BoxData>();
		foreach (Box box in Singleton<StorageStreet>.Instance.boxes)
		{
			if (box.HasProducts && !box.Racked && box.Product.ID == productID && ((Component)box).gameObject.activeInHierarchy)
			{
				street.Add(box.Data);
			}
		}
		List<List<BoxData>> result = new List<List<BoxData>>();
		result.Add(street);
		if (RackSlots.ContainsKey(productID))
		{
			foreach (RackSlot rack in RackSlots[productID])
			{
				if (rack.HasProduct)
				{
					result.Add(Il2CppHelpers.ToSystemList(rack.Data.RackedBoxDatas));
				}
			}
		}
		return result;
	}

	private List<Box> GetBoxListOnStreet()
	{
		Il2CppSystem.Collections.Generic.List<int> idList = restocker.GetAvailableProductIDList();
		Il2CppSystem.Collections.Generic.List<Box> boxes = Singleton<StorageStreet>.Instance.boxes;
		boxes.RemoveAll((Il2CppSystem.Predicate<Box>)((Box x) => x.Racked));
		boxes.RemoveAll((Il2CppSystem.Predicate<Box>)((Box x) => !((Component)x).gameObject.activeInHierarchy));
		List<Box> list = new List<Box>();
		HashSet<int> ids = new HashSet<int>();
		foreach (int id in idList)
		{
			ids.Add(id);
		}
		foreach (Box x in boxes)
		{
			if (x.HasProducts && !x.Racked && ids.Contains(x.Data.ProductID) && ((Component)x).gameObject.activeInHierarchy)
			{
				list.Add(x);
			}
			else if (!x.HasProducts)
			{
				list.Add(x);
			}
		}
		return list;
	}

	private int GetRackCapacityOfSpaceFor(int productID)
	{
		LogStat($"called GetRackCapacityOfSpaceFor: productId={productID}");
		Plugin.LogDebug($"Racks: {Racks?.Count}");
		ProductSO pso = Singleton<IDManager>.Instance.ProductSO(productID);
		BoxSO bso = FindBoxSO(pso.GridLayoutInBox.boxSize);
		int num = 0;
		foreach (Rack rack in Racks)
		{
			foreach (RackSlot slot in rack.RackSlots)
			{
				if (slot.Data.ProductID == productID && !slot.Full)
				{
					num += bso.GridLayout.boxCount - slot.Data.BoxCount;
				}
			}
		}
		if (num > 0)
		{
			Plugin.LogDebug($"Slots: {num}");
			return num;
		}
		LogStat("collecting empty rack slots");
		int empty = 0;
		foreach (Rack rack in Racks)
		{
			foreach (RackSlot slot in rack.RackSlots)
			{
				if (slot.Data.ProductID == -1 && !slot.HasBox)
				{
					empty += bso.GridLayout.boxCount;
				}
			}
		}
		return empty;
	}

	private Dictionary<int, int> CollectBoxesBeingCarried()
	{
		return (from i in RestockerSkillManager.Instance.GetActiveLogics()
			from j in i.carryingBoxes
			select j).GroupBy(delegate(KeyValuePair<int, int> k)
		{
			KeyValuePair<int, int> keyValuePair = k;
			return keyValuePair.Key;
		}, delegate(KeyValuePair<int, int> k)
		{
			KeyValuePair<int, int> keyValuePair = k;
			return keyValuePair.Value;
		}).ToDictionary((IGrouping<int, int> g) => g.Key, (IGrouping<int, int> g) => g.Sum());
	}

	public List<int> Internal_GetAvailableProductIDList()
	{
		List<int> list = new List<int>();
		foreach (Box item in Singleton<StorageStreet>.Instance.GetBoxesFromStreet())
		{
			if ((Object)(object)item != (Object)null && (Object)(object)item.Product != (Object)null && item.Product.ID > -1)
			{
				if (IsAvailableRackSlotToPlaceBox(item.Data.ProductID, item.BoxID))
				{
					list.Add(item.Data.ProductID);
				}
				else if (HasEmptySpaceForMergeInAnyRack(item))
				{
					list.Add(item.Data.ProductID);
				}
			}
		}
		return list;
	}

	public IEnumerator Internal_PlaceBoxFromVehicle()
	{
		UsingVehicle = false;
		List<Box> boxesToCarry = new List<Box>();
		Dictionary<int, int> totalCarryingBoxes = CollectBoxesBeingCarried();
		VehicleRigidbodyStopDuration vehicleRigidbodyStopDuration = default(VehicleRigidbodyStopDuration);
		RestockAreaBoxController restockAreaBoxController = default(RestockAreaBoxController);
		Box box = default(Box);
		foreach (GameObject vehicleObj in EnumerateVehicles())
		{
			if (((vehicleObj) != null) && vehicleObj.activeInHierarchy && Singleton<StorageStreet>.Instance.IsWithinRestockableArea(vehicleObj.transform.position) && (!vehicleObj.TryGetComponent<VehicleRigidbodyStopDuration>(out vehicleRigidbodyStopDuration) || vehicleRigidbodyStopDuration.HasStopped))
			{
				IPlacementArea componentInChildren = vehicleObj.GetComponentInChildren<IPlacementArea>();
				if (componentInChildren != null)
				{
					foreach (SortableBox sortableBox in EnumerateBoxes(componentInChildren))
					{
						if (((sortableBox) != null) && ((Component)sortableBox).TryGetComponent<RestockAreaBoxController>(out restockAreaBoxController) && restockAreaBoxController.HasLeftArea && ((Component)sortableBox).TryGetComponent<Box>(out box))
						{
							if (!box.IsBoxOccupied && !box.Racked && box.HasProducts && !IsBoxOvercapacity(box))
							{
								int pid = box.Data.ProductID;
								int productBoxCnt = boxesToCarry.Where((Box b) => b.Data.ProductID == pid).Count();
								if (productBoxCnt < GetRackCapacityOfSpaceFor(pid) - (totalCarryingBoxes.TryGetValue(pid, out int __tmp) ? __tmp : 0))
								{
									TargetBox = box;
									Box = TargetBox;
									TargetProductID = Box.Product.ID;
									box.SetOccupy(true, ((Component)restocker).transform);
									boxesToCarry.Add(box);
									AddCarryingBox(box);
								}
							}
							TargetBox = null;
						}
						restockAreaBoxController = null;
						box = null;
					}
				}
			}
			vehicleRigidbodyStopDuration = null;
		}
		foreach (Box item in boxesToCarry)
		{
			Box box2 = (TargetBox = item);
			Box = box2;
			UsingVehicle = true;
			Vector3 position = ((Component)TargetBox).transform.position;
			Vector3 val = ((Component)TargetBox).transform.position - ((Component)restocker).transform.position;
			Vector3 target = position - val.normalized * 1f;
			Quaternion rotation = Quaternion.LookRotation(((Component)TargetBox).transform.position, Vector3.up);
			yield return GoTo(target, rotation);
			if ((Object)(object)Box.OccupyOwner != (Object)(object)((Component)restocker).transform)
			{
				State = (RestockerState)0;
				CheckTasks = true;
				TargetBox = null;
				Box = null;
			}
			else
			{
				CheckTasks = false;
				yield return PickUpBox(arg: false);
				GetAvailableDisplaySlotToRestock();
				CheckTasks = true;
			}
		}
		yield return DropBox();
		yield return null;
	}

	public IEnumerator Internal_PlaceBoxFromStreet()
	{
		LogStat("called PlaceBoxFromStreet");
		IsCarryBoxToRack = false;
		List<Box> boxesToCarry = new List<Box>();
		Dictionary<int, int> totalCarryingBoxes = CollectBoxesBeingCarried();
		foreach (Box box in GetBoxListOnStreet())
		{
			if ((Object)(object)box == (Object)null || (box.IsBoxOccupied && (Object)(object)box.OccupyOwner != (Object)(object)((Component)restocker).transform) || box.Racked || !box.HasProducts || IsBoxOvercapacity(box))
			{
				continue;
			}
			int pid = box.Data.ProductID;
			int productBoxCnt = boxesToCarry.Where((Box b) => b.Data.ProductID == pid).Count();
			if (productBoxCnt < GetRackCapacityOfSpaceFor(pid) - (totalCarryingBoxes.TryGetValue(pid, out int __tmp) ? __tmp : 0))
			{
				box.SetOccupy(true, ((Component)restocker).transform);
				boxesToCarry.Add(box);
				AddCarryingBox(box);
				if (!carryingBoxes.TryAdd(pid, 1))
				{
					carryingBoxes[pid]++;
				}
			}
		}
		foreach (Box box2 in boxesToCarry)
		{
			TargetBox = box2;
			TargetProductID = TargetBox.Data.ProductID;
			Box = TargetBox;
			Vector3 target = Vector3.MoveTowards(((Component)TargetBox).transform.position, ((Component)restocker).transform.position, 0.35f);
			Quaternion rotation = Quaternion.LookRotation(((Component)TargetBox).transform.position, Vector3.up);
			yield return ((MonoBehaviour)restocker).StartCoroutine(GoTo(target, rotation));
			if (!((TargetBox) != null) || TargetBox.Racked)
			{
				DoneCarryingBox(TargetBox);
				TargetBox = null;
				TargetProductID = -1;
				continue;
			}
			LogStat("picking up from street: " + TargetBox.ToBoxInfo());
			yield return ((MonoBehaviour)restocker).StartCoroutine(PickUpBox(arg: false));
			if (HasBox())
			{
				IsCarryBoxToRack = true;
			}
		}
		yield return DropBox();
		if (IsCarryBoxToRack)
		{
			skill.AddExp(2);
		}
		LogStat("finished PlaceBoxFromStreet");
	}

	public IEnumerator Internal_DropBox()
	{
		if ((Object)(object)TargetDisplaySlot != (Object)null && TargetDisplaySlot.IsOccupiedByOthers(restocker != null ? restocker.TryCast<Clerk>() : null))
		{
			occupiedDisplaySlots.Remove(TargetDisplaySlot);
			TargetDisplaySlot.m_OccupiedRestocker = null;
		}
		if (!HasBox())
		{
			UpdateCarryingWeightAndHeight();
			yield break;
		}
		if (inventory.Boxes.Any((Box b) => !b.HasProducts))
		{
			yield return ((MonoBehaviour)restocker).StartCoroutine(ThrowBoxToTrashBin());
		}
		if (inventory.Boxes.Any((Box b) => b.HasProducts))
		{
			yield return ((MonoBehaviour)restocker).StartCoroutine(PlaceBoxToRack());
		}
		UpdateCarryingWeightAndHeight();
	}

	public IEnumerator Internal_PerformRestocking()
	{
		DoneRestocking(Box);
		yield return ((MonoBehaviour)restocker).StartCoroutine(PlaceProducts());
		Box = inventory.Boxes.FirstOrDefault((Box b) => b.HasProducts && b.Data.ProductID == TargetProductID);
		while ((Object)(object)Box != (Object)null && Box.HasProducts && GetAvailableDisplaySlotToRestock())
		{
			LogSimple("Trying to restock " + Box.ToBoxInfo());
			yield return ((MonoBehaviour)restocker).StartCoroutine(GoTo(TargetDisplaySlot.InteractionPosition - TargetDisplaySlot.InteractionPositionForward * 0.3f, TargetDisplaySlot.InteractionRotation));
			DoneRestocking(Box);
			if ((Object)(object)TargetDisplaySlot == (Object)null || !((Component)TargetDisplaySlot).gameObject.activeInHierarchy || TargetDisplaySlot.Full || !IsDisplaySlotAvailableToRestock(TargetDisplaySlot) || TargetProductID != TargetDisplaySlot.ProductID)
			{
				occupiedDisplaySlots.Remove(TargetDisplaySlot);
				TargetDisplaySlot.m_OccupiedRestocker = null;
			}
			else
			{
				LogStat("calling PlaceProducts");
				yield return ((MonoBehaviour)restocker).StartCoroutine(PlaceProducts());
			}
			Box = inventory.Boxes.FirstOrDefault((Box b) => b.HasProducts && b.Data.ProductID == TargetProductID);
		}
		if ((int)State == 2)
		{
			((MonoBehaviour)restocker).StartCoroutine(DropBox());
		}
	}

	public IEnumerator Internal_PlaceBoxToRack()
	{
		LogStat("called PlaceBoxToRack");
		Box = inventory.Boxes.FirstOrDefault((Box b) => b.HasProducts);
		if ((Object)(object)Box == (Object)null)
		{
			LogSimple("No box to place");
			if (inventory.Boxes.Any((Box b) => !b.HasProducts))
			{
				yield return ((MonoBehaviour)restocker).StartCoroutine(ThrowBoxToTrashBin());
			}
			restocker.CarryingBox = false;
			State = (RestockerState)0;
			((MonoBehaviour)restocker).StartCoroutine(TryRestocking());
			yield break;
		}
		TargetProductID = Box.Data.ProductID;
		while (restocker.CarryingBox)
		{
			RackSlot rackSlot = HasBoxAtRackForMerge(Box);
			LogStat("trying to merge " + Box?.ToBoxInfo() + " to rack slot");
			yield return ((MonoBehaviour)restocker).StartCoroutine(MergeBox(rackSlot));
			if (!Box.HasProducts)
			{
				DoneCarryingBox(TargetProductID);
				Box = inventory.Boxes.FirstOrDefault((Box b) => b.HasProducts && b.Data.ProductID == TargetProductID);
				if ((Object)(object)Box != (Object)null)
				{
					continue;
				}
				Box = inventory.Boxes.FirstOrDefault((Box b) => b.HasProducts);
				if (!((Object)(object)Box == (Object)null))
				{
					TargetProductID = Box.Data.ProductID;
					continue;
				}
				Box = inventory.Boxes.FirstOrDefault();
			}
			if (Box.HasProducts)
			{
				if (!CheckForAvailableRackSlotToPlaceBox())
				{
					break;
				}
				LogStat($"going to rack {TargetRackSlot}");
				yield return ((MonoBehaviour)restocker).StartCoroutine(GoTo(TargetRackSlot.InteractionPosition, TargetRackSlot.InteractionRotation));
				if (!((Object)(object)TargetRackSlot == (Object)null) && ((Component)TargetRackSlot).gameObject.activeInHierarchy && !TargetRackSlot.Full && (!TargetRackSlot.HasProduct || TargetRackSlot.Data.ProductID == TargetProductID) && (TargetRackSlot.Data.ProductID == -1 || TargetRackSlot.Data.ProductID == TargetProductID) && (TargetRackSlot.HasProduct || !TargetRackSlot.HasBox) && (restocker.ManagementData.UseUnlabeledRacks || TargetRackSlot.HasLabel))
				{
					LogStat("placing box " + Box?.ToBoxInfo());
					DoneCarryingBox(Box);
					PlaceBox();
				}
			}
			if (!HasBox() || !inventory.Boxes.Any((Box b) => b.HasProducts))
			{
				if (inventory.Boxes.Any((Box b) => !b.HasProducts))
				{
					yield return ((MonoBehaviour)restocker).StartCoroutine(ThrowBoxToTrashBin());
				}
				LogStat("done placing box");
				restocker.CarryingBox = false;
				State = (RestockerState)0;
				((MonoBehaviour)restocker).StartCoroutine(TryRestocking());
				yield break;
			}
		}
		if (restocker.CarryingBox)
		{
			LogStat("not done placing box, wating for rack to place");
			((MonoBehaviour)restocker).StartCoroutine(GoToWaiting((RestockerState)2));
		}
	}

	public IEnumerator Internal_ThrowBoxToTrashBin()
	{
		LogStat("Called ThrowBoxToTrashBin");
		yield return ((MonoBehaviour)restocker).StartCoroutine(GoTo(Singleton<FurnitureManager>.Instance.TrashBin.position, Singleton<FurnitureManager>.Instance.TrashBin.rotation));
		Box = inventory.Boxes.FirstOrDefault((Box b) => !b.HasProducts);
		while ((Object)(object)Box != (Object)null)
		{
			yield return (object)new WaitForSeconds(ThrowingBoxTime);
			RestockerEventApi.BoxThrownIntoTrashEventRegistry(restocker, Box);
			Singleton<InventoryManager>.Instance.RemoveBox(Box.Data);
			LeanPool.Despawn(((Component)Box).gameObject);
			((Component)Box).gameObject.layer = inventory.BoxLayer(Box);
			Box.ResetBox();
			inventory.Remove(Box);
			ArrangeBoxTower();
			DoneCarryingBox(Box);
			Box = inventory.Boxes.FirstOrDefault((Box b) => !b.HasProducts);
		}
		LogStat("threw boxes to trash bin");
		Box = inventory.Boxes.FirstOrDefault();
		if ((Object)(object)Box == (Object)null)
		{
			TargetBox = null;
			restocker.CarryingBox = false;
			State = (RestockerState)0;
			((MonoBehaviour)restocker).StartCoroutine(TryRestocking());
		}
		else if ((int)State == 2)
		{
			LogSimple("waiting for avalilable rack slot");
			((MonoBehaviour)restocker).StartCoroutine(PlaceBoxToRack());
		}
	}

	public IEnumerator Internal_MoveTo(Vector3 target)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		float boost = Employee.BoostStacking.SpeedMultiplier(RestockerWalkingSpeeds, CurrentBoostLevel);
		float speed = MovingSpeed * boost;
		Agent.speed = speed;
		Agent.angularSpeed = AngularSpeed * boost;
		Agent.acceleration = Acceleration * boost;
		yield return EmployeeLogicHelper.MoveTo((MonoBehaviour)(object)restocker, target, Agent, boost, TurningSpeed, 20f);
	}

	public IEnumerator Internal_RotateTo(Quaternion rotation)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		ShortcutExtensions.DORotateQuaternion(((Component)restocker).transform, rotation, RotationTime);
		yield return (object)new WaitForSeconds(RotationTime);
	}

	public IEnumerator Internal_PickUpBox(bool isFromRack)
	{
		if (isFromRack)
		{
			SortableBox sortableBox = default(SortableBox);
			while (productsNeeded > 0 && TargetRackSlot.Data.BoxCount != 0 && TargetRackSlot.Data.BoxID != -1)
			{
				Il2CppSystem.Collections.Generic.List<BoxData> racked = TargetRackSlot.Data.RackedBoxDatas;
				BoxData nextBoxData = racked[racked.Count - 1];
				if (IsBoxOvercapacity(nextBoxData))
				{
					LogSimple($"Overcapacity! {productsNeeded} of needs decreasing by {nextBoxData.ProductCount}");
					productsNeeded -= nextBoxData.ProductCount;
					DoneRestocking(nextBoxData);
					continue;
				}
				Box box = TargetRackSlot.TakeBoxFromRack();
				if ((Object)(object)box == (Object)null)
				{
					break;
				}
				if (inventory.Contains(box))
				{
					LogSimple("already picked up " + box.ToBoxInfo());
					break;
				}
				if (box.OnPlacementArea && ((Component)box).TryGetComponent<SortableBox>(out sortableBox))
				{
					((Component)box).GetComponentInParent<IPlacementArea>().RemoveBox(sortableBox);
				}
				if (!carryingBoxes.TryAdd(box.Data.ProductID, 1))
				{
					carryingBoxes[box.Data.ProductID]++;
				}
				AddCarryingBox(box);
				LogStat("picking up " + box.ToBoxInfo() + " from a rack");
				yield return GrabBox(box, isFromRack);
				sortableBox = null;
			}
		}
		else
		{
			if ((Object)(object)TargetBox == (Object)null)
			{
				yield break;
			}
			if ((Object)(object)TargetBox.OccupyOwner != (Object)(object)((Component)restocker).transform)
			{
				TargetBox = null;
				yield break;
			}
			if (inventory.Contains(TargetBox))
			{
				LogSimple("already picked up " + TargetBox.ToBoxInfo());
				yield break;
			}
			LogStat("picking up " + TargetBox?.ToBoxInfo() + " from the ground or vehicles");
			SortableBox sortableBox2 = default(SortableBox);
			if (TargetBox.OnPlacementArea && ((Component)TargetBox).TryGetComponent<SortableBox>(out sortableBox2))
			{
				((Component)TargetBox).GetComponentInParent<IPlacementArea>().RemoveBox(sortableBox2);
			}
			yield return GrabBox(TargetBox, isFromRack);
		}
	}

	private IEnumerator GrabBox(Box box, bool isFromRack)
	{
		productsNeeded -= box.Data.ProductCount;
		Collider[] componentsInChildren = ((Component)box).GetComponentsInChildren<Collider>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].isTrigger = true;
		}
		if (!isFromRack)
		{
			Singleton<StorageStreet>.Instance.OnTakeBoxFromStreet?.Invoke(box);
		}
		inventory.Add(box);
		box.FrezeeBox();
		CharacterModelObjectReference characterModelObjectReference = default(CharacterModelObjectReference);
		if (ModelComponent.TryGetReference("BoxHolder", out characterModelObjectReference))
		{
			((Component)box).transform.SetParent(((Component)characterModelObjectReference).transform);
			ShortcutExtensions.DOLocalMove(((Component)box).transform, Vector3.zero, TakingBoxTime, false);
			ShortcutExtensions.DOLocalRotate(((Component)box).transform, Vector3.zero, TakingBoxTime, (RotateMode)0);
		}
		ArrangeBoxTower();
		Box = box;
		Box.SetOccupy(true, ((Component)restocker).transform);
		Box.Racked = false;
		CurrentBoxLayer = (LayerMask)((Component)box).gameObject.layer;
		((Component)box).gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
		restocker.CarryingBox = true;
		yield return (object)new WaitForSeconds(TakingBoxTime);
	}

	private void DoneRestocking(Box box)
	{
		if (!((Object)(object)box == (Object)null))
		{
			DoneRestocking(box.Data);
		}
	}

	private void DoneRestocking(BoxData box)
	{
		int productID = box.ProductID;
		if (planList.ContainsKey(productID))
		{
			int capacityInDisplaySlot = GetCapacityInDisplaySlot(TargetDisplaySlot);
			int num = Math.Min(box.ProductCount, capacityInDisplaySlot - TargetDisplaySlot.Data.FirstItemCount);
			if (planList[productID] <= num)
			{
				planList.Remove(productID);
			}
			else
			{
				planList[productID] -= num;
			}
		}
	}

	private void DoneCarryingBox(Box box)
	{
		if (!((Object)(object)box == (Object)null) && box.Data.ProductID != -1)
		{
			int productID = box.Data.ProductID;
			DoneCarryingBox(productID);
		}
	}

	private void DoneCarryingBox(int id)
	{
		if (id != -1 && carryingBoxes.ContainsKey(id))
		{
			if (carryingBoxes[id] <= 1)
			{
				carryingBoxes.Remove(id);
			}
			else
			{
				carryingBoxes[id]--;
			}
		}
	}

	public IEnumerator Internal_PlaceProducts()
	{
		LogSimple($"Called PlaceProducts(): Box={Box?.ToBoxInfo()}, TargetProductID={TargetProductID}, TargetDisplaySlot={TargetDisplaySlot}");
		if ((Object)(object)Box == (Object)null || (Object)(object)TargetDisplaySlot == (Object)null || TargetProductID != TargetDisplaySlot.ProductID)
		{
			LogSimple("Not passed validation");
			yield break;
		}
		float placeBoost = Employee.BoostStacking.IntervalMultiplier(RestockerPlacingSpeeds, CurrentBoostLevel);
		if (!Box.IsOpen)
		{
			Box.OpenBox();
			yield return (object)new WaitForSeconds(UnpackingTime * placeBoost);
		}
		if (TargetProductID != TargetDisplaySlot.ProductID)
		{
			LogSimple("Tried to place wrong product ID");
			yield break;
		}
		int exp = 0;
		while (((TargetDisplaySlot) != null) && !TargetDisplaySlot.Full && Box.HasProducts)
		{
			Product productFromBox = null;
			try
			{
				productFromBox = Box.GetProductFromBox(false);
				LogSimple("Placing a product from the box " + Box.ToBoxInfo());
			}
			catch (ArgumentOutOfRangeException)
			{
			}
			if ((Object)(object)productFromBox == (Object)null)
			{
				break;
			}
			if (productFromBox.ProductSO.ID != TargetDisplaySlot.ProductID)
			{
				LogSimple("Tried to mix the wrong product");
				break;
			}
			TargetDisplaySlot.AddProduct(TargetProductID, productFromBox);
			Il2CppSystem.Collections.Generic.Dictionary<int, int> products = new Il2CppSystem.Collections.Generic.Dictionary<int, int>();
			products.Add(TargetProductID, 1);
			Singleton<InventoryManager>.Instance.AddProductToDisplay(new ItemQuantity
			{
				Products = products
			});
			exp++;
			yield return (object)new WaitForSeconds(ProductPlacingInterval * placeBoost);
		}
		occupiedDisplaySlots.Remove(TargetDisplaySlot);
		TargetDisplaySlot.m_OccupiedRestocker = null;
		skill.AddExp(exp);
		LogSimple("Finished PlaceProducts()");
	}

	public void Internal_PlaceBox()
	{
		LogStat("Called PlaceBox");
		if ((Object)(object)Box == (Object)null)
		{
			return;
		}
		if (TargetRackSlot.Data.ProductID != -1 && TargetRackSlot.Data.ProductID != TargetProductID)
		{
			LogSimple($"tried to place box with wrong product ID: {TargetRackSlot.Data.ProductID} != {TargetProductID}");
		}
		else
		{
			if (TargetRackSlot.IsBoxAlreadyExistInRack(Box.BoxID, Box) || Box.Racked)
			{
				return;
			}
			LogSimple(string.Format("placing {0} on {1}", Box?.ToBoxInfo() ?? "[EMPTY]", TargetDisplaySlot));
			((Component)Box).gameObject.layer = inventory.BoxLayer(Box);
			Collider[] componentsInChildren = ((Component)Box).GetComponentsInChildren<Collider>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].isTrigger = false;
			}
			TargetRackSlot.AddBox(Box.BoxID, Box);
			Box.Racked = true;
			Box.SetOccupy(false, (Transform)null);
			TargetBox = null;
			inventory.Remove(Box);
			ArrangeBoxTower();
			Box val = inventory.Boxes.Where((Box b) => b.Data.ProductID == TargetProductID).FirstOrDefault();
			if ((Object)(object)val == (Object)null)
			{
				val = inventory.Boxes.FirstOrDefault((Box b) => b.HasProducts);
			}
			if ((Object)(object)val != (Object)null)
			{
				TargetProductID = val.Data.ProductID;
			}
			Box = val;
		}
	}

	public bool Internal_GetAvailableDisplaySlotToRestock()
	{
		LogSimple("called GetAvailableDisplaySlotToRestock");
		if (Singleton<DisplayManager>.Instance.GetDisplaySlots(TargetProductID, false, CachedSlots) <= 0)
		{
			Plugin.LogDebug("-> Not found");
			return false;
		}
		DisplaySlot val = null;
		foreach (DisplaySlot d in CachedSlots)
		{
			if (IsDisplaySlotAvailableToRestock(d))
			{
				val = d;
				break;
			}
		}
		if ((Object)(object)val == (Object)null)
		{
			Plugin.LogDebug("-> finding labeledEmptyDisplaySlots");
			if (Singleton<DisplayManager>.Instance.GetLabeledEmptyDisplaySlots(TargetProductID, labeledEmptySlotsCache) <= 0)
			{
				Plugin.LogDebug("-> Not found");
				return false;
			}
			DisplaySlot val2 = CachedSlots[UnityEngine.Random.Range(0, labeledEmptySlotsCache.Count)];
			if (val2.IsOccupiedByOthers(restocker != null ? restocker.TryCast<Clerk>() : null))
			{
				return false;
			}
			TargetDisplaySlot = val2;
		}
		else
		{
			Plugin.LogDebug($"-> Found: {val}");
			TargetDisplaySlot = val;
		}
		if (!occupiedDisplaySlots.Contains(TargetDisplaySlot))
		{
			occupiedDisplaySlots.Add(TargetDisplaySlot);
		}
		TargetDisplaySlot.m_OccupiedRestocker = restocker != null ? restocker.TryCast<Clerk>() : null;
		return true;
	}

	public bool IsDisplaySlotAvailableToRestock(DisplaySlot displaySlot)
	{
		if (displaySlot.Data == null || displaySlot.Data.FirstItemID <= 0 || displaySlot.IsOccupiedByOthers(restocker != null ? restocker.TryCast<Clerk>() : null))
		{
			return false;
		}
		ProductSO val = Singleton<IDManager>.Instance.ProductSO(displaySlot.Data.FirstItemID);
		if ((Object)(object)Box != (Object)null && Box.Data.ProductID == val.ID)
		{
			return !displaySlot.Full;
		}
		return true;
	}

	private void ArrangeBoxTower()
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		List<Box> list = new List<Box>(inventory.Boxes);
		list.Sort((Box a, Box b) => System.Array.IndexOf(BoxTowerOrder, a.Size) - System.Array.IndexOf(BoxTowerOrder, b.Size));
		float num = 0f;
		foreach (Box item in list)
		{
			ShortcutExtensions.DOLocalMove(((Component)item).transform, new Vector3(0f, num, 0f), 0.3f, false);
			num += ((Component)item).GetComponent<BoxCollider>().size.y;
		}
	}

	private bool HasBox()
	{
		return inventory.Count > 0;
	}

	private bool IsBoxOvercapacity(BoxData box)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		int num = ProductWeight.CalcWeight(box);
		int num2 = BoxHeights[box.Size];
		return (totalCarryingWeight > 0 && num + totalCarryingWeight > CarryingCapacity) || (totalCarryingHeight > 0 && num2 + totalCarryingHeight > CarryingMaxHeight);
	}

	private bool IsBoxOvercapacity(Box box)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		int num = ProductWeight.CalcWeight(box);
		int num2 = BoxHeights[box.Size];
		LogSimple($"{box.ToBoxInfo()} weight={num} + {totalCarryingWeight} <= {CarryingCapacity}, height={num2} + {totalCarryingHeight} <= {CarryingMaxHeight}");
		return (totalCarryingWeight > 0 && num + totalCarryingWeight > CarryingCapacity) || (totalCarryingHeight > 0 && num2 + totalCarryingHeight > CarryingMaxHeight);
	}

	private void AddCarryingBox(Box box)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		totalCarryingWeight += ProductWeight.CalcWeight(box);
		totalCarryingHeight += BoxHeights[box.Size];
	}

	private void AddCarryingBox(BoxData box)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		totalCarryingWeight += ProductWeight.CalcWeight(box);
		totalCarryingHeight += BoxHeights[box.Size];
	}

	private void UpdateCarryingWeightAndHeight()
	{
		totalCarryingWeight = 0;
		totalCarryingHeight = 0;
		foreach (Box __box in inventory.Boxes) { AddCarryingBox(__box); };
		LogSimple($"carrying {totalCarryingWeight / 1000:0.#}kg");
	}

	public void LogSimple(string msg = null)
	{
		Plugin.LogDebug($"Restocker[{skill.Id}] {msg}");
	}

	public void LogStat(string msg = null)
	{
		string text = ((msg != null) ? (msg + " ") : "");
		Plugin.LogDebug($"Restocker[{skill.Id}] {text}carryingBox={Box}, boxCount={inventory.Count}");
		if (Plugin.Instance.Settings.RestockerLog)
		{
			Plugin.LogDebug($"Restocker[{skill.Id}] {inventory.Boxes.ToBoxStackInfo()}");
		}
	}

	internal void AfterFreeTargetDisplaySlot()
	{
		foreach (DisplaySlot s in occupiedDisplaySlots.ToArray())
		{
			if (s.IsOccupiedByMe(restocker != null ? restocker.TryCast<Clerk>() : null))
			{
				s.m_OccupiedRestocker = null;
			}
		}
		occupiedDisplaySlots.Clear();
	}

	public void SetEmptyBox()
	{
		Box = inventory.Boxes.FirstOrDefault((Box b) => !b.HasProducts);
	}


	private static BoxSO FindBoxSO(BoxSize size)
	{
		foreach (BoxSO so in Singleton<IDManager>.Instance.Boxes)
		{
			if (so.BoxSize == size)
			{
				return so;
			}
		}
		return null;
	}

	private static List<GameObject> EnumerateVehicles()
	{
		List<GameObject> list = new List<GameObject>();
		Il2CppSystem.Collections.IEnumerable vehicles = Singleton<VehicleManager>.Instance.GetVehicles().Cast<Il2CppSystem.Collections.IEnumerable>();
		Il2CppSystem.Collections.IEnumerator enumerator = vehicles.GetEnumerator();
		while (enumerator.MoveNext())
		{
			GameObject go = enumerator.Current.TryCast<GameObject>();
			if (go != null)
			{
				list.Add(go);
			}
		}
		return list;
	}

	private static List<SortableBox> EnumerateBoxes(IPlacementArea area)
	{
		List<SortableBox> list = new List<SortableBox>();
		Il2CppSystem.Collections.IEnumerable boxes = area.GetBoxes().Cast<Il2CppSystem.Collections.IEnumerable>();
		Il2CppSystem.Collections.IEnumerator enumerator = boxes.GetEnumerator();
		while (enumerator.MoveNext())
		{
			SortableBox box = enumerator.Current.TryCast<SortableBox>();
			if (box != null)
			{
				list.Add(box);
			}
		}
		return list;
	}
	static RestockerLogic()
	{
		BoxTowerOrder = new BoxSize[8]
		{
			(BoxSize)1,
			(BoxSize)0,
			(BoxSize)7,
			(BoxSize)2,
			(BoxSize)5,
			(BoxSize)3,
			(BoxSize)4,
			(BoxSize)6
		};
		BoxHeights = new Dictionary<BoxSize, int>
		{
			[(BoxSize)1] = 223,
			[(BoxSize)0] = 256,
			[(BoxSize)7] = 256,
			[(BoxSize)2] = 316,
			[(BoxSize)5] = 462,
			[(BoxSize)3] = 618,
			[(BoxSize)4] = 618,
			[(BoxSize)6] = 839
		};
	}
}
