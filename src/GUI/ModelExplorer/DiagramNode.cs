﻿// NClass - Free class diagram editor
// Copyright (C) 2006-2009 Balazs Tihanyi
// 
// This program is free software; you can redistribute it and/or modify it under 
// the terms of the GNU General Public License as published by the Free Software 
// Foundation; either version 3 of the License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful, but WITHOUT 
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS 
// FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License along with 
// this program; if not, write to the Free Software Foundation, Inc., 
// 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

using NClass.Core;
using NClass.DiagramEditor;
using NClass.DiagramEditor.ClassDiagram;
using NClass.GUI.Properties;
using NClass.Translations;
using System;
using System.Windows.Forms;

namespace NClass.GUI.ModelExplorer
{
  public sealed class DiagramNode : ProjectItemNode
  {
    private static readonly ContextMenuStrip _contextMenu = new ContextMenuStrip();

    static DiagramNode()
    {
      _contextMenu.Items.AddRange(new ToolStripItem[] {
        new ToolStripMenuItem(Strings.MenuOpen, Resources.Open, open_Click),
        new ToolStripMenuItem(Strings.MenuRename, null, renameItem_Click, Keys.F2),
        new ToolStripSeparator(),
        new ToolStripMenuItem(Strings.MenuDeleteProjectItem, Resources.Delete, deleteProjectItem_Click)
      });
    }

    public DiagramNode(Diagram diagram)
    {
      Diagram = diagram ?? throw new ArgumentNullException("diagram");
      Text = diagram.Name;
      ImageKey = SelectedImageKey = KeyFromLanguage(diagram.Language);

      diagram.Renamed += new EventHandler(diagram_Renamed);
    }

    public Diagram Diagram { get; }

    public override IProjectItem ProjectItem
    {
      get { return Diagram; }
    }

    public override ContextMenuStrip ContextMenuStrip
    {
      get
      {
        _contextMenu.Tag = this;
        return _contextMenu;
      }
      set
      {
        base.ContextMenuStrip = value;
      }
    }

    public override void BeforeDelete()
    {
      Diagram.Renamed -= new EventHandler(diagram_Renamed);
      base.BeforeDelete();
    }

    public override void LabelModified(NodeLabelEditEventArgs e)
    {
      Diagram.Name = e.Label;
    }

    public override void DoubleClick()
    {
      ModelView.OnDocumentOpening(new DocumentEventArgs(Diagram));
    }

    public override void EnterPressed()
    {
        ModelView?.OnDocumentOpening(new DocumentEventArgs(Diagram));
    }

    private void diagram_Renamed(object sender, EventArgs e)
    {
      Text = Diagram.Name;
    }

    private static void open_Click(object sender, EventArgs e)
    {
      ToolStripItem menuItem = (ToolStripItem)sender;
      ModelView modelView = (ModelView)((ContextMenuStrip)menuItem.Owner).SourceControl;
      DiagramNode node = (DiagramNode)menuItem.Owner.Tag;

      modelView.OnDocumentOpening(new DocumentEventArgs(node.Diagram));
    }

    private static void renameItem_Click(object sender, EventArgs e)
    {
      ToolStripItem menuItem = (ToolStripItem)sender;
      DiagramNode node = (DiagramNode)menuItem.Owner.Tag;

      node.EditLabel();
    }

    private static void deleteProjectItem_Click(object sender, EventArgs e)
    {
      ToolStripItem menuItem = (ToolStripItem)sender;
      Diagram diagram = ((DiagramNode)menuItem.Owner.Tag).Diagram;
      Project project = diagram.Project;

      DialogResult result = MessageBox.Show(
        string.Format(Strings.DeleteProjectItemConfirmation, diagram.Name),
        Strings.Confirmation, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

      if (result == DialogResult.Yes)
      {
        project.Remove(diagram);
      }
    }

    private static string KeyFromLanguage(Language language)
    {
      if (language is CSharp.CSharpLanguage)
      {
        return "diagram";
      }

      if (language is EntityRelationshipDiagram.ErdLanguage)
      {
        return "erd";
      }

      if (language is StateTransitionDiagram.StdLanguage)
      {
        return "std";
      }

      return "diagram";
    }
  }
}
