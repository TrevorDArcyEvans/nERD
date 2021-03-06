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

namespace NClass.Core
{
  public abstract class Parameter : LanguageElement
  {
    /// <exception cref="BadSyntaxException">
    /// The <paramref name="name"/> or <paramref name="type"/> 
    /// does not fit to the syntax.
    /// </exception>
    protected Parameter(string name, string type, ParameterModifier modifier, string defaultValue)
    {
      Initializing = true;
      Name = name;
      Type = type;
      Modifier = modifier;
      DefaultValue = defaultValue;
      Initializing = false;
    }

    private string _name;
    /// <exception cref="BadSyntaxException">
    /// The <paramref name="value"/> does not fit to the syntax.
    /// </exception>
    public override string Name
    {
      get
      {
        return _name;
      }
      set
      {
        string newName = Language.GetValidName(value, false);

        if (newName != _name)
        {
          OnBeginUndoableOperation();
          _name = newName;
          Changed();
        }
      }
    }

    private string _type;
    /// <exception cref="BadSyntaxException">
    /// The <paramref name="value"/> does not fit to the syntax.
    /// </exception>
    public virtual string Type
    {
      get
      {
        return _type;
      }
      protected set
      {
        string newType = Language.GetValidTypeName(value);

        if (newType != _type)
        {
          OnBeginUndoableOperation();
          _type = newType;
          Changed();
        }
      }
    }

    private ParameterModifier _modifier;
    public virtual ParameterModifier Modifier
    {
      get
      {
        return _modifier;
      }
      protected set
      {
        if (_modifier != value)
        {
          OnBeginUndoableOperation();
          _modifier = value;
          Changed();
        }
      }
    }

    private string _defaultValue;
    public virtual string DefaultValue
    {
      get
      {
        return _defaultValue;
      }
      protected set
      {
        if (string.IsNullOrWhiteSpace(value))
          value = null;

        if (_defaultValue != value)
        {
          OnBeginUndoableOperation();
          _defaultValue = value;
          Changed();
        }
      }
    }

    public bool IsOptional
    {
      get { return (DefaultValue != null); }
    }

    public abstract Language Language
    {
      get;
    }

    private static string GetModifierString(ParameterModifier modifier)
    {
      switch (modifier)
      {
        case ParameterModifier.Inout:
          return "inout";

        case ParameterModifier.Out:
          return "out";

        case ParameterModifier.Params:
          return "params";

        default:
          return "in";
      }
    }

    public string GetUmlDescription(bool getName)
    {
      if (getName)
      {
        if (Modifier == ParameterModifier.In)
          return Name + ": " + Type;
        else
          return string.Format("{0} {1}: {2}", GetModifierString(Modifier), Name, Type);
      }
      else
      {
        return Type;
      }
    }

    public abstract Parameter Clone();

    public override string ToString()
    {
      return GetDeclaration();
    }
  }
}
