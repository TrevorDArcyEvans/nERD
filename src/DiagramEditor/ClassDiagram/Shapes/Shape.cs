// NClass - Free class diagram editor
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
using NClass.DiagramEditor.ClassDiagram.ContextMenus;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

namespace NClass.DiagramEditor.ClassDiagram.Shapes
{
  public abstract class Shape : DiagramElement
  {
    public event MoveEventHandler Move;
    public event MoveEventHandler Dragging;
    public event ResizeEventHandler Resizing;
    public event ResizeEventHandler Resize;

    protected enum ResizeMode
    {
      None = 0,
      Right = 1,
      Bottom = 2
    }

    public const int SelectionMargin = 12;

    private static readonly Pen SelectionSquarePen = new Pen(Color.Black);
    protected static readonly float[] BorderDashPattern = new float[] { 3, 3 };
    protected static readonly SolidBrush ShadowBrush = new SolidBrush(Color.Gray);
    protected static readonly Size DefaultMinSize = new Size(50, 50);

    private ResizeMode _resizeMode = ResizeMode.None;
    private PointF _mouseDownLocation = PointF.Empty;
    private bool _mouseLeaved = true;
    private Cursor _cursor = Cursors.Default;

    protected Shape(IEntity entity)
    {
      if (entity == null)
      {
        throw new ArgumentNullException("entity");
      }

      _location = Point.Empty;
      _size = DefaultSize;

      entity.Modified += delegate { OnModified(EventArgs.Empty); };

      entity.Serializing += delegate (object sender, SerializeEventArgs e)
      {
        OnSerializing(e);
      };
      entity.Deserializing += delegate (object sender, SerializeEventArgs e)
      {
        OnDeserializing(e);
      };
    }

    protected abstract Size DefaultSize
    {
      get;
    }

    private Size _minimumSize = DefaultMinSize;
    protected Size MinimumSize
    {
      get
      {
        return _minimumSize;
      }
      set
      {
        _minimumSize = value;
        if (_minimumSize.Width > _size.Width)
        {
          Width = value.Width;
        }
        if (_minimumSize.Height > _size.Height)
        {
          Height = value.Height;
        }
      }
    }

    public abstract IEntity Entity
    {
      get;
    }

    private Point _location;
    public Point Location
    {
      get
      {
        return _location;
      }
      set
      {
        if (_location != value)
        {
          Size offset = new Size(value.X - X, value.Y - Y);
          _mouseDownLocation += offset;
          _location = value;
          OnMove(new MoveEventArgs(offset));
          OnModified(EventArgs.Empty);
        }
      }
    }

    public int X
    {
      get
      {
        return _location.X;
      }
      set
      {
        if (_location.X != value)
        {
          Size offset = new Size(value - X, 0);
          _mouseDownLocation.X += offset.Width;
          _location.X = value;
          OnMove(new MoveEventArgs(offset));
          OnModified(EventArgs.Empty);
        }
      }
    }

    public int Y
    {
      get
      {
        return _location.Y;
      }
      set
      {
        if (_location.Y != value)
        {
          Size offset = new Size(0, value - Y);
          _mouseDownLocation.Y += offset.Height;
          _location.Y = value;
          OnMove(new MoveEventArgs(offset));
          OnModified(EventArgs.Empty);
        }
      }
    }

    private Size _size;
    public virtual Size Size
    {
      get
      {
        return _size;
      }
      set
      {
        if (value.Width < MinimumSize.Width)
        {
          value.Width = MinimumSize.Width;
        }
        if (value.Height < MinimumSize.Height)
        {
          value.Height = MinimumSize.Height;
        }

        if (_size != value)
        {
          Size change = new Size(value.Width - Width, value.Height - Height);
          if (IsResizing)
          {
            _mouseDownLocation += change;
          }
          _size = value;
          OnResize(new ResizeEventArgs(change));
          OnModified(EventArgs.Empty);
        }
      }
    }

