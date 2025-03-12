using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Dungeon Generator")]
    [SerializeField] private RectInt dungeon;
    [SerializeField] private int minRoomSize;
    [SerializeField] private int wallThickness;
    [SerializeField] private List<RectInt> rooms = new List<RectInt>();
    

    private void Start()
    {
        rooms.Add(new RectInt(dungeon.xMin - wallThickness, dungeon.yMin - wallThickness, dungeon.width + wallThickness * 2, dungeon.height + wallThickness * 2));
    }
    private void Update()
    {
        foreach (RectInt room in rooms)
        {
            AlgorithmsUtils.DebugRectInt(room, Color.blue);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            SplitRooms(true);
            Debug.Log("Splitting rooms vertically");
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            SplitRooms(false);
            Debug.Log("Splitting rooms horizontally");
        }
        AlgorithmsUtils.DebugRectInt(dungeon, Color.green);
    }

    private void SplitRooms(bool direction)
    {
        List<RectInt> newRooms = new List<RectInt>();
        newRooms.Clear();
        foreach (RectInt room in rooms)
        {
            newRooms.Add(room);
        }
        
        if (direction) //vertical
        {
            int i = 0;
            foreach (RectInt room in newRooms)
            {
                if (room.height/2f >= minRoomSize)
                {
                    int newRoomHeight = UnityEngine.Random.Range(minRoomSize, room.height - minRoomSize);
                    rooms[i] = new RectInt(room.xMin, room.yMin, room.width, room.height - newRoomHeight + wallThickness);
                    rooms.Add(new RectInt(room.xMin, room.yMax - newRoomHeight - wallThickness, room.width, newRoomHeight + wallThickness));
                }
                i++;
            }
        }
        else //horizontal
        {
            int i = 0;
            foreach (RectInt room in newRooms)
            {
                if (room.width / 2f >= minRoomSize)
                {
                    int newRoomWidth = UnityEngine.Random.Range(minRoomSize, room.width - minRoomSize);
                    rooms[i] = new RectInt(room.xMin, room.yMin, room.width - newRoomWidth + wallThickness, room.height);
                    rooms.Add(new RectInt(room.xMax - newRoomWidth - wallThickness, room.yMin, newRoomWidth + wallThickness, room.height));
                }
                i++;
            }
        }
    }
}
