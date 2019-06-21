﻿using EpForceDirectedGraph.cs;
using NClass.DiagramEditor.ClassDiagram.Shapes;
using System.Drawing;

namespace Layouts
{
  public sealed class DiagramNode : INode
  {

    public string Id { get; private set; }
    public NodeData Data { get; private set; } = new NodeData();
    public bool Pinned { get; set; } = false;

    public DiagramNode(string iId, NodeData iData = null)
    {
      Id = iId;

      if (iData != null)
      {
        Data.InitialPosition = iData.InitialPosition;
        Data.Label = iData.Label;
        Data.Mass = iData.Mass;
      }
    }

    public override int GetHashCode()
    {
      return Id.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      // If parameter is null return false.
      if (obj == null)
      {
        return false;
      }

      // If parameter cannot be cast to Point return false.
      INode p = obj as INode;
      if (p is null)
      {
        return false;
      }

      // Return true if the fields match:
      return (Id == p.Id);
    }
  }
}
