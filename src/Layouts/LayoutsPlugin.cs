﻿using EpForceDirectedGraph.cs;
using Layouts.Lang;
using Layouts.Properties;
using NClass.DiagramEditor.ClassDiagram;
using NClass.GUI;
using System;
using System.Linq;
using System.Windows.Forms;

namespace Layouts
{
  public sealed class LayoutsPlugin : Plugin
  {
    private readonly ToolStripMenuItem _menuItem;

    public LayoutsPlugin(NClassEnvironment environment) :
      base(environment)
    {
      _menuItem = new ToolStripMenuItem
      {
        Text = Strings.Menu_Title,
        Image = Resources.Layouts_16,
        ToolTipText = Strings.Menu_ToolTip
      };
      _menuItem.DropDownItems.Add(Strings.ForceDirected_Menu_Title, null, DoForceDirectedLayout);
    }

    public override bool IsAvailable
    {
      get { return DocumentManager.HasDocument; }
    }

    public override ToolStripItem MenuItem
    {
      get { return _menuItem; }
    }

    private void DoForceDirectedLayout(object sender, EventArgs e)
    {
      if (!DocumentManager.HasDocument)
      {
        return;
      }

      using (new AutoWaitCursor())
      {
        var graph = new Graph(new Creator());
        var diagram = (Diagram)DocumentManager.ActiveDocument;

        // add nodes
        diagram
          .Shapes
          .ToList()
          .ForEach(x =>
          {
            var data = new NodeData
            {
              InitialPosition = new FDGVector2(x.Location.X, x.Location.Y)
            };
            var node = new Node(x.Entity.Id.ToString(), data);
            graph.AddNode(node);
          });

        // add edges
        diagram
          .Connections
          .ToList()
          .ForEach(x =>
          {
            var source = new Node(x.Relationship.First.Id.ToString());
            var target = new Node(x.Relationship.Second.Id.ToString());
            var edge = new Edge(x.Relationship.Id.ToString(), source, target);
            graph.AddEdge(edge);
          });

        const float Stiffness = 81.76f;
        const float Repulsion = 40000.0f;
        const float Damping = 0.5f;
        var physics = new DiagramForceDirected2D(diagram, graph, Stiffness, Repulsion, Damping);

        const int MaxIterations = 10000;
        const float TimeStep = 0.01f;
        foreach (var _ in Enumerable.Range(0, MaxIterations))
        {
          physics.Calculate(TimeStep);
          if (physics.TotalEnergy < physics.Threshold)
          {
            break;
          }
        }

        // update diagram
        physics.EachNode(delegate (INode node, Particle pos)
        {
          var nodeId = Guid.Parse(node.Id);
          var shape = diagram.Shapes.Single(x => x.Entity.Id == nodeId);
          shape.Location = new System.Drawing.Point((int)pos.Position.X, (int)pos.Position.Y);
        });
        physics.EachEdge(delegate (IEdge edge, Spring spring)
        {
          var connId = Guid.Parse(edge.Id);
          var conn = diagram.Connections.Single(x => x.Relationship.Id == connId);
          conn.AutoRoute();
        });
      }
    }
  }
}
