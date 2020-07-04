// NClass - Free class diagram editor
// Copyright (C) 2006-2009 Balazs Tihanyi
// Copyright (C) 2016-2020 Trevor D'Arcy-Evans
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

namespace NClass.DiagramEditor.ClassDiagram.Editors
{
  using System;
  using System.Windows.Forms;
  using NClass.DiagramEditor.ClassDiagram.Connections;

  public sealed partial class SourceSinkEditor : TypeEditor
  {
    private SourceSinkConnection Connection { get; set; } = null;

    public SourceSinkEditor()
    {
      InitializeComponent();
    }

    internal override void Init(DiagramElement element)
    {
      Connection = (SourceSinkConnection)element;
      RefreshValues();
    }

    private void RefreshValues()
    {
      SuspendLayout();

      var cursorPosition = txtName.SelectionStart;
      txtName.Text = Connection.SourceSink.Label;
      txtName.SelectionStart = cursorPosition;

      ResumeLayout();
    }

    public override void ValidateData()
    {
      Connection.SourceSink.Label = txtName.Text;
      RefreshValues();
    }

    private void txtName_KeyDown(object sender, KeyEventArgs e)
    {
      switch (e.KeyCode)
      {
        case Keys.Escape:
          Connection.HideEditor();
          e.Handled = true;
          break;
      }
    }

    private void txtName_LostFocus(object sender, EventArgs e)
    {
      Connection.HideEditor();
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
      base.OnVisibleChanged(e);
      txtName.SelectionStart = 0;
    }
  }
}