    public virtual int Width
    {
      get
      {
        return _size.Width;
      }
      set
      {
        if (value < MinimumSize.Width)
        {
          value = MinimumSize.Width;
        }

        if (_size.Width != value)
        {
          Size change = new Size(value - Width, 0);
          if (IsResizing)
          {
            _mouseDownLocation.X += change.Width;
          }
          _size.Width = value;
          OnResize(new ResizeEventArgs(change));
          OnModified(EventArgs.Empty);
        }
      }
    }

    public virtual int Height
    {
      get
      {
        return _size.Height;
      }
      set
      {
        if (value < MinimumSize.Height)
        {
          value = MinimumSize.Height;
        }

        if (_size.Height != value)
        {
          Size change = new Size(0, value - Height);
          if (IsResizing)
          {
            _mouseDownLocation.Y += change.Height;
          }
          _size.Height = value;
          OnResize(new ResizeEventArgs(change));
          OnModified(EventArgs.Empty);
        }
      }
    }

    public int Left
    {
      get { return X; }
      set { X = value; }
    }

    public int Right
    {
      get { return X + Width; }
      set { X = value - Width; }
    }

    public int Top
    {
      get { return Y; }
      set { Y = value; }
    }

    public int Bottom
    {
      get { return Y + Height; }
      set { Y = value - Height; }
    }

    public Rectangle BorderRectangle
    {
      get { return new Rectangle(Location, Size); }
    }

    public Point CenterPoint
    {
      get { return new Point(HorizontalCenter, VerticalCenter); }
    }

    public int HorizontalCenter
    {
      get
      {
        return (Left + Right) / 2;
      }
    }

    public int VerticalCenter
    {
      get
      {
        return (Top + Bottom) / 2;
      }
    }

    protected abstract int GetBorderWidth(Style style);

    protected override RectangleF CalculateDrawingArea(Style style, bool printing, float zoom)
    {
      RectangleF area = BorderRectangle;

      if (printing || !IsSelected)
      {
        float borderSize = (float)GetBorderWidth(style) / 2;
        area.Inflate(borderSize, borderSize);

        if (style.ShadowOffset.Width > borderSize)
        {
          area.Width += style.ShadowOffset.Width - borderSize;
        }
        if (style.ShadowOffset.Height > borderSize)
        {
          area.Height += style.ShadowOffset.Height - borderSize;
        }

        return area;
      }
      else
      {
        float borderSize = (float)GetBorderWidth(style) / 2;
        float selectionSize = SelectionMargin / zoom;
        float inflation = Math.Max(borderSize, selectionSize);

        area.Inflate(inflation, inflation);
        return area;
      }
    }

    internal bool IsResizing
    {
      get { return (_resizeMode != ResizeMode.None); }
    }

    public virtual void Collapse()
    {
    }

    public virtual void Expand()
    {
    }

    protected virtual ResizeMode GetResizeMode(AbsoluteMouseEventArgs e)
    {
      if (e.Zoom <= UndreadableZoom)
      {
        return ResizeMode.None;
      }

      ResizeMode mode = ResizeMode.None;
      float squareSize = SelectionMargin / e.Zoom;
      int horCenter = HorizontalCenter;
      int verCenter = VerticalCenter;

      bool left = (e.X >= Left - squareSize && e.X < Left);
      bool top = (e.Y >= Top - squareSize && e.Y < Top);
      bool right = (e.X > Right && e.X < Right + squareSize);
      bool bottom = (e.Y > Bottom && e.Y < Bottom + squareSize);
      bool center = (e.X >= horCenter - squareSize / 2 &&
        e.X < horCenter + squareSize / 2);
      bool middle = (e.Y >= verCenter - squareSize / 2 &&
        e.Y < verCenter + squareSize / 2);

      if (right && (top || middle || bottom))
      {
        mode |= ResizeMode.Right;
      }

      if (bottom && (left || center || right))
      {
        mode |= ResizeMode.Bottom;
      }

      return mode;
    }

