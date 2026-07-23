using UnityEngine;

namespace MultiBoxCarry;

internal interface IQueuableBox
{
	Transform transform { get; }

	object Raw { get; }

	void Drop(PlayerInteraction player);

	int GetID();

	ProductSO GetProduct();

	void HideAndAttach(Transform playerTransform, Vector3 offset);

	bool IsOccupied();

	void Restore(Transform playerTransform);
}
