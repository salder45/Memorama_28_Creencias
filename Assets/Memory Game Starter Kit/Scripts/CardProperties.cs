using UnityEngine;
using System.Collections;

public class CardProperties : MonoBehaviour
{
	int pair = 0;
	public int Pair
	{
		get
		{
			return pair;
		}
		set
		{
			pair = value;
		}
	}

	bool solved = false;
	public bool Solved
	{
		get
		{
			return solved;
		}
		set
		{
			solved = value;
		}
	}

	bool selected = false;
	public bool Selected
	{
		get
		{
			return selected;
		}
		set
		{
			selected = value;
		}
	}
}

