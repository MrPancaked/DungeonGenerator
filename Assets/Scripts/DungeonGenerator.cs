using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Dungeon Generator")]
    [SerializeField] private RectInt dungeon;
    [SerializeField] private int minRoomSize;
    [SerializeField] private int wallThickness;
    [SerializeField] private int doorWidth;
    [SerializeField] private List<RectInt> rooms = new List<RectInt>();
    [SerializeField] private List<RectInt> doors = new List<RectInt>();
    private bool canSplitH = true;
    private bool canSplitV = true;
    

    private void Start()
    {
        rooms.Add(new RectInt(dungeon.xMin - wallThickness, dungeon.yMin - wallThickness, dungeon.width + wallThickness * 2, dungeon.height + wallThickness * 2));
        StartCoroutine("SplitRooms");
    }
    private void Update()
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
    }

    private IEnumerator SplitRooms()
    {
        while (canSplitH || canSplitV)
        {
            bool coinFlip = UnityEngine.Random.value > 0.5f;

            if (coinFlip && canSplitH)
            {
                canSplitH = false;
                SplitHorizontal();
            }
            else if (!coinFlip && canSplitV)
            {
                canSplitV = false;
                SplitVertical();
            }
            yield return null;
        }

        GenerateDoors();
    }

    private void SplitVertical()
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
    private void SplitHorizontal()
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

    private void GenerateDoors()
    {
        int i = 1;
        
        foreach (RectInt room in rooms)
        {
            //starting the second for loop after the index of the room in the rooms list
            //which makes sure the room doesn't check with itself and already checked rooms don't get checked twice
            //an alternative would be deleting the rooms from a copied list and then iterating over that list with a foreach loop
            for (int j = i; j < rooms.Count; j++)
            {
                if (AlgorithmsUtils.Intersects(rooms[j], room))
                {
                    RectInt intersection = AlgorithmsUtils.Intersect(rooms[j], room);
                    //a check is needed to check in which orientation the wall between adjacent rooms are
                    //this way an accurate calculation of the area of the overlapping part can be made
                    if (intersection.width > intersection.height)
                    {
                        int area = (intersection.width - 4 * wallThickness) * intersection.height;
                        if (area > doorWidth * wallThickness * 2)
                        {
                            doors.Add(new RectInt(intersection.x + intersection.width / 2 - doorWidth / 2, intersection.y, doorWidth, intersection.height));
                        }
                    }
                    else if (intersection.width < intersection.height)
                    {
                        int area = (intersection.height - 4 * wallThickness) * intersection.width;
                        if (area > doorWidth * wallThickness * 2)
                        {
                            doors.Add(new RectInt(intersection.x, intersection.y + intersection.height / 2 - doorWidth / 2, intersection.width, doorWidth));
                        }
                    }
                }
            }
            i++;
        }
    }
}


