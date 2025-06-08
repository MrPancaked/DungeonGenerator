using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Linq;
using System.Diagnostics;
using Unity.AI.Navigation;
using Random = UnityEngine.Random;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Dungeon Generator")]
    [SerializeField] private int seed;
    [SerializeField] private bool skipRoomCoroutine;
    [SerializeField] private bool skipDoorCoroutine;
    [SerializeField] private bool skipGraphCoroutine;
    [SerializeField] private bool skipRoomRemoveCoroutine;
    [SerializeField] private bool skipRemoveCycleCoroutine;
    [SerializeField] private bool skipStructureCoroutine;
    [SerializeField] private float waitTime;
    [SerializeField] private RectInt dungeon;
    [SerializeField] private int minRoomSize;
    [SerializeField] private int wallThickness;
    [SerializeField] private int doorWidth;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private NavMeshSurface navMeshSurface;
    [SerializeField] private GameObject player; 
    [SerializeField] private List<RectInt> rooms = new List<RectInt>();
    [SerializeField] private List<RectInt> doors = new List<RectInt>();
    private Graph<Vector3> graph = new Graph<Vector3>();
    private bool canSplitH = true;
    private bool canSplitV = true;
    private bool isGraphConnected;
    

    private void Start()
    {
        wallThickness = Math.Abs(wallThickness);
        if (seed == 0)
        {
            seed = Random.Range(0, int.MaxValue);
        }
        Random.InitState(seed);
        rooms.Add(new RectInt(dungeon.xMin - wallThickness, dungeon.yMin - wallThickness, dungeon.width + wallThickness * 2, dungeon.height + wallThickness * 2));
        StartCoroutine(nameof(GenerateRooms));
    }

    private IEnumerator GenerateRooms()
    {
        while (canSplitH || canSplitV)
        {
            canSplitV = false;
            canSplitH =  false;
            List<RectInt> newRooms = new List<RectInt>();
            foreach (RectInt room in rooms)
            {
                newRooms.Add(room);
            }

            for (int i = 0; i < newRooms.Count; i++)
            {
                bool coinFlip = Random.value > 0.5f;
                RectInt room = newRooms[i];

                if (coinFlip)
                {
                    SplitVertical(room, i);
                    if (!canSplitV)
                    {
                        SplitHorizontal(room, i);
                    }
                }
                else
                {
                    SplitHorizontal(room, i);
                    if (!canSplitH)
                    {
                        SplitVertical(room, i);
                    }
                }

                if (!skipRoomCoroutine)
                {
                    yield return new WaitForSeconds(waitTime);
                }
            }
        }
        Stopwatch stopwatch = Stopwatch.StartNew();
        yield return StartCoroutine(nameof(GenerateRoomNodes));
        yield return StartCoroutine(nameof(GenerateDoors));
        isGraphConnected = IsGraphConnected(graph);
        stopwatch.Stop();
        print("Dungeon Generated and The graph is connected: " + isGraphConnected +  " time:" + stopwatch.Elapsed.TotalSeconds + "seconds");
        Stopwatch stopwatch2 = Stopwatch.StartNew();
        yield return StartCoroutine(nameof(RemoveRooms));
        stopwatch.Stop();
        isGraphConnected = IsGraphConnected(graph);
        print("Rooms removed and the graph is connected: " + isGraphConnected +  ", time: " + stopwatch2.Elapsed.TotalSeconds + "seconds");
        Stopwatch stopwatch3 = Stopwatch.StartNew();
        yield return StartCoroutine(nameof(SpanningTree));
        isGraphConnected = IsGraphConnected(graph);
        stopwatch.Stop();
        print("Cycles removed and the graph is connected: " + isGraphConnected +  ", time: " + stopwatch3.Elapsed.TotalSeconds + "seconds");
        print("total time: " + (stopwatch3.Elapsed.TotalSeconds + stopwatch2.Elapsed.TotalSeconds + stopwatch.Elapsed.TotalSeconds) + "seconds");
        yield return StartCoroutine(nameof(GenerateStructure));
        BakeNavMesh();
        SpawnPlayer();
        print("Generation finished");
    }
    private void SplitVertical(RectInt room, int roomnumber)
    {
        if ((room.height - 4f * wallThickness) / 2f >= minRoomSize) //maybe a devision imprecision
        {
            int newRoomHeight = Random.Range(2 * wallThickness + minRoomSize, room.height - (2 * wallThickness + minRoomSize));
            rooms[roomnumber] = new RectInt(room.xMin, room.yMin, room.width, room.height - newRoomHeight + wallThickness);
            rooms.Add(new RectInt(room.xMin, room.yMax - newRoomHeight - wallThickness, room.width, newRoomHeight + wallThickness));
            canSplitV = true;
        }
    }
    private void SplitHorizontal(RectInt room, int roomnumber)
    {
        if ((room.width - 4f * wallThickness) / 2f >= minRoomSize) //maybe a devision imprecision
        {
            int newRoomWidth = Random.Range(2 * wallThickness + minRoomSize, room.width - (2 * wallThickness + minRoomSize));
            rooms[roomnumber] = new RectInt(room.xMin, room.yMin, room.width - newRoomWidth + wallThickness, room.height);
            rooms.Add(new RectInt(room.xMax - newRoomWidth - wallThickness, room.yMin, newRoomWidth + wallThickness, room.height));
            canSplitH = true;
        }
    }
    private IEnumerator GenerateDoors()
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            //starting the second for loop after the index of the room in the rooms list
            //which makes sure the room doesn't check with itself and already checked rooms don't get checked twice
            //an alternative would be deleting the rooms from a copied list and then iterating over that list with a foreach loop
            for (int j = i + 1; j < rooms.Count; j++)
            {
                if (AlgorithmsUtils.Intersects(rooms[j], rooms[i]))
                {
                    RectInt intersection = AlgorithmsUtils.Intersect(rooms[j], rooms[i]);
                    //a check is needed to check in which orientation the wall between adjacent rooms are
                    //this way an accurate calculation of the area of the overlapping part can be made
                    //maybe do this whole section in a method
                    if (intersection.width > intersection.height)
                    {
                        int area = (intersection.width - 4 * wallThickness) * intersection.height;
                        if (area > doorWidth * wallThickness * 2)
                        {
                            int randomDoorPosition = Random.Range(intersection.xMin + (wallThickness * 2), intersection.xMax - doorWidth - (wallThickness * 2) + 1);
                            RectInt door = new RectInt(randomDoorPosition, intersection.y, doorWidth, intersection.height);
                            doors.Add(door);
                            Vector3 node = new Vector3(door.center.x, 0, door.center.y);
                            graph.AddNode(node);
                            graph.AddEdge(node, new Vector3(rooms[i].center.x, 0, rooms[i].center.y));
                            graph.AddEdge(node, new Vector3(rooms[j].center.x, 0, rooms[j].center.y));
                            if (!skipDoorCoroutine)
                            {
                                yield return new WaitForSeconds(waitTime);
                            }
                        }
                    }
                    else if (intersection.width < intersection.height)
                    {
                        int area = (intersection.height - 4 * wallThickness) * intersection.width;
                        if (area > doorWidth * wallThickness * 2)
                        {
                            int randomDoorPosition = Random.Range(intersection.yMin + (wallThickness * 2), intersection.yMax - doorWidth - (wallThickness * 2) + 1);
                            RectInt door = new RectInt(intersection.x, randomDoorPosition, intersection.width, doorWidth);
                            doors.Add(door);
                            Vector3 node = new Vector3(door.center.x, 0, door.center.y);
                            graph.AddNode(node);
                            graph.AddEdge(node, new Vector3(rooms[i].center.x, 0, rooms[i].center.y));
                            graph.AddEdge(node, new Vector3(rooms[j].center.x, 0, rooms[j].center.y));
                            if (!skipDoorCoroutine)
                            {
                                yield return new WaitForSeconds(waitTime);
                            }
                        }
                    }
                }
            }
        }
    }
    private IEnumerator GenerateRoomNodes()
    {
        foreach (RectInt room in rooms)
        {
            graph.AddNode(new Vector3(room.center.x, 0, room.center.y));
            if (!skipGraphCoroutine)
            {
                yield return new WaitForSeconds(waitTime);
            }
        }
    }
    private IEnumerator RemoveRooms()
    {
        // Rooms for deletion (smallest 10%)
        int roomsCount = rooms.Count;
        if (rooms.Count == 0) yield break; 
        int removeTargetCount = Mathf.FloorToInt(roomsCount * 0.1f);
        if (removeTargetCount == 0) yield break;
        rooms = rooms.OrderBy(r => r.width * r.height).ToList(); // Sort
        for (int i = 0; i < roomsCount / 4; i++)
        {
            Graph<Vector3> copyGraph = CopyGraph(graph);  
            copyGraph.DeleteNode(new Vector3(rooms[i].center.x, 0f , rooms[i].center.y));
            if (IsGraphConnected(copyGraph))
            {
                //deleting nodes
                Vector3 nodeToDelete = new Vector3(rooms[i].center.x, 0f, rooms[i].center.y);
                foreach (Vector3 node in graph.GetNeighbors(nodeToDelete).ToArray())
                {
                    //removing all neighbor (doors) nodes of the room that is being removed
                    graph.DeleteNode(node);
                    if (!skipRoomRemoveCoroutine)
                    {
                        yield return new WaitForSeconds(waitTime);
                    }
                }
                graph.DeleteNode(nodeToDelete);
                if (!skipRoomRemoveCoroutine)
                {
                    yield return new WaitForSeconds(waitTime);
                }
                RectInt[] doorsCopy = new RectInt[doors.Count];
                for (int j  = 0; j < doors.Count; j++)
                {
                    doorsCopy[j] = doors[j];
                }
                for (int j = 0; j < doorsCopy.Length; j++)
                {
                    if (AlgorithmsUtils.Intersects(doorsCopy[j], rooms[i]))
                    {
                        doors.Remove(doorsCopy[j]);
                        if (!skipRoomRemoveCoroutine)
                        {
                            yield return new WaitForSeconds(waitTime);
                        }
                    }
                }
                rooms.Remove(rooms[i]);
                if (!skipRoomRemoveCoroutine)
                {
                    yield return new WaitForSeconds(waitTime);
                }
            }
        }
    }
    private bool IsGraphConnected(Graph<Vector3> checkGraph)
    {
        Vector3[] nodes = checkGraph.GetNodes().ToArray();
        HashSet<Vector3> discoveredNodes = new HashSet<Vector3>();
        Queue<Vector3> queue = new Queue<Vector3>();
        Vector3 startNode = checkGraph.GetNodes().ToArray()[0];
        queue.Enqueue(startNode);
        discoveredNodes.Add(startNode);
        while (queue.Count > 0)
        {
            Vector3 node = queue.Dequeue();
            foreach (Vector3 neighbor in checkGraph.GetNeighbors(node))
            {
                if (!discoveredNodes.Contains(neighbor))
                {
                    queue.Enqueue(neighbor);
                    discoveredNodes.Add(neighbor);
                }
            }
        }
        return discoveredNodes.Count == nodes.Length;
    }
    
    private bool IsGraphConnectedOld(Graph<Vector3> checkGraph)
    {
        Graph<Vector3> newGraph = CopyGraph(checkGraph);
        foreach (Vector3 node1 in checkGraph.GetNodes())
        {
            newGraph.DeleteNode(node1);
            foreach (Vector3 node2 in newGraph.GetNodes())
            {
                //checking if every node is connected to every other node takes VERY long, instead just check if all the nodes can be discovered from a single node
                if (!AreNodesConnected(checkGraph, node1, node2))
                {
                    return false;
                }
            }
        }
        return true;
    }

    private bool AreNodesConnected(Graph<Vector3> checkGraph, Vector3 fromNode, Vector3 toNode)
    {
        List<Vector3> discoveredNodes = new List<Vector3>();
        Queue<Vector3> queue = new Queue<Vector3>();
        queue.Enqueue(fromNode);
        discoveredNodes.Add(fromNode);
        while (queue.Count > 0)
        {
            Vector3 node = queue.Dequeue();
            foreach (Vector3 neighbor in checkGraph.GetNeighbors(node))
            {
                if (!discoveredNodes.Contains(neighbor))
                {
                    queue.Enqueue(neighbor);
                    discoveredNodes.Add(neighbor);
                    if (neighbor == toNode)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    } //only used in IsGraphConnectedOld() which is not being used

    private IEnumerator SpanningTree()
    {
        Vector2[] doorCenters = new Vector2[doors.Count];
        for (int i = 0; i < doors.Count; i++)
        {
            doorCenters[i] = doors[i].center;
        }
        HashSet<Vector3> discoveredNodes = new HashSet<Vector3>();
        HashSet<Vector3> deletedNodes = new HashSet<Vector3>();
        Dictionary<Vector3, Vector3> parentMap = new Dictionary<Vector3, Vector3>();
        Queue<Vector3> queue = new Queue<Vector3>();

        Vector3 startNode = graph.GetNodes().First();
        queue.Enqueue(startNode);
        discoveredNodes.Add(startNode);
        parentMap[startNode] = startNode;

        while (queue.Count > 0)
        {
            Vector3 node = queue.Dequeue();
            if (deletedNodes.Contains(node)) continue;
            Vector3[] neighbors = graph.GetNeighbors(node).ToArray();

            foreach (Vector3 neighbor in neighbors)
            {
                if (!discoveredNodes.Contains(neighbor))
                {
                    queue.Enqueue(neighbor);
                    discoveredNodes.Add(neighbor);
                    parentMap[neighbor] = node;
                }
                else if (parentMap[node] != neighbor)
                {
                    graph.RemoveEdge(node, neighbor);
                    for (int i = 0; i < doors.Count; i++)
                    {
                        if (doors[i].center == new Vector2(neighbor.x, neighbor.z))
                        {
                            graph.DeleteNode(neighbor);
                            deletedNodes.Add(neighbor);
                            doors.Remove(doors[i]);
                            
                        }
                        else if (doors[i].center == new Vector2(node.x, node.z))
                        {
                            graph.DeleteNode(node);
                            deletedNodes.Add(node);
                            doors.Remove(doors[i]);
                        }
                    }
                    if (!skipRemoveCycleCoroutine)
                    {
                        yield return new WaitForSeconds(waitTime);
                    }
                }
            }
        }
    }

    private IEnumerator GenerateStructure()
    {
        HashSet<Vector3> walls = new HashSet<Vector3>();
        HashSet<Vector3> floors = new HashSet<Vector3>();
        HashSet<Vector3> doorSpace = new HashSet<Vector3>();
        Transform wallParent = GameObject.FindGameObjectWithTag("wallParent").transform;
        Transform floorParent = GameObject.FindGameObjectWithTag("floorParent").transform;
        foreach (RectInt door in doors) {
            for (int i = 0; i < door.width; i++) {
                for (int j = 0; j < door.height; j++) {
                    doorSpace.Add(new Vector3(door.x + i, 0, door.y + j));
                }
            }
        }
        foreach (RectInt room in rooms)
        {
            for (int i = 0; i < room.width; i++) {
                for (int j = 0; j < room.height; j++) {
                    Vector3 position = new Vector3(room.x + i, 0, room.y + j);
                    if (i <= wallThickness || j <= wallThickness || i >= room.width - (1 + wallThickness) || j >= room.height - (1 + wallThickness)) {
                        if (!doorSpace.Contains(position)) {
                            walls.Add(position); //no check if theres already a wall because hash set only contains one of each item. //maybe instantiate the object right away?
                        }
                        else {
                            floors.Add(position); //maybe instantiate the object right away?
                        }
                    }
                    else {
                        floors.Add(position);//maybe instantiate the object right away?
                    }
                }
            }
        }
        foreach (Vector3 wall in walls)
        {
            Instantiate(wallPrefab, wall, Quaternion.identity, wallParent);
            if (!skipStructureCoroutine)
            {
                yield return new WaitForSeconds(waitTime);
            }
        }

        foreach (Vector3 floor in floors)
        {
            Instantiate(floorPrefab, floor, Quaternion.identity, floorParent);
            if (!skipStructureCoroutine)
            {
                yield return new WaitForSeconds(waitTime);
            }
        }
    }

    private void BakeNavMesh()
    {
        navMeshSurface.BuildNavMesh();
    }

    private void SpawnPlayer()
    {
        Instantiate(player, new Vector3(rooms[0].center.x, 1f, rooms[0].center.y), Quaternion.identity);
    }

    private Graph<Vector3> CopyGraph(Graph<Vector3> graphToCopy)
    {
        Graph<Vector3> newGraph = new Graph<Vector3>();
        HashSet<Vector3> checkedNodes = new HashSet<Vector3>();
        Vector3[] nodes = graphToCopy.GetNodes().ToArray();
        foreach (Vector3 node in nodes)
        {
            newGraph.AddNode(node);
        }
        foreach (Vector3 node in nodes)
        {
            checkedNodes.Add(node);
            foreach (Vector3 neighbor in graphToCopy.GetNeighbors(node))
            {
                if (!checkedNodes.Contains(neighbor))
                {
                    newGraph.AddEdge(node, neighbor);
                }
            }
        }
        return newGraph;
    }
    void OnDrawGizmos()
    {
        foreach (RectInt room in rooms)
        {
            AlgorithmsUtils.DebugRectInt(room, Color.blue);
        }

        foreach (RectInt door in doors)
        {
            AlgorithmsUtils.DebugRectInt(door, Color.red);
        }
        AlgorithmsUtils.DebugRectInt(dungeon, Color.green);
        
        foreach (Vector3 node in graph.GetNodes())
        {
            List<Vector3> connections = graph.GetNeighbors(node);
            
            foreach (Vector3 connection in connections)
            {
                Gizmos.color = Color.yellow;
                if (isGraphConnected)
                {
                    Gizmos.color = Color.green;
                }
                Gizmos.DrawLine(node, connection);
            }

            DebugExtension.DrawCircle(node, Color.yellow);
        }
    }
}