using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Graph<T> {
    private Dictionary<T, List<T>> adjacencyList;
    public Graph() { adjacencyList = new Dictionary<T, List<T>>(); }
    
    public void AddNode(T node) {
        if (!adjacencyList.ContainsKey(node)) {
            adjacencyList[node] = new List<T>();
        }
    }

    public void DeleteNode(T node)
    {
        if (!adjacencyList.ContainsKey(node))
        {
            Debug.Log($"Node {node} was not found in the graph.");
            return;
        }

        foreach (T neighbor in adjacencyList[node])
        {
            adjacencyList[neighbor].Remove(node);
        }
        adjacencyList.Remove(node);
    }

    public void RemoveEdge(T fromNode, T toNode)
    {
        if (!adjacencyList.ContainsKey(fromNode) || !adjacencyList.ContainsKey(toNode)) {
            Debug.Log("One or both nodes do not exist in the graph.");
            return;
        }
        adjacencyList[fromNode].Remove(toNode);
        adjacencyList[toNode].Remove(fromNode);
    }
    public void AddEdge(T fromNode, T toNode) {
        if (!adjacencyList.ContainsKey(fromNode) || !adjacencyList.ContainsKey(toNode)) {
            Debug.Log("One or both nodes do not exist in the graph.");
            return;
        }
        adjacencyList[fromNode].Add(toNode);
        adjacencyList[toNode].Add(fromNode);
    }

    public IEnumerable<T> GetNodes()
    {
        return adjacencyList.Keys;
    }
    public List<T> GetNeighbors(T node) {
        if (!adjacencyList.ContainsKey(node)) 
        {
            Debug.Log("Node does not exist in the graph.");
        }
        return adjacencyList[node];
    }

}
