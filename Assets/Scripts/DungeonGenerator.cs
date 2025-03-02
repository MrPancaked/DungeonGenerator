using System;
using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Dungeon Generator")]
    [SerializeField] private RectInt dungeon;
    [SerializeField] private int roomAmount;
    [SerializeField] private int wallThickness;
    [SerializeField] private List<RectInt> rooms = new List<RectInt>();
    

    private void Start()
    {
        rooms.Add(new RectInt(dungeon.xMin, dungeon.yMin, dungeon.xMax - dungeon.xMin, dungeon.yMax - dungeon.yMin));
    }
    private void Update()
    {
        AlgorithmsUtils.DebugRectInt(dungeon, Color.green);
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
    }

    private void SplitRooms(bool direction)
    {
        List<RectInt> newRooms = new List<RectInt>();
        newRooms.Clear();
        foreach (RectInt room in rooms)
        {
            newRooms.Add(room);
        }
        
        if (direction)
        {
            int i = 0;
            foreach (RectInt room in newRooms)
            {
                rooms[i] = new RectInt(room.xMin, room.yMin, (room.xMax - room.xMin), (room.yMax -  room.yMin)/2);
                rooms.Add(new RectInt(room.xMin, room.yMin + room.height/2, (room.xMax - room.xMin), (room.yMax -  room.yMin)/2));
                i++;
            }
        }
        else
        {
            int i = 0;
            foreach (RectInt room in newRooms)
            {
                rooms[i] = new RectInt(room.xMin, room.yMin, (room.xMax - room.xMin)/2, (room.yMax -  room.yMin));
                rooms.Add(new RectInt(room.xMin + room.width/2, room.yMin, (room.xMax - room.xMin)/2, (room.yMax -  room.yMin)));
                i++;
            }
        }
    }
}
