using System.Collections.Generic;

namespace ShipTest.Core.Graphs;

public interface IGraphNode
{
    public List<IGraphNode> Neighbors { get; set; }

    public bool Visited { get; set; }

    public List<IGraphNode> DetermineNeighbors();
}