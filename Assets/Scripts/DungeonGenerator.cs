using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.LightTransport.PostProcessing;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Dungeon Generator")]
    [SerializeField] private int seed;
    [SerializeField] private bool skipRoomCoroutine;
    [SerializeField] private bool skipDoorCoroutine;
    [SerializeField] private bool skipGraphCoroutine;
    [SerializeField] private RectInt dungeon;
    [SerializeField] private int minRoomSize;
    [SerializeField] private int wallThickness;
    [SerializeField] private int doorWidth;
    [SerializeField] private List<RectInt> rooms = new List<RectInt>();
    [SerializeField] private List<RectInt> doors = new List<RectInt>();
    private Graph<Vector3> graph = new Graph<Vector3>();
    private bool canSplitH = true;
    private bool canSplitV = true;
    private bool isGraphConnected = false;
    

    private void Start()
    {
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
                    yield return null;
                }
            }
        }
        yield return StartCoroutine(nameof(GenerateRoomNodes));
        yield return StartCoroutine(nameof(GenerateDoors));
        isGraphConnected = IsGraphConnected(graph);
        print("the graph is " + isGraphConnected);
        RemoveRooms();
        
        //this was testing if the algorithm works and it seems to work
        
        //Graph<Vector3> testGraph = new Graph<Vector3>();
        //testGraph.AddNode(new Vector3(1, 1, 1));
        //testGraph.AddNode(new Vector3(0, 0, 0));
        //testGraph.AddNode(new Vector3(2, 2, 2));
        //testGraph.AddEdge(new Vector3(1, 1, 1), new Vector3(0, 0, 0));
        //testGraph.AddEdge(new Vector3(0, 0, 0), new Vector3(2, 2, 2));
        //print("the `testgraph is " + IsGraphConnected(testGraph));
        
        
    }
    private void SplitVertical(RectInt room, int roomnumber)
    {
        if ((room.height - 2 * wallThickness) / 2f >= minRoomSize)
        {
            int newRoomHeight = Random.Range(minRoomSize, room.height - minRoomSize);
            rooms[roomnumber] = new RectInt(room.xMin, room.yMin, room.width, room.height - newRoomHeight + wallThickness);
            rooms.Add(new RectInt(room.xMin, room.yMax - newRoomHeight - wallThickness, room.width, newRoomHeight + wallThickness));
            canSplitV = true;
        }
    }
    private void SplitHorizontal(RectInt room, int roomnumber)
    {
        if ((room.width - 2 * wallThickness) / 2f >= minRoomSize)
        {
            int newRoomWidth = Random.Range(minRoomSize, room.width - minRoomSize);
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
                                yield return null;
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
                                yield return null;
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
                yield return null;
            }
        }
    }

    private void RemoveRooms()
    {
        List<int> areas = new List<int>();
        foreach (RectInt room in rooms)
        {
            areas.Add(room.width * room.height);
        }
        areas.Sort();

        Graph<Vector3> copyGraph = CopyGraph(graph);  
        for (int i = 0; i < areas.Count / 10; i++)
        {
            copyGraph.DeleteNode(new Vector3(rooms[i].center.x, 0f , rooms[i].center.y));
            print(copyGraph.GetNodes().ToString());
            if (IsGraphConnected(copyGraph))
            {
                print("safe to remove room");
            }
        }
    }
    private bool IsGraphConnected(Graph<Vector3> checkGraph)
    {
        Graph<Vector3> newGraph = CopyGraph(checkGraph);
        foreach (Vector3 node1 in checkGraph.GetNodes())
        {
            newGraph.DeleteNode(node1);
            foreach (Vector3 node2 in newGraph.GetNodes())
            {
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
    }

    private Graph<Vector3> CopyGraph(Graph<Vector3> graphToCopy)
    {
        Graph<Vector3> newGraph = new Graph<Vector3>();
        foreach (Vector3 node in graphToCopy.GetNodes())
        {
            newGraph.AddNode(node);
        }
        //NODE AND NEIGHBOR GET COMPARED TWICE CAUSING ISSUES WITH THE CONNECTIVITY CHECK OF A DUPLICATED GRAPH WITH DELETED NODES
        //(nodes get deleted once, leaving behind a ghost neighbor that doesn't have a node -> the ghost neighbor gets assigned a discovered status and is now seen as a node.
        //When it's their turn in queue they get checked for neighbors but the node doesn't exist, throwing an error)
        foreach (Vector3 node in graphToCopy.GetNodes())
        {
            foreach (Vector3 neighbor in graphToCopy.GetNeighbors(node))
            {
                newGraph.AddEdge(node, neighbor);
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

            DebugExtension.DrawCircle(node, Color.yellow, 1f);
        }
    }
}