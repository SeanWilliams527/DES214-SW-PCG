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

    Queue<Vector2Int> Branches = new Queue<Vector2Int>();

    struct Room
    {
        public int w;
        public int h;

        public int Right;
        public int Left;
        public int Up;
        public int Down;

        public Vector2Int entrance;
    };

    //////////////////////////////////////////////////////////////////////////
    // DESIGNER ADJUSTABLE VALUES
    //////////////////////////////////////////////////////////////////////////

    //Maximum height and width of tile map (must be odd, somewhere between 21 and 101 works well)
    private int MaxMapSize = 101;
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

        //P to generate custom room
        if (Input.GetKey(KeyCode.P))
        {
            GenerateCustomRoom();
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
            if (result == false) // We failed to make a corridor
            {
                if (Branches.Count == 0)
                    break;  // Generation over

                // Pick up at next branch
                cursor = Branches.Dequeue();
            }
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
            if (!CanCorridor(cursor + direction, direction))  // Cannot move in this direction
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
            if (!CanCorridor(cursor + direction, direction))
                return true;  // Do not continue
            // Move cursor forward
            cursor += direction;
            SpawnTile(cursor.x, cursor.y);

            // Randomly place branches
            if (PercentRoll(20))
                Branches.Enqueue(cursor);
            // Randomly place a room
            if (PercentRoll(10))
            {
                MakeRoom(cursor, direction);
                return false;
            }
        }

        // Corridor creation can continue from this point
        return true;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //// --- Room Spawning ---
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    // Make a room
    void MakeRoom(Vector2Int entrance, Vector2Int dir)
    {
        // Dimensions for rooms
        int maxWidth = 26;
        int maxHeight = 26;
        int minWidth = 10;
        int minHeight = 10;

        // Make room object
        int tries = 3;  // Number of attempts we have to make a room
        Room room = new Room();
        while (tries > 0)
        {
            int w = RandInt(minWidth, maxWidth);
            int h = RandInt(minHeight, maxHeight);
            // Ensure that dimensions are even
            if (w % 2 != 0)
                w += 1;
            if (h % 2 != 0)
                h += 1;

            room = ConstructRoomObject(cursor, dir, w, h);
            if (CheckRoom(room))  // We have room to make this room!
                break;
            tries--;
        }
        if (tries == 0)
            return;


        // Place room
        for (int x = room.Left; x <= room.Right; x++)
        {
            for (int y = room.Down; y <= room.Up; y++)
            {
                SpawnTile(x, y);
            }
        }

        // Roll for exit on each wall
        if (PercentRoll(60))  // Roll for exit left
        {
            Branches.Enqueue(new Vector2Int(room.Left, RandInt(room.Down, room.Up)));
        }
        if (PercentRoll(60))  // Roll for exit right
        {
            Branches.Enqueue(new Vector2Int(room.Right, RandInt(room.Down, room.Up)));
        }
        if (PercentRoll(60))  // Roll for exit Up
        {
            Branches.Enqueue(new Vector2Int(RandInt(room.Left, room.Right), room.Up));
        }
        if (PercentRoll(60))  // Roll for exit Down
        {
            Branches.Enqueue(new Vector2Int(RandInt(room.Left, room.Right), room.Down));
        }

        //RoomAddCrossPillars(room);
        RoomAddMiddlePillar(room);
    }

    // Constructs the room object, does not guarentee it can be placed
    Room ConstructRoomObject(Vector2Int entrance, Vector2Int dir, int w, int h)
    {
        Room room = new Room();
        room.w = w;
        room.h = h;
        room.entrance = entrance;

        // Entrance is on bottom wall
        if (dir == N)
        {
            room.Down = entrance.y + dir.y;
            room.Up = entrance.y + h;
            room.Left = entrance.x - w / 2;
            room.Right = room.Left + w - 1;
        }
        // Entrance is on top wall
        else if (dir == S)
        {
            room.Up = entrance.y + dir.y;
            room.Down = room.Up - h;
            room.Left = entrance.x - w / 2;
            room.Right = room.Left + (w - 1);
        }
        // Entrance is on left wall
        else if (dir == E)
        {
            room.Up = entrance.y + h / 2;
            room.Down = entrance.y - (h - 1);
            room.Left = entrance.x + dir.x;
            room.Right = room.Left + (w - 1);
        }
        // Entrance is on right wall
        else if (dir == W)
        {
            room.Up = entrance.y + h / 2;
            room.Down = entrance.y - (h - 1);
            room.Right = entrance.x + dir.x;
            room.Left = room.Right - (w - 1);
        }

        return room;
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

    // Generate a custom room for testing purposes
    void GenerateCustomRoom()
    {
        //Clear any game object first
        ClearLevel();

        //Create the camera
        SpawnCamera();

        //Create the starting tile
        SpawnTile(0, 0);
        Spawn("player", 0.0f, 0.0f);
        cursor.x = 0; cursor.y = 0;

        DeleteTile(cursor);
        GetTile(cursor);
        Vector2Int dir = RandomDir();
        MakeRoom(cursor + dir, dir);

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

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //// --- Room Modification ---
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // Add cross pillars to the room
    void RoomAddCrossPillars(Room room)
    {
        // Minimum dimensions for room to qualify for cross pillars
        int RoomwMin = 10;
        int RoomhMin = 10;
        if (room.w < RoomwMin || room.h < RoomhMin)
            return;  // Room is too small!

        // Make top row of pillars
        // Position cursor on first pillar location for top row
        cursor.x = room.Left + 2;
        cursor.y = room.Up - 2;
        // Add pillars from left to right
        int end = room.Right - 1;
        while (cursor.x < end)
        {
            // Make cross pillar
            MakeCrossPillar();
            // Move the cursor
            cursor.x += 5;
        }

        // Make bottom row of pillars
        // Position cursor on first pillar location for bottom row
        cursor.x = room.Left + 2;
        cursor.y = room.Down + 2;
        // Add pillars from left to right
        while (cursor.x < end)
        {
            // Make cross pillar
            MakeCrossPillar();
            // Move the cursor
            cursor.x += 5;
        }
    }

    // Makes a cross pillar at the cursor
    void MakeCrossPillar()
    {
        DeleteTile(cursor);
        DeleteTile(cursor + N);
        DeleteTile(cursor + E);
        DeleteTile(cursor + S);
        DeleteTile(cursor + W);
    }

    void RoomAddMiddlePillar(Room room)
    {
        // Minimum dimensions for room to qualify for middle pillar
        int RoomwMin = 6;
        int RoomhMin = 6;
        if (room.w < RoomwMin || room.h < RoomhMin)
            return;  // Room is too small!

        // Make middle pillar
        cursor.x = room.Left + (room.w / 2) - 2;
        cursor.y = room.Down + (room.h / 2) - 2;
        DeleteTile(cursor + W);
        DeleteTile(cursor + W + W);
        cursor += N;
        DeleteTile(cursor);
        DeleteTile(cursor + W);
        DeleteTile(cursor + W + W);
        DeleteTile(cursor + W + W + W);
        cursor += N;
        DeleteTile(cursor);
        DeleteTile(cursor + W);
        DeleteTile(cursor + W + W);
        DeleteTile(cursor + W + W + W);
        cursor += N;
        DeleteTile(cursor + W);
        DeleteTile(cursor + W + W);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //// --- AREA CHECKING ---
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    bool CheckRoom(Room room)
    {
        // Check area for any tiles
        for (int x = room.Left; x <= room.Right; x++)
        {
            for (int y = room.Down; y <= room.Up; y++)
            {
                if (GetTile(x, y) != null)
                    return false;
            }
        }

        return true;
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

    // Can we continue a corridor at pos going in dir direction
    bool CanCorridor(Vector2Int pos, Vector2Int dir)
    {
        if (GetTile(pos) != null)
            return false;

        // We want to add padding to sides of corridors
        int temp = dir.y;
        dir.y = dir.x;
        dir.x = temp;
        if (GetTile(pos + dir) != null || GetTile(pos - dir) != null)
            return false;

        return true;
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

    void DeleteTile(Vector2Int pos)
    {
        // Check if out of bounds
        if (Math.Abs(pos.x) > MaxMapSize / 2 || Math.Abs(pos.y) > MaxMapSize / 2)
            return;
        Destroy(TileMap[(pos.y * MaxMapSize) + pos.x + TileMapMidPoint]);
        TileMap[(pos.y * MaxMapSize) + pos.x + TileMapMidPoint] = null;
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
        return RNG.Next(1, n + 1);
    }

    // Rolls for a random percent chance
    bool PercentRoll(int percentage)
    {
        return RNG.Next(1, 101) <= percentage;
    }

    // Generates random integer between min and max inclusively
    int RandInt(int min, int max)
    {
        return RNG.Next(min, max + 1);
    }
}
