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

    // Room Sizing
    int minSmallRoomSize = 4; int maxSmallRoomSize = 6;
    int minMediumRoomSize = 8; int maxMediumRoomSize = 16;
    int minLargeRoomSize = 18; int maxLargeRoomSize = 26;

    // Corridor Sizing
    int minShortCorridorSize = 1; int maxShortCorridorSize = 6;
    int minMediumCorridorSize = 7; int maxMediumCorridorSize = 12;
    int minLongCorridorSize = 13; int maxLongCorridorSize = 23;

    // Generation mode
    struct Mode
    {
        // Room size chances
        public int smallRoomChance;
        public int mediumRoomChance;
        public int largeRoomChance;
        // Room variation chances
        public int roomMiddlePillarChance;
        public int roomCrossPillarChance;
        public int roomCourtyardWallChance;
        public int roomRoundedChance;
        // Room exit chances
        public int roomExitChance;

        // Corridor type chances
        public int normalCorridorChance;
        public int snakeCorridorChance;
        public int outcoveCorridorChance;
        // Corridor length chances
        public int shortCorridorChance;
        public int medCorridorChance;
        public int longCorridorChance;
        // Corridor room spawning chances
        public int normalCorridorRoomchance;
        public int snakeCorridorRoomchance;
        public int outcoveCorridorRoomchance;
        // Corridor branch chances
        public int normalCorridorBranchchance;
        public int snakeCorridorBranchchance;
        public int outcoveCorridorBranchchance;
    };

    // The current generation mode
    Mode CurrentGenerationMode;
    Mode GenerationModeSetup;
    Mode GenerationModeDevelopment1;
    Mode GenerationModeDevelopment2;
    Mode GenerationModeDevelopment3;


    //////////////////////////////////////////////////////////////////////////
    // DESIGNER ADJUSTABLE VALUES
    //////////////////////////////////////////////////////////////////////////

    //Maximum height and width of tile map (must be odd, somewhere between 21 and 101 works well)
    private int MaxMapSize = 201;
    //Size of floor and wall tiles in Unity units (somewhere between 1.0f and 10.0f works well)
    private float GridSize = 5.0f;

    [SerializeField]
    bool useSeed;
    [SerializeField]
    int seed;

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

        // Make all Generation Modes
        InitializeGenerationModes();

        //Create the tile map
        if (MaxMapSize % 2 == 0)
            ++MaxMapSize; //This needs to be an odd number
        TileMap = new GameObject[MaxMapSize * MaxMapSize];
        TileMapMidPoint = (MaxMapSize * MaxMapSize) / 2; //The 0,0 point

        //Get a new random generator
        //Could feed it a set seed if need the same level for debugging purposes
        ReSeedRNG();

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

        // Press number to generate enemy test room
        if (Input.GetKey(KeyCode.Alpha1))
            GenerateEnemyRoom(1);
        if (Input.GetKey(KeyCode.Alpha2))
            GenerateEnemyRoom(2);
        if (Input.GetKey(KeyCode.Alpha3))
            GenerateEnemyRoom(3);
        if (Input.GetKey(KeyCode.Alpha4))
            GenerateEnemyRoom(4);
        if (Input.GetKey(KeyCode.Alpha5))
            GenerateEnemyRoom(5);
        if (Input.GetKey(KeyCode.Alpha6))
            GenerateEnemyRoom(6);
        if (Input.GetKey(KeyCode.Alpha7))
            GenerateEnemyRoom(7);
        if (Input.GetKey(KeyCode.Alpha8))
            GenerateEnemyRoom(8);
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
        GenerateStartingRooms();
        Spawn("player", 0.0f, 0.0f);

        CurrentGenerationMode = GenerationModeSetup;

        cursor = Branches.Dequeue();

        // Main Generation code
        while (true)
        {
            // Start corridoring in random direction
            bool result = false;
            // Roll for what corridor to use
            int[] corridors = { CurrentGenerationMode.normalCorridorChance, CurrentGenerationMode.snakeCorridorChance, CurrentGenerationMode.outcoveCorridorChance };
            int corridor = RandomOutcome(corridors);
            switch (corridor)
            {
                case 0: result = MakeCorridor(); break;
                case 1: result = MakeCorridor(); break;
                case 2: result = MakeCorridor(); break;
                default: Debug.LogError("Invalid Corridor Chances. Make sure chances to spawn each corridor add up to 100"); break;
            }

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

        // Make sure the player cant escape by making perimeter wall
        MakePerimeterWall();

        //Do an initial cinematic pan (if desired)
        InitialCinematicPan();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //// --- Corridor Spawning ---
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // Makes a corridor in a random direction
    // Returns false if cannot continue corridoring from last tile in corridor
    bool MakeCorridor()
    {
        // Decide Random direction to go
        List<Vector2Int> possibleDirections = CorridorGetPossibleDirectionsNoSides();
        if (possibleDirections.Count == 0)
            return false;  // No directions to go
        Vector2Int direction = possibleDirections[DieRoll(possibleDirections.Count) - 1];

        // Make corridor
        int length = DieRoll(10);
        for (int i = 0; i < length; i++)
        {
            // Check if can continue corridor
            if (!CanCorridorNoSides(cursor + direction, direction))
                return true;  // Do not continue
            // Move cursor forward
            cursor += direction;
            SpawnTile(cursor.x, cursor.y);

            // Randomly place branches
            if (PercentRoll(CurrentGenerationMode.normalCorridorBranchchance))
                Branches.Enqueue(cursor);
            // Randomly place a room
            if (PercentRoll(CurrentGenerationMode.normalCorridorRoomchance))
            {
                // If we successfully create room, stop corridoring
                if (MakeRoom(cursor, direction))
                    return false;
            }
        }

        // Corridor creation can continue from this point
        return true;
    }

    // Make a snake corridoor
    bool MakeSnakeCorridor()
    {
        // ---------------- Snake Variables ---------------------
        int snakeMinLength = 3;
        int snakeMaxLength = 30;
        // Snake goes back and forth
        // How many tiles should we go before snaking to other side
        int snakeMinStride = 3;
        int snakeMaxStride = 8;
        // Percent chance for branch on each side tile placed
        int branchChance = 5;
        // ------------------------------------------------------

        // Decide direction to go
        List<Vector2Int> possibleDirections = CorridorGetPossibleDirectionsNoSides();
        if (possibleDirections.Count == 0)
            return false;  // No directions to go
        Vector2Int dir = possibleDirections[DieRoll(possibleDirections.Count) - 1];
        // Corridor will go forward and side to side
        Vector2Int side1 = InvertVector(dir);
        Vector2Int side2 = side1 * -1;

        // Determine snake length and stride
        int length = RandInt(snakeMinLength, snakeMaxLength);
        int stride = RandInt(snakeMinStride, snakeMaxStride);

        // Construct snake
        Vector2Int currentSide = side1;   // Which side are we currently going forward in
        int strideAmountLeft = stride;    // How much length is left in the current stride
        for (int i = 0; i < length; i++)
        {
            // Snake will trample over anything in its way
            // Only check if going out of bounds
            if (IsOutOfBounds(cursor + dir + currentSide))
                return false; // Cannot continue

            // Move cursor forward and to side (move diagonally)
            cursor += (dir + currentSide);
            SpawnTile(cursor);
            SpawnTile(cursor + side1);
            SpawnTile(cursor + side2);

            strideAmountLeft--;

            // Check if we are done with current stride
            if (strideAmountLeft == 0)
            {
                // Swap current side
                if (currentSide == side1)
                    currentSide = side2;
                else
                    currentSide = side1;
                // Reset Stride
                strideAmountLeft = stride;
            }

            // Place branches at random
            if (PercentRoll(branchChance))  // Roll for branch on side 1
                Branches.Enqueue(cursor + side1);
            if (PercentRoll(branchChance))  // Roll for branch on side 2
                Branches.Enqueue(cursor + side2);
        }

        return true;
    }

    // Make a drunk corridoor
    bool MakeDrunkCorridor()
    {
        // ---------------- Drunk Variables ---------------------
        int minLength = 5;
        int maxLength = 20;
        // ------------------------------------------------------

        int length = RandInt(minLength, maxLength);

        for (int i = 0; i < length; i++)
        {
            // Decide Random direction to walk
            // This direction will change every step
            List<Vector2Int> possibleDirections = CorridorGetPossibleDirections();
            if (possibleDirections.Count == 0)
                return false;  // No directions to go
            Vector2Int dir = possibleDirections[DieRoll(possibleDirections.Count) - 1];

            // Walk cursor forward
            cursor += dir;
            // Place tile at cursor
            SpawnTile(cursor.x, cursor.y);
        }

        return true;
    }

    // Make an outcove corridoor
    bool MakeOutcoveCorridoor()
    {
        // ---------------- Outcove Variables ---------------------
        int minLength = 6;
        int maxLength = 20;

        int roomChance = 5;
        int branchChance = 5;
        // --------------------------------------------------------

        List<Vector2Int> possibleDirections = CorridorGetPossibleDirections();
        if (possibleDirections.Count == 0)
            return false;  // No directions to go
        Vector2Int dir = possibleDirections[DieRoll(possibleDirections.Count) - 1];
        // Corridor will have outcoves that go side to side
        Vector2Int side1 = InvertVector(dir);
        Vector2Int side2 = side1 * -1;
        int length = RandInt(minLength, maxLength);

        bool makeOutcove = false;  // Should we make an outcove on this step?
        // Make a corridor with alternating skinny and outcove sections
        for (int i = 0; i < length; i++)
        {
            // Outcove corridor will trample over anything in its way
            // Only check if going out of bounds
            if (IsOutOfBounds(cursor + dir))
                return false; // Cannot continue

            // Move cursor forward
            cursor += dir;
            SpawnTile(cursor);
            // Make outcove
            if (makeOutcove)
            {
                SpawnTile(cursor + side1);
                SpawnTile(cursor + side2);

                if (PercentRoll(branchChance))
                    Branches.Enqueue(cursor + side1);
                if (PercentRoll(branchChance))
                    Branches.Enqueue(cursor + side2);
            }

            makeOutcove = !makeOutcove;
        }

        return true;
    }

    // Returns a list of directions that a corridor can go in if you arent allowed to connect on the side
    List<Vector2Int> CorridorGetPossibleDirectionsNoSides()
    {
        // Make a list of all directions
        List<Vector2Int> possibleDirections = new List<Vector2Int>();
        if (CanCorridorNoSides(cursor + N, N))
            possibleDirections.Add(N);
        if (CanCorridorNoSides(cursor + E, E))
            possibleDirections.Add(E);
        if (CanCorridorNoSides(cursor + S, S))
            possibleDirections.Add(S);
        if (CanCorridorNoSides(cursor + W, W))
            possibleDirections.Add(W);

        return possibleDirections;
    }

    // Returns a list of directions that a corridor can go in
    List<Vector2Int> CorridorGetPossibleDirections()
    {
        // Make a list of all directions
        List<Vector2Int> possibleDirections = new List<Vector2Int>();
        if (GetTile(cursor + N) == null)
            possibleDirections.Add(N);
        if (GetTile(cursor + E) == null)
            possibleDirections.Add(E);
        if (GetTile(cursor + S) == null)
            possibleDirections.Add(S);
        if (GetTile(cursor + W) == null)
            possibleDirections.Add(W);

        return possibleDirections;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //// --- Room Spawning ---
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // Make a room. Returns false if could not create room
    bool MakeRoom(Vector2Int entrance, Vector2Int dir)
    {
        // Dimensions for rooms
        int maxWidth = 0;
        int maxHeight = 0;
        int minWidth = 0;
        int minHeight = 0;
        // Determine if small, medium, or large room
        int[] roomSize = { CurrentGenerationMode.smallRoomChance, CurrentGenerationMode.mediumRoomChance, CurrentGenerationMode.largeRoomChance };
        switch (RandomOutcome(roomSize))
        {
            case 0: 
                maxWidth = maxSmallRoomSize; maxHeight = maxSmallRoomSize;
                minWidth = minSmallRoomSize; minHeight = minSmallRoomSize;
                break;
            case 1:
                maxWidth = maxMediumRoomSize; maxHeight = maxMediumRoomSize;
                minWidth = minMediumRoomSize; minHeight = minMediumRoomSize;
                break;
            case 2:
                maxWidth = maxLargeRoomSize; maxHeight = maxLargeRoomSize;
                minWidth = minLargeRoomSize; minHeight = minLargeRoomSize;
                break;
        }


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
            return false;  // Room creation unsuccessful


        // Place room
        for (int x = room.Left; x <= room.Right; x++)
        {
            for (int y = room.Down; y <= room.Up; y++)
            {
                SpawnTile(x, y);
            }
        }


        // Roll to modify room

        if (PercentRoll(40))
            RoomAddMiddlePillar(room);

        int roll = DieRoll(6);
        if (roll == 1)
            RoomAddCrossPillars(room);
        else if (roll == 2 || roll == 3 || roll == 4)
            RoomAddCourtYardWalls(room);

        if (PercentRoll(35))
            RoomMakeRound(room);

        // Roll for exit on each wall
        int exitChance = 60;
        if (PercentRoll(exitChance))  // Roll for exit left
        {
            RoomAddExit(room, N);
        }
        if (PercentRoll(exitChance))  // Roll for exit right
        {
            RoomAddExit(room, E);
        }
        if (PercentRoll(exitChance))  // Roll for exit Up
        {
            RoomAddExit(room, S);
        }
        if (PercentRoll(exitChance))  // Roll for exit Down
        {
            RoomAddExit(room, W);
        }

        return true;  // Successfully created room
    }

    // Fill an area with floor tiles
    // Bottom left corner of room will be the cursor
    void FillAreaWithTile(int width, int height)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                SpawnTile(cursor.x + x, cursor.y + y);
            }
        }
    }

    void RoomAddExit(Room room, Vector2Int side)
    {
        // Decide location of exit
        Vector2Int exit = new Vector2Int(0, 0);
        if (side == N)
            exit = new Vector2Int(room.Left, RandInt(room.Down, room.Up));
        else if (side == E)
            exit = new Vector2Int(room.Right, RandInt(room.Down, room.Up));
        else if (side == S)
            exit = new Vector2Int(RandInt(room.Left, room.Right), room.Up);
        else if (side == W)
            exit = new Vector2Int(RandInt(room.Left, room.Right), room.Down);

        Branches.Enqueue(exit);

        // Make sure player can reach exitr
        cursor = exit;
        Vector2Int opDir = -1 * side;
        // Make exit reachable
        while(!IsTileReachable(cursor))
        {
            cursor += opDir;
            SpawnTile(cursor);
        }
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
        Spawn("player", 0.0f, -8.0f);

        //Create a square room for enemies
        //Note that already placed tiles will not be over-written
        for (int x = -3; x <= 3; x++)
            for (int y = -3; y <= 3; y++)
                SpawnTile(x, y);

        SpawnTile(0, -4);
        SpawnTile(0, -5);

        for (int x = -3; x <= 3; x++)
            for (int y = -12; y <= -6; y++)
                SpawnTile(x, y);

        //Create a square room for items
        //Note that already placed tiles will not be over-written
        for (int x = -3; x <= 3; x++)
            for (int y = -3; y <= 3; y++)
                SpawnTile(x, y);

        //Put a bunch of pick-ups around the player
        Spawn("heart", 0.5f, 0.5f - 8.0f);
        Spawn("healthboost", 0.0f, 0.5f - 8.0f);
        Spawn("speedboost", 0.5f, 0.0f - 8.0f);
        Spawn("shotboost", 0.5f, -0.5f - 8.0f);
        Spawn("heart", -0.5f, -0.5f - 8.0f);
        Spawn("healthboost", 0.0f, -0.5f - 8.0f);
        Spawn("speedboost", -0.5f, 0.0f - 8.0f);
        Spawn("shotboost", -0.5f, 0.5f - 8.0f);

        //Add some test enemies
        Spawn("enemy", 2.5f, 2.5f);
        Spawn("fast", 0.0f, -2.5f);
        Spawn("areaEnemy", 0.0f, -1.5f);
        Spawn("tank", 2.5f, 0.0f);
        Spawn("ultra", -2.5f, 2.5f);
        Spawn("spread", -2.5f, -2.5f);
        Spawn("ultraArea", 0.0f, 0.0f);
        Spawn("boss", 0.0f, 2.5f);

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

        MakeDrunkCorridor();
        //Vector2Int dir = RandomDir();
        //MakeRoom(cursor + dir, dir);

        FillWithWalls();
    }

    // Generate a room with one custom enemy
    void GenerateEnemyRoom(int room)
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

        // Spawn enemy
        if (room == 1)
            Spawn("enemy", 0.0f, 2.5f);
        if (room == 2)
            Spawn("fast", 0.0f, 2.5f);
        if (room == 3)
            Spawn("areaEnemy", 0.0f, 2.5f);
        if (room == 4)
            Spawn("tank", 0.0f, 2.5f);
        if (room == 5)
            Spawn("spread", 0.0f, 2.5f);
        if (room == 6)
            Spawn("ultra", 0.0f, 2.5f);
        if (room == 7)
            Spawn("ultraArea", 0.0f, 2.5f);
        if (room == 8)
            Spawn("boss", 0.0f, 2.5f);

        //Fill all empty tiles with walls
        FillWithWalls();
    }

    // Generate starting rooms at the 4 corners of the map
    void GenerateStartingRooms()
    {
        // Dimensions of each starting room
        int roomWidth = 3;
        int roomHeight = 3;

        // Calculate edge of map
        int halfMapSize = MaxMapSize / 2;

        // Generate NW room
        cursor.x = -halfMapSize; cursor.y = halfMapSize - (roomHeight - 1);
        FillAreaWithTile(roomWidth, roomHeight);
        Branches.Enqueue(new Vector2Int(-halfMapSize + 1, halfMapSize - 2));
        Branches.Enqueue(new Vector2Int(-halfMapSize + 2, halfMapSize - 1));
        // Generate NE room
        cursor.x = halfMapSize - (roomWidth - 1); cursor.y = halfMapSize - (roomHeight - 1);
        FillAreaWithTile(roomWidth, roomHeight);
        Branches.Enqueue(new Vector2Int(halfMapSize - 2, halfMapSize - 1));
        Branches.Enqueue(new Vector2Int(halfMapSize - 1, halfMapSize - 2));
        // Generate SE room
        cursor.x = halfMapSize - (roomWidth - 1); cursor.y = -halfMapSize;
        FillAreaWithTile(roomWidth, roomHeight);
        Branches.Enqueue(new Vector2Int(halfMapSize - 1, -halfMapSize + 2));
        Branches.Enqueue(new Vector2Int(halfMapSize - 2, -halfMapSize + 1));
        // Generate SW room
        cursor.x = -halfMapSize; cursor.y = -halfMapSize;
        FillAreaWithTile(roomWidth, roomHeight);
        Branches.Enqueue(new Vector2Int(-halfMapSize + 2, -halfMapSize + 1));
        Branches.Enqueue(new Vector2Int(-halfMapSize + 1, -halfMapSize + 2));
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
        ReSeedRNG();

        var currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //// --- Room Modification ---
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // Make the room rounded on the down left edge
    void RoomAddRoundEdgeDL(Room room)
    {
        int height = (room.h / 2) - 1;

        //for ()
    }

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
        DeleteTile(cursor + E);
        DeleteTile(cursor + E + E);
        cursor += N;
        DeleteTile(cursor);
        DeleteTile(cursor + E);
        DeleteTile(cursor + E + E);
        DeleteTile(cursor + E + E + E);
        cursor += N;
        DeleteTile(cursor);
        DeleteTile(cursor + E);
        DeleteTile(cursor + E + E);
        DeleteTile(cursor + E + E + E);
        cursor += N;
        DeleteTile(cursor + E);
        DeleteTile(cursor + E + E);
    }

    void RoomAddCourtYardWalls(Room room)
    {
        // Minimum dimensions for room to qualify for middle pillar
        int RoomwMin = 8;
        int RoomhMin = 8;
        if (room.w < RoomwMin || room.h < RoomhMin)
            return;  // Room is too small!

        int wallWidth = room.w / 4;
        int wallHeight = room.h / 4;

        // Calculate starting points (corners) for each wall
        Vector2Int downLeftStart = new Vector2Int(room.Left + (wallWidth - 1), room.Down + (wallHeight - 1));
        Vector2Int downRightStart = new Vector2Int(room.Right - (wallWidth - 1), downLeftStart.y);
        Vector2Int upLeftStart = new Vector2Int(downLeftStart.x, room.Up - (wallHeight - 1));
        Vector2Int upRightStart = new Vector2Int(downRightStart.x, upLeftStart.y);

        // Make vertical parts of wall
        cursor = downLeftStart;
        for (int i = 0; i < wallHeight; i++)
        {
            DeleteTile(cursor);
            cursor += N;
        }
        cursor = downRightStart;
        for (int i = 0; i < wallHeight; i++)
        {
            DeleteTile(cursor);
            cursor += N;
        }
        cursor = upLeftStart;
        for (int i = 0; i < wallHeight; i++)
        {
            DeleteTile(cursor);
            cursor += S;
        }
        cursor = upRightStart;
        for (int i = 0; i < wallHeight; i++)
        {
            DeleteTile(cursor);
            cursor += S;
        }
        //Make horizontal parts of wall
        cursor = downLeftStart;
        for (int i = 0; i < wallWidth; i++)
        {
            DeleteTile(cursor);
            cursor += E;
        }
        cursor = downRightStart;
        for (int i = 0; i < wallWidth; i++)
        {
            DeleteTile(cursor);
            cursor += W;
        }
        cursor = upLeftStart;
        for (int i = 0; i < wallWidth; i++)
        {
            DeleteTile(cursor);
            cursor += E;
        }
        cursor = upRightStart;
        for (int i = 0; i < wallWidth; i++)
        {
            DeleteTile(cursor);
            cursor += W;
        }
    }

    // Make a room rounded
    void RoomMakeRound(Room room)
    {
        // Minimum dimensions for room to qualify for rounding
        int RoomwMin = 6;
        int RoomhMin = 6;
        if (room.w < RoomwMin || room.h < RoomhMin)
            return;  // Room is too small!

        // Radius of the rounded corner
        int radius = Math.Min(room.h, room.w) / 4;

        // Round the NW corner
        cursor.x = room.Left; cursor.y = room.Up;
        for (int i = 0; i < radius; i++)
        {
            int height = radius - i;
            for (int h = 0; h < height; h++)
                DeleteTile(cursor + (h * S));

            // Continue cursor left or right
            cursor += E;
        }

        // Round the NE corner
        cursor.x = room.Right; cursor.y = room.Up;
        for (int i = 0; i < radius; i++)
        {
            int height = radius - i;
            for (int h = 0; h < height; h++)
                DeleteTile(cursor + (h * S));

            // Continue cursor left or right
            cursor += W;
        }

        // Round the SW corner
        cursor.x = room.Left; cursor.y = room.Down;
        for (int i = 0; i < radius; i++)
        {
            int height = radius - i;
            for (int h = 0; h < height; h++)
                DeleteTile(cursor + (h * N));

            // Continue cursor left or right
            cursor += E;
        }

        // Round the SE corner
        cursor.x = room.Right; cursor.y = room.Down;
        for (int i = 0; i < radius; i++)
        {
            int height = radius - i;
            for (int h = 0; h < height; h++)
                DeleteTile(cursor + (h * N));

            // Continue cursor left or right
            cursor += W;
        }
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

    // Can this tile be reached by the player from another tile
    bool IsTileReachable(Vector2Int pos)
    {
        int reachableFrom = 4;  // How many directions is this reachable from
        if (IsOutOfBounds(pos + N) || GetTile(pos + N) == null)
            reachableFrom--;
        if (IsOutOfBounds(pos + S) || GetTile(pos + S) == null)
            reachableFrom--;
        if (IsOutOfBounds(pos + E) || GetTile(pos + E) == null)
            reachableFrom--;
        if (IsOutOfBounds(pos + W) || GetTile(pos + W) == null)
            reachableFrom--;

        return (reachableFrom > 0);
    }


    //Get a tile object (only walls and floors, currently)
    GameObject GetTile(int x, int y)
    {
        if (Math.Abs(x) > MaxMapSize / 2 || Math.Abs(y) > MaxMapSize / 2)
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

    // Returns true if a position is out of bounds
    bool IsOutOfBounds(Vector2Int pos)
    {
        if (Math.Abs(pos.x) > MaxMapSize / 2 || Math.Abs(pos.y) > MaxMapSize / 2)
            return true;
        else
            return false;
    }

    // Can we continue a corridor at pos going in dir direction
    bool CanCorridorNoSides(Vector2Int pos, Vector2Int dir)
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
        if (GetTile(x, y) != null)
            return;
        TileMap[(y * MaxMapSize) + x + TileMapMidPoint] = Spawn("floor", x, y);
    }

    //Spawn a tile object if somthing isn't already there
    void SpawnTile(Vector2Int pos)
    {
        if (GetTile(pos.x, pos.y) != null)
            return;
        TileMap[(pos.y * MaxMapSize) + pos.x + TileMapMidPoint] = Spawn("floor", pos.x, pos.y);
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

    // Spawn walls around the perimeter of the map
    void MakePerimeterWall()
    {
        // Values of where the wall should be placed
        int mapUpEdge = (MaxMapSize / 2) + 1;
        int mapDownEdge = (-MaxMapSize / 2) - 1;
        int mapRightEdge = (MaxMapSize / 2) + 1;
        int mapLeftEdge = (-MaxMapSize / 2) - 1;

        // Construct North wall from left to right
        for (int x = mapLeftEdge; x <= mapRightEdge; x++)
            Spawn("wall", x, mapUpEdge);
        // Construct South wall from left to right
        for (int x = mapLeftEdge; x <= mapRightEdge; x++)
            Spawn("wall", x, mapDownEdge);
        // Construct West wall from bottom to top
        for (int y = mapDownEdge; y <= mapUpEdge; y++)
            Spawn("wall", mapLeftEdge, y);
        // Construct East wall from bottom to top
        for (int y = mapDownEdge; y <= mapUpEdge; y++)
            Spawn("wall", mapRightEdge, y);
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
        Prefabs.Add("areaEnemy", Resources.Load<GameObject>("Prefabs/Enemies/AreaEnemy"));
        Prefabs.Add("fast", Resources.Load<GameObject>("Prefabs/Enemies/FastEnemy"));
        Prefabs.Add("spread", Resources.Load<GameObject>("Prefabs/Enemies/SpreadEnemy"));
        Prefabs.Add("tank", Resources.Load<GameObject>("Prefabs/Enemies/TankEnemy"));
        Prefabs.Add("ultra", Resources.Load<GameObject>("Prefabs/Enemies/UltraEnemy"));
        Prefabs.Add("ultraArea", Resources.Load<GameObject>("Prefabs/Enemies/UltraAreaEnemy"));
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

    void InitializeGenerationModes()
    {
        // Room size chances
        GenerationModeSetup.smallRoomChance = 100;
        GenerationModeSetup.mediumRoomChance = 0;
        GenerationModeSetup.largeRoomChance = 0;
        // Room variation chances
        GenerationModeSetup.roomMiddlePillarChance = 20;
        GenerationModeSetup.roomCrossPillarChance = 0;
        GenerationModeSetup.roomCourtyardWallChance = 0;
        GenerationModeSetup.roomRoundedChance = 100;
        // Room exit chances
        GenerationModeSetup.roomExitChance = 100;

        // Corridor type chances
        GenerationModeSetup.normalCorridorChance = 100;
        GenerationModeSetup.snakeCorridorChance = 0;
        GenerationModeSetup.outcoveCorridorChance = 0;
        // Corridor length chances
        GenerationModeSetup.shortCorridorChance = 80;
        GenerationModeSetup.medCorridorChance = 20;
        GenerationModeSetup.longCorridorChance = 0;
        // Corridor room spawning chances
        GenerationModeSetup.normalCorridorRoomchance = 15;
        GenerationModeSetup.snakeCorridorRoomchance = 0;
        GenerationModeSetup.outcoveCorridorRoomchance = 0;
        // Corridor branch chances
        GenerationModeSetup.normalCorridorBranchchance = 50;
        GenerationModeSetup.snakeCorridorBranchchance = 0;
        GenerationModeSetup.outcoveCorridorBranchchance = 0;
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
        switch (roll)
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

    // Given an array of chances for outcomes to happen
    // Returns index of outcome that happened
    // Returns length of array if no outcome came true
    int RandomOutcome(int[] outcomeChances)
    {
        int random = RandInt(1, 100);
        int sum = 0;
        for (int i = 0; i < outcomeChances.Length; i++)
        {
            sum += outcomeChances[i];
            if (random <= sum)
                return i;
        }

        // No outcome came true
        return outcomeChances.Length;
    }

    void ReSeedRNG()
    {
        if (!useSeed)
        {
            System.Random SeedGenerator = new System.Random();
            seed = SeedGenerator.Next(1, 1000000);
        }
        RNG = new System.Random(seed);
        Debug.Log("Seed: " + seed.ToString());
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //// --- OTHER ---
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // Returns a vector with the x and y swapped
    Vector2Int InvertVector(Vector2Int vec)
    {
        return new Vector2Int(vec.y, vec.x);
    }
}
