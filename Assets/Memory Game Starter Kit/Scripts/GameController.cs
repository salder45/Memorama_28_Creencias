using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GameController : MonoBehaviour 
{
	public Sprite[] spriteCardsBack;
	public Sprite[] spriteCardsFront;
	public Sprite spriteCardShadow;

	/// <summary>
	/// How fast to uncover a card, higher values = faster
	/// </summary>
	[Range(1f, 16f)]
	public float uncoverTime = 4f;

	/// <summary>
	/// how fast to deal one card, higher values = faster
	/// </summary>
	[Range(1f, 16f)]
	public float dealTime = 4f;

	[Range(0.1f, 10f)]
	public float checkPairTime = 0.5f;

	/// <summary>
	/// The Padding between 2 Cards
	/// </summary>
	[Range(2, 32)]
	public int cardsPadding = 4;

	int pairCount = 4;

	/// <summary>
	/// Create a fake shadow?
	/// </summary>
	public bool shadow = true;
	[Range(-32, 32)]
	public float shadowOffsetX = 4;
	[Range(-32, 32)]
	public float shadowOffsetY = -4;

	float shadOffsetX;
	float shadOffsetY;

	int chosenCardsBack = 0;
	int[] chosenCardsFront;

	Vector3 dealingStartPosition = new Vector3(-12800, -12800, -8);

	// move counters
	int totalMoves = 0;
	int bestMoves = 0;
	int uncoveredCards = 0;
	Transform[] selectedCards = new Transform[2];

	//bool memorySolved = false;

	int oldPairCount;

	// Input check
	bool isTouching = false;
	bool isUncovering = false;
	bool isDealing = false;

	// GUI Skin
	public GUISkin skin;

	// Soundeffects
	public AudioClip soundDealCard;
	public AudioClip soundButtonClick;
	public AudioClip soundUncoverCard;
	public AudioClip soundFoundPair;
	public AudioClip soundNoPair;

	// Use this for initialization
	void Start () 
	{
	}

	void OnGUI()
	{
		if(skin != null)
			GUI.skin = skin;

		GUI.BeginGroup(new Rect(32, 32, 160, Screen.height - 64));
		{
			GUI.Box(new Rect(0, 0, 160, Screen.height - 64), "");

			// Play clears the Game Field and Deals a new set of Cards
			if(GUI.Button(new Rect(16, 16, 128, 48), "Play") && !(isDealing || isUncovering))
			{
				if(soundButtonClick != null)
					GetComponent<AudioSource>().PlayOneShot(soundButtonClick);

				CreateDeck();
			}

			/*
			// create new Buttons, change Rect(x,y,w,h) to the position where you want the button to appear and the Size
			if(GUI.Button(new Rect(16, 64, 128, 48), "Easy") && !(isDealing || isUncovering))
			{
				if(soundButtonClick != null)
					audio.PlayOneShot(soundButtonClick);
				
				// set pairCount to how many Pairs you want for this difficulty
				pairCount = 4;
				CreateDeck();
			}
			
			if(GUI.Button(new Rect(16, 104, 128, 48), "Medium") && !(isDealing || isUncovering))
			{
				if(soundButtonClick != null)
					audio.PlayOneShot(soundButtonClick);
				
				// set pairCount to how many Pairs you want for this difficulty
				pairCount = 8;
				CreateDeck();
			}
			*/

			GUI.skin.label.alignment = TextAnchor.UpperLeft;
			GUI.skin.label.fontSize = 16;
			GUI.Label(new Rect(16, 80, 128, 32), "Pairs " + pairCount);

			pairCount = (int)GUI.HorizontalSlider(new Rect(16, 112, 128, 32), pairCount, 2, spriteCardsFront.Length);

			if(pairCount != oldPairCount)
			{
				oldPairCount = pairCount;

				bestMoves = PlayerPrefs.GetInt("Memory_" + pairCount + "_Pairs", 0);
			}

			GUI.skin.label.alignment = TextAnchor.UpperLeft;
			GUI.skin.label.fontSize = 16;
			GUI.Label(new Rect(16, 160, 128, 32), "Moves: " + totalMoves);

			GUI.skin.label.alignment = TextAnchor.UpperLeft;
			GUI.skin.label.fontSize = 16;
			GUI.Label(new Rect(16, 192, 128, 32), "Best: " + bestMoves);

		}
		GUI.EndGroup();
	}

	void CreateDeck()
	{
		isDealing = true;

		// clear the game field and reset variables
		DestroyImmediate(GameObject.Find("DeckParent"));
		DestroyImmediate(GameObject.Find("Temp"));
		selectedCards = new Transform[2];
		totalMoves = 0;
		uncoveredCards = 0;
		//memorySolved = false;

		// get personal best for this board size
		bestMoves = PlayerPrefs.GetInt("Memory_" + pairCount + "_Pairs", 0);

		// randomly chose the back graphic of the cards
		chosenCardsBack = Random.Range(0, spriteCardsBack.Length);

		// randomly chose the card motives to play with
		List<int> tmp = new List<int>();
		for(int i = 0; i < spriteCardsFront.Length; i++)
		{
			tmp.Add(i);
		}
		tmp.Shuffle();
		chosenCardsFront = tmp.GetRange(0, pairCount).ToArray();

		GameObject deckParent = new GameObject("DeckParent"); // this holds all the cards
		GameObject temp = new GameObject("Temp");

		int cur = 0;

		float minX = Mathf.Infinity;
		float maxX = Mathf.NegativeInfinity;
		float minY = Mathf.Infinity;
		float maxY = Mathf.NegativeInfinity;

		// calculate columns and rows needed for the selected pair count
		int cCards = pairCount * 2;
		int cols = (int)Mathf.Sqrt(cCards);
		int rows = (int)Mathf.Ceil(cCards / (float)cols);

		List<int> deck = new List<int>();
		for(int i = 0; i < pairCount; i++)
		{
			deck.AddRange(new int[] {i, i});
		}
		deck.Shuffle();

		int cardWidth = 0;
		int cardHeight = 0;

		for(int x = 0; x < rows; x++)
		{
			for(int y = 0; y < cols; y++)
			{
				if(cur > cCards-1)
					break;

				// Create the Card
				GameObject card = new GameObject("Card"); // parent object
				GameObject cardFront = new GameObject("CardFront");
				GameObject cardBack = new GameObject("CardBack");
				GameObject destination = new GameObject("Destination");

				cardFront.transform.parent = card.transform; // make front child of card
				cardBack.transform.parent = card.transform; // make back child of card

				// front (motive)
				cardFront.AddComponent<SpriteRenderer>();
				cardFront.GetComponent<SpriteRenderer>().sprite = spriteCardsFront[chosenCardsFront[deck[cur]]];
				cardFront.GetComponent<SpriteRenderer>().sortingOrder = -1;

				// back
				cardBack.AddComponent<SpriteRenderer>();
				cardBack.GetComponent<SpriteRenderer>().sprite = spriteCardsBack[chosenCardsBack];
				cardBack.GetComponent<SpriteRenderer>().sortingOrder = 1;

				cardWidth = (int)spriteCardsBack[chosenCardsBack].rect.width;
				cardHeight = (int)spriteCardsBack[chosenCardsBack].rect.height;

				// now add the Card GameObject to the Deck GameObject "deckParent"
				card.tag = "Card";
				card.transform.parent = deckParent.transform;
				card.transform.position = dealingStartPosition;
				card.AddComponent<BoxCollider2D>();
				card.GetComponent<BoxCollider2D>().size = new Vector2(cardWidth, cardHeight);
				card.AddComponent<CardProperties>().Pair = deck[cur];

				destination.transform.parent = temp.transform;
				destination.tag = "Destination";
				destination.transform.position = new Vector3(x * (cardWidth + cardsPadding), y * (cardHeight + cardsPadding));

				if(shadow)
				{
					GameObject cardShadow = new GameObject("CardShadow");

					cardShadow.tag = "CardShadow";
					cardShadow.transform.parent = deckParent.transform;
					cardShadow.transform.position = dealingStartPosition;
					cardShadow.AddComponent<SpriteRenderer>();
					cardShadow.GetComponent<SpriteRenderer>().sprite = spriteCardShadow;
					cardShadow.GetComponent<SpriteRenderer>().sortingOrder = -2;
				}
				cur++;

				// determine positions for the camera helper objects
				Vector3 pos = destination.transform.position;
				minX = Mathf.Min(minX, pos.x - cardWidth);
				minY = Mathf.Min(minY, pos.y - cardHeight);
				maxX = Mathf.Max(maxX, pos.x + cardWidth + shadowOffsetX);
				maxY = Mathf.Max(maxY, pos.y + cardHeight + shadowOffsetY);
			}
		}

		// scale to fit onto the "table"
		float tableScale = (GameObject.Find("Table") == null) ? 1f : GameObject.Find("Table").transform.localScale.x;
		float scale = tableScale / (maxX + cardsPadding);

		Vector2 point = LineIntersectionPoint(
			new Vector2(minX, maxY),
			new Vector2(maxX, minY),
			new Vector2(minX, minY),
			new Vector2(maxX, maxY)
			);

		temp.transform.position -= new Vector3(point.x * scale, point.y * scale);

		shadOffsetX = shadowOffsetX * scale;
		shadOffsetY = shadowOffsetY * scale;

		deckParent.transform.localScale = new Vector3(scale, scale, scale);
		temp.transform.localScale = new Vector3(scale, scale, scale);

		DealCards ();
	}

	void DealCards()
	{
		StartCoroutine (dealCards ());
	}

	IEnumerator dealCards()
	{
		GameObject[] cards = GameObject.FindGameObjectsWithTag("Card");
		GameObject[] cardsShadow = GameObject.FindGameObjectsWithTag("CardShadow");
		GameObject[] destinations = GameObject.FindGameObjectsWithTag("Destination");

		for(int i = 0; i < cards.Length; i++)
		{
			float t = 0; 

			if(soundDealCard != null)
				GetComponent<AudioSource>().PlayOneShot(soundDealCard);

			while(t < 1f)
			{
				t += Time.deltaTime * dealTime;

				cards[i].transform.position = Vector3.Lerp(
					dealingStartPosition, destinations[i].transform.position, t);

				if(cardsShadow.Length > 0)
				{
					cardsShadow[i].transform.position = Vector3.Lerp(
						dealingStartPosition, 
						destinations[i].transform.position + new Vector3(shadOffsetX, shadOffsetY), t);
				}

				yield return null;
			}
			yield return null;
		}

		isDealing = false;

		yield return 0;
	}

	void UncoverCard(Transform card)
	{
		StartCoroutine (uncoverCard(card, true));
	}

	IEnumerator uncoverCard(Transform card, bool uncover)
	{
		isUncovering = true;

		float minAngle = uncover ? 0 : 180;
		float maxAngle = uncover ? 180 : 0; 

		float t = 0;
		bool uncovered = false;

		if(soundUncoverCard != null)
			GetComponent<AudioSource>().PlayOneShot(soundUncoverCard);

		// find the shadow for the selected card
		var shadow = GameObject.FindGameObjectsWithTag("CardShadow").Where(
			g => (g.transform.position == card.position + new Vector3(shadOffsetX, shadOffsetY))).FirstOrDefault();

		while(t < 1f)
		{
			t += Time.deltaTime * uncoverTime;

			float angle = Mathf.LerpAngle(minAngle, maxAngle, t);
			card.eulerAngles = new Vector3(0, angle, 0);

			if(shadow != null)
				shadow.transform.eulerAngles = new Vector3(0, angle, 0);

			if( ( (angle >= 90 && angle < 180) || 
			      (angle >= 270 && angle < 360) ) && !uncovered)
			{
				uncovered = true;
				for(int i = 0; i < card.childCount; i++)
				{
					// reverse sorting order to show the otherside of the card
					// otherwise you would still see the same sprite because they are sorted 
					// by order not distance (by default)
					Transform c = card.GetChild(i);
					c.GetComponent<SpriteRenderer>().sortingOrder *= -1;

					yield return null;
				}
			}

			yield return null;
		}

		// check if we uncovered 2 cards
		if(uncoveredCards == 2)
		{
			// if so compare the cards
			if(selectedCards[0].GetComponent<CardProperties>().Pair !=
			   selectedCards[1].GetComponent<CardProperties>().Pair)
			{
				if(soundNoPair != null)
					GetComponent<AudioSource>().PlayOneShot(soundNoPair);

				// if they are not equal cover back
				yield return new WaitForSeconds(checkPairTime);
				StartCoroutine (uncoverCard(selectedCards[0], false));
				StartCoroutine (uncoverCard(selectedCards[1], false));
			}
			else
			{
				if(soundFoundPair != null)
					GetComponent<AudioSource>().PlayOneShot(soundFoundPair);

				// set as solved
				selectedCards[0].GetComponent<CardProperties>().Solved = true;
				selectedCards[1].GetComponent<CardProperties>().Solved = true;
			}
			selectedCards[0].GetComponent<CardProperties>().Selected = false;
			selectedCards[1].GetComponent<CardProperties>().Selected = false;
			uncoveredCards = 0;
			totalMoves++;

			yield return new WaitForSeconds(0.1f);
		}

		// check if the memory is solved
		if(IsSolved())
		{
			int score = PlayerPrefs.GetInt("Memory_" + pairCount + "_Pairs", 0);
			
			if(score > totalMoves || score == 0)
			{
				bestMoves = totalMoves;
			}
			PlayerPrefs.SetInt("Memory_" + pairCount + "_Pairs", bestMoves);
			
			//memorySolved = true;
		}

		isUncovering = false;
		yield return 0;
	}

	bool IsSolved()
	{
		foreach(GameObject g in GameObject.FindGameObjectsWithTag("Card"))
		{
			if(!g.GetComponent<CardProperties>().Solved)
				return false;
		}

		return true;
	}
	
	// Update is called once per frame
	void Update () 
	{
		if(isDealing)
			return;

		if((Input.GetMouseButtonDown(0) || Input.touchCount > 0) && !isTouching && !isUncovering && uncoveredCards < 2)
		{
			isTouching = true;

			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

			// we hit a card
			if (hit.collider != null)
			{
				if(!hit.collider.GetComponent<CardProperties>().Selected)
				{
					// and its not one of the already solved ones
					if(!hit.collider.GetComponent<CardProperties>().Solved)
					{
						// uncover it
						UncoverCard(hit.collider.transform);
						selectedCards[uncoveredCards] = hit.collider.transform;
						selectedCards[uncoveredCards].GetComponent<CardProperties>().Selected = true;
						uncoveredCards += 1;
					}
				}
			}
		}
		else
		{
			isTouching = false;
		}
	}

	Vector2 LineIntersectionPoint(Vector2 ps1, Vector2 pe1, Vector2 ps2, Vector2 pe2)
	{
		// Get A,B,C of first line - points : ps1 to pe1
		float A1 = pe1.y-ps1.y;
		float B1 = ps1.x-pe1.x;
		float C1 = A1*ps1.x+B1*ps1.y;
		
		// Get A,B,C of second line - points : ps2 to pe2
		float A2 = pe2.y-ps2.y;
		float B2 = ps2.x-pe2.x;
		float C2 = A2*ps2.x+B2*ps2.y;
		
		// Get delta and check if the lines are parallel
		float delta = A1*B2 - A2*B1;
		if(delta == 0)
			return new Vector2();
		
		// now return the Vector2 intersection point
		return new Vector2(
			(B2*C1 - B1*C2)/delta,
			(A1*C2 - A2*C1)/delta
			);
	}
}
