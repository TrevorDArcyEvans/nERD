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
using NClass.DiagramEditor.ClassDiagram.Dialogs;
using NClass.DiagramEditor.ClassDiagram.Editors;
using NClass.Translations;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace NClass.DiagramEditor.ClassDiagram.Shapes
{
  internal sealed class CommentShape : Shape
  {
    private const int PaddingSize = 10;
    private const int DefaultWidth = 160;
    private const int DefaultHeight = 75;
    private static readonly Pen BorderPen = new Pen(Color.Black);
    private static readonly SolidBrush BackgroundBrush = new SolidBrush(Color.White);
    private static readonly SolidBrush TextBrush = new SolidBrush(Color.Black);
    private static readonly StringFormat Format = new StringFormat(StringFormat.GenericTypographic)
    {
      Trimming = StringTrimming.EllipsisWord,
      FormatFlags = StringFormatFlags.LineLimit
    };
    private static readonly CommentEditor _editor = new CommentEditor();

    private bool _editorShowed = false;

    internal CommentShape(Comment comment) :
      base(comment)
    {
      Comment = comment;
    }

    public override IEntity Entity
    {
      get { return Comment; }
    }

    public Comment Comment { get; }

    public string Text
    {
      get { return Comment.Text; }
      set { Comment.Text = value; }
    }

    protected override Size DefaultSize
    {
      get
      {
        return new Size(DefaultWidth, DefaultHeight);
      }
    }

    protected override bool CloneEntity(Diagram diagram)
    {
      return diagram.InsertComment(Comment.Clone());
    }

    public static Rectangle GetOutline(Style style)
    {
      return new Rectangle(0, 0, DefaultWidth, DefaultHeight);
    }

    protected override int GetBorderWidth(Style style)
    {
      return style.CommentBorderWidth;
    }

    internal Rectangle GetTextRectangle()
    {
      return new Rectangle(
        Left + PaddingSize, Top + PaddingSize,
        Width - 2 * PaddingSize, Height - 2 * PaddingSize
      );
    }

    protected override void OnMove(MoveEventArgs e)
    {
      base.OnMove(e);
      HideEditor();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
      base.OnResize(e);
      if (_editorShowed)
      {
        _editor.Relocate(this);
        if (!_editor.Focused)
          _editor.Focus();
      }
    }

    protected override void OnMouseDown(AbsoluteMouseEventArgs e)
    {
      if (e.Button == MouseButtons.Left)
      {
        IsActive = true;
      }
      base.OnMouseDown(e);
    }

    protected override void OnDoubleClick(AbsoluteMouseEventArgs e)
    {
      if (Contains(e.Location) && e.Button == MouseButtons.Left)
        ShowEditor();
    }

    protected internal override void ShowEditor()
    {
      if (!_editorShowed)
      {
        _editor.Relocate(this);
        _editor.Init(this);
        ShowWindow(_editor);
        _editor.Focus();
        _editorShowed = true;
      }
    }

    protected internal override void HideEditor()
    {
      if (_editorShowed)
      {
        HideWindow(_editor);
        _editorShowed = false;
      }
    }

    protected internal override void MoveWindow()
    {
      HideEditor();
    }

    internal void EditText()
    {
      using (EditCommentDialog dialog = new EditCommentDialog(Text))
      {
        if (dialog.ShowDialog() == DialogResult.OK)
          Text = dialog.InputText;
      }
    }

    protected internal override IEnumerable<ToolStripItem> GetContextMenuItems(Diagram diagram)
    {
      return CommentShapeContextMenu.Default.GetMenuItems(diagram);
    }

    private void DrawSurface(IGraphics g, bool onScreen, Style style)
    {
      // Update graphical objects
      BackgroundBrush.Color = style.CommentBackColor;
      BorderPen.Color = style.CommentBorderColor;
      BorderPen.Width = style.CommentBorderWidth;
      if (style.IsCommentBorderDashed)
        BorderPen.DashPattern = BorderDashPattern;
      else
        BorderPen.DashStyle = DashStyle.Solid;

      // Create shape pattern
      GraphicsPath path = new GraphicsPath();
      path.AddLine(Left, Top, Right - PaddingSize, Top);
      path.AddLine(Right, Top + PaddingSize, Right, Bottom);
      path.AddLine(Right, Bottom, Left, Bottom);
      path.CloseFigure();

      // Draw shadow first
      if ((!onScreen || !IsSelected) && !style.ShadowOffset.IsEmpty)
      {
        ShadowBrush.Color = style.ShadowColor;
        g.TranslateTransform(style.ShadowOffset.Width, style.ShadowOffset.Height);
        g.FillPath(ShadowBrush, path);
        g.TranslateTransform(-style.ShadowOffset.Width, -style.ShadowOffset.Height);
      }

      // Draw borders & background
      g.FillPath(BackgroundBrush, path);
      g.DrawPath(BorderPen, path);

      // Draw earmark
      path.Reset();
      path.AddLine(Right - PaddingSize, Top, Right - PaddingSize, Top + PaddingSize);
      path.AddLine(Right - PaddingSize, Top + PaddingSize, Right, Top + PaddingSize);
      g.DrawPath(BorderPen, path);

      path.Dispose();
    }

    private void DrawText(IGraphics g, bool onScreen, Style style)
    {
      Rectangle textBounds = GetTextRectangle();

      if (string.IsNullOrEmpty(Text) && onScreen)
      {
        TextBrush.Color = Color.FromArgb(128, style.CommentTextColor);
        g.DrawString(Strings.DoubleClickToEdit,
          style.CommentFont, TextBrush, textBounds, Format);
      }
      else
      {
        TextBrush.Color = style.CommentTextColor;
        g.DrawString(Text, style.CommentFont, TextBrush, textBounds, Format);
      }
    }

    public override void Draw(IGraphics g, bool onScreen, Style style)
    {
      DrawSurface(g, onScreen, style);
      DrawText(g, onScreen, style);
    }

    protected override float GetRequiredWidth(IGraphics g, Style style)
    {
      return Width;
    }

    protected override int GetRequiredHeight()
    {
      return Height;
    }

    public override string ToString()
    {
      return Strings.Comment;
    }
  }
}
