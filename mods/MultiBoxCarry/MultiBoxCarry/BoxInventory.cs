using System.Collections.Generic;
using UnityEngine;

namespace MultiBoxCarry;

internal class BoxInventory
{
	public const int MaxQueuedBoxes = 8;

	private readonly List<IQueuableBox> _queuedBoxes = new List<IQueuableBox>();

	public int Count => _queuedBoxes.Count;

	public bool IsFull => _queuedBoxes.Count >= 8;

	public bool IsEmpty => _queuedBoxes.Count == 0;

	public IReadOnlyList<IQueuableBox> QueuedBoxes => _queuedBoxes;

	public bool Enqueue(IQueuableBox boxToQueue, ProductSO newHandProduct)
	{
		if (boxToQueue == null)
		{
			return false;
		}
		if (IsFull)
		{
			return false;
		}
		foreach (IQueuableBox queuedBox in _queuedBoxes)
		{
			if (queuedBox != null && queuedBox.Raw == boxToQueue.Raw)
			{
				return false;
			}
		}
		_queuedBoxes.Add(boxToQueue);
		FixQueue(newHandProduct);
		return true;
	}

	private void FixQueue(ProductSO newHandProduct)
	{
		if ((Object)(object)newHandProduct == (Object)null || _queuedBoxes.Count <= 1)
		{
			return;
		}
		List<IQueuableBox> list = new List<IQueuableBox>();
		List<IQueuableBox> list2 = new List<IQueuableBox>();
		foreach (IQueuableBox queuedBox in _queuedBoxes)
		{
			if (queuedBox != null)
			{
				ProductSO product = queuedBox.GetProduct();
				if ((Object)(object)product == (Object)(object)newHandProduct)
				{
					list2.Add(queuedBox);
				}
				else
				{
					list.Add(queuedBox);
				}
			}
		}
		_queuedBoxes.Clear();
		_queuedBoxes.AddRange(list);
		_queuedBoxes.AddRange(list2);
	}

	public IQueuableBox Dequeue()
	{
		if (_queuedBoxes.Count == 0)
		{
			return null;
		}
		int index = _queuedBoxes.Count - 1;
		IQueuableBox result = _queuedBoxes[index];
		_queuedBoxes.RemoveAt(index);
		return result;
	}

	public IQueuableBox Peek()
	{
		if (_queuedBoxes.Count == 0)
		{
			return null;
		}
		return _queuedBoxes[_queuedBoxes.Count - 1];
	}

	public bool Remove(IQueuableBox box)
	{
		if (box == null)
		{
			return false;
		}
		return _queuedBoxes.Remove(box);
	}

	public void Clear()
	{
		_queuedBoxes.Clear();
	}
}
