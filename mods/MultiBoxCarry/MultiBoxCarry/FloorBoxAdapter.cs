using UnityEngine;
using __Project__.Scripts.FloorPaintSystem;
using __Project__.Scripts.Interaction;

namespace MultiBoxCarry;

internal sealed class FloorBoxAdapter : IQueuableBox
{
	private readonly FloorBox _floorBox;

	public object Raw => _floorBox;

	public Transform transform => ((Component)_floorBox).transform;

	public FloorBoxAdapter(FloorBox floorBox)
	{
		_floorBox = floorBox;
	}

	public FloorBox GetFloorBox()
	{
		return _floorBox;
	}

	public int GetID()
	{
		return _floorBox.FloorId;
	}

	public ProductSO GetProduct()
	{
		return null;
	}

	public void Drop(PlayerInteraction player)
	{
		if (!((Object)(object)player == (Object)null))
		{
			FloorBoxInteraction component = ((Component)player).GetComponent<FloorBoxInteraction>();
			if (!((Object)(object)component == (Object)null))
			{
				component.DropBox();
			}
		}
	}

	public void HideAndAttach(Transform playerTransform, Vector3 offset)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		BoxUtility.HideAndAttachShared(playerTransform, this, offset);
	}

	public bool IsOccupied()
	{
		return NetworkBoxSync.IsNetworkOccupied(this);
	}

	public void Restore(Transform playerTransform)
	{
		BoxUtility.RestoreShared(playerTransform, this);
	}
}