    public Cursor GetCursor(AbsoluteMouseEventArgs e)
    {
      ResizeMode mode = GetResizeMode(e);

      switch (mode)
      {
        case ResizeMode.Bottom:
          return Cursors.SizeNS;

        case ResizeMode.Right:
          return Cursors.SizeWE;

        case ResizeMode.Bottom | ResizeMode.Right:
          return Cursors.SizeNWSE;

        default:
          return Cursors.Default;
      }
    }

    protected internal sealed override Rectangle GetLogicalArea()
    {
      return BorderRectangle;
    }

    protected static Rectangle TransformRelativeToAbsolute(Rectangle rectangle,
      float zoom, Point offset)
    {
      rectangle = Rectangle.FromLTRB(
        (int)(rectangle.Left * zoom),
        (int)(rectangle.Top * zoom),
        (int)Math.Ceiling(rectangle.Right * zoom),
        (int)Math.Ceiling(rectangle.Bottom * zoom));
      rectangle.Offset(-offset.X, -offset.Y);

      return rectangle;
    }

    protected internal override void DrawSelectionLines(IGraphics g, float zoom, Point offset)
    {
      if (IsSelected)
      {
        if (zoom > UndreadableZoom)
        {
          // Draw selection border and resizing squares
          Rectangle frame = TransformRelativeToAbsolute(BorderRectangle, zoom, offset);
          int borderOffset = SelectionMargin / 2;
          frame.Inflate(borderOffset, borderOffset);

          g.DrawRectangle(Diagram.SelectionPen, frame);
          DrawResizingSquares(g, frame);
        }
        else
        {
          // Draw only selection border
          const int BorderOffset = 2;
          Rectangle frame = TransformRelativeToAbsolute(BorderRectangle, zoom, offset);
          frame.Inflate(BorderOffset, BorderOffset);

          g.DrawRectangle(Diagram.SelectionPen, frame);
        }
      }
    }

    private void DrawResizingSquares(IGraphics g, Rectangle frame)
    {
      int squareSize = (SelectionMargin - 4);

      for (int row = 0; row < 3; row++)
      {
        for (int column = 0; column < 3; column++)
        {
          if (row != 1 || column != 1) // It's not the center point
          {
            int x = frame.X + (frame.Width * column / 2) - squareSize / 2;
            int y = frame.Y + (frame.Height * row / 2) - squareSize / 2;
            var rect = new Rectangle(x, y, squareSize, squareSize);

            g.FillRectangle(Brushes.White, rect);
            g.DrawRectangle(SelectionSquarePen, rect);
          }
        }
      }
    }

    protected internal sealed override bool TrySelect(RectangleF frame)
    {
      if (frame.IntersectsWith(BorderRectangle))
      {
        IsSelected = true;
        return true;
      }
      else
      {
        return false;
      }
    }

    protected internal sealed override void Offset(Size offset)
    {
      Location += offset;
    }

    protected internal override Size GetMaximalOffset(Size offset, int padding)
    {
      if (IsSelected)
      {
        Rectangle newPosition = BorderRectangle;
        newPosition.Offset(offset.Width, offset.Height);

        if (newPosition.Left < padding)
        {
          offset.Width += (padding - newPosition.Left);
        }
        if (newPosition.Top < padding)
        {
          offset.Height += (padding - newPosition.Top);
        }
      }
      return offset;
    }

    protected internal bool Contains(PointF point)
    {
      return (
        point.X >= Left && point.X <= Right &&
        point.Y >= Top && point.Y <= Bottom
      );
    }

    internal void AutoWidth()
    {
      if (Graphics != null)
      {
        Width = (int)GetRequiredWidth(Graphics, Style.CurrentStyle) + 1;
      }
    }

    protected virtual float GetRequiredWidth(IGraphics g, Style style)
    {
      return MinimumSize.Width;
    }

