using UnityEngine;

namespace MultiBoxCarry;

public class BoxAdapter : IQueuableBox
{
	private readonly Box _box;

	public object Raw => _box;

	public Transform transform => ((Component)_box).transform;

	public BoxAdapter(Box box)
	{
		_box = box;
	}

	public Box GetBox()
	{
		return _box;
	}

	public int GetID()
	{
		return _box.BoxID;
	}

	public ProductSO GetProduct()
	{
		return _box.Product;
	}

	public void Drop(PlayerInteraction player)
	{
		BoxInteraction component = ((Component)player).GetComponent<BoxInteraction>();
		if (!((Object)(object)component == (Object)null))
		{
			component.DropBox();
		}
	}

	public void HideAndAttach(Transform player, Vector3 offset)
	{
		BoxUtility.ClearMatchingHighlightBeforeQueue(_box);
		_box.SetOccupy(true, player);
		BoxUtility.HideAndAttachShared(player, this, offset);
	}

	public bool IsOccupied()
	{
		if ((Object)(object)_box.OccupyOwner != (Object)null)
		{
			return true;
		}

		try
		{
			NetworkBox networkBox = ((Component)_box).GetComponent<NetworkBox>()
				?? ((Component)_box).GetComponentInParent<NetworkBox>();
			if ((Object)(object)networkBox != (Object)null && networkBox.IsNetworkOccupied)
			{
				return true;
			}
		}
		catch
		{
		}

		return false;
	}

	public void Restore(Transform player)
	{
		_box.SetOccupy(false, player);
		BoxUtility.RestoreShared(player, this);
	}
}
