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
using NClass.DiagramEditor.ClassDiagram.Connections;
using NClass.DiagramEditor.ClassDiagram.ContextMenus;
using NClass.DiagramEditor.ClassDiagram.Dialogs;
using NClass.DiagramEditor.ClassDiagram.Shapes;
using NClass.Translations;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace NClass.DiagramEditor.ClassDiagram
{
  public sealed class Diagram : Model, IDocument, IEditable, IPrintable
  {
    public event EventHandler OffsetChanged;
    public event EventHandler SizeChanged;
    public event EventHandler ZoomChanged;
    public event EventHandler StatusChanged;
    public event EventHandler SelectionChanged;
    public event EventHandler NeedsRedraw;
    public event EventHandler ClipboardAvailabilityChanged;
    public event PopupWindowEventHandler ShowingWindow;
    public event PopupWindowEventHandler HidingWindow;

    private enum Mode
    {
      Normal,
      Multiselecting,
      CreatingShape,
      CreatingConnection,
      Dragging
    }

    private const int DiagramPadding = 10;
    private const int PrecisionSize = 10;
    private const int MaximalPrecisionDistance = 500;
    private const float DashSize = 3;
    private static readonly Size MinSize = new Size(3000, 2000);

    public static readonly Pen SelectionPen = new Pen(Color.Black)
    {
      DashPattern = new float[] { DashSize, DashSize }
    };

    private Mode _state = Mode.Normal;
    private bool _selectioning = false;
    private RectangleF _selectionFrame = RectangleF.Empty;
    private PointF _mouseLocation = PointF.Empty;
    private bool _redrawSuspended = false;
    private Rectangle _shapeOutline = Rectangle.Empty;
    private EntityType _shapeType;
    private EntityType _newShapeType = EntityType.Class;
    private ConnectionCreator _connectionCreator = null;

    // required for XML deserialisation
    private Diagram()
    {
    }

    public Diagram(Language language) :
      base(language)
    {
    }

    public Diagram(string name, Language language) :
      base(name, language)
    {
    }

    public IEnumerable<Shape> Shapes
    {
      get { return ShapeList; }
    }

    internal ElementList<Shape> ShapeList { get; } = new ElementList<Shape>();

    public IEnumerable<Connection> Connections
    {
      get { return ConnectionList; }
    }

    internal ElementList<Connection> ConnectionList { get; } = new ElementList<Connection>();

    private Point _offset = Point.Empty;
    public Point Offset
    {
      get
      {
        return _offset;
      }
      set
      {
        if (value.X < 0)
        {
          value.X = 0;
        }
        if (value.Y < 0)
        {
          value.Y = 0;
        }

        if (_offset != value)
        {
          _offset = value;
          OnOffsetChanged(EventArgs.Empty);
        }
      }
    }

    private Size _size = MinSize;
    public Size Size
    {
      get
      {
        return _size;
      }
      set
      {
        if (value.Width < MinSize.Width)
        {
          value.Width = MinSize.Width;
        }
        if (value.Height < MinSize.Height)
        {
          value.Height = MinSize.Height;
        }

        if (_size != value)
        {
          _size = value;
          OnSizeChanged(EventArgs.Empty);
        }
      }
    }

    private float _zoom = 1.0F;
    public float Zoom
    {
      get
      {
        return _zoom;
      }
      set
      {
        if (value < Canvas.MinZoom)
        {
          value = Canvas.MinZoom;
        }
        if (value > Canvas.MaxZoom)
        {
          value = Canvas.MaxZoom;
        }

        if (_zoom != value)
        {
          _zoom = value;
          OnZoomChanged(EventArgs.Empty);
        }
      }
    }

    public Color BackColor
    {
      get { return Style.CurrentStyle.BackgroundColor; }
    }

    public bool RedrawSuspended
    {
      get
      {
        return _redrawSuspended;
      }
      set
      {
        if (_redrawSuspended != value)
        {
          _redrawSuspended = value;
          if (!_redrawSuspended)
          {
            RecalculateSize();
            RequestRedrawIfNeeded();
          }
        }
      }
    }

    public bool CanCutToClipboard
    {
      get { return SelectedShapeCount > 0; }
    }

    public bool CanCopyToClipboard
    {
      get { return SelectedShapeCount > 0; }
    }

    public bool CanPasteFromClipboard
    {
      get { return Clipboard.Item is ElementContainer; }
    }

    public int ShapeCount
    {
      get { return ShapeList.Count; }
    }

    public int ConnectionCount
    {
      get { return ConnectionList.Count; }
    }

    private DiagramElement _activeElement = null;
    public DiagramElement ActiveElement
    {
      get
      {
        return _activeElement;
      }
      private set
      {
        if (_activeElement != null)
        {
          _activeElement.IsActive = false;
        }
        _activeElement = value;
      }
    }

    public DiagramElement TopSelectedElement
    {
      get
      {
        if (SelectedConnectionCount > 0)
        {
          return ConnectionList.FirstValue;
        }

        if (SelectedShapeCount > 0)
        {
          return ShapeList.FirstValue;
        }

        return null;
      }
    }

    public bool HasSelectedElement
    {
      get
      {
        return (SelectedElementCount > 0);
      }
    }

    public int SelectedElementCount
    {
      get { return SelectedShapeCount + SelectedConnectionCount; }
    }

    public int SelectedShapeCount { get; private set; } = 0;

    public int SelectedConnectionCount { get; private set; } = 0;

    public string GetSelectedElementName()
    {
      if (HasSelectedElement && SelectedElementCount == 1)
      {
        foreach (Shape shape in ShapeList)
        {
          if (shape.IsSelected)
          {
            return shape.Entity.Name;
          }
        }
      }

      return null;
    }

    public IEnumerable<Shape> GetSelectedShapes()
    {
      return ShapeList.GetSelectedElements();
    }

    public IEnumerable<Connection> GetSelectedConnections()
    {
      return ConnectionList.GetSelectedElements();
    }

    public IEnumerable<DiagramElement> GetSelectedElements()
    {
      foreach (Shape shape in ShapeList)
      {
        if (shape.IsSelected)
        {
          yield return shape;
        }
      }
      foreach (Connection connection in ConnectionList)
      {
        if (connection.IsSelected)
        {
          yield return connection;
        }
      }
    }

    private IEnumerable<DiagramElement> GetElementsInDisplayOrder()
    {
      foreach (Shape shape in ShapeList.GetSelectedElements())
      {
        yield return shape;
      }

      foreach (Connection connection in ConnectionList.GetSelectedElements())
      {
        yield return connection;
      }

      foreach (Connection connection in ConnectionList.GetUnselectedElements())
      {
        yield return connection;
      }

      foreach (Shape shape in ShapeList.GetUnselectedElements())
      {
        yield return shape;
      }
    }

    private IEnumerable<DiagramElement> GetElementsInReversedDisplayOrder()
    {
      foreach (Shape shape in ShapeList.GetUnselectedElementsReversed())
      {
        yield return shape;
      }

      foreach (Connection connection in ConnectionList.GetUnselectedElementsReversed())
      {
        yield return connection;
      }

      foreach (Connection connection in ConnectionList.GetSelectedElementsReversed())
      {
        yield return connection;
      }

      foreach (Shape shape in ShapeList.GetSelectedElementsReversed())
      {
        yield return shape;
      }
    }

    public void CloseWindows()
    {
      if (ActiveElement != null)
      {
        ActiveElement.HideEditor();
      }
    }

    public void Cut()
    {
      if (CanCutToClipboard)
      {
        Copy();
        DeleteSelectedElements(false);
      }
    }

    public void Copy()
    {
      if (CanCopyToClipboard)
      {
        ElementContainer elements = new ElementContainer();
        foreach (Shape shape in GetSelectedShapes())
        {
          elements.AddShape(shape);
        }
        foreach (Connection connection in GetSelectedConnections())
        {
          elements.AddConnection(connection);
        }
        Clipboard.Item = elements;
      }
    }

    public void Paste()
    {
      if (CanPasteFromClipboard)
      {
        DeselectAll();
        RedrawSuspended = true;
        Clipboard.Paste(this);
        RedrawSuspended = false;
        OnClipboardAvailabilityChanged(EventArgs.Empty);
      }
    }

    public void Display(IGraphics g)
    {
      var clip = g.ClipBounds;

      // Draw diagram elements
      foreach (DiagramElement element in GetElementsInReversedDisplayOrder())
      {
        if (clip.IntersectsWith(element.GetVisibleArea(Zoom)))
        {
          element.Draw(g, true);
        }
        element.NeedsRedraw = false;
      }
      if (_state == Mode.CreatingShape)
      {
        g.DrawRectangle(SelectionPen, new Rectangle(_shapeOutline.X, _shapeOutline.Y, _shapeOutline.Width, _shapeOutline.Height));
      }
      else if (_state == Mode.CreatingConnection)
      {
        _connectionCreator.Draw(g);
      }

      // Draw selection lines
      var savedState = g.Save();
      g.ResetTransform();
      g.SmoothingMode = SmoothingMode.None;
      foreach (var shape in ShapeList.GetSelectedElementsReversed())
      {
        if (clip.IntersectsWith(shape.GetVisibleArea(Zoom)))
        {
          shape.DrawSelectionLines(g, Zoom, Offset);
        }
      }
      foreach (var connection in ConnectionList.GetSelectedElementsReversed())
      {
        if (clip.IntersectsWith(connection.GetVisibleArea(Zoom)))
        {
          connection.DrawSelectionLines(g, Zoom, Offset);
        }
      }

      if (_state == Mode.Multiselecting)
      {
        var frame = RectangleF.FromLTRB(
          Math.Min(_selectionFrame.Left, _selectionFrame.Right),
          Math.Min(_selectionFrame.Top, _selectionFrame.Bottom),
          Math.Max(_selectionFrame.Left, _selectionFrame.Right),
          Math.Max(_selectionFrame.Top, _selectionFrame.Bottom));
        g.DrawRectangle(SelectionPen, new Rectangle(
          (int)(frame.X * Zoom - Offset.X),
          (int)(frame.Y * Zoom - Offset.Y),
          (int)(frame.Width * Zoom),
          (int)(frame.Height * Zoom)));
      }

      // Draw diagram border
      clip = g.ClipBounds;
      float borderWidth = Size.Width * Zoom;
      float borderHeight = Size.Height * Zoom;
      if (clip.Right > borderWidth || clip.Bottom > borderHeight)
      {
        SelectionPen.DashOffset = Offset.Y - Offset.X;
        g.DrawLines(SelectionPen, new Point[] {
          new Point((int)borderWidth, 0),
          new Point((int)borderWidth, (int)borderHeight),
          new Point(0, (int)borderHeight)
        });
        SelectionPen.DashOffset = 0;
      }

      // Restore original state
      g.Restore(savedState);
    }

    public void CopyAsImage()
    {
      ImageCreator.CopyAsImage(this);
    }

    public void CopyAsImage(bool selectedOnly)
    {
      ImageCreator.CopyAsImage(this, selectedOnly);
    }

    public void SaveAsImage()
    {
      ImageCreator.SaveAsImage(this);
    }

    public void SaveAsImage(bool selectedOnly)
    {
      ImageCreator.SaveAsImage(this, selectedOnly);
    }

    public void ShowPrintDialog()
    {
      var dialog = new DiagramPrintDialog
      {
        Document = this
      };
      dialog.ShowDialog();
    }

    public void Print(IGraphics g)
    {
      Print(g, false, Style.CurrentStyle);
    }

    public void Print(IGraphics g, bool selectedOnly, Style style)
    {
      foreach (var shape in ShapeList.GetReversedList())
      {
        if (!selectedOnly || shape.IsSelected)
        {
          shape.Draw(g, false, style);
        }
      }
      foreach (Connection connection in ConnectionList.GetReversedList())
      {
        if (!selectedOnly || connection.IsSelected)
        {
          connection.Draw(g, false, style);
        }
      }
    }

    private void RecalculateSize()
    {
      const int Padding = 500;
      int rightMax = MinSize.Width, bottomMax = MinSize.Height;

      foreach (var shape in ShapeList)
      {
        var area = shape.GetLogicalArea();
        if (area.Right + Padding > rightMax)
        {
          rightMax = area.Right + Padding;
        }
        if (area.Bottom + Padding > bottomMax)
        {
          bottomMax = area.Bottom + Padding;
        }
      }
      foreach (Connection connection in ConnectionList)
      {
        var area = connection.GetLogicalArea();
        if (area.Right + Padding > rightMax)
        {
          rightMax = area.Right + Padding;
        }
        if (area.Bottom + Padding > bottomMax)
        {
          bottomMax = area.Bottom + Padding;
        }
      }

      Size = new Size(rightMax, bottomMax);
    }

    public void AlignLeft()
    {
      if (SelectedShapeCount >= 2)
      {
        int left = Size.Width;
        RedrawSuspended = true;

        foreach (var shape in ShapeList.GetSelectedElements())
        {
          left = Math.Min(left, shape.Left);
        }
        foreach (var shape in ShapeList.GetSelectedElements())
        {
          shape.Left = left;
        }

        RedrawSuspended = false;
      }
    }

    public void AlignRight()
    {
      if (SelectedShapeCount >= 2)
      {
        int right = 0;
        RedrawSuspended = true;

        foreach (var shape in ShapeList.GetSelectedElements())
        {
          right = Math.Max(right, shape.Right);
        }
        foreach (var shape in ShapeList.GetSelectedElements())
        {
          shape.Right = right;
        }

        RedrawSuspended = false;
      }
    }

    public void AlignTop()
    {
      if (SelectedShapeCount >= 2)
      {
        int top = Size.Height;
        RedrawSuspended = true;

        foreach (var shape in ShapeList.GetSelectedElements())
        {
          top = Math.Min(top, shape.Top);
        }
        foreach (var shape in ShapeList.GetSelectedElements())
        {
          shape.Top = top;
        }

        RedrawSuspended = false;
      }
    }

    public void AlignBottom()
    {
      if (SelectedShapeCount >= 2)
      {
        int bottom = 0;
        RedrawSuspended = true;

        foreach (var shape in ShapeList.GetSelectedElements())
        {
          bottom = Math.Max(bottom, shape.Bottom);
        }
        foreach (var shape in ShapeList.GetSelectedElements())
        {
          shape.Bottom = bottom;
        }

        RedrawSuspended = false;
      }
    }

    public void AlignHorizontal()
    {
      if (SelectedShapeCount >= 2)
      {
        int center = 0;
        RedrawSuspended = true;

        foreach (var shape in ShapeList.GetSelectedElements())
        {
          center += (shape.Top + shape.Bottom) / 2;
        }
        center /= SelectedShapeCount;

        foreach (var shape in ShapeList.GetSelectedElements())
        {
          shape.Top = center - shape.Height / 2;
        }

        RedrawSuspended = false;
      }
    }

    public void AlignVertical()
    {
      if (SelectedShapeCount >= 2)
      {
        int center = 0;
        RedrawSuspended = true;

        foreach (var shape in ShapeList.GetSelectedElements())
        {
          center += (shape.Left + shape.Right) / 2;
        }
        center /= SelectedShapeCount;

        foreach (var shape in ShapeList.GetSelectedElements())
        {
          shape.Left = center - shape.Width / 2;
        }

        RedrawSuspended = false;
      }
    }

    public void AdjustToSameWidth()
    {
      if (SelectedShapeCount >= 2)
      {
        int maxWidth = 0;
        RedrawSuspended = true;

        foreach (var shape in ShapeList.GetSelectedElements())
        {
          maxWidth = Math.Max(maxWidth, shape.Width);
        }
        foreach (var shape in ShapeList.GetSelectedElements())
        {
          shape.Width = maxWidth;
        }
        RedrawSuspended = false;
      }
    }

    public void AdjustToSameHeight()
    {
      if (SelectedShapeCount >= 2)
      {
        int maxHeight = 0;
        RedrawSuspended = true;

        foreach (var shape in ShapeList.GetSelectedElements())
        {
          maxHeight = Math.Max(maxHeight, shape.Height);
        }
        foreach (var shape in ShapeList.GetSelectedElements())
        {
          shape.Height = maxHeight;
        }

        RedrawSuspended = false;
      }
    }

    public void AdjustToSameSize()
    {
      if (SelectedShapeCount >= 2)
      {
        var maxSize = Size.Empty;
        RedrawSuspended = true;

        foreach (var shape in ShapeList.GetSelectedElements())
        {
          maxSize.Width = Math.Max(maxSize.Width, shape.Width);
          maxSize.Height = Math.Max(maxSize.Height, shape.Height);
        }
        foreach (Shape shape in ShapeList.GetSelectedElements())
        {
          shape.Size = maxSize;
        }

        RedrawSuspended = false;
      }
    }

    public void AutoSizeOfSelectedShapes()
    {
      RedrawSuspended = true;
      foreach (var shape in ShapeList.GetSelectedElements())
      {
        shape.AutoWidth();
        shape.AutoHeight();
      }
      RedrawSuspended = false;
    }

    public void AutoWidthOfSelectedShapes()
    {
      RedrawSuspended = true;
      foreach (var shape in ShapeList.GetSelectedElements())
      {
        shape.AutoWidth();
      }
      RedrawSuspended = false;
    }

    public void AutoHeightOfSelectedShapes()
    {
      RedrawSuspended = true;
      foreach (var shape in ShapeList.GetSelectedElements())
      {
        shape.AutoHeight();
      }
      RedrawSuspended = false;
    }

    public void CollapseAll()
    {
      bool selectedOnly = HasSelectedElement;
      CollapseAll(selectedOnly);
    }

    public void CollapseAll(bool selectedOnly)
    {
      RedrawSuspended = true;

      foreach (var shape in ShapeList)
      {
        if (shape.IsSelected || !selectedOnly)
        {
          shape.Collapse();
        }
      }

      RedrawSuspended = false;
    }

    public void ExpandAll()
    {
      bool selectedOnly = HasSelectedElement;
      ExpandAll(selectedOnly);
    }

    public void ExpandAll(bool selectedOnly)
    {
      RedrawSuspended = true;

      foreach (var shape in ShapeList)
      {
        if (shape.IsSelected || !selectedOnly)
        {
          shape.Expand();
        }
      }

      RedrawSuspended = false;
    }

    public void SelectAll()
    {
      RedrawSuspended = true;
      _selectioning = true;

      foreach (var shape in ShapeList)
      {
        shape.IsSelected = true;
      }
      foreach (var connection in ConnectionList)
      {
        connection.IsSelected = true;
      }

      SelectedShapeCount = ShapeList.Count;
      SelectedConnectionCount = ConnectionList.Count;

      OnSelectionChanged(EventArgs.Empty);
      OnClipboardAvailabilityChanged(EventArgs.Empty);
      OnStatusChanged(EventArgs.Empty);

      _selectioning = false;
      RedrawSuspended = false;
    }

    private bool ConfirmDelete()
    {
      var result = MessageBox.Show(
        Strings.DeleteElementsConfirmation, Strings.Confirmation,
        MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

      return (result == DialogResult.Yes);
    }

    public void DeleteSelectedElements()
    {
      DeleteSelectedElements(true);
    }

    private void DeleteSelectedElements(bool showConfirmation)
    {
      if (HasSelectedElement && (!showConfirmation || ConfirmDelete()))
      {
        if (SelectedShapeCount > 0)
        {
          foreach (var shape in ShapeList.GetModifiableList())
          {
            if (shape.IsSelected)
            {
              RemoveEntity(shape.Entity);
            }
          }
        }
        if (SelectedConnectionCount > 0)
        {
          foreach (Connection connection in ConnectionList.GetModifiableList())
          {
            if (connection.IsSelected)
            {
              RemoveRelationship(connection.Relationship);
            }
          }
        }
        Redraw();
      }
    }

    public void Redraw()
    {
      OnNeedsRedraw(EventArgs.Empty);
    }

    private void RequestRedrawIfNeeded()
    {
      if (Loading)
      {
        return;
      }

      foreach (var shape in ShapeList)
      {
        if (shape.NeedsRedraw)
        {
          OnNeedsRedraw(EventArgs.Empty);
          return;
        }
      }
      foreach (Connection connection in ConnectionList)
      {
        if (connection.NeedsRedraw)
        {
          OnNeedsRedraw(EventArgs.Empty);
          return;
        }
      }
    }

    public DynamicMenu GetDynamicMenu()
    {
      DynamicMenu dynamicMenu = DiagramDynamicMenu.Default;
      dynamicMenu.SetReference(this);
      return dynamicMenu;
    }

    public ContextMenuStrip GetContextMenu(AbsoluteMouseEventArgs e)
    {
      if (HasSelectedElement)
      {
        var intersector = new Intersector<ToolStripItem>();
        ContextMenu.MenuStrip.Items.Clear();

        foreach (var shape in GetSelectedShapes())
        {
          intersector.AddSet(shape.GetContextMenuItems(this));
        }
        foreach (var connection in GetSelectedConnections())
        {
          intersector.AddSet(connection.GetContextMenuItems(this));
        }

        foreach (var menuItem in intersector.GetIntersection())
        {
          ContextMenu.MenuStrip.Items.Add(menuItem);
        }

        return ContextMenu.MenuStrip;
      }
      else
      {
        ContextMenu.MenuStrip.Items.Clear();
        foreach (var menuItem in BlankContextMenu.Default.GetMenuItems(this))
        {
          ContextMenu.MenuStrip.Items.Add(menuItem);
        }

        return ContextMenu.MenuStrip;
      }
    }

    public string GetStatus()
    {
      if (SelectedElementCount == 1)
      {
        return TopSelectedElement.ToString();
      }

      if (SelectedElementCount > 1)
      {
        return string.Format(Strings.ItemsSelected, SelectedElementCount);
      }

      return Strings.Ready;
    }

    public string GetShortDescription()
    {
      return Strings.Language + ": " + Language.ToString();
    }

    public void DeselectAll()
    {
      foreach (var shape in ShapeList)
      {
        shape.IsSelected = false;
        shape.IsActive = false;
      }
      foreach (var connection in ConnectionList)
      {
        connection.IsSelected = false;
        connection.IsActive = false;
      }
      ActiveElement = null;
    }

    private void DeselectAllOthers(DiagramElement onlySelected)
    {
      foreach (var shape in ShapeList)
      {
        if (shape != onlySelected)
        {
          shape.IsSelected = false;
          shape.IsActive = false;
        }
      }
      foreach (var connection in ConnectionList)
      {
        if (connection != onlySelected)
        {
          connection.IsSelected = false;
          connection.IsActive = false;
        }
      }
    }

    public void MouseDown(AbsoluteMouseEventArgs e)
    {
      RedrawSuspended = true;
      if (_state == Mode.CreatingShape)
      {
        AddCreatedShape();
      }
      else if (_state == Mode.CreatingConnection)
      {
        _connectionCreator.MouseDown(e);
        if (_connectionCreator.Created)
        {
          _state = Mode.Normal;
        }
      }
      else
      {
        SelectElements(e);
      }

      if (e.Button == MouseButtons.Right)
      {
        ActiveElement = null;
      }

      RedrawSuspended = false;
    }

    private void AddCreatedShape()
    {
      try
      {
        DeselectAll();
        var shape = AddShape(_shapeType);
        shape.Location = _shapeOutline.Location;
        RecalculateSize();

        shape.IsSelected = true;
        shape.IsActive = true;
        if (shape is TypeShape)
        {
          shape.ShowEditor();
        }
      }
      finally
      {
        _state = Mode.Normal;
      }
    }

    private void SelectElements(AbsoluteMouseEventArgs e)
    {
      DiagramElement firstElement = null;
      var multiSelection = (Control.ModifierKeys == Keys.Control);

      foreach (var element in GetElementsInDisplayOrder())
      {
        var isSelected = element.IsSelected;
        element.MousePressed(e);
        if (e.Handled && firstElement == null)
        {
          firstElement = element;
          if (isSelected)
          {
            multiSelection = true;
          }
        }
      }

      if (firstElement != null && !multiSelection)
      {
        DeselectAllOthers(firstElement);
      }

      if (!e.Handled)
      {
        if (!multiSelection)
        {
          DeselectAll();
        }

        if (e.Button == MouseButtons.Left)
        {
          _state = Mode.Multiselecting;
          _selectionFrame.Location = e.Location;
          _selectionFrame.Size = Size.Empty;
        }
      }
    }

    public void MouseMove(AbsoluteMouseEventArgs e)
    {
      RedrawSuspended = true;

      _mouseLocation = e.Location;
      if (_state == Mode.Multiselecting)
      {
        _selectionFrame = RectangleF.FromLTRB(_selectionFrame.Left, _selectionFrame.Top, e.X, e.Y);
        Redraw();
      }
      else if (_state == Mode.CreatingShape)
      {
        _shapeOutline.Location = new Point((int)e.X, (int)e.Y);
        Redraw();
      }
      else if (_state == Mode.CreatingConnection)
      {
        _connectionCreator.MouseMove(e);
      }
      else
      {
        foreach (DiagramElement element in GetElementsInDisplayOrder())
        {
          element.MouseMoved(e);
        }
      }

      RedrawSuspended = false;
    }

    public void MouseUp(AbsoluteMouseEventArgs e)
    {
      RedrawSuspended = true;

      if (_state == Mode.Multiselecting)
      {
        TrySelectElements();
        _state = Mode.Normal;
      }
      else
      {
        foreach (var element in GetElementsInDisplayOrder())
        {
          element.MouseUpped(e);
        }
      }

      RedrawSuspended = false;
    }

    private void TrySelectElements()
    {
      _selectionFrame = RectangleF.FromLTRB(
        Math.Min(_selectionFrame.Left, _selectionFrame.Right),
        Math.Min(_selectionFrame.Top, _selectionFrame.Bottom),
        Math.Max(_selectionFrame.Left, _selectionFrame.Right),
        Math.Max(_selectionFrame.Top, _selectionFrame.Bottom));
      _selectioning = true;

      foreach (var shape in ShapeList)
      {
        if (shape.TrySelect(_selectionFrame))
        {
          SelectedShapeCount++;
        }
      }
      foreach (Connection connection in ConnectionList)
      {
        if (connection.TrySelect(_selectionFrame))
        {
          SelectedConnectionCount++;
        }
      }

      OnSelectionChanged(EventArgs.Empty);
      OnClipboardAvailabilityChanged(EventArgs.Empty);
      OnStatusChanged(EventArgs.Empty);
      Redraw();

      _selectioning = false;
    }

    public void DoubleClick(AbsoluteMouseEventArgs e)
    {
      foreach (DiagramElement element in GetElementsInDisplayOrder())
      {
        element.DoubleClicked(e);
      }
    }

    public void KeyDown(KeyEventArgs e)
    {
      try
      {
        RedrawSuspended = true;

        // Delete
        if (e.KeyCode == Keys.Delete)
        {
          if (SelectedElementCount >= 2 || ActiveElement == null ||
            !ActiveElement.DeleteSelectedMember())
          {
            DeleteSelectedElements();
          }
        }
        // Escape
        else if (e.KeyCode == Keys.Escape)
        {
          _state = Mode.Normal;
          DeselectAll();
          Redraw();
        }
        // Enter
        else if (e.KeyCode == Keys.Enter && ActiveElement != null)
        {
          ActiveElement.ShowEditor();
        }
        // Up
        else if (e.KeyCode == Keys.Up && ActiveElement != null)
        {
          if (e.Shift || e.Control)
          {
            ActiveElement.MoveUp();
          }
          else
          {
            ActiveElement.SelectPrevious();
          }
        }
        // Down
        else if (e.KeyCode == Keys.Down && ActiveElement != null)
        {
          if (e.Shift || e.Control)
          {
            ActiveElement.MoveDown();
          }
          else
          {
            ActiveElement.SelectNext();
          }
        }
        // Ctrl + X
        else if (e.KeyCode == Keys.X && e.Modifiers == Keys.Control)
        {
          Cut();
        }
        // Ctrl + C
        else if (e.KeyCode == Keys.C && e.Modifiers == Keys.Control)
        {
          Copy();
        }
        // Ctrl + Z
        else if (e.KeyCode == Keys.Z && e.Modifiers == Keys.Control)
        {
          Undo();
        }
        // Ctrl + Y
        else if (e.KeyCode == Keys.Y && e.Modifiers == Keys.Control)
        {
          Redo();
        }
        // Ctrl + V
        else if (e.KeyCode == Keys.V && e.Modifiers == Keys.Control)
        {
          Paste();
        }
        // Ctrl + Shift + ?
        else if (e.Modifiers == (Keys.Control | Keys.Shift))
        {
          switch (e.KeyCode)
          {
            case Keys.A:
              CreateShape();
              break;

            case Keys.C:
              CreateShape(EntityType.Class);
              break;

            case Keys.S:
              CreateShape(EntityType.Structure);
              break;

            case Keys.I:
              CreateShape(EntityType.Interface);
              break;

            case Keys.E:
              CreateShape(EntityType.Enum);
              break;

            case Keys.D:
              CreateShape(EntityType.Delegate);
              break;

            case Keys.N:
              CreateShape(EntityType.Comment);
              break;
          }
        }
      }
      finally
      {
        RedrawSuspended = false;
      }
    }

    public RectangleF GetPrintingArea(bool selectedOnly)
    {
      RectangleF area = Rectangle.Empty;
      var first = true;

      foreach (var shape in ShapeList)
      {
        if (!selectedOnly || shape.IsSelected)
        {
          if (first)
          {
            area = shape.GetPrintingClip(Zoom);
            first = false;
          }
          else
          {
            area = RectangleF.Union(area, shape.GetPrintingClip(Zoom));
          }
        }
      }
      foreach (var connection in ConnectionList)
      {
        if (!selectedOnly || connection.IsSelected)
        {
          if (first)
          {
            area = connection.GetPrintingClip(Zoom);
            first = false;
          }
          else
          {
            area = RectangleF.Union(area, connection.GetPrintingClip(Zoom));
          }
        }
      }

      return area;
    }

    private void UpdateWindowPosition()
    {
      if (ActiveElement != null)
      {
        ActiveElement.MoveWindow();
      }
    }

    internal void ShowWindow(PopupWindow window)
    {
      Redraw();
      OnShowingWindow(new PopupWindowEventArgs(window));
    }

    internal void HideWindow(PopupWindow window)
    {
      window.Closing();
      OnHidingWindow(new PopupWindowEventArgs(window));
    }

    private void AddShape(Shape shape)
    {
      shape.Diagram = this;
      shape.BeginUndoableOperation += new EventHandler(OnBeginUndoableOperation);
      shape.Modified += new EventHandler(Element_Modified);
      shape.Activating += new EventHandler(Element_Activating);
      shape.Dragging += new MoveEventHandler(Shape_Dragging);
      shape.Resizing += new ResizeEventHandler(Shape_Resizing);
      shape.SelectionChanged += new EventHandler(Shape_SelectionChanged);
      ShapeList.AddFirst(shape);
      RecalculateSize();
    }

    private void Element_Modified(object sender, EventArgs e)
    {
      if (!RedrawSuspended)
      {
        RequestRedrawIfNeeded();
      }
      OnModified(EventArgs.Empty);
    }

    private void Element_Activating(object sender, EventArgs e)
    {
      foreach (var shape in ShapeList)
      {
        if (shape != sender)
        {
          shape.IsActive = false;
        }
      }
      foreach (var connection in ConnectionList)
      {
        if (connection != sender)
        {
          connection.IsActive = false;
        }
      }
      ActiveElement = (DiagramElement)sender;
    }

    private void Shape_Dragging(object sender, MoveEventArgs e)
    {
      var offset = e.Offset;

      // Align to other shapes
      if (Settings.Default.UsePrecisionSnapping && Control.ModifierKeys != Keys.Shift)
      {
        var shape = (Shape)sender;

        foreach (var otherShape in ShapeList.GetUnselectedElements())
        {
          var xDist = otherShape.X - (shape.X + offset.Width);
          var yDist = otherShape.Y - (shape.Y + offset.Height);

          if (Math.Abs(xDist) <= PrecisionSize)
          {
            var distance1 = Math.Abs(shape.Top - otherShape.Bottom);
            var distance2 = Math.Abs(otherShape.Top - shape.Bottom);
            var distance = Math.Min(distance1, distance2);

            if (distance <= MaximalPrecisionDistance)
            {
              offset.Width += xDist;
            }
          }
          if (Math.Abs(yDist) <= PrecisionSize)
          {
            var distance1 = Math.Abs(shape.Left - otherShape.Right);
            var distance2 = Math.Abs(otherShape.Left - shape.Right);
            var distance = Math.Min(distance1, distance2);

            if (distance <= MaximalPrecisionDistance)
            {
              offset.Height += yDist;
            }
          }
        }
      }

      // Get maxmimal avaiable offset for the selected elements
      foreach (var shape in ShapeList)
      {
        offset = shape.GetMaximalOffset(offset, DiagramPadding);
      }
      foreach (var connection in ConnectionList)
      {
        offset = connection.GetMaximalOffset(offset, DiagramPadding);
      }
      if (!offset.IsEmpty)
      {
        foreach (var shape in ShapeList.GetSelectedElements())
        {
          shape.Offset(offset);
        }
        foreach (var connection in ConnectionList.GetSelectedElements())
        {
          connection.Offset(offset);
        }
      }

      RecalculateSize();
    }

    private void Shape_Resizing(object sender, ResizeEventArgs e)
    {
      if (Settings.Default.UsePrecisionSnapping && Control.ModifierKeys != Keys.Shift)
      {
        var shape = (Shape)sender;
        var change = e.Change;

        // Horizontal resizing
        if (change.Width != 0)
        {
          foreach (var otherShape in ShapeList.GetUnselectedElements())
          {
            if (otherShape != shape)
            {
              var xDist = otherShape.Right - (shape.Right + change.Width);
              if (Math.Abs(xDist) <= PrecisionSize)
              {
                var distance1 = Math.Abs(shape.Top - otherShape.Bottom);
                var distance2 = Math.Abs(otherShape.Top - shape.Bottom);
                var distance = Math.Min(distance1, distance2);

                if (distance <= MaximalPrecisionDistance)
                {
                  change.Width += xDist;
                  break;
                }
              }
            }
          }
        }

        // Vertical resizing
        if (change.Height != 0)
        {
          foreach (var otherShape in ShapeList.GetUnselectedElements())
          {
            if (otherShape != shape)
            {
              var yDist = otherShape.Bottom - (shape.Bottom + change.Height);
              if (Math.Abs(yDist) <= PrecisionSize)
              {
                var distance1 = Math.Abs(shape.Left - otherShape.Right);
                var distance2 = Math.Abs(otherShape.Left - shape.Right);
                var distance = Math.Min(distance1, distance2);

                if (distance <= MaximalPrecisionDistance)
                {
                  change.Height += yDist;
                  break;
                }
              }
            }
          }
        }

        e.Change = change;
      }
    }

    private void RemoveShape(Shape shape)
    {
      if (shape.IsActive)
      {
        ActiveElement = null;
      }
      if (shape.IsSelected)
      {
        SelectedShapeCount--;
        OnSelectionChanged(EventArgs.Empty);
        OnClipboardAvailabilityChanged(EventArgs.Empty);
        OnStatusChanged(EventArgs.Empty);
      }
      shape.Diagram = null;
      shape.BeginUndoableOperation -= new EventHandler(OnBeginUndoableOperation);
      shape.Modified -= new EventHandler(Element_Modified);
      shape.Activating -= new EventHandler(Element_Activating);
      shape.Dragging -= new MoveEventHandler(Shape_Dragging);
      shape.Resizing -= new ResizeEventHandler(Shape_Resizing);
      shape.SelectionChanged -= new EventHandler(Shape_SelectionChanged);
      ShapeList.Remove(shape);
      RecalculateSize();
    }

    private Shape GetShape(IEntity entity)
    {
      foreach (var shape in ShapeList)
      {
        if (shape.Entity == entity)
        {
          return shape;
        }
      }
      return null;
    }

    private Connection GetConnection(Relationship relationship)
    {
      foreach (var connection in ConnectionList)
      {
        if (connection.Relationship == relationship)
        {
          return connection;
        }
      }
      return null;
    }

    private void AddConnection(Connection connection)
    {
      connection.Diagram = this;
      connection.BeginUndoableOperation += new EventHandler(OnBeginUndoableOperation);
      connection.Modified += new EventHandler(Element_Modified);
      connection.Activating += new EventHandler(Element_Activating);
      connection.SelectionChanged += new EventHandler(Connection_SelectionChanged);
      connection.RouteChanged += new EventHandler(Connection_RouteChanged);
      connection.BendPointMove += new BendPointEventHandler(Connection_BendPointMove);
      ConnectionList.AddFirst(connection);
      RecalculateSize();
    }

    private void RemoveConnection(Connection connection)
    {
      if (connection.IsSelected)
      {
        SelectedConnectionCount--;
        OnSelectionChanged(EventArgs.Empty);
        OnClipboardAvailabilityChanged(EventArgs.Empty);
        OnStatusChanged(EventArgs.Empty);
      }
      connection.Diagram = null;
      connection.BeginUndoableOperation -= new EventHandler(OnBeginUndoableOperation);
      connection.Modified -= new EventHandler(Element_Modified);
      connection.Activating += new EventHandler(Element_Activating);
      connection.SelectionChanged -= new EventHandler(Connection_SelectionChanged);
      connection.RouteChanged -= new EventHandler(Connection_RouteChanged);
      connection.BendPointMove -= new BendPointEventHandler(Connection_BendPointMove);
      ConnectionList.Remove(connection);
      RecalculateSize();
    }

    private void Shape_SelectionChanged(object sender, EventArgs e)
    {
      if (!_selectioning)
      {
        var shape = (Shape)sender;

        if (shape.IsSelected)
        {
          SelectedShapeCount++;
          ShapeList.ShiftToFirstPlace(shape);
        }
        else
        {
          SelectedShapeCount--;
        }

        OnSelectionChanged(EventArgs.Empty);
        OnClipboardAvailabilityChanged(EventArgs.Empty);
        OnStatusChanged(EventArgs.Empty);
      }
    }

    private void Connection_SelectionChanged(object sender, EventArgs e)
    {
      if (!_selectioning)
      {
        var connection = (Connection)sender;

        if (connection.IsSelected)
        {
          SelectedConnectionCount++;
          ConnectionList.ShiftToFirstPlace(connection);
        }
        else
        {
          SelectedConnectionCount--;
        }

        OnSelectionChanged(EventArgs.Empty);
        OnClipboardAvailabilityChanged(EventArgs.Empty);
        OnStatusChanged(EventArgs.Empty);
      }
    }

    private void Connection_RouteChanged(object sender, EventArgs e)
    {
      var connection = (Connection)sender;
      connection.ValidatePosition(DiagramPadding);

      RecalculateSize();
    }

    private void Connection_BendPointMove(object sender, BendPointEventArgs e)
    {
      if (e.BendPoint.X < DiagramPadding)
      {
        e.BendPoint.X = DiagramPadding;
      }
      if (e.BendPoint.Y < DiagramPadding)
      {
        e.BendPoint.Y = DiagramPadding;
      }

      // Snap bend points to others if possible
      if (Settings.Default.UsePrecisionSnapping && Control.ModifierKeys != Keys.Shift)
      {
        foreach (var connection in ConnectionList.GetSelectedElements())
        {
          foreach (var point in connection.BendPoints)
          {
            if (point != e.BendPoint && !point.AutoPosition)
            {
              var xDist = Math.Abs(e.BendPoint.X - point.X);
              var yDist = Math.Abs(e.BendPoint.Y - point.Y);

              if (xDist <= Connection.PrecisionSize)
              {
                e.BendPoint.X = point.X;
              }
              if (yDist <= Connection.PrecisionSize)
              {
                e.BendPoint.Y = point.Y;
              }
            }
          }
        }
      }
    }

    public void CreateShape()
    {
      CreateShape(_newShapeType);
    }

    public void CreateShape(EntityType type)
    {
      _state = Mode.CreatingShape;
      _shapeType = type;
      _newShapeType = type;

      switch (type)
      {
        case EntityType.Class:
        case EntityType.Delegate:
        case EntityType.Enum:
        case EntityType.Interface:
        case EntityType.Structure:
        case EntityType.State:
          _shapeOutline = TypeShape.GetOutline(Style.CurrentStyle);
          break;

        case EntityType.Comment:
          _shapeOutline = CommentShape.GetOutline(Style.CurrentStyle);
          break;

        default:
          throw new ArgumentOutOfRangeException($"Unknown EntityType: {type}");
      }
      _shapeOutline.Location = new Point((int)_mouseLocation.X, (int)_mouseLocation.Y);
      Redraw();
    }

    public Shape AddShape(EntityType type)
    {
      switch (type)
      {
        case EntityType.Class:
          AddClass();
          break;

        case EntityType.Comment:
          AddComment();
          break;

        case EntityType.Delegate:
          AddDelegate();
          break;

        case EntityType.Enum:
          AddEnum();
          break;

        case EntityType.Interface:
          AddInterface();
          break;

        case EntityType.Structure:
          AddStructure();
          break;

        case EntityType.State:
          AddState();
          break;

        default:
          throw new ArgumentOutOfRangeException($"Unknown EntityType: {type}");
      }

      RecalculateSize();
      return ShapeList.FirstValue;
    }

    protected override void AddClass(ClassType newClass)
    {
      base.AddClass(newClass);
      AddShape(new ClassShape(newClass));
    }

    protected override void AddStructure(StructureType structure)
    {
      base.AddStructure(structure);
      AddShape(new StructureShape(structure));
    }

    protected override void AddInterface(InterfaceType newInterface)
    {
      base.AddInterface(newInterface);
      AddShape(new InterfaceShape(newInterface));
    }

    protected override void AddEnum(EnumType newEnum)
    {
      base.AddEnum(newEnum);
      AddShape(new EnumShape(newEnum));
    }

    protected override void AddDelegate(DelegateType newDelegate)
    {
      base.AddDelegate(newDelegate);
      AddShape(new DelegateShape(newDelegate));
    }

    protected override void AddComment(Comment comment)
    {
      base.AddComment(comment);
      AddShape(new CommentShape(comment));
    }

    protected override void AddState(State state)
    {
      base.AddState(state);
      AddShape(new StateShape(state));
    }

    public void CreateConnection(RelationshipType type)
    {
      _connectionCreator = new ConnectionCreator(this, type);
      _state = Mode.CreatingConnection;
    }

    protected override void AddAssociation(AssociationRelationship association)
    {
      base.AddAssociation(association);

      var startShape = GetShape(association.First);
      var endShape = GetShape(association.Second);
      AddConnection(new Association(association, startShape, endShape));
    }

    protected override void AddGeneralization(GeneralizationRelationship generalization)
    {
      base.AddGeneralization(generalization);

      var startShape = GetShape(generalization.First);
      var endShape = GetShape(generalization.Second);
      AddConnection(new Generalization(generalization, startShape, endShape));
    }

    protected override void AddRealization(RealizationRelationship realization)
    {
      base.AddRealization(realization);

      var startShape = GetShape(realization.First);
      var endShape = GetShape(realization.Second);
      AddConnection(new Realization(realization, startShape, endShape));
    }

    protected override void AddDependency(DependencyRelationship dependency)
    {
      base.AddDependency(dependency);

      var startShape = GetShape(dependency.First);
      var endShape = GetShape(dependency.Second);
      AddConnection(new Dependency(dependency, startShape, endShape));
    }

    protected override void AddNesting(NestingRelationship nesting)
    {
      base.AddNesting(nesting);

      var startShape = GetShape(nesting.First);
      var endShape = GetShape(nesting.Second);
      AddConnection(new Nesting(nesting, startShape, endShape));
    }

    protected override void AddCommentRelationship(CommentRelationship commentRelationship)
    {
      base.AddCommentRelationship(commentRelationship);

      var startShape = GetShape(commentRelationship.First);
      var endShape = GetShape(commentRelationship.Second);
      AddConnection(new CommentConnection(commentRelationship, startShape, endShape));
    }

    protected override void AddEntityRelationship(EntityRelationship dependency)
    {
      base.AddEntityRelationship(dependency);

      var startShape = GetShape(dependency.First);
      var endShape = GetShape(dependency.Second);
      AddConnection(new EntityRelationshipConnection(dependency, startShape, endShape));
    }

    protected override void AddTransitionRelationship(Transition trans)
    {
      base.AddTransitionRelationship(trans);

      var startShape = GetShape(trans.First);
      var endShape = GetShape(trans.Second);
      AddConnection(new TransitionConnection(trans, startShape, endShape));
    }

    protected override void AddSourceSinkRelationship(SourceSinkRelationship trans)
    {
      base.AddSourceSinkRelationship(trans);

      var startShape = GetShape(trans.First);
      var endShape = GetShape(trans.Second);
      AddConnection(new SourceSinkConnection(trans, startShape, endShape));
    }

    protected override void OnEntityRemoved(EntityEventArgs e)
    {
      var shape = GetShape(e.Entity);
      RemoveShape(shape);

      base.OnEntityRemoved(e);
    }

    protected override void OnRelationRemoved(RelationshipEventArgs e)
    {
      var connection = GetConnection(e.Relationship);
      RemoveConnection(connection);

      base.OnRelationRemoved(e);
    }

    private void OnOffsetChanged(EventArgs e)
    {
      OffsetChanged?.Invoke(this, e);
      UpdateWindowPosition();
    }

    private void OnSizeChanged(EventArgs e)
    {
      SizeChanged?.Invoke(this, e);
    }

    private void OnZoomChanged(EventArgs e)
    {
      ZoomChanged?.Invoke(this, e);
      CloseWindows();
    }

    private void OnStatusChanged(EventArgs e)
    {
      StatusChanged?.Invoke(this, e);
    }

    private void OnSelectionChanged(EventArgs e)
    {
      SelectionChanged?.Invoke(this, e);
    }

    private void OnNeedsRedraw(EventArgs e)
    {
      NeedsRedraw?.Invoke(this, e);
    }

    private void OnClipboardAvailabilityChanged(EventArgs e)
    {
      ClipboardAvailabilityChanged?.Invoke(this, e);
    }

    private void OnShowingWindow(PopupWindowEventArgs e)
    {
      ShowingWindow?.Invoke(this, e);
    }

    private void OnHidingWindow(PopupWindowEventArgs e)
    {
      HidingWindow?.Invoke(this, e);
    }

    public bool CanUndo
    {
      get
      {
        return UndoModels.Any();
      }
    }

    public bool CanRedo
    {
      get
      {
        // TODO   CanRedo
        return false;
      }
    }

    public void Undo()
    {
      if (!CanUndo)
      {
        return;
      }

      Reset();

      var xmlElem = UndoModels.Pop();
      Deserialize(xmlElem);

      // fire event so UI updates
      OnBeginUndoableOperation(EventArgs.Empty);
    }

    public void Redo()
    {
      if (!CanRedo)
      {
        return;
      }

      throw new NotImplementedException();
    }

    protected override void Reset()
    {
      base.Reset();

      ConnectionList.Clear();
      ShapeList.Clear();
    }
  }
}
