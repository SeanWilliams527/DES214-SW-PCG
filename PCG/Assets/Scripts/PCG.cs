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
        public RoomType type;
    };
    enum RoomType { small, medium, large };

    // Room Sizing
    int minSmallRoomSize = 2; int maxSmallRoomSize = 4;
    int minMediumRoomSize = 6; int maxMediumRoomSize = 8;
    int minLargeRoomSize = 10; int maxLargeRoomSize = 12;

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

        // The threat level of the each room
        public int minThreatSmallRooms; 
        public int maxThreatSmallRooms;
        public int minThreatMediumRooms;
        public int maxThreatMediumRooms;
        public int minThreatLargeRooms;
        public int maxThreatLargeRooms;
        // Enemy Spawning chances
        public int trivialEnemySpawnChance;
        public int easyEnemySpawnChance;
        public int mediumEnemySpawnChance;
        public int hardEnemySpawnChance;
        // Enemy Spawning in corridor chances
        public int mediumEnemySpawnSnakeCorridorChance;
        public int mediumEnemySpawnOutcoveCorridorChance;
        // Corridor dead end pick up chances
        public int deadEndHeartPickupChance;
        public int deadEndBoostPickupChance;

        // Color for debug mode
        public Color debugColor;
    };

    // The current generation mode
    Mode CurrentGenerationMode;
    Mode GenerationModeSetup;
    Mode GenerationModeDevelopment1;
    Mode GenerationModeDevelopment2;
    Mode GenerationModeDevelopment3;

    // Radius of the circles of generation
    [SerializeField]
    float genSetupRadius;
    [SerializeField]
    float genDev1Radius;
    [SerializeField]
    float genDev2Radius;

    // Enemy Spawning
    int trivialEnemyThreat = 1;
    int easyEnemyThreat = 2;
    int mediumEnemyThreat = 4;
    int hardEnemyThreat = 8;

    // Keep track of where player spawned
    Vector2Int PlayerSpawnCorner;
    Vector3 PlayerSpawnLocation;


    //////////////////////////////////////////////////////////////////////////
    // DESIGNER ADJUSTABLE VALUES
    //////////////////////////////////////////////////////////////////////////

    //Maximum height and width of tile map (must be odd, somewhere between 21 and 101 works well)
    private int MaxMapSize = 101;
    //Size of floor and wall tiles in Unity units (somewhere between 1.0f and 10.0f works well)
    private float GridSize = 5.0f;

    // Debug items
    [SerializeField]
    bool useSeed;
    [SerializeField]
    int seed;
    [SerializeField]
    bool showGenerationDebugColors;

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

        ///////////////////////////////////////////////////////
        //Add PCG logic here...
        //The level will be solid walls until you add code here. 
        //See the GenerateTestRoom() function for an example,
        //(press T to see the test room).
        ///////////////////////////////////////////////////////

        GenerateStartingRooms();
        MakeBossRoom();
        SpawnPlayer();
        // Spawn the camera
        SpawnCamera();

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
                case 1: result = MakeSnakeCorridor(); break;
                case 2: result = MakeOutcoveCorridor(); break;
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
        {
            // Dead end. Roll to spawn pickups
            int roll = RandomOutcome(new int[] { CurrentGenerationMode.deadEndHeartPickupChance, CurrentGenerationMode.deadEndBoostPickupChance });
            if (roll == 0)
                SpawnRandomPickup(cursor);
            else if (roll == 1)
                SpawnRandomPickup(cursor);
            return false;  // No directions to go
        }
        Vector2Int direction = possibleDirections[DieRoll(possibleDirections.Count) - 1];

        // Determine length of corridor
        int length = 1;
        int[] corridorLengths = { CurrentGenerationMode.shortCorridorChance, CurrentGenerationMode.medCorridorChance, CurrentGenerationMode.longCorridorChance };
        switch (RandomOutcome(corridorLengths))
        {
            case 0: length = RandInt(minShortCorridorSize, maxShortCorridorSize); break;
            case 1: length = RandInt(minMediumCorridorSize, maxMediumCorridorSize); break;
            case 2: length = RandInt(minLongCorridorSize, maxLongCorridorSize); break;
        }

        // Make corridor
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
        {
            // Dead end. Roll to spawn pickups
            int roll = RandomOutcome(new int[] { CurrentGenerationMode.deadEndHeartPickupChance, CurrentGenerationMode.deadEndBoostPickupChance });
            if (roll == 0)
                SpawnRandomPickup(cursor);
            else if (roll == 1)
                SpawnRandomPickup(cursor);
            return false;  // No directions to go
        }
        Vector2Int dir = possibleDirections[DieRoll(possibleDirections.Count) - 1];
        // Corridor will go forward and side to side
        Vector2Int side1 = InvertVector(dir);
        Vector2Int side2 = side1 * -1;

        // Determine snake length and stride
        int length = 1;
        int[] corridorLengths = { CurrentGenerationMode.shortCorridorChance, CurrentGenerationMode.medCorridorChance, CurrentGenerationMode.longCorridorChance };
        switch (RandomOutcome(corridorLengths))
        {
            case 0: length = RandInt(minShortCorridorSize, maxShortCorridorSize); break;
            case 1: length = RandInt(minMediumCorridorSize, maxMediumCorridorSize); break;
            case 2: length = RandInt(minLongCorridorSize, maxLongCorridorSize); break;
        }
        int stride = RandInt(snakeMinStride, snakeMaxStride);

        // Construct snake
        Vector2Int currentSide = side1;   // Which side are we currently going forward in
        int strideAmountLeft = stride;    // How much length is left in the current stride
        for (int i = 0; i < length; i++)
        {
            // Snake will trample over anything in its way
            // Only check if going out of bounds
            if (IsOutOfBounds(cursor + dir + currentSide))
            {
                // Dead end. Roll to spawn pickups
                int roll = RandomOutcome(new int[] { CurrentGenerationMode.deadEndHeartPickupChance, CurrentGenerationMode.deadEndBoostPickupChance });
                if (roll == 0)
                    SpawnRandomPickup(cursor);
                else if (roll == 1)
                    SpawnRandomPickup(cursor);
                return false; // Cannot continue
            }

            // Move cursor forward and to side (move diagonally)
            cursor += (dir + currentSide);
            SpawnTile(cursor);
            SpawnTile(cursor + side1);
            SpawnTile(cursor + side2);

            // Roll to add enemies
            if (PercentRoll(CurrentGenerationMode.mediumEnemySpawnSnakeCorridorChance))
                SpawnMediumEnemy(cursor);
            if (PercentRoll(CurrentGenerationMode.mediumEnemySpawnSnakeCorridorChance))
                SpawnMediumEnemy(cursor + side1);
            if (PercentRoll(CurrentGenerationMode.mediumEnemySpawnSnakeCorridorChance))
                SpawnMediumEnemy(cursor + side2);

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
    bool MakeOutcoveCorridor()
    {
        // ---------------- Outcove Variables ---------------------

        int roomChance = CurrentGenerationMode.outcoveCorridorRoomchance;
        int branchChance = CurrentGenerationMode.outcoveCorridorBranchchance;
        // --------------------------------------------------------

        List<Vector2Int> possibleDirections = CorridorGetPossibleDirections();
        if (possibleDirections.Count == 0)
            return false;  // No directions to go
        Vector2Int dir = possibleDirections[DieRoll(possibleDirections.Count) - 1];
        // Corridor will have outcoves that go side to side
        Vector2Int side1 = InvertVector(dir);
        Vector2Int side2 = side1 * -1;
        // Determine length of corridor
        int length = 1;
        int[] corridorLengths = { CurrentGenerationMode.shortCorridorChance, CurrentGenerationMode.medCorridorChance, CurrentGenerationMode.longCorridorChance };
        switch (RandomOutcome(corridorLengths))
        {
            case 0: length = RandInt(minShortCorridorSize, maxShortCorridorSize); break;
            case 1: length = RandInt(minMediumCorridorSize, maxMediumCorridorSize); break;
            case 2: length = RandInt(minLongCorridorSize, maxLongCorridorSize); break;
        }

        bool makeOutcove = false;  // Should we make an outcove on this step?
        // Make a corridor with alternating skinny and outcove sections
        for (int i = 0; i < length; i++)
        {
            // Outcove corridor will trample over anything in its way
            // Only check if going out of bounds
            if (IsOutOfBounds(cursor + dir))
            {
                // Dead end. Roll to spawn pickups
                int roll = RandomOutcome(new int[] { CurrentGenerationMode.deadEndHeartPickupChance, CurrentGenerationMode.deadEndBoostPickupChance });
                if (roll == 0)
                    SpawnRandomPickup(cursor);
                else if (roll == 1)
                    SpawnRandomPickup(cursor);
                return false; // Cannot continue
            }

            // Move cursor forward
            cursor += dir;
            SpawnTile(cursor);
            // Roll to add enemies
            // Roll to add enemies
            if (PercentRoll(CurrentGenerationMode.mediumEnemySpawnOutcoveCorridorChance))
                SpawnMediumEnemy(cursor);
            // Make outcove
            if (makeOutcove)
            {
                SpawnTile(cursor + side1);
                SpawnTile(cursor + side2);

                // Roll to add branches
                if (PercentRoll(branchChance))
                    Branches.Enqueue(cursor + side1);
                if (PercentRoll(branchChance))
                    Branches.Enqueue(cursor + side2);
                // Roll to add enemies
                if (PercentRoll(CurrentGenerationMode.mediumEnemySpawnOutcoveCorridorChance))
                    SpawnMediumEnemy(cursor + side1);
                if (PercentRoll(CurrentGenerationMode.mediumEnemySpawnOutcoveCorridorChance))
                    SpawnMediumEnemy(cursor + side2);
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

        if (PercentRoll(CurrentGenerationMode.roomMiddlePillarChance))
            RoomAddMiddlePillar(room);

        int[] outcomes = { CurrentGenerationMode.roomCrossPillarChance, CurrentGenerationMode.roomCourtyardWallChance };
        switch (RandomOutcome(outcomes))
        {
            case 0: RoomAddCrossPillars(room); break;
            case 1: RoomAddCourtYardWalls(room); break;
            default: break;
        }

        if (PercentRoll(CurrentGenerationMode.roomRoundedChance))
            RoomMakeRound(room);

        // Roll for exit on each wall
        int exitChance = CurrentGenerationMode.roomExitChance;
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

        // Populate the room with enemies
        RoomPopulateEnemies(room);

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
        if (side == W)
            exit = new Vector2Int(room.Left, RandInt(room.Down, room.Up));
        else if (side == E)
            exit = new Vector2Int(room.Right, RandInt(room.Down, room.Up));
        else if (side == N)
            exit = new Vector2Int(RandInt(room.Left, room.Right), room.Up);
        else if (side == S)
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

        // Determine size type of room
        int largestDimension = Math.Max(w, h);
        if (largestDimension <= maxSmallRoomSize)
            room.type = RoomType.small;
        else if (largestDimension <= maxMediumRoomSize)
            room.type = RoomType.medium;
        else
            room.type = RoomType.large;

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
        //SpawnRandomPickup(new Vector2Int(-halfMapSize + 2, halfMapSize - 2));
        // Generate NE room
        cursor.x = halfMapSize - (roomWidth - 1); cursor.y = halfMapSize - (roomHeight - 1);
        FillAreaWithTile(roomWidth, roomHeight);
        Branches.Enqueue(new Vector2Int(halfMapSize - 2, halfMapSize - 1));
        Branches.Enqueue(new Vector2Int(halfMapSize - 1, halfMapSize - 2));
        //SpawnRandomPickup(new Vector2Int(halfMapSize - 2, halfMapSize - 2));
        // Generate SE room
        cursor.x = halfMapSize - (roomWidth - 1); cursor.y = -halfMapSize;
        FillAreaWithTile(roomWidth, roomHeight);
        Branches.Enqueue(new Vector2Int(halfMapSize - 1, -halfMapSize + 2));
        Branches.Enqueue(new Vector2Int(halfMapSize - 2, -halfMapSize + 1));
        //SpawnRandomPickup(new Vector2Int(halfMapSize - 2, -halfMapSize + 2));
        // Generate SW room
        cursor.x = -halfMapSize; cursor.y = -halfMapSize;
        FillAreaWithTile(roomWidth, roomHeight);
        Branches.Enqueue(new Vector2Int(-halfMapSize + 2, -halfMapSize + 1));
        Branches.Enqueue(new Vector2Int(-halfMapSize + 1, -halfMapSize + 2));
        //SpawnRandomPickup(new Vector2Int(-halfMapSize + 2, -halfMapSize + 2));
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

    // Clear the level of any enemies
    public void ClearEnemies()
    {
        // Delete all enemies
        var objsToDelete = GameObject.FindGameObjectsWithTag("Enemy");
        for (int i = 0; i < objsToDelete.Length; i++)
        {
            UnityEngine.Object.Destroy(objsToDelete[i]);
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
    //// --- ROOM ENEMY SPAWNING ---
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    void RoomPopulateEnemies(Room room)
    {
        // Determine the threat level of the room
        int threat;
        if (room.type == RoomType.small)
            threat = RandInt(CurrentGenerationMode.minThreatSmallRooms, CurrentGenerationMode.maxThreatSmallRooms);
        else if (room.type == RoomType.medium)
            threat = RandInt(CurrentGenerationMode.minThreatMediumRooms, CurrentGenerationMode.maxThreatMediumRooms);
        else
            threat = RandInt(CurrentGenerationMode.minThreatLargeRooms, CurrentGenerationMode.maxThreatLargeRooms);

        // Populate the room with enemies
        int tries = 3;  // We have three tries to not overthreat the room
        int threatAmountLeft = threat;
        while (tries > 0)
        {
            // Determine which enemy to spawn
            int[] enemyChances = {CurrentGenerationMode.trivialEnemySpawnChance,
                CurrentGenerationMode.easyEnemySpawnChance,
                CurrentGenerationMode.mediumEnemySpawnChance,
                CurrentGenerationMode.hardEnemySpawnChance };
            bool didSpawnEnemy = false;
            switch (RandomOutcome(enemyChances))
            {
                // Try to spawn the enemy
                case 0: 
                    didSpawnEnemy = SpawnEnemy(room, 0, threatAmountLeft);
                    // Decrement the threat left
                    if (didSpawnEnemy)
                        threatAmountLeft -= trivialEnemyThreat;
                    break;
                case 1: 
                    didSpawnEnemy = SpawnEnemy(room, 1, threatAmountLeft);
                    // Decrement the threat left
                    if (didSpawnEnemy)
                        threatAmountLeft -= easyEnemyThreat;
                    break;
                case 2: 
                    didSpawnEnemy = SpawnEnemy(room, 2, threatAmountLeft);
                    // Decrement the threat left
                    if (didSpawnEnemy)
                        threatAmountLeft -= mediumEnemyThreat;
                    break;
                case 3: 
                    didSpawnEnemy = SpawnEnemy(room, 3, threatAmountLeft);
                    // Decrement the threat left
                    if (didSpawnEnemy)
                        threatAmountLeft -= hardEnemyThreat;
                    break;
            }

            // If we failed to spawn an enemy
            if (!didSpawnEnemy)
                tries--;
        }
    }

    // Spawns a medium enemy on a tile
    void SpawnMediumEnemy(Vector2Int pos)
    {
        // Determine which medium enemy to spawn
        int roll = DieRoll(2);
        if (roll == 1)
            Spawn("tank", pos.x, pos.y);
        else
            Spawn("spread", pos.x, pos.y);
    }

    // Spawns a trivial enemy on a tile
    void SpawnTrivialEnemy(Vector2Int pos)
    {
        Spawn("enemy", pos.x, pos.y);
    }

    // Spawn an enemy of a certain type in a random location of the room
    bool SpawnEnemy(Room room, int enemyType, int threatAmountLeft)
    {
        // Spawn trivial enemy
        if (enemyType == 0)
        {
            // Ensure we dont run out of threat
            if (threatAmountLeft - trivialEnemyThreat < 0)
                return false;
            Vector2Int pos = RoomGetRandomTile(room);
            Spawn("enemy", pos.x, pos.y);
        }

        // Spawn easy enemy
        else if (enemyType == 1)
        {
            // Ensure we dont run out of threat
            if (threatAmountLeft - easyEnemyThreat < 0)
                return false;
            Vector2Int pos = RoomGetRandomTile(room);
            // Determine which easy enemy to spawn
            int roll = DieRoll(2);
            if (roll == 1)
                Spawn("areaEnemy", pos.x, pos.y);
            else
                Spawn("fast", pos.x, pos.y);
        }

        // Spawn medium enemy
        else if (enemyType == 2)
        {
            // Ensure we dont run out of threat
            if (threatAmountLeft - easyEnemyThreat < 0)
                return false;
            Vector2Int pos = RoomGetRandomTile(room);
            // Determine which medium enemy to spawn
            int roll = DieRoll(2);
            if (roll == 1)
                Spawn("tank", pos.x, pos.y);
            else
                Spawn("spread", pos.x, pos.y);
        }

        // Spawn hard enemy
        else if (enemyType == 3)
        {
            // Ensure we dont run out of threat
            if (threatAmountLeft - easyEnemyThreat < 0)
                return false;
            Vector2Int pos = RoomGetRandomTile(room);
            // Determine which hard enemy to spawn
            int roll = DieRoll(2);
            if (roll == 1)
                Spawn("ultra", pos.x, pos.y);
            else
                Spawn("ultraArea", pos.x, pos.y);
        }

        // Successfully spawned an enemy
        return true;
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

    Vector2Int RoomGetRandomTile(Room room)
    {
        // Generate coordinates
        int x = RandInt(room.Left, room.Right);
        int y = RandInt(room.Down, room.Up);
        // Check if a floor tile is there
        GameObject tile = GetTile(x, y);

        while (tile == null)
        {
            // Generate coordinates
            x = RandInt(room.Left, room.Right);
            y = RandInt(room.Down, room.Up);
            // Check if a floor tile is there
            tile = GetTile(x, y);
        }

        return new Vector2Int(x, y);
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
        // Update the generation mode if it needs to be updated
        UpdateGenerationMode(new Vector2Int(x, y));

        if (GetTile(x, y) != null)
            return;
        TileMap[(y * MaxMapSize) + x + TileMapMidPoint] = Spawn("floor", x, y);

        // Change color if using debug colors
        if (showGenerationDebugColors)
            TileMap[(y * MaxMapSize) + x + TileMapMidPoint].GetComponent<SpriteRenderer>().color = CurrentGenerationMode.debugColor;
    }

    //Spawn a tile object if somthing isn't already there
    void SpawnTile(Vector2Int pos)
    {
        // Update the generation mode if it needs to be updated
        UpdateGenerationMode(pos);

        if (GetTile(pos.x, pos.y) != null)
            return;
        TileMap[(pos.y * MaxMapSize) + pos.x + TileMapMidPoint] = Spawn("floor", pos.x, pos.y);

        // Change color if using debug colors
        if (showGenerationDebugColors)
            TileMap[(pos.y * MaxMapSize) + pos.x + TileMapMidPoint].GetComponent<SpriteRenderer>().color = CurrentGenerationMode.debugColor;
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

    // Make the boss room
    void MakeBossRoom()
    {
        // Size of the boss room
        int size = 17;

        // Make room object
        Room room = ConstructRoomObject(new Vector2Int(0, -(size/2 + 1)), N, size, size);
        // Place room
        for (int x = room.Left; x <= room.Right; x++)
        {
            for (int y = room.Down; y <= room.Up; y++)
            {
                SpawnTile(x, y);
            }
        }
        // Add modifications to the room
        RoomMakeRound(room);
        RoomAddCourtYardWalls(room);

        // Spawn the boss
        Spawn("boss", 0.0f, 0.0f);
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
        cam.GetComponent<Transform>().position = new Vector3(PlayerSpawnLocation.x, PlayerSpawnLocation.y, -10.0f);
        var wct = Instantiate(PCGObject.Prefabs["cameratarget"]);
        cam.GetComponent<CameraFollow>().ObjectToFollow = wct.transform;
    }

    // Spawn player in one of the 4 corners of the map
    void SpawnPlayer()
    {
        int roll = DieRoll(4);

        int cornerSpawnDistance = (MaxMapSize / 2) - 1;
        GameObject player;  // Reference to player

        if (roll == 1)  // NW corner
        {
            player = Spawn("player", -cornerSpawnDistance, cornerSpawnDistance);
            PlayerSpawnCorner = N + W;
        }
        else if (roll == 2)  // NE corner
        {
            player = Spawn("player", cornerSpawnDistance, cornerSpawnDistance);
            PlayerSpawnCorner = N + E;
        }
        else if (roll == 3)  // SW corner
        {
            player = Spawn("player", -cornerSpawnDistance, -cornerSpawnDistance);
            PlayerSpawnCorner = S + W;
        }
        else  // SE corner
        {
            player = Spawn("player", cornerSpawnDistance, -cornerSpawnDistance);
            PlayerSpawnCorner = S + E;
        }

        PlayerSpawnLocation = player.transform.position;
    }

    // Perform the final actions once the boss is defeated
    public void OnDefeatFinalBoss()
    {
        float delay = 3.0f;
        Invoke("ClearEnemies", delay);
        Invoke("MakeFinalPath", delay);
        Invoke("FinalCinematicPan", delay);
    }

    // Make the final path to the portal
    void MakeFinalPath()
    {
        int cornerSpawnDistance = (MaxMapSize / 2) - 1;

        // Make the path and spawn the portal in a random corner of the map
        int roll = DieRoll(4);

        if (roll == 1)  // NW corner
        {
            Spawn("portal", -cornerSpawnDistance, cornerSpawnDistance);
            GenerateFinalPath(N + W);
        }
        else if (roll == 2)  // NE corner
        {
            Spawn("portal", cornerSpawnDistance, cornerSpawnDistance);
            GenerateFinalPath(N + E);
        }
        else if (roll == 3)  // SW corner
        {
            Spawn("portal", -cornerSpawnDistance, -cornerSpawnDistance);
            GenerateFinalPath(S + W);
        }
        else  // SE corner
        {
            Spawn("portal", cornerSpawnDistance, -cornerSpawnDistance);
            GenerateFinalPath(S + E);
        }
    }

    void GenerateFinalPath(Vector2Int direction)
    {
        // Start the path at (0,0)
        cursor.x = 0; cursor.y = 0;

        // Calculate the length of the path
        int length = (MaxMapSize / 2) - 1;
        // Ammount of tiles on each side of the path
        int width = 3;
        // Spawn chance for enemies on each tile
        int trivialEnemySpawnChance = 25;
        // Make sure enemies aren't immediately spawned on the player
        int enemySpawnDistance = 10;

        // Construct the path
        for (int i = 0; i < length; i++)
        {
            // Delete all tiles on the path
            DeleteTile(cursor);
            // Expand path based on width
            for (int j = 1; j <= width; j++)
            {
                DeleteTile(cursor + (W * j));
                DeleteTile(cursor + (E * j));
            }

            // Replace them with floor tiles
            SpawnTile(cursor);
            // Expand path based on width
            for (int j = 1; j <= width; j++)
            {
                SpawnTile(cursor + (W * j));
                SpawnTile(cursor + (E * j));

                // Roll to Spawn an enemy
                if (i >= enemySpawnDistance)
                {
                    if (PercentRoll(trivialEnemySpawnChance))
                        SpawnTrivialEnemy(cursor + (W * j));
                    if (PercentRoll(trivialEnemySpawnChance))
                        SpawnTrivialEnemy(cursor + (E * j));
                }
            }

            cursor += direction;
        }
    }

    // Spawn a random upgrade pickup
    void SpawnRandomPickup(Vector2Int tilePos)
    {
        int roll = DieRoll(3);
        if (roll == 1)
            Spawn("healthboost", tilePos.x, tilePos.y);
        else if (roll == 2)
            Spawn("shotboost", tilePos.x, tilePos.y);
        else
            Spawn("speedboost", tilePos.x, tilePos.y);
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

    // Pan to something of interest
    public void FinalCinematicPan()
    {
        var panTo = GameObject.Find("Portal(Clone)");

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
        /////////////////////// SETUP GENERATION MODE ///////////////////////
        // Room size chances
        GenerationModeSetup.smallRoomChance = 100;
        GenerationModeSetup.mediumRoomChance = 0;
        GenerationModeSetup.largeRoomChance = 0;
        // Room variation chances
        GenerationModeSetup.roomMiddlePillarChance = 0;
        GenerationModeSetup.roomCrossPillarChance = 0;
        GenerationModeSetup.roomCourtyardWallChance = 0;
        GenerationModeSetup.roomRoundedChance = 0;
        // Room exit chances
        GenerationModeSetup.roomExitChance = 100;
        // Corridor type chances
        GenerationModeSetup.normalCorridorChance = 100;
        GenerationModeSetup.snakeCorridorChance = 0;
        GenerationModeSetup.outcoveCorridorChance = 0;
        // Corridor length chances
        GenerationModeSetup.shortCorridorChance = 90;
        GenerationModeSetup.medCorridorChance = 10;
        GenerationModeSetup.longCorridorChance = 0;
        // Corridor room spawning chances
        GenerationModeSetup.normalCorridorRoomchance = 25;
        GenerationModeSetup.snakeCorridorRoomchance = 0;
        GenerationModeSetup.outcoveCorridorRoomchance = 0;
        // Corridor branch chances
        GenerationModeSetup.normalCorridorBranchchance = 30;
        GenerationModeSetup.snakeCorridorBranchchance = 15;
        GenerationModeSetup.outcoveCorridorBranchchance = 15;
        // The threat level of the each room
        GenerationModeSetup.minThreatSmallRooms = 1;
        GenerationModeSetup.maxThreatSmallRooms = 3;
        GenerationModeSetup.minThreatMediumRooms = 0;
        GenerationModeSetup.maxThreatMediumRooms = 0;
        GenerationModeSetup.minThreatLargeRooms = 0;
        GenerationModeSetup.maxThreatLargeRooms = 0;
        // Enemy Spawning chances
        GenerationModeSetup.trivialEnemySpawnChance = 100;
        GenerationModeSetup.easyEnemySpawnChance = 0;
        GenerationModeSetup.mediumEnemySpawnChance = 0;
        GenerationModeSetup.hardEnemySpawnChance = 0;
        // Dead end spawn chances
        GenerationModeSetup.deadEndHeartPickupChance = 0;
        GenerationModeSetup.deadEndBoostPickupChance = 0;
        // Debug color
        GenerationModeSetup.debugColor = Color.white;

        /////////////////////// DEVELOPMENT 1 GENERATION MODE ///////////////////////
        GenerationModeDevelopment1.smallRoomChance = 75;
        GenerationModeDevelopment1.mediumRoomChance = 25;
        GenerationModeDevelopment1.largeRoomChance = 0;
        // Room variation chances
        GenerationModeDevelopment1.roomMiddlePillarChance = 50;
        GenerationModeDevelopment1.roomCrossPillarChance = 15;
        GenerationModeDevelopment1.roomCourtyardWallChance = 0;
        GenerationModeDevelopment1.roomRoundedChance = 100;
        // Room exit chances
        GenerationModeDevelopment1.roomExitChance = 100;
        // Corridor type chances
        GenerationModeDevelopment1.normalCorridorChance = 95;
        GenerationModeDevelopment1.snakeCorridorChance = 5;
        GenerationModeDevelopment1.outcoveCorridorChance = 0;
        // Corridor length chances
        GenerationModeDevelopment1.shortCorridorChance = 80;
        GenerationModeDevelopment1.medCorridorChance = 20;
        GenerationModeDevelopment1.longCorridorChance = 0;
        // Corridor room spawning chances
        GenerationModeDevelopment1.normalCorridorRoomchance = 15;
        GenerationModeDevelopment1.snakeCorridorRoomchance = 15;
        GenerationModeDevelopment1.outcoveCorridorRoomchance = 15;
        // Corridor branch chances
        GenerationModeDevelopment1.normalCorridorBranchchance = 15;
        GenerationModeDevelopment1.snakeCorridorBranchchance = 15;
        GenerationModeDevelopment1.outcoveCorridorBranchchance = 15;
        // The threat level of the each room
        GenerationModeDevelopment1.minThreatSmallRooms = 2;
        GenerationModeDevelopment1.maxThreatSmallRooms = 10;
        GenerationModeDevelopment1.minThreatMediumRooms = 5;
        GenerationModeDevelopment1.maxThreatMediumRooms = 14;
        GenerationModeDevelopment1.minThreatLargeRooms = 0;
        GenerationModeDevelopment1.maxThreatLargeRooms = 0;
        // Enemy Spawning chances
        GenerationModeDevelopment1.trivialEnemySpawnChance = 20;
        GenerationModeDevelopment1.easyEnemySpawnChance = 80;
        GenerationModeDevelopment1.mediumEnemySpawnChance = 0;
        GenerationModeDevelopment1.hardEnemySpawnChance = 0;
        // Dead end spawn chances
        GenerationModeDevelopment1.deadEndHeartPickupChance = 0;
        GenerationModeDevelopment1.deadEndBoostPickupChance = 0;
        // Debug color
        GenerationModeDevelopment1.debugColor = new Color(0.8795988f, 1.0f, 0.7987421f);

        /////////////////////// DEVELOPMENT 2 GENERATION MODE ///////////////////////
        GenerationModeDevelopment2.smallRoomChance = 35;
        GenerationModeDevelopment2.mediumRoomChance = 50;
        GenerationModeDevelopment2.largeRoomChance = 15;
        // Room variation chances
        GenerationModeDevelopment2.roomMiddlePillarChance = 35;
        GenerationModeDevelopment2.roomCrossPillarChance = 40;
        GenerationModeDevelopment2.roomCourtyardWallChance = 40;
        GenerationModeDevelopment2.roomRoundedChance = 50;
        // Room exit chances
        GenerationModeDevelopment2.roomExitChance = 100;
        // Corridor type chances
        GenerationModeDevelopment2.normalCorridorChance = 80;
        GenerationModeDevelopment2.snakeCorridorChance = 10;
        GenerationModeDevelopment2.outcoveCorridorChance = 10;
        // Corridor length chances
        GenerationModeDevelopment2.shortCorridorChance = 65;
        GenerationModeDevelopment2.medCorridorChance = 25;
        GenerationModeDevelopment2.longCorridorChance = 10;
        // Corridor room spawning chances
        GenerationModeDevelopment2.normalCorridorRoomchance = 15;
        GenerationModeDevelopment2.snakeCorridorRoomchance = 15;
        GenerationModeDevelopment2.outcoveCorridorRoomchance = 15;
        // Corridor branch chances
        GenerationModeDevelopment2.normalCorridorBranchchance = 50;
        GenerationModeDevelopment2.snakeCorridorBranchchance = 30;
        GenerationModeDevelopment2.outcoveCorridorBranchchance = 30;
        // The threat level of the each room
        GenerationModeDevelopment2.minThreatSmallRooms = 4;
        GenerationModeDevelopment2.maxThreatSmallRooms = 5;
        GenerationModeDevelopment2.minThreatMediumRooms = 8;
        GenerationModeDevelopment2.maxThreatMediumRooms = 18;
        GenerationModeDevelopment2.minThreatLargeRooms = 10;
        GenerationModeDevelopment2.maxThreatLargeRooms = 30;
        // Enemy Spawning chances
        GenerationModeDevelopment2.trivialEnemySpawnChance = 5;
        GenerationModeDevelopment2.easyEnemySpawnChance = 15;
        GenerationModeDevelopment2.mediumEnemySpawnChance = 75;
        GenerationModeDevelopment2.hardEnemySpawnChance = 5;
        // Enemy Spawning in corridor chances
        GenerationModeDevelopment2.mediumEnemySpawnSnakeCorridorChance = 8;
        GenerationModeDevelopment2.mediumEnemySpawnOutcoveCorridorChance = 8;
        // Dead end spawn chances
        GenerationModeDevelopment2.deadEndHeartPickupChance = 0;
        GenerationModeDevelopment2.deadEndBoostPickupChance = 0;
        // Debug color
        GenerationModeDevelopment2.debugColor = new Color(0.8553458f, 1.0f, 0.993212f);

        /////////////////////// DEVELOPMENT 3 GENERATION MODE ///////////////////////
        GenerationModeDevelopment3.smallRoomChance = 35;
        GenerationModeDevelopment3.mediumRoomChance = 35;
        GenerationModeDevelopment3.largeRoomChance = 30;
        // Room variation chances
        GenerationModeDevelopment3.roomMiddlePillarChance = 50;
        GenerationModeDevelopment3.roomCrossPillarChance = 25;
        GenerationModeDevelopment3.roomCourtyardWallChance = 50;
        GenerationModeDevelopment3.roomRoundedChance = 10;
        // Room exit chances
        GenerationModeDevelopment3.roomExitChance = 100;
        // Corridor type chances
        GenerationModeDevelopment3.normalCorridorChance = 85;
        GenerationModeDevelopment3.snakeCorridorChance = 7;
        GenerationModeDevelopment3.outcoveCorridorChance = 8;
        // Corridor length chances
        GenerationModeDevelopment3.shortCorridorChance = 80;
        GenerationModeDevelopment3.medCorridorChance = 10;
        GenerationModeDevelopment3.longCorridorChance = 10;
        // Corridor room spawning chances
        GenerationModeDevelopment3.normalCorridorRoomchance = 15;
        GenerationModeDevelopment3.snakeCorridorRoomchance = 15;
        GenerationModeDevelopment3.outcoveCorridorRoomchance = 15;
        // Corridor branch chances
        GenerationModeDevelopment3.normalCorridorBranchchance = 15;
        GenerationModeDevelopment3.snakeCorridorBranchchance = 15;
        GenerationModeDevelopment3.outcoveCorridorBranchchance = 15;
        // The threat level of the each room
        GenerationModeDevelopment3.minThreatSmallRooms = 14;
        GenerationModeDevelopment3.maxThreatSmallRooms = 20;
        GenerationModeDevelopment3.minThreatMediumRooms = 18;
        GenerationModeDevelopment3.maxThreatMediumRooms = 25;
        GenerationModeDevelopment3.minThreatLargeRooms = 20;
        GenerationModeDevelopment3.maxThreatLargeRooms = 30;
        // Enemy Spawning chances
        GenerationModeDevelopment3.trivialEnemySpawnChance = 5;
        GenerationModeDevelopment3.easyEnemySpawnChance = 5;
        GenerationModeDevelopment3.mediumEnemySpawnChance = 15;
        GenerationModeDevelopment3.hardEnemySpawnChance = 75;
        // Enemy Spawning in corridor chances
        GenerationModeDevelopment3.mediumEnemySpawnSnakeCorridorChance = 2;
        GenerationModeDevelopment3.mediumEnemySpawnOutcoveCorridorChance = 2;
        // Dead end spawn chances
        GenerationModeDevelopment3.deadEndHeartPickupChance = 0;
        GenerationModeDevelopment3.deadEndBoostPickupChance = 0;
    // Debug color
    GenerationModeDevelopment3.debugColor = new Color(0.8244471f, 0.81761f, 1.0f);
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
        // Check if chances add up to more than 100
        int total = 0;
        foreach (int i in outcomeChances)
            total += i;
        if (total > 100)
        {
            Debug.LogError("RandomOutcome detected chances that are greater than 100");
            return 0;
        }

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

    // Check if the generation mode needs to be updated based on the position
    void UpdateGenerationMode(Vector2Int pos)
    {
        // Calculate distance between pos and center
        float distance = pos.magnitude;

        // Update generation mode if needed
        if (distance < genDev2Radius)
            CurrentGenerationMode = GenerationModeDevelopment3;
        else if (distance < genDev1Radius)
            CurrentGenerationMode = GenerationModeDevelopment2;
        else if (distance < genSetupRadius)
            CurrentGenerationMode = GenerationModeDevelopment1;
        else
            CurrentGenerationMode = GenerationModeSetup;
    }
}
