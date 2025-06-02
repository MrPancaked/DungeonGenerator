using System.Collections.Generic;
using UnityEngine;
using System.Collections;
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
    

    private void Start()
    {
        if (seed == 0)
        {
            seed = Random.Range(0, int.MaxValue);
        }
        Random.InitState(seed);
        rooms.Add(new RectInt(dungeon.xMin - wallThickness, dungeon.yMin - wallThickness, dungeon.width + wallThickness * 2, dungeon.height + wallThickness * 2));
        StartCoroutine(nameof(SplitRooms));
    }

    private IEnumerator SplitRooms()
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
    }
    
    private void SplitVerticalOld()
    {
        List<RectInt> newRooms = new List<RectInt>();
        newRooms.Clear();
        foreach (RectInt room in rooms)
        {
            newRooms.Add(room);
        }
        int i = 0;
        foreach (RectInt room in newRooms)
        {
            if ((room.height - 2 * wallThickness) / 2f >= minRoomSize)
            {
                int newRoomHeight = Random.Range(minRoomSize, room.height - minRoomSize);
                rooms[i] = new RectInt(room.xMin, room.yMin, room.width, room.height - newRoomHeight + wallThickness);
                rooms.Add(new RectInt(room.xMin, room.yMax - newRoomHeight - wallThickness, room.width, newRoomHeight + wallThickness));
                canSplitV = true;
            }
            i++;
        }
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
    private void SplitHorizontalOld()
    {
        List<RectInt> newRooms = new List<RectInt>();
        newRooms.Clear();
        foreach (RectInt room in rooms)
        {
            newRooms.Add(room);
        }
        int i = 0;
        foreach (RectInt room in newRooms)
        {
           // RectInt room = newRooms[i];
            if ((room.width - 2 * wallThickness) / 2f >= minRoomSize)
            {
                int newRoomWidth = Random.Range(minRoomSize, room.width - minRoomSize);
                rooms[i] = new RectInt(room.xMin, room.yMin, room.width - newRoomWidth + wallThickness, room.height);
                rooms.Add(new RectInt(room.xMax - newRoomWidth - wallThickness, room.yMin, newRoomWidth + wallThickness, room.height));
                canSplitH = true;
            }
            i++;
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
                Gizmos.DrawLine(node, connection);
            }

            DebugExtension.DrawCircle(node, Color.yellow, 1f);
        }
    }
}


