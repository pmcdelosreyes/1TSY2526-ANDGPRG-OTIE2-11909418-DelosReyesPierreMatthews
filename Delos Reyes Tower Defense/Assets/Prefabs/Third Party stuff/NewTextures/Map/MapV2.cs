using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;





public class MapV2 : MonoBehaviour
{
    [SerializeField] bool m_autoStart = false;
    OddsTable m_tileOddsTable;
    OddsTable m_orientationOddsTable;
    OddsTable m_environmentOddsTable;
    OddsTable m_shrubOddsTable;
    OddsTable m_objectOddsTable;
    [SerializeField] public GameObject m_startScene;
    [SerializeField] public GameObject m_finalScene;
    [SerializeField] public Vector2Int m_finalSceneSize = new Vector2Int(5, 6);

    // OutterWall Prefabs
    [SerializeField] public GameObject m_leftEntrancePillar;
    [SerializeField] public GameObject m_rightEntrancePillar;
    [SerializeField] public GameObject m_outterPillar;

    // Path Prefabs 
    [SerializeField] public GameObject m_grassTile;
    [SerializeField] public GameObject m_pathTile;
    [SerializeField] public GameObject m_pathWall;
    [SerializeField] public GameObject m_pathPiller;
    [SerializeField] public GameObject m_pathLamp;


    // Tiles Prefabs
    [SerializeField] public GameObject m_pathTile1;
    [SerializeField] public GameObject m_pathTile2;
    [SerializeField] public GameObject m_pathTile3;
    [SerializeField] public GameObject m_pathTile4;

    // Perlin Noise Generator
    PerlinNoise2D m_envPerlinNoise;

    // Envrioment Prefabs

    [SerializeField] public bool m_sketchCameraBounds = true;

    // Default Map size
    [SerializeField]
    int m_mapSizeX = 16;

    [SerializeField]
    int m_mapSizeY = 16;

    const int m_tileSize = 4;

    // Table containing Nodes
    SortedDictionary<int, SortedDictionary<int, NodeV2>> m_nodeList;

    [SerializeField]
    public int m_mapSeed;

    [SerializeField]
    public bool m_useRandomMap = true;

    // Define odds
    [SerializeField]
    float m_exitDirectionWeight = 0.0f; // will result in more direct paths to exit 
    [SerializeField]
    float m_sameDirectionWeight = 5.0f; // says the likley hood of continuing in the same direction
    float m_windyWeight = 5.0f; // weight to add some windyness to the path
    [SerializeField]
    float m_randomDirectionWeight = 3.0f; // weight to add some random direction changes to the path

    [SerializeField]
    Waypoints m_waypoints;

    List<PathSegment> m_pathSegments;

    MapTemplate m_currentMapTemplate;
    public InputField m_userSeedField;
    public GameObject m_mapObject;
    private List<GameObject> squadlist = new List<GameObject>();

    public NodeV2 GetNode(int row, int col)
    {
        return m_nodeList[row][col];
    }

    public NodeV2 GetNode(Vector2Int coord)
    {
        // Debug.Log("GetNode: " + coord);
        // Debug.Log("GetNode: " + m_nodeList[coord.y][coord.x]);
        return m_nodeList[coord.y][coord.x];
    }

    private void Start()
    {
        // Start the map generation
        if (m_autoStart)
            StartMap();
    }

    void StartMap()
    {
        Debug.Log("START MAP");
        if (m_useRandomMap)
            m_mapSeed = Random.Range(0, System.Int32.MaxValue);

        // Randomize 90 deg increments
        // Define the odds for the orientation of the enviromental objects
        m_orientationOddsTable = new OddsTable();
        m_orientationOddsTable.Add("0", 5, 0);
        m_orientationOddsTable.Add("90", 5, 90);
        m_orientationOddsTable.Add("180", 5, 180);
        m_orientationOddsTable.Add("270", 5, 270);

        // Define the odds of the path tiles
        m_tileOddsTable = new OddsTable();
        m_tileOddsTable.Add("Full", 5, m_pathTile1);
        m_tileOddsTable.Add("Damaged 75%", 5, m_pathTile2);
        m_tileOddsTable.Add("Damaged 50%", 2, m_pathTile3);
        m_tileOddsTable.Add("Damaged 25%", 2, m_pathTile4);

        // Define the odds for trees
        m_objectOddsTable = new OddsTable();
        int n = 0;
        int times = 30;
        float campFireSum = 1.5f;
        float tentSum = 2.0f;
        float nothingSum = 2.0f;
        float campfire = campFireSum / times;
        float tent = tentSum / times;
        float nothing = nothingSum / times;
        m_environmentOddsTable = new OddsTable();

        if (m_useRandomMap)
        {
            // Generate random map template
            m_currentMapTemplate = GenerateMapTemplateFromSeed(m_mapSeed);
            m_mapSeed = m_currentMapTemplate.GetMapSeed();
        }

        else
        {
            // Generate map template from seed
            m_currentMapTemplate = GenerateMapTemplateFromSeed(m_mapSeed);
        }

        ApplyTemplateToMap(m_currentMapTemplate);

        if (m_waypoints != null)
        {
            // Create waypoints for enemies
            m_waypoints.CreateWaypoints();
        }

    }
    
    // These are the getters for map size
    public int GetMapRowCount()
    {
        // Debug.Log("GetMapRowCount: " + m_mapSizeY);
        return m_mapSizeY;
    }

    public int GetMapColCount()
    {
        // Debug.Log("GetMapColCount: " + m_mapSizeX);
        return m_mapSizeX;
    }

    public void ResetMap()
    {
        // Clear existing map
        foreach (Transform t in m_mapObject.transform)
        {
            if (t.tag == "Tile")
                squadlist.Add(t.gameObject);
                //Destroy(t.gameObject);
                // Debug.Log("Destroying Tile");
                // t.parent = null;
        }
        foreach (GameObject t in squadlist)
        {
            Destroy(t);
            // Debug.Log("Destroyed Tile");
            // t.parent = null;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UpdateMapSeedDisplay();
        }
    }

    public List<PathSegment> GetPathSegments()
    {
        return m_pathSegments;
    }

    void SpawnNodeEnv(NodeV2 tile, GameObject envPrefab)
    {
        // Determine position and rotation
        Vector3 envPos = tile.gameObject.transform.position + tile.GetEnvOffset();
        Quaternion envRot = Quaternion.identity;

        // Random 90 degree rotation
        int envAngle = (int)(m_orientationOddsTable.GetPayload(m_orientationOddsTable.Roll()));
        envRot = Quaternion.AngleAxis(envAngle, Vector3.up);

        GameObject spawnedEnv = Instantiate(envPrefab, envPos, envRot, this.gameObject.transform);

        // Set reference so it can be deleted
        tile.SetEnvObject(spawnedEnv);
    }

    public void ApplyTemplateToMap(MapTemplate mapTemplate)
    {
        Debug.Log("ApplyTemplateToMap");

        // Initialize perlin noise generator to make terrain
        m_envPerlinNoise = new PerlinNoise2D(GetMapRowCount(), GetMapColCount());

        // Initialize node table
        m_nodeList = new SortedDictionary<int, SortedDictionary<int, NodeV2>>();

        Vector3 envPos;
        int col = 0;
        int row = 0;

        int N = -m_tileSize / 2;
        int S = m_tileSize / 2;
        int E = m_tileSize / 2;
        int W = -m_tileSize / 2;
        int westWallCol = GetMapColCount() - 1;

        int northWallRow = GetMapRowCount() - 1;
        //isPlayablePerimeter

        Vector3 westWallOffset = new Vector3(0, 0, -m_tileSize / 2);
        Vector3 eastWallOffset = new Vector3(0, 0, m_tileSize / 2);
        Vector3 southWallOffset = new Vector3(m_tileSize / 2, 0, 0);
        Vector3 northWallOffset = new Vector3(-m_tileSize / 2, 0, 0);

        int northRow = 0;
        int eastCol = GetMapRowCount() - 1;
        int westCol = 0;
        int southRow = GetMapColCount() - 1;

        Vector2Int cornerNE = new Vector2Int(northRow, eastCol);
        Vector2Int cornerSE = new Vector2Int(southRow, eastCol);
        Vector2Int cornerSW = new Vector2Int(southRow, westCol);
        Vector2Int cornerNW = new Vector2Int(northRow, westCol);

        if (m_sketchCameraBounds)
        {
            // Set camera limitations
            float axisVal;
            float posOffsetVal;

            // X AXIS
            axisVal = (m_tileSize * GetMapColCount() - 1) / 2F;
            posOffsetVal = transform.position.x;
            Vector2 rangeX = new Vector2(posOffsetVal, posOffsetVal) + new Vector2(-axisVal, axisVal - m_tileSize);

            // Z AXIS
            axisVal = (m_tileSize * GetMapRowCount() - 1) / 2F;
            posOffsetVal = transform.position.z;
            Vector2 rangeZ = new Vector2(posOffsetVal, posOffsetVal) + new Vector2(-axisVal, axisVal - m_tileSize);

            // Y AXIS
            axisVal = 100;
            posOffsetVal = transform.position.y + 5;
            Vector2 rangeY = new Vector2(posOffsetVal, posOffsetVal + axisVal);


            UpdateCameraLimitations(rangeX, rangeY, rangeZ);
        }

        // Generate Tiles
        float mapWidth = m_tileSize * GetMapColCount();
        float mapHeight = m_tileSize * GetMapRowCount();

        NodeTemplate nodeTemplate;
        for (row = 0; row < GetMapRowCount(); ++row)
        {
            m_nodeList.Add(row, new SortedDictionary<int, NodeV2>());

            for (col = 0; col < GetMapColCount(); ++col)
            {
                // Create Tile at position
                float tileRowPosX = m_tileSize * col - mapWidth / 2;
                float tileRowPosZ = m_tileSize * row - mapHeight / 2;

                Vector3 tilePos = transform.position + new Vector3(tileRowPosX, 0, tileRowPosZ);

                // Decide which prefab
                nodeTemplate = mapTemplate.GetNode(row, col);
                GameObject prefab = nodeTemplate.type == "path"
                                    || nodeTemplate.type == "start"
                                    || nodeTemplate.type == "end"
                                    ? m_pathTile : m_grassTile;


                GameObject spawnNode = Instantiate(prefab, tilePos, Quaternion.identity, this.gameObject.transform);
                NodeV2 spawnInstance = spawnNode.GetComponent<NodeV2>();
                m_nodeList[row].Add(col, spawnInstance);

                // Add vegitation to terrain

                if (nodeTemplate.type == "terrain" && nodeTemplate.isEnvPlacable)
                {
                    OddsTable subCategoryTable;

                    // Choose which table to role on
                    if (nodeTemplate.isPlayablePerimeter)
                    {
                        // More likely to spawn shrubs on playable perimeter
                        subCategoryTable = m_shrubOddsTable;
                    }
                    else
                    {
                        // Roll on environment table
                        string rolledKey;
                        if (Random.Range(0F, 10f) / 10F > 0.2F)
                            rolledKey = m_environmentOddsTable.GetForNormalizedRoll(m_envPerlinNoise.GetValue(row, col));
                        // 20% chance to roll randomly
                        else
                            rolledKey = m_environmentOddsTable.Roll();

                        subCategoryTable = (OddsTable)m_environmentOddsTable.GetPayload(rolledKey);
                    }


                    // Table exists
                    if (subCategoryTable != null)
                    {
                        // Decide prefab at Random
                        string rolledKey;

                        if (Random.Range(0, 1) > 0.5)
                            rolledKey = subCategoryTable.GetForNormalizedRoll(Mathf.Clamp(Random.Range(-0.2F, 0.2F) + m_envPerlinNoise.GetValue(row, col), 0, 1));
                        else
                            rolledKey = subCategoryTable.Roll();
                        GameObject envPrefab = (GameObject)subCategoryTable.GetPayload(rolledKey);
                        if (envPrefab != null)
                            SpawnNodeEnv(spawnInstance, envPrefab);
                    }
                }
            }
        }

        // Read directly from the map
        NodeTemplate template;
        for (row = 0; row < GetMapRowCount(); ++row)
            for (col = 0; col < GetMapColCount(); ++col)
            {
                if (GetNode(row, col))
                {
                    NodeV2 node = GetNode(row, col);
                    template = mapTemplate.GetNode(row, col);
                    node.SetNodeType(template.type);
                    node.SetIsPlayablePerimeter(template.isPlayablePerimeter);
                    node.SetIsTowerPlacable(template.isTowerPlacable);
                    node.SetIsEnvPlacable(template.isEnvPlacable);
                    node.SetCoordinate(new Vector2Int(col, row));
                }
            }

        // Decorate Path
        m_pathSegments = mapTemplate.GetPathSegments();
        int globalPathTileCount = 0;
        int segmentLargestIndex = m_pathSegments.Count - 1;
        int previousSegmentIndex = -1;
        Vector2Int currentPos = new Vector2Int(0, 0);
        Vector2Int previousPos = new Vector2Int(0, 0);
        for (int s = 0; s <= segmentLargestIndex; ++s)
        {
            // Determine if last segment
            // Used to decide if last tile in path
            bool isLastSegment = s == segmentLargestIndex;

            // Get segment
            PathSegment segment = m_pathSegments[s];

            int deltaX = segment.end.x - segment.start.x;
            int deltaY = segment.end.y - segment.start.y;

            int directionX = deltaX == 0 ? 0 : deltaX / Mathf.Abs(deltaX);
            int directionY = deltaY == 0 ? 0 : deltaY / Mathf.Abs(deltaY);

            //isLastTile, isFirstTile

            // Set up 2D iteration 
            int dy = 0;
            int dx = 0;
            bool isLastIteration = false;
            bool isDone = false;
            while (!isDone)
            {
                // Set loop ending flag
                if (isLastIteration)
                    isDone = true;
                //----------------------
                // Execute for-loop contents with dx and dy

                // Get tile location
                row = segment.start.y + dy;
                col = segment.start.x + dx;

                // Select colour
                bool isFirstTile = globalPathTileCount == 0;
                bool isLastTile = isLastSegment && isLastIteration;
                bool isDirectionChange = s != previousSegmentIndex;


                currentPos = new Vector2Int(col, row);
                if (isFirstTile)
                    previousPos = currentPos;
                    
                if (previousPos != currentPos || isFirstTile)
                {
                    NodeV2 currentTile = GetNode(row, col);

                    Transform parentTransform = currentTile.gameObject.transform;
                    envPos = currentTile.gameObject.transform.position + currentTile.GetEnvOffset();


                    Vector3 wallPos;
                    Quaternion wallRotation;
                    nodeTemplate = mapTemplate.GetNode(row, col);

                    if (currentTile.GetNodeType() != "end" && m_pathWall != null)
                    {
                        if (nodeTemplate.hasWall[(int)NodeTemplate.Wall.N])
                        {
                            wallPos = envPos + new Vector3(N, 0, 0);
                            wallRotation = Quaternion.AngleAxis(90, Vector3.up);
                            Instantiate(m_pathWall, wallPos, wallRotation, parentTransform);
                        }
                        if (nodeTemplate.hasWall[(int)NodeTemplate.Wall.S])
                        {
                            wallPos = envPos + new Vector3(S, 0, 0);
                            wallRotation = Quaternion.AngleAxis(-90, Vector3.up);
                            Instantiate(m_pathWall, wallPos, wallRotation, parentTransform);
                        }
                        if (nodeTemplate.hasWall[(int)NodeTemplate.Wall.E])
                        {
                            wallPos = envPos + new Vector3(0, 0, E);
                            wallRotation = Quaternion.AngleAxis(180, Vector3.up);
                            Instantiate(m_pathWall, wallPos, wallRotation, parentTransform);
                        }
                        if (nodeTemplate.hasWall[(int)NodeTemplate.Wall.W])
                        {
                            wallPos = envPos + new Vector3(0, 0, W);
                            wallRotation = Quaternion.AngleAxis(0, Vector3.up);
                            Instantiate(m_pathWall, wallPos, wallRotation, parentTransform);
                        }
                    }

                    Quaternion pillarRotation = Quaternion.identity;
                    if (m_pathPiller != null)
                    {
                        if (nodeTemplate.hasPillar[(int)NodeTemplate.Pillar.NE])
                        {
                            Vector3 pillarPos = envPos + new Vector3(N, 0, E);
                            Instantiate(m_pathPiller, pillarPos, pillarRotation, parentTransform);
                        }
                        if (nodeTemplate.hasPillar[(int)NodeTemplate.Pillar.NW])
                        {
                            Vector3 pillarPos = envPos + new Vector3(N, 0, W);
                            Instantiate(m_pathPiller, pillarPos, pillarRotation, parentTransform);
                        }
                        if (nodeTemplate.hasPillar[(int)NodeTemplate.Pillar.SE])
                        {
                            Vector3 pillarPos = envPos + new Vector3(S, 0, E);
                            Instantiate(m_pathPiller, pillarPos, pillarRotation, parentTransform);
                        }
                        if (nodeTemplate.hasPillar[(int)NodeTemplate.Pillar.SW])
                        {
                            Vector3 pillarPos = envPos + new Vector3(S, 0, W);
                            Instantiate(m_pathPiller, pillarPos, pillarRotation, parentTransform);
                        }
                    }

                    if (currentTile.GetIsPath() && currentTile.GetPlayablePerimeter() && currentTile.GetNodeType() != "end")
                    {
                        Quaternion pillarRot = Quaternion.identity;
                        if (m_leftEntrancePillar != null)
                            Instantiate(m_leftEntrancePillar, envPos + westWallOffset + northWallOffset, pillarRot, this.gameObject.transform);
                        if (m_rightEntrancePillar != null)
                            Instantiate(m_rightEntrancePillar, envPos + westWallOffset + southWallOffset, pillarRot, this.gameObject.transform);
                    }

                    if (currentTile.GetNodeType() == "end" && m_finalScene != null)
                    {
                        currentTile.m_height = 11F;
                        Vector3 endPos = currentTile.gameObject.transform.position;
                        Quaternion endRot = Quaternion.AngleAxis(0, Vector3.up);
                        Instantiate(m_finalScene, endPos, endRot, this.gameObject.transform);
                    }

                    if (currentTile.GetNodeType() == "start" && m_startScene != null)
                    {
                        Vector3 startPos = currentTile.gameObject.transform.position + new Vector3(0, 0, -m_tileSize);//HERE
                        Quaternion startRot = Quaternion.AngleAxis(0, Vector3.up);
                        Instantiate(m_startScene, startPos, startRot, this.gameObject.transform);
                    }

                    Quaternion tileRotation = Quaternion.identity;
                    Vector3 tilePos;
                    GameObject tilePrefrab = null;
                    float tileOffset = 0;

                    tilePos = envPos + new Vector3(N / 2, tileOffset, E / 2);
                    tilePrefrab = (GameObject)m_tileOddsTable.GetPayload(m_tileOddsTable.Roll());
                    if (tilePrefrab != null)
                        Instantiate(tilePrefrab, tilePos, tileRotation, parentTransform);

                    tilePos = envPos + new Vector3(S / 2, tileOffset, E / 2);
                    tilePrefrab = (GameObject)m_tileOddsTable.GetPayload(m_tileOddsTable.Roll());
                    if (tilePrefrab != null)
                        Instantiate(tilePrefrab, tilePos, tileRotation, parentTransform);

                    tilePos = envPos + new Vector3(S / 2, tileOffset, W / 2);
                    tilePrefrab = (GameObject)m_tileOddsTable.GetPayload(m_tileOddsTable.Roll());
                    if (tilePrefrab != null)
                        Instantiate(tilePrefrab, tilePos, tileRotation, parentTransform);

                    tilePos = envPos + new Vector3(N / 2, tileOffset, W / 2);
                    tilePrefrab = (GameObject)m_tileOddsTable.GetPayload(m_tileOddsTable.Roll());
                    if (tilePrefrab != null)
                        Instantiate(tilePrefrab, tilePos, tileRotation, parentTransform);

                    if (currentTile.GetNodeType() != "end" && m_pathLamp != null)
                    {
                        float lampOffset = 3;
                        Quaternion lampRotation = Quaternion.identity;
                        if (nodeTemplate.hasLamp[(int)NodeTemplate.Pillar.NE])
                        {
                            Vector3 pillarPos = envPos + new Vector3(N, lampOffset, E);
                            lampRotation = Quaternion.AngleAxis(-90, Vector3.up);
                           // Instantiate(m_pathLamp, pillarPos, lampRotation, parentTransform);
                        }
                        if (nodeTemplate.hasLamp[(int)NodeTemplate.Pillar.SE])
                        {
                            Vector3 pillarPos = envPos + new Vector3(S, lampOffset, E);
                            lampRotation = Quaternion.AngleAxis(0, Vector3.up);
                          //  Instantiate(m_pathLamp, pillarPos, lampRotation, parentTransform);
                        }
                        if (nodeTemplate.hasLamp[(int)NodeTemplate.Pillar.SW])
                        {
                            Vector3 pillarPos = envPos + new Vector3(S, lampOffset, W);
                            lampRotation = Quaternion.AngleAxis(90, Vector3.up);
                          //  Instantiate(m_pathLamp, pillarPos, lampRotation, parentTransform);
                        }
                        if (nodeTemplate.hasLamp[(int)NodeTemplate.Pillar.NW])
                        {
                            Vector3 pillarPos = envPos + new Vector3(N, lampOffset, W);
                            lampRotation = Quaternion.AngleAxis(180, Vector3.up);
                           // Instantiate(m_pathLamp, pillarPos, lampRotation, parentTransform);
                        }
                    }

                } // End segment does not overlatp

                ++globalPathTileCount;
                //----------------------
                // Increment loop
                if (dx != deltaX)
                    dx += directionX;
                else if (dy != deltaY)
                    dy += directionY;
                // Detect going on last iteration
                isLastIteration = (dx == deltaX && dy == deltaY);

                previousPos = currentPos;
                previousSegmentIndex = s;
            }
        }
    }

