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

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace NClass.DiagramEditor
{
  public sealed class GdiGraphics : IGraphics
  {
    private readonly Graphics _graphics;

    public GdiGraphics(Graphics graphics)
    {
      _graphics = graphics ?? throw new ArgumentNullException("graphics");
    }

    public Region Clip
    {
      get { return _graphics.Clip; }
      set { _graphics.Clip = value; }
    }

    public RectangleF ClipBounds
    {
      get
      {
        if (!MonoHelper.IsRunningOnMono)
        {
          return _graphics.ClipBounds;
        }

        // START_HACK
        // There are memory issues dating back to 2008 (!) when running under Mono on Linux:
        //
        //    [Mono-list] OutOfMemoryException after scale transform a Region
        //    https://mono.github.io/mail-archives/mono-list/2008-September/039693.html
        //
        // This typically occurs when running full screen on a high resolution monitor and will result
        // in an exception with an error message like:
        //
        // **(mono: 29613): WARNING * *: 14:41:06.763: Path conversion requested 12110400 bytes(2320 x 1305).Maximum size is 8388608 bytes.
        // System.OutOfMemoryException: Not enough memory to complete operation[GDI + status: OutOfMemory]
        //   at System.Drawing.GDIPlus.CheckStatus(System.Drawing.Status status)[0x000b7] in < 5e8677e298c34b8b9ba421a545129d69 >:0
        //   at System.Drawing.Graphics.get_ClipBounds()[0x00015] in < 5e8677e298c34b8b9ba421a545129d69 >:0
        //   at(wrapper remoting - invoke - with - check) System.Drawing.Graphics.get_ClipBounds()
        //   at NClass.DiagramEditor.GdiGraphics.get_ClipBounds()[0x00001] in < 64616348232f476fac04199c6feee710 >:0
        //   at NClass.DiagramEditor.ClassDiagram.Diagram.Display(NClass.DiagramEditor.IGraphics g)[0x00001] in < 64616348232f476fac04199c6feee710 >:0
        //   at NClass.DiagramEditor.Canvas.DrawContent(NClass.DiagramEditor.IGraphics g)[0x000e7] in < 64616348232f476fac04199c6feee710 >:0
        //   at NClass.DiagramEditor.Canvas.OnPaint(System.Windows.Forms.PaintEventArgs e)[0x00020] in < 64616348232f476fac04199c6feee710 >:0
        //   at System.Windows.Forms.Control.WmPaint(System.Windows.Forms.Message & m)[0x0007b] in < 5bcdf2a3746f4353acffcd001a2c6899 >:0
        //   at System.Windows.Forms.Control.WndProc(System.Windows.Forms.Message & m)[0x001a4] in < 5bcdf2a3746f4353acffcd001a2c6899 >:0
        //   at System.Windows.Forms.ScrollableControl.WndProc(System.Windows.Forms.Message & m)[0x00000] in < 5bcdf2a3746f4353acffcd001a2c6899 >:0
        //   at System.Windows.Forms.ContainerControl.WndProc(System.Windows.Forms.Message & m)[0x00029] in < 5bcdf2a3746f4353acffcd001a2c6899 >:0
        //   at System.Windows.Forms.UserControl.WndProc(System.Windows.Forms.Message & m)[0x00027] in < 5bcdf2a3746f4353acffcd001a2c6899 >:0
        //   at NClass.DiagramEditor.Canvas.WndProc(System.Windows.Forms.Message & m)[0x00001] in < 64616348232f476fac04199c6feee710 >:0
        //   at System.Windows.Forms.Control + ControlWindowTarget.OnMessage(System.Windows.Forms.Message & m)[0x00000] in < 5bcdf2a3746f4353acffcd001a2c6899 >:0
        //   at System.Windows.Forms.Control + ControlNativeWindow.WndProc(System.Windows.Forms.Message & m)[0x0000b] in < 5bcdf2a3746f4353acffcd001a2c6899 >:0
        //   at System.Windows.Forms.NativeWindow.WndProc(System.IntPtr hWnd, System.Windows.Forms.Msg msg, System.IntPtr wParam, System.IntPtr lParam)[0x00085] in < 5bcdf2a3746f4353acffcd001a2c6899 >:0
        //
        // One workaround is to run in windowed mode ie less that full screen.  However, as soon as the user swwitches to full screen,
        // either accidentally or otherwise, then the app will crash.
        //
        // Thus, this hack swallows the out-of-memory exception and returns a minimal clipping box.
        // The effect of this is that the view will occasionally turn blank/white.
        // Bizzarely, if the view is scrolled slightly to the right, everything is OK - go figure...
        try
        {
          return _graphics.ClipBounds;
        }
        catch (OutOfMemoryException)
        {
          return new RectangleF();
        }
        // END_HACK
      }
    }

    public Matrix Transform
    {
      get { return _graphics.Transform; }
      set { _graphics.Transform = value; }
    }

    public void DrawEllipse(Pen pen, int x, int y, int width, int height)
    {
      _graphics.DrawEllipse(pen, x, y, width, height);
    }

    public void DrawImage(Image image, int x, int y)
    {
      _graphics.DrawImage(image, x, y, image.Width, image.Height);
    }

    public void DrawImage(Image image, Point point)
    {
      _graphics.DrawImage(image, point.X, point.Y, image.Width, image.Height);
    }

    public void DrawLine(Pen pen, int x1, int y1, int x2, int y2)
    {
      _graphics.DrawLine(pen, x1, y1, x2, y2);
    }

    public void DrawLine(Pen pen, Point pt1, Point pt2)
    {
      _graphics.DrawLine(pen, pt1, pt2);
    }

    public void DrawLines(Pen pen, Point[] points)
    {
      _graphics.DrawLines(pen, points);
    }

    public void DrawPath(Pen pen, GraphicsPath path)
    {
      _graphics.DrawPath(pen, path);
    }

    public void DrawPolygon(Pen pen, Point[] points)
    {
      _graphics.DrawPolygon(pen, points);
    }

    public void DrawRectangle(Pen pen, Rectangle rect)
    {
      _graphics.DrawRectangle(pen, rect);
    }

    public void DrawString(string s, Font font, Brush brush, PointF point)
    {
      _graphics.DrawString(s, font, brush, point);
    }

    public void DrawString(string s, Font font, Brush brush, RectangleF layoutRectangle)
    {
      _graphics.DrawString(s, font, brush, layoutRectangle);
    }

    public void DrawString(string s, Font font, Brush brush, PointF point, StringFormat format)
    {
      _graphics.DrawString(s, font, brush, point, format);
    }

    public void DrawString(string s, Font font, Brush brush, RectangleF layoutRectangle, StringFormat format)
    {
      _graphics.DrawString(s, font, brush, layoutRectangle, format);
    }

    public void FillEllipse(Brush brush, Rectangle rect)
    {
      _graphics.FillEllipse(brush, rect);
    }

    public void FillEllipse(Brush brush, int x, int y, int width, int height)
    {
      _graphics.FillEllipse(brush, x, y, width, height);
    }

    public void FillPath(Brush brush, GraphicsPath path)
    {
      _graphics.FillPath(brush, path);
    }

    public void FillPolygon(Brush brush, Point[] points)
    {
      _graphics.FillPolygon(brush, points);
    }

    public void FillRectangle(Brush brush, Rectangle rect)
    {
      _graphics.FillRectangle(brush, rect);
    }

    public void SetClip(GraphicsPath path, CombineMode combineMode)
    {
      _graphics.SetClip(path, combineMode);
    }

    public void SetClip(Rectangle rect, CombineMode combineMode)
    {
      _graphics.SetClip(rect, combineMode);
    }

    public void SetClip(RectangleF rect, CombineMode combineMode)
    {
      _graphics.SetClip(rect, combineMode);
    }

    public void SetClip(Region region, CombineMode combineMode)
    {
      _graphics.SetClip(region, combineMode);
    }

    public void ResetTransform()
    {
      _graphics.ResetTransform();
    }

    public void RotateTransform(float angle)
    {
      _graphics.RotateTransform(angle);
    }

    public void ScaleTransform(float sx, float sy)
    {
      _graphics.ScaleTransform(sx, sy);
    }

    public void TranslateTransform(float dx, float dy)
    {
      _graphics.TranslateTransform(dx, dy);
    }

    public GraphicsState Save()
    {
      return _graphics.Save();
    }

    public void Restore(GraphicsState state)
    {
      _graphics.Restore(state);
    }

    public SmoothingMode SmoothingMode
    {
      get
      {
        return _graphics.SmoothingMode;
      }
      set
      {
        _graphics.SmoothingMode = value;
      }
    }

    public TextRenderingHint TextRenderingHint
    {
      get
      {
        return _graphics.TextRenderingHint;
      }
      set
      {
        _graphics.TextRenderingHint = value;
      }
    }

    public float DpiX => _graphics.DpiX;

    public float DpiY => _graphics.DpiY;

    public SizeF MeasureString(string text, Font font, PointF origin, StringFormat stringFormat)
    {
      return _graphics.MeasureString(text, font, origin, stringFormat);
    }
  }
}
