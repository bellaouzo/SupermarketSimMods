using UnityEngine;

namespace MultiBoxCarry;

internal class FurnitureBoxAdapter : IQueuableBox
{
	private readonly FurnitureBox _furnitureBox;

	public object Raw => _furnitureBox;

	public Transform transform => ((Component)_furnitureBox).transform;

	public FurnitureBoxAdapter(FurnitureBox furnitureBox)
	{
		_furnitureBox = furnitureBox;
	}

	public void Drop(PlayerInteraction player)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		FurnitureBoxInteraction component = ((Component)player).GetComponent<FurnitureBoxInteraction>();
		if (!((Object)(object)component == (Object)null))
		{
			component.DropBox(transform.position);
		}
	}

	public int GetID()
	{
		return ((Component)_furnitureBox).GetComponent<FurnitureBoxData>().FurnitureID;
	}

	public ProductSO GetProduct()
	{
		return null;
	}

	public void HideAndAttach(Transform player, Vector3 offset)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		BoxUtility.HideAndAttachShared(player, this, offset);
	}

	public bool IsOccupied()
	{
		return false;
	}

	public void Restore(Transform playerTransform)
	{
		BoxUtility.RestoreShared(playerTransform, this);
	}

	public FurnitureBox GetFurnitureBox()
	{
		return _furnitureBox;
	}
}
