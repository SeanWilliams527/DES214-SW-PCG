using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class PCG : MonoBehaviour
{
    //////////////////////////////////////////////////////////////////////////
    // GENERATION
    //////////////////////////////////////////////////////////////////////////
    Vector2Int cursor;
    Vector2Int N = new Vector2Int(0, 1);
    Vector2Int E = new Vector2Int(1, 0);
    Vector2Int S = new Vector2Int(0, -1);
    Vector2Int W = new Vector2Int(-1, 0);

    List<Vector2Int> Branches;

    //////////////////////////////////////////////////////////////////////////
    // DESIGNER ADJUSTABLE VALUES
    //////////////////////////////////////////////////////////////////////////

    //Maximum height and width of tile map (must be odd, somewhere between 21 and 101 works well)
    private int MaxMapSize = 41;
    //Size of floor and wall tiles in Unity units (somewhere between 1.0f and 10.0f works well)
    private float GridSize = 5.0f;

    //////////////////////////////////////////////////////////////////////////

    //Tilemap array to make sure we don't put walls over floors--has no effect during gameplay
    private GameObject[] TileMap;
    //The 0,0 point of the tile map array, since the map goes from -MaxMapSize/2 to +MaxMapSize/2
    private int TileMapMidPoint;

    //A random number generator for this run
	private System.Random RNG;

    //Dictionary of all PCG prefabs
    public Dictionary<string, GameObject> Prefabs;

    //A static reference to allow easy access from other scripts
    //(if you add a "using static PCG;" statement to that script)
    public static PCG PCGObject;
	
    //Start is called before the first frame update
    void Start()
    {
        //Store a static reference to this generator
        PCGObject = this;

        //Load all the prefabs into the prefabs dictionary
		LoadPrefabs(); 

        //Create the tile map
        if (MaxMapSize % 2 == 0)
            ++MaxMapSize; //This needs to be an odd number
		TileMap = new GameObject[MaxMapSize * MaxMapSize];
		TileMapMidPoint = (MaxMapSize * MaxMapSize) / 2; //The 0,0 point

        //Get a new random generator
        //Could feed it a set seed if need the same level for debugging purposes
		RNG = new System.Random();

        //Generate a level
        GenerateLevel();
    }

    //Update is called every frame
    void Update()
    {
        //ESC to quit
        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit(); //Don't need a return from this...
        }

        //T to generate a test room
        if (Input.GetKey(KeyCode.T))
        {
            GenerateTestRoom();
            return;
        }

        //R to reset the level
        if (Input.GetKey(KeyCode.R))
        {
            ResetLevel();
            return;
        }
    }

    //Generate an actual level to play
    void GenerateLevel()
	{
        //Clear any game objects first, just in case
        ClearLevel();

        //Create the camera
        SpawnCamera();

        ///////////////////////////////////////////////////////
        //Add PCG logic here...
        //The level will be solid walls until you add code here. 
        //See the GenerateTestRoom() function for an example,
        //(press T to see the test room).
        ///////////////////////////////////////////////////////

        //Create the starting tile
        SpawnTile(0, 0);
        Spawn("player", 0.0f, 0.0f);
        cursor.Set(0, 0);  // Set cursor to starting tile

        // Main Generation code
        while (true)
        {
            // Start corridoring in random direction
            bool result = MakeCorridor();
            if (result == false)
                break;
        }

        //Fill all empty tiles with walls
        FillWithWalls();

        //Do an initial cinematic pan (if desired)
        InitialCinematicPan();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //// --- Corridor Spawning ---
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    // Makes a corridor in a random direction
    // Returns false if cannot continue in any direction
    bool MakeCorridor()
    {
        // Make a list of all possible directions
        List <Vector2Int> possibleDirections = new List<Vector2Int>();
        possibleDirections.Add(N);
        possibleDirections.Add(S);
        possibleDirections.Add(E);
        possibleDirections.Add(W);

        // Chose a direction to go in
        Vector2Int direction = new Vector2Int(0, 0);
        while (possibleDirections.Count > 0)
        {
            // Chose a random possible direction
            int roll = DieRoll(possibleDirections.Count) - 1;
            direction = possibleDirections[roll];
            // Check if we can move in that direction
            if (GetTile(cursor + direction) != null)  // Cannot move in this direction
                possibleDirections.RemoveAt(roll);
            else                                      // Can move in this direction!
                break;
        }
        // If we can't go in any direction, return false
        if (possibleDirections.Count == 0)
            return false;

        // Make corridor
        int length = DieRoll(10);
        for (int i = 0; i < length; i++)
        {
            // Check if can continue corridor
            if (GetTile(cursor + direction) != null)
                return true;  // Do not continue
            // Move cursor forward
            cursor += direction;
            SpawnTile(cursor.x, cursor.y);
        }

        // Corridor creation successful
        return true;
    }

    //Generate a test room with enemies, pick-ups, etc.
    void GenerateTestRoom()
    {
        //Clear any game object first
        ClearLevel();

        //Create the camera
        SpawnCamera();

        //Create the starting tile
        SpawnTile(0, 0);
        Spawn("player", 0.0f, 0.0f);

        //Create a square room
        //Note that already placed tiles will not be over-written
        for (int x = -3; x <= 3; x++)
            for (int y = -3; y <= 3; y++)
                SpawnTile(x, y);

        //Put a bunch of pick-ups around the player
        Spawn("heart", 0.5f, 0.5f);
        Spawn("healthboost", 0.0f, 0.5f);
        Spawn("speedboost", 0.5f, 0.0f);
        Spawn("shotboost", 0.5f, -0.5f);
        Spawn("heart", -0.5f, -0.5f);
        Spawn("healthboost", 0.0f, -0.5f);
        Spawn("speedboost", -0.5f, 0.0f);
        Spawn("shotboost", -0.5f, 0.5f);

        //Add some test enemies
        Spawn("enemy", 2.5f, 2.5f);
        Spawn("fast", 0.0f, 2.5f);
        Spawn("tank", 2.5f, 0.0f);
        Spawn("ultra", -2.5f, 2.5f);
        Spawn("spread", -2.5f, -2.5f);
        Spawn("boss", 0.0f, -2.5f);

        //Don't forget the exit...
        Spawn("portal", 3.0f, -3.0f);

        //Fill all empty tiles with walls
        FillWithWalls();
    }

    //Clear the entire level except for the PCG object
    void ClearLevel()
    {
        //Delete everything       
        var objsToDelete = FindObjectsOfType<GameObject>();
        for (int i = 0; i < objsToDelete.Length; i++)
        {
            if (objsToDelete[i].GetComponent<PCG>() == null) //Don't delete the PCG object
                UnityEngine.Object.DestroyImmediate(objsToDelete[i]);
        }
    }

    //Reset the level after a delay
    //This is public so it can be called from other files using the static PCGObject from above
    public void ResetLevel(float delay)
    {
        Invoke("ResetLevel", delay);
    }

    //Reset the level immediately
    //This is public so it can be called from other files using the static PCGObject from above
    public void ResetLevel()
    {
        var currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }

    //Get a tile object (only walls and floors, currently)
    GameObject GetTile(int x, int y)
	{
		if (Math.Abs(x) > MaxMapSize/2 || Math.Abs(y) > MaxMapSize/2)
			return Prefabs["wall"];
		return TileMap[(y * MaxMapSize) + x + TileMapMidPoint];
	}
    //Get a tile object (only walls and floors, currently)
    GameObject GetTile(Vector2Int pos)
    {
        if (Math.Abs(pos.x) > MaxMapSize / 2 || Math.Abs(pos.y) > MaxMapSize / 2)
            return Prefabs["wall"];
        return TileMap[(pos.y * MaxMapSize) + pos.x + TileMapMidPoint];
    }

    //Spawn a tile object if somthing isn't already there
    void SpawnTile(int x, int y)
	{
		if (GetTile(x,y) != null)
			return;
		TileMap[(y * MaxMapSize) + x + TileMapMidPoint] = Spawn("floor", x, y);
	}

    //Spawn a wall object if something isn't already there
    void SpawnWall(int x, int y)
    {
        if (GetTile(x, y) != null)
            return;
        TileMap[(y * MaxMapSize) + x + TileMapMidPoint] = Spawn("wall", x, y);
    }

    //Fill any empty tiles with walls
    void FillWithWalls()
    {
        for (int x = -MaxMapSize / 2; x <= MaxMapSize / 2; x++)
            for (int y = -MaxMapSize / 2; y <= MaxMapSize / 2; y++)
                SpawnWall(x, y);
    }

    //Spawn any object
    GameObject Spawn(string obj, float x, float y)
	{
		return Instantiate(Prefabs[obj], new Vector3(x * GridSize, y * GridSize, 0.0f), Quaternion.identity);
	}

	//Spawn any object rotated 90 degrees left
	GameObject SpawnRotateLeft(string obj, float x, float y)
	{
		return Instantiate(Prefabs[obj], new Vector3(x * GridSize, y * GridSize, 0.0f), Quaternion.AngleAxis(-90, Vector3.forward));
	}

	//Spawn any object rotated 90 degrees right
	GameObject SpawnRotateRight(string obj, float x, float y)
	{
		return Instantiate(Prefabs[obj], new Vector3(x * GridSize, y * GridSize, 0.0f), Quaternion.AngleAxis(90, Vector3.forward));
	}

    //Spawn main camera and the weighted camera target
    void SpawnCamera()
    {
        var cam = Instantiate(PCGObject.Prefabs["camera"]);
        var wct = Instantiate(PCGObject.Prefabs["cameratarget"]);
        cam.GetComponent<CameraFollow>().ObjectToFollow = wct.transform;
    }

    //Try to pan to something of interest
    void InitialCinematicPan()
    {
        //Pan to the first silver key if one exists
        var panTo = GameObject.Find("SilverKey(Clone)");
        //Otherwise pan to the first gold key if one exists
        if (panTo == null)
            panTo = GameObject.Find("GoldKey(Clone)");
        //Otherwise pan to the first boss enemy if one exists
        if (panTo == null)
            panTo = GameObject.Find("BossEnemy(Clone)");
        //Otherwise pan to the first exit portal (which should exist)
        if (panTo == null)
            panTo = GameObject.Find("Portal(Clone)");
        //Did we find something?
        if (panTo == null)
            return;
        //Create a cinematic anchor to pan to, and set it's position
        var cinematicAnchor = Instantiate(PCGObject.Prefabs["cinematicanchor"]);
        cinematicAnchor.transform.position = panTo.transform.position;
	}		

	void LoadPrefabs()
	{
		//Create a prefabs dictionary
        Prefabs = new Dictionary<string, GameObject>();

        //Load all the prefabs we need for map generation (note that these must be in the "Resources" folder)
        Prefabs.Add("floor", Resources.Load<GameObject>("Prefabs/Tiles/Floor"));
        Prefabs["floor"].transform.localScale = new Vector3(GridSize, GridSize, 1.0f); //Scale the floor properly
        Prefabs.Add("special", Resources.Load<GameObject>("Prefabs/Tiles/FloorSpecial"));
        Prefabs["special"].transform.localScale = new Vector3(GridSize, GridSize, 1.0f); //Scale the floor properly
        Prefabs.Add("wall", Resources.Load<GameObject>("Prefabs/Tiles/Wall"));
        Prefabs["wall"].transform.localScale = new Vector3(GridSize, GridSize, 1.0f); //Scale the wall properly
        Prefabs.Add("silverdoor", Resources.Load<GameObject>("Prefabs/Tiles/SilverDoor"));
        Prefabs["silverdoor"].transform.localScale = new Vector3(GridSize / 2.0f, 1.0f, 1.0f); //Scale the door properly
        Prefabs.Add("golddoor", Resources.Load<GameObject>("Prefabs/Tiles/GoldDoor"));
        Prefabs["golddoor"].transform.localScale = new Vector3(GridSize / 2.0f, 1.0f, 1.0f); //Scale the door properly
        Prefabs.Add("portal", Resources.Load<GameObject>("Prefabs/Tiles/Portal"));

        //Load the player prefab
        Prefabs.Add("player", Resources.Load<GameObject>("Prefabs/Player/Player"));

        //Load all the enemy prefabs
        Prefabs.Add("enemy", Resources.Load<GameObject>("Prefabs/Enemies/BaseEnemy"));
        Prefabs.Add("fast", Resources.Load<GameObject>("Prefabs/Enemies/FastEnemy"));
        Prefabs.Add("spread", Resources.Load<GameObject>("Prefabs/Enemies/SpreadEnemy"));
        Prefabs.Add("tank", Resources.Load<GameObject>("Prefabs/Enemies/TankEnemy"));
        Prefabs.Add("ultra", Resources.Load<GameObject>("Prefabs/Enemies/UltraEnemy"));
        Prefabs.Add("boss", Resources.Load<GameObject>("Prefabs/Enemies/BossEnemy"));

		//Load all the pick-ups
        Prefabs.Add("heart", Resources.Load<GameObject>("Prefabs/Pickups/HeartPickup"));
        Prefabs.Add("healthboost", Resources.Load<GameObject>("Prefabs/Pickups/HealthBoost"));
        Prefabs.Add("shotboost", Resources.Load<GameObject>("Prefabs/Pickups/ShotBoost"));
        Prefabs.Add("speedboost", Resources.Load<GameObject>("Prefabs/Pickups/SpeedBoost"));
        Prefabs.Add("silverkey", Resources.Load<GameObject>("Prefabs/Pickups/SilverKey"));
        Prefabs.Add("goldkey", Resources.Load<GameObject>("Prefabs/Pickups/GoldKey"));

        //Load all the camera and UI prefabs
        Prefabs.Add("camera", Resources.Load<GameObject>("Prefabs/Camera/Main Camera"));
        Prefabs.Add("cameratarget", Resources.Load<GameObject>("Prefabs/Camera/WeightedCameraTarget"));
        Prefabs.Add("cinematicanchor", Resources.Load<GameObject>("Prefabs/Camera/CinematicAnchor"));
        Prefabs.Add("uicanvas", Resources.Load<GameObject>("Prefabs/Player/UICanvas"));
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //// --- RNG ---
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    // Returns a random direction
    Vector2Int RandomDir()
    {
        int roll = DieRoll(4);
        switch(roll)
        {
            case 1: return N;
            case 2: return E;
            case 3: return S;
            case 4: return W;
            default: return new Vector2Int(0, 0);
        }
    }

    // Rolls an n sided dice, returns the result
    int DieRoll(int n)
    {
        return RNG.Next(1, n);
    }
}