    internal void AutoHeight()
    {
      Height = GetRequiredHeight();
    }

    protected virtual int GetRequiredHeight()
    {
      return MinimumSize.Height;
    }

    protected internal override IEnumerable<ToolStripItem> GetContextMenuItems(Diagram diagram)
    {
      return ShapeContextMenu.Default.GetMenuItems(diagram);
    }

    protected internal Shape Paste(Diagram diagram, Size offset)
    {
      if (CloneEntity(diagram))
      {
        Shape shape = diagram.ShapeList.FirstValue;
        shape.Location = new Point(offset);
        shape.Size = Size;
        shape.IsSelected = true;
        return shape;
      }
      else
      {
        return null;
      }
    }

    protected abstract bool CloneEntity(Diagram diagram);

    [Obsolete]
    protected internal sealed override void Serialize(XmlElement node)
    {
      OnSerializing(new SerializeEventArgs(node));
    }

    [Obsolete]
    protected internal sealed override void Deserialize(XmlElement node)
    {
      OnDeserializing(new SerializeEventArgs(node));
    }

    protected virtual void OnSerializing(SerializeEventArgs e)
    {
      XmlElement locationNode = e.Node.OwnerDocument.CreateElement("Location");
      locationNode.SetAttribute("left", Left.ToString());
      locationNode.SetAttribute("top", Top.ToString());
      e.Node.AppendChild(locationNode);

      XmlElement sizeNode = e.Node.OwnerDocument.CreateElement("Size");
      sizeNode.SetAttribute("width", Width.ToString());
      sizeNode.SetAttribute("height", Height.ToString());
      e.Node.AppendChild(sizeNode);
    }

    protected virtual void OnDeserializing(SerializeEventArgs e)
    {
      XmlElement locationNode = e.Node["Location"];
      if (locationNode != null)
      {
        int.TryParse(locationNode.GetAttribute("left"), out int left);
        int.TryParse(locationNode.GetAttribute("top"), out int top);
        Location = new Point(left, top);
      }

      XmlElement sizeNode = e.Node["Size"];
      if (sizeNode != null)
      {
        int.TryParse(sizeNode.GetAttribute("width"), out int width);
        int.TryParse(sizeNode.GetAttribute("height"), out int height);
        Size = new Size(width, height);
      }
    }

    internal sealed override void MousePressed(AbsoluteMouseEventArgs e)
    {
      if (!e.Handled)
      {
        bool pressed = Contains(e.Location);

        if (e.Button == MouseButtons.Left)
        {
          pressed |= (IsSelected && GetResizeMode(e) != ResizeMode.None);
        }

        if (pressed)
        {
          e.Handled = true;
          var oldResizing = IsResizing;
          _resizeMode = GetResizeMode(e);
          if (oldResizing != IsResizing)
          {
            _currentOperation = DiagramElementOperation.Resizing;
            OnBeginUndoableOperation(EventArgs.Empty);
          }
          Cursor.Current = _cursor;
          OnMouseDown(e);
        }
      }
    }

    internal sealed override void MouseMoved(AbsoluteMouseEventArgs e)
    {
      if (!e.Handled)
      {
        bool contains = IsResizing;

        if (!IsResizing)
        {
          contains = Contains(e.Location);

          if (Contains(e.Location) && _mouseLeaved)
          {
            OnMouseEnter(EventArgs.Empty);
          }
          else if (!Contains(e.Location) && !_mouseLeaved)
          {
            OnMouseLeave(EventArgs.Empty);
          }

          contains |= (IsSelected && GetResizeMode(e) != ResizeMode.None);
        }

        if (IsMousePressed || contains)
        {
          e.Handled = true;
          if (IsMousePressed)
          {
            Cursor.Current = _cursor;
          }
          else
          {
            _cursor = GetCursor(e);
            Cursor.Current = _cursor;
          }
          OnMouseMove(e);
        }
      }
      else // Already handled
      {
        if (!_mouseLeaved)
        {
          OnMouseLeave(EventArgs.Empty);
        }
      }
    }

    internal sealed override void MouseUpped(AbsoluteMouseEventArgs e)
    {
      if (!e.Handled)
      {
        bool upped = IsMousePressed;

        if (upped)
        {
          e.Handled = true;
          OnMouseUp(e);
        }
      }
    }

    internal sealed override void DoubleClicked(AbsoluteMouseEventArgs e)
    {
      if (!e.Handled)
      {
        bool doubleClicked = Contains(e.Location);

        if (e.Button == MouseButtons.Left)
        {
          doubleClicked |= (IsSelected && GetResizeMode(e) != ResizeMode.None);
        }

        if (doubleClicked)
        {
          e.Handled = true;
          OnDoubleClick(e);
        }
      }
    }

    private void PerformResize(PointF mouseLocation)
    {
      Size change = Size.Empty;

      if ((_resizeMode & ResizeMode.Right) != 0)
      {
        int offset = (int)(mouseLocation.X - _mouseDownLocation.X);
        change.Width += offset;
      }
      if ((_resizeMode & ResizeMode.Bottom) != 0)
      {
        int offset = (int)(mouseLocation.Y - _mouseDownLocation.Y);
        change.Height += offset;
      }

      ResizeEventArgs e = new ResizeEventArgs(change);
      OnResizing(e);
      Size += e.Change;
    }

    protected virtual void CopyFrom(Shape shape)
    {
      _location = shape._location;
      _size = shape._size;
    }

    protected override void OnDeactivating(EventArgs e)
    {
      base.OnDeactivated(e);
      HideEditor();
    }

    protected override void OnMouseDown(AbsoluteMouseEventArgs e)
    {
      _mouseDownLocation = e.Location;
      base.OnMouseDown(e);
    }

    protected override void OnMouseMove(AbsoluteMouseEventArgs e)
    {
      base.OnMouseMove(e);

      if (IsResizing)
      {
        PerformResize(e.Location);
      }
      else if (IsMousePressed && e.Button == MouseButtons.Left)
      {
        Size offset = new Size(
          (int)(e.X - _mouseDownLocation.X),
          (int)(e.Y - _mouseDownLocation.Y));

        if (_currentOperation == DiagramElementOperation.None)
        {
          _currentOperation = DiagramElementOperation.Dragging;
          OnBeginUndoableOperation(EventArgs.Empty);
        }

        OnDragging(new MoveEventArgs(offset));
      }
    }

    protected override void OnMouseUp(AbsoluteMouseEventArgs e)
    {
      base.OnMouseUp(e);
      _resizeMode = ResizeMode.None;
      _currentOperation = DiagramElementOperation.None;
    }

    protected override void OnDoubleClick(AbsoluteMouseEventArgs e)
    {
      base.OnDoubleClick(e);
      ResizeMode resizeMode = GetResizeMode(e);

      if ((resizeMode & ResizeMode.Right) != 0)
      {
        AutoWidth();
      }
      if ((resizeMode & ResizeMode.Bottom) != 0)
      {
        AutoHeight();
      }
    }

    protected virtual void OnMove(MoveEventArgs e)
    {
      Move?.Invoke(this, e);
    }

    protected virtual void OnDragging(MoveEventArgs e)
    {
      Dragging?.Invoke(this, e);
    }

    protected virtual void OnResizing(ResizeEventArgs e)
    {
      Resizing?.Invoke(this, e);
    }

    protected virtual void OnResize(ResizeEventArgs e)
    {
      Resize?.Invoke(this, e);
    }

    protected virtual void OnMouseEnter(EventArgs e)
    {
      _mouseLeaved = false;
    }

    protected virtual void OnMouseLeave(EventArgs e)
    {
      _mouseLeaved = true;
    }

    public override string ToString()
    {
      return Entity.ToString();
    }
  }
}