    MapTemplate GenerateRandomMapTemplate()
    {
        MapGenerator mapGenerator = new MapGenerator(GetMapColCount(), GetMapRowCount());

        // Set map Seed
        mapGenerator.SetSeed(Random.Range(0, System.Int32.MaxValue));

        // Set map generation values
        mapGenerator.SetExitDirectionWeight(m_exitDirectionWeight);
        mapGenerator.SetSameDirectionWeight(m_sameDirectionWeight);
        mapGenerator.SetWindyWeight(m_windyWeight);
        mapGenerator.SetRandomDirectionWeight(m_randomDirectionWeight);

        MapTemplate randomMap = mapGenerator.GenerateRandomMap();

        return randomMap;
    }

    MapTemplate GenerateMapTemplateFromSeed(int seed)
    {
        MapGenerator mapGenerator = new MapGenerator(GetMapColCount(), GetMapRowCount());
        mapGenerator.SetSeed(seed);
        return mapGenerator.GenerateRandomMap();
    }

    public void GenerateMapFromSeed()
    {
        m_useRandomMap = false;
        int result;
        string userInput = m_userSeedField.text;
        if (userInput != "")
        {
            try
            {
                result = System.Int32.Parse(userInput);
                m_mapSeed = result;
                ResetMap();
                Debug.Log("ResetMap");
                m_waypoints.ResetWaypoints();
                StartMap();
            }
            catch (System.FormatException)
            {
                System.Console.WriteLine($"Unable to parse '{userInput}'");
            }
        }
    }

    public void GenerateMapFromSeed(int seed)
    {
        Debug.Log("GenerateMapFromSeed");
        m_useRandomMap = false;
        m_mapSeed = seed;
        StartMap();
    }

    public void GenerateMap()
    {
        m_useRandomMap = true;
        ResetMap();
        m_waypoints.ResetWaypoints();
        StartMap();

    }
    protected void UpdateCameraLimitations(Vector2 rangeX, Vector2 rangeY, Vector2 rangeZ)
    {
        GameObject mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        if (mainCamera != null)
        {
            CameraController cameraController = mainCamera.GetComponent<CameraController>();
            cameraController.rangePosX = rangeX;
            cameraController.rangePosY = rangeY;
            cameraController.posRangeZ = rangeZ;
        }
    }

    protected void UpdateMapSeedDisplay()
    {
        GameObject textObject = GameObject.FindGameObjectWithTag("MapSeed");
        if (textObject != null)
        {
            Text text = textObject.GetComponent<Text>();
            if (text != null)
            {
                text.text = "Map Seed: " + m_mapSeed;
            }
        }
    }
}