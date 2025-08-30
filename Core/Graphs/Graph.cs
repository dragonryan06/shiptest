using System.Collections.Generic;
using System.Linq;
using Godot;
using Microsoft.VisualBasic.CompilerServices;

namespace ShipTest.Core.Graphs;

public class Graph<T> where T: IGraphNode
{
    public List<T> Nodes { get; } = [];

    public void AddNode(T node, bool determineNeighbors = true)
    {
        Nodes.Add(node);

        if (determineNeighbors)
        {
            node.Neighbors = node.DetermineNeighbors();
        }
    }

    public void RemoveNode(T node)
    {
        Nodes.Remove(node);
        foreach (var neighbor in node.Neighbors)
        {
            neighbor.Neighbors.Remove(node);
        }
    }

    public void UpdateNeighborsOf(T node)
    {
        var newNeighbors = node.DetermineNeighbors();
        
        foreach (var neighbor in newNeighbors.Where(neighbor => !node.Neighbors.Contains(neighbor)))
        {
            node.Neighbors.Add(neighbor);
            neighbor.Neighbors.Add(node);
        }

        var toRemove = new List<IGraphNode>();
        foreach (var neighbor in node.Neighbors.Where(neighbor => !newNeighbors.Contains(neighbor)))
        {
            toRemove.Add(neighbor);
            neighbor.Neighbors.Remove(node);
        }
        
        node.Neighbors.RemoveAll(n => toRemove.Contains(n));
    }
    
    public List<List<T>> GetConnectedComponents()
    {
        Nodes.ForEach(n => n.Visited = false);
        List<List<T>> result = [];

        while (Nodes.Any(n => !n.Visited))
        {
            var component = new List<T>();
            IGraphNode node = Nodes.First(n => !n.Visited);
            node.Visited = true;
            component.Add((T)node);
            List<IGraphNode> neighborStack = node.Neighbors.Where(n => !n.Visited).ToList();
            
            while (neighborStack.Count != 0)
            {
                node = neighborStack.First();
                if (!node.Visited)
                {
                    node.Visited = true;
                    component.Add((T)node);
                    neighborStack.AddRange(node.Neighbors);
                }
                
                neighborStack.RemoveAt(0);
            }
            
            result.Add(component);
        }

        return result;
    }
}