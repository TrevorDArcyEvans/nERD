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
using System;
using System.Text;

namespace NClass.StateTransitionDiagram
{
  public sealed class StdClass : ClassType
  {
    public StdClass() :
      this("NewClass")
    {
    }

    internal StdClass(string name) :
      base(name)
    {
    }

    public override AccessModifier DefaultAccess => AccessModifier.Internal;
    public override AccessModifier DefaultMemberAccess => AccessModifier.Private;

    public override bool SupportsProperties => false;
    public override bool SupportsEvents => false;
    public override bool SupportsDestructors => false;
    public override bool SupportsNesting => false;
    public override bool SupportsConstuctors => false;
    public override bool SupportsMethods => false;
    public override bool SupportsFields => false;

    public override Language Language => StdLanguage.Instance;

    public override void AddInterface(InterfaceType interfaceType) => throw new NotImplementedException();
    public override Field AddField() => throw new NotImplementedException();
    public override Constructor AddConstructor() => throw new NotImplementedException();
    public override Destructor AddDestructor() => throw new NotImplementedException();
    public override Method AddMethod() => throw new NotImplementedException();
    public override Property AddProperty() => throw new NotImplementedException();
    public override Event AddEvent() => throw new NotImplementedException();

    public override string GetDeclaration()
    {
      var builder = new StringBuilder();

      if (AccessModifier != AccessModifier.Default)
      {
        builder.Append(Language.GetAccessString(AccessModifier, true));
        builder.Append(" ");
      }
      if (Modifier != ClassModifier.None)
      {
        builder.Append(Language.GetClassModifierString(Modifier, true));
        builder.Append(" ");
      }
      builder.AppendFormat("class {0}", Name);

      if (HasExplicitBase || InterfaceList.Count > 0)
      {
        builder.Append(" : ");
        if (HasExplicitBase)
        {
          builder.Append(BaseClass.Name);
          if (InterfaceList.Count > 0)
            builder.Append(", ");
        }
        for (int i = 0; i < InterfaceList.Count; i++)
        {
          builder.Append(InterfaceList[i].Name);
          if (i < InterfaceList.Count - 1)
            builder.Append(", ");
        }
      }

      return builder.ToString();
    }

    public override ClassType Clone()
    {
      var newClass = new StdClass();
      newClass.CopyFrom(this);
      return newClass;
    }
  }
}
