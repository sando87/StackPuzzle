using UnityEngine;
using System.Collections;
using Holoville.HOTween;

/// <summary>
///  This class is the main entry point of the game it should be attached to a gameobject and be instanciate in the scene
/// Author : Pondomaniac Games
/// </summary>
public class Main : MonoBehaviour
{

    public GameObject _indicator;//The indicator to know the selected tile
    public GameObject[,] _arrayOfShapes;//The main array that contain all games tiles
    private GameObject _currentIndicator;//The current indicator to replace and destroy each time the player change the selection
    private GameObject _FirstObject;//The first object selected
    private GameObject _SecondObject;//The second object selected
    public GameObject[] _listOfGems;//The list of tiles we cant to see in the game you can remplace them in unity's inspector and choose all what you want
    public GameObject _emptyGameobject;//After destroying object they are replaced with this one so we will replace them after with new ones
    public GameObject _particleEffect;//The object we want to use in the effect of shining stars 
    public GameObject _particleEffectWhenMatch;//The gameobject of the effect when the objects are matching
    public bool _canTransitDiagonally = false;//Indicate if we can switch diagonally
    public int _scoreIncrement;//The amount of point to increment each time we find matching tiles
    private int _scoreTotal = 0;//The score 
    private ArrayList _currentParticleEffets = new ArrayList();//the array that will contain all the matching particle that we will destroy after
    public AudioClip MatchSound;//the sound effect when matched tiles are found
    public int _gridWidth;//the grid number of cell horizontally
    public int _gridHeight;//the grid number of cell vertically
    //inside class
    private Vector2 _firstPressPos;
    private Vector2 _secondPressPos;
    private Vector2 _currentSwipe;

    // Use this for initialization
    void Start()
    {
        //Initializing the array with _gridWidth and _gridHeight passed in parameter
        _arrayOfShapes = new GameObject[_gridWidth, _gridHeight];
        //Creating the gems from the list of gems passed in parameter
        for (int i = 0; i <= _gridWidth - 1; i++)
        {
            for (int j = 0; j <= _gridHeight - 1; j++)
            {
                var gameObject = GameObject.Instantiate(_listOfGems[Random.Range(0, _listOfGems.Length)] as GameObject, new Vector3(i, j, 0), transform.rotation) as GameObject;
                _arrayOfShapes[i, j] = gameObject;
            }
        }
        
        //Adding the star effect to the gems and call the DoShapeEffect continuously
        InvokeRepeating("DoShapeEffect", 1f, 0.21F);
      
    }



