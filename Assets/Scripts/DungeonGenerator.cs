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
        rooms.Add(new RectInt(dungeon.xMin, dungeon.yMin, dungeon.xMax - dungeon.xMin, dungeon.yMax - dungeon.yMin));
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
        AlgorithmsUtils.DebugRectInt(new RectInt(dungeon.xMin - 1, dungeon.yMin - 1, dungeon.width + 2, dungeon.height + 2), Color.green);
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
                    rooms[i] = new RectInt(room.xMin, room.yMin, room.width, room.height - newRoomHeight);
                    rooms.Add(new RectInt(room.xMin, room.yMax - newRoomHeight, room.width, newRoomHeight));
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
                    rooms[i] = new RectInt(room.xMin, room.yMin, room.width - newRoomWidth, room.height);
                    rooms.Add(new RectInt(room.xMax - newRoomWidth, room.yMin, newRoomWidth, room.height));
                }
                i++;
            }
        }
    }
}