    // Update is called once per frame
    void Update()
    {
      
        bool shouldTransit = false;
        var direction = Swipe();
        if (direction != Direction.NONE)
        {
            //Detecting if the player clicked on the left mouse button and also if there is no animation playing
            if ( HOTween.GetTweenInfos() == null)
            {

                Destroy(_currentIndicator);
                //The 3 following lines is to get the clicked GameObject and getting the RaycastHit2D that will help us know the clicked object
                //Ray ray   = Camera.main.ScreenPointToRay (Input.mousePosition);
                RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(_firstPressPos), Vector2.zero);
                if (hit.transform != null)
                {  //To know if the user already selected a tile or not yet

                    if (_FirstObject == null)

                    {
                        _FirstObject = hit.transform.gameObject;
                        if (direction != Direction.STATIONARY)
                        {
                            Vector3 hit2Position = hit.transform.position;
                            switch (direction)
                            {
                               case Direction.UP:
                                    hit2Position.y++;break;
                                case Direction.DOWN:
                                    hit2Position.y--;  break;
                                case Direction.LEFT:
                                    hit2Position.x--; break;
                                case Direction.RIGHT:
                                    hit2Position.x++; break;

                            }

                            RaycastHit2D hit2 =  Physics2D.Raycast(hit2Position, Vector2.zero);
                            if (hit2.transform != null )
                            {
                                _SecondObject = hit2.transform.gameObject;
                                shouldTransit = true;
                            }
                        }
                    }
                    else
                    {
                        _SecondObject = hit.transform.gameObject;
                        shouldTransit = true;
                    }

                    _currentIndicator = GameObject.Instantiate(_indicator, new Vector3(hit.transform.gameObject.transform.position.x, hit.transform.gameObject.transform.position.y, -1), transform.rotation) as GameObject;
                    //If the user select the second tile we will swap the two tile and animate them
                    if (shouldTransit)
                    {
                        //Getting the position between the 2 tiles
                        var distance =_FirstObject.transform.position - _SecondObject.transform.position;
                        //Testing if the 2 tiles are next to each others otherwise we will not swap them 
                        if (Mathf.Abs(distance.x) <= 1 && Mathf.Abs(distance.y) <= 1)
                        {   //If we dont want the player to swap diagonally
                            if (!_canTransitDiagonally)
                            {
                                if (distance.x != 0 && distance.y != 0)
                                {
                                    Destroy(_currentIndicator);
                                    _FirstObject = null;
                                    _SecondObject = null;
                                    return;
                                }
                            }
                            //Animate the transition
                            DoSwapMotion(_FirstObject.transform, _SecondObject.transform);
                            //Swap the object in array
                            DoSwapTile(_FirstObject, _SecondObject, ref _arrayOfShapes);


                        }
                        else
                        {
                            _FirstObject = null;
                            _SecondObject = null;

                        }
                        Destroy(_currentIndicator);

                    }

                }

            }
        }
        //If no animation is playing
        if (HOTween.GetTweenInfos() == null)
        {
            var Matches = FindMatch(_arrayOfShapes);
            //If we find a matched tiles
            if (Matches.Count > 0)
            {//Update the score
                _scoreTotal += Matches.Count * _scoreIncrement;

                foreach (GameObject go in Matches)
                {
                    //Playing the matching sound
                    GetComponent<AudioSource>().PlayOneShot(MatchSound);
                    //Creating and destroying the effect of matching
                    var destroyingParticle = GameObject.Instantiate(_particleEffectWhenMatch as GameObject, new Vector3(go.transform.position.x, go.transform.position.y, -2), transform.rotation) as GameObject;
                    Destroy(destroyingParticle, 1f);
                    //Replace the matching tile with an empty one
                    _arrayOfShapes[(int)go.transform.position.x, (int)go.transform.position.y] = GameObject.Instantiate(_emptyGameobject, new Vector3((int)go.transform.position.x, (int)go.transform.position.y, -1), transform.rotation) as GameObject;
                    //Destroy the ancient matching tiles
                    Destroy(go, 0.1f);
                }
                _FirstObject = null;
                _SecondObject = null;
                //Moving the tiles down to replace the empty ones
                DoEmptyDown(ref _arrayOfShapes);
            }
            //If no matching tiles are found remake the tiles at their places
            else if (_FirstObject != null
                     && _SecondObject != null
                     )
            {
                //Animate the tiles
                DoSwapMotion(_FirstObject.transform, _SecondObject.transform);
                //Swap the tiles in the array
                DoSwapTile(_FirstObject, _SecondObject, ref _arrayOfShapes);
                _FirstObject = null;
                _SecondObject = null;

            }
        }
        //Update the score
        (GetComponent(typeof(TextMesh)) as TextMesh).text = _scoreTotal.ToString();
    }

    // Find Match-3 Tile
    private ArrayList FindMatch(GameObject[,] cells)
    {//creating an arraylist to store the matching tiles
        ArrayList stack = new ArrayList();
        //Checking the vertical tiles
        for (var x = 0; x <= cells.GetUpperBound(0); x++)
        {
            for (var y = 0; y <= cells.GetUpperBound(1); y++)
            {
                var thiscell = cells[x, y];
                //If it's an empty tile continue
                if (thiscell.name == "Empty(Clone)") continue;
                int matchCount = 0;
                int y2 = cells.GetUpperBound(1);
                int y1;
                //Getting the number of tiles of the same kind
                for (y1 = y + 1; y1 <= y2; y1++)
                {
                    if (cells[x, y1].name == "Empty(Clone)" || thiscell.name != cells[x, y1].name) break;
                    matchCount++;
                }
                //If we found more than 2 tiles close we add them in the array of matching tiles
                if (matchCount >= 2)
                {
                    y1 = Mathf.Min(cells.GetUpperBound(1), y1 - 1);
                    for (var y3 = y; y3 <= y1; y3++)
                    {
                        if (!stack.Contains(cells[x, y3]))
                        {
                            stack.Add(cells[x, y3]);
                        }
                    }
                }
            }
        }
        //Checking the horizontal tiles , in the following loops we will use the same concept as the previous ones
        for (var y = 0; y < cells.GetUpperBound(1) + 1; y++)
        {
            for (var x = 0; x < cells.GetUpperBound(0) + 1; x++)
            {
                var thiscell = cells[x, y];
                if (thiscell.name == "Empty(Clone)") continue;
                int matchCount = 0;
                int x2 = cells.GetUpperBound(0);
                int x1;
                for (x1 = x + 1; x1 <= x2; x1++)
                {
                    if (cells[x1, y].name == "Empty(Clone)" || thiscell.name != cells[x1, y].name) break;
                    matchCount++;
                }
                if (matchCount >= 2)
                {
                    x1 = Mathf.Min(cells.GetUpperBound(0), x1 - 1);
                    for (var x3 = x; x3 <= x1; x3++)
                    {
                        if (!stack.Contains(cells[x3, y]))
                        {
                            stack.Add(cells[x3, y]);
                        }
                    }
                }
            }
        }
        return stack;
    }

    // Swap Motion Animation, to animate the switching arrays
    void DoSwapMotion(Transform a, Transform b)
    {
        Vector3 posA = a.localPosition;
        Vector3 posB = b.localPosition;
        TweenParms parms = new TweenParms().Prop("localPosition", posB).Ease(EaseType.EaseOutQuart);
        HOTween.To(a, 0.25f, parms).WaitForCompletion();
        parms = new TweenParms().Prop("localPosition", posA).Ease(EaseType.EaseOutQuart);
        HOTween.To(b, 0.25f, parms).WaitForCompletion();
    }


    // Swap Two Tile, it swaps the position of two objects in the grid array
    void DoSwapTile(GameObject a, GameObject b, ref GameObject[,] cells)
    {
        GameObject cell = cells[(int)a.transform.position.x, (int)a.transform.position.y];
        cells[(int)a.transform.position.x, (int)a.transform.position.y] = cells[(int)b.transform.position.x, (int)b.transform.position.y];
        cells[(int)b.transform.position.x, (int)b.transform.position.y] = cell;
    }

    // Do Empty Tile Move Down
    private void DoEmptyDown(ref GameObject[,] cells)
    {   //replace the empty tiles with the ones above
        for (int x = 0; x <= cells.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= cells.GetUpperBound(1); y++)
            {

                var thisCell = cells[x, y];
                if (thisCell.name == "Empty(Clone)")
                {

                    for (int y2 = y; y2 <= cells.GetUpperBound(1); y2++)
                    {
                        if (cells[x, y2].name != "Empty(Clone)")
                        {
                            cells[x, y] = cells[x, y2];
                            cells[x, y2] = thisCell;
                            break;
                        }

                    }

                }

            }
        }
        //Instantiate new tiles to replace the ones destroyed
        for (int x = 0; x <= cells.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= cells.GetUpperBound(1); y++)
            {
                if (cells[x, y].name == "Empty(Clone)")
                {
                    Destroy(cells[x, y]);
                    cells[x, y] = GameObject.Instantiate(_listOfGems[Random.Range(0, _listOfGems.Length)] as GameObject, new Vector3(x, cells.GetUpperBound(1) + 2, 0), transform.rotation) as GameObject;

                }
            }
        }

        for (int x = 0; x <= cells.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= cells.GetUpperBound(1); y++)
            {

                TweenParms parms = new TweenParms().Prop("position", new Vector3(x, y, -1)).Ease(EaseType.EaseOutQuart);
                HOTween.To(cells[x, y].transform, .4f, parms);
            }
        }



    }
    //Instantiate the star objects
    void DoShapeEffect()
    {
        foreach (GameObject row in _currentParticleEffets)
            Destroy(row);
        for (int i = 0; i <= 2; i++)
            _currentParticleEffets.Add(GameObject.Instantiate(_particleEffect, new Vector3(Random.Range(0, _arrayOfShapes.GetUpperBound(0) + 1), Random.Range(0, _arrayOfShapes.GetUpperBound(1) + 1), -1), new Quaternion(0, 0, Random.Range(0, 1000f), 100)) as GameObject);
    }


    enum Direction
    {       NONE,
            STATIONARY,
            UP,
            DOWN,
            LEFT,
            RIGHT,
    }

    private Direction Swipe()
    {
        if (Input.GetMouseButtonDown(0))
        {
            //save began touch 2d point
            _firstPressPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        }
        if (Input.GetMouseButtonUp(0))
        {
            //save ended touch 2d point
            _secondPressPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

            //create vector from the two points
            _currentSwipe = new Vector2(_secondPressPos.x - _firstPressPos.x, _secondPressPos.y - _firstPressPos.y);

            //normalize the 2d vector
            _currentSwipe.Normalize();

            //swipe upwards
            if (_currentSwipe.y > 0 && _currentSwipe.x > -0.5f && _currentSwipe.x < 0.5f)
        {
                Debug.Log("up swipe");
                return Direction.UP;
            }
            //swipe down
            if (_currentSwipe.y < 0 && _currentSwipe.x > -0.5f && _currentSwipe.x < 0.5f)
        {
                Debug.Log("down swipe");
                return Direction.DOWN;
            }
            //swipe left
            if (_currentSwipe.x < 0 && _currentSwipe.y > -0.5f && _currentSwipe.y < 0.5f)
        {
                Debug.Log("left swipe");
                return Direction.LEFT;
            }
            //swipe right
            if (_currentSwipe.x > 0 && _currentSwipe.y > -0.5f && _currentSwipe.y < 0.5f)
        {
                Debug.Log("right swipe");
                return Direction.RIGHT;
            }
            return Direction.STATIONARY;
        }
        return Direction.NONE;
    }
}
