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

using NClass.Translations;
using System;
using System.Text;
using System.Xml;

namespace NClass.Core
{
  public sealed class AssociationRelationship : TypeRelationship
  {
    public event EventHandler Reversed;

    internal AssociationRelationship(TypeBase first, TypeBase second)
      : base(first, second)
    {
      Attach();
    }

    internal AssociationRelationship(TypeBase first, TypeBase second, AssociationType type) :
      base(first, second)
    {
      _associationType = type;
      RelationshipType = RelationshipType.Association;
      SupportsLabel = true;
      Attach();
    }

    private Direction _direction = Direction.Unidirectional;
    public Direction Direction
    {
      get
      {
        return _direction;
      }
      set
      {
        if (_direction != value)
        {
          OnBeginUndoableOperation();
          _direction = value;
          Changed();
        }
      }
    }

    private AssociationType _associationType = AssociationType.Association;
    public AssociationType AssociationType
    {
      get
      {
        return _associationType;
      }
      set
      {
        if (_associationType != value)
        {
          OnBeginUndoableOperation();
          _associationType = value;
          Changed();
        }
      }
    }

    public bool IsAggregation
    {
      get
      {
        return (_associationType == AssociationType.Aggregation);
      }
    }

    public bool IsComposition
    {
      get
      {
        return (_associationType == AssociationType.Composition);
      }
    }

    private string _startRole;
    public string StartRole
    {
      get
      {
        return _startRole;
      }
      set
      {
        if (value == "")
          value = null;

        if (_startRole != value)
        {
          OnBeginUndoableOperation();
          _startRole = value;
          Changed();
        }
      }
    }

    private string _endRole;
    public string EndRole
    {
      get
      {
        return _endRole;
      }
      set
      {
        if (value == "")
          value = null;

        if (_endRole != value)
        {
          OnBeginUndoableOperation();
          _endRole = value;
          Changed();
        }
      }
    }

    private string _startMultiplicity;
    public string StartMultiplicity
    {
      get
      {
        return _startMultiplicity;
      }
      set
      {
        if (value == "")
          value = null;

        if (_startMultiplicity != value)
        {
          OnBeginUndoableOperation();
          _startMultiplicity = value;
          Changed();
        }
      }
    }

    private string _endMultiplicity;
    public string EndMultiplicity
    {
      get
      {
        return _endMultiplicity;
      }
      set
      {
        if (value == "")
          value = null;

        if (_endMultiplicity != value)
        {
          OnBeginUndoableOperation();
          _endMultiplicity = value;
          Changed();
        }
      }
    }

    public void Reverse()
    {
      OnBeginUndoableOperation();

      IEntity first = First;
      First = Second;
      Second = first;

      OnReversed(EventArgs.Empty);
      Changed();
    }

    protected override void CopyFrom(Relationship relationship)
    {
      base.CopyFrom(relationship);

      AssociationRelationship association = (AssociationRelationship)relationship;
      _associationType = association._associationType;
      _direction = association._direction;
      _startRole = association._startRole;
      _endRole = association._endRole;
      _startMultiplicity = association._startMultiplicity;
      _endMultiplicity = association._endMultiplicity;
    }

    public AssociationRelationship Clone(TypeBase first, TypeBase second)
    {
      AssociationRelationship association = new AssociationRelationship(first, second);
      association.CopyFrom(this);
      return association;
    }

    public override void Serialize(XmlElement node)
    {
      base.Serialize(node);

      XmlElement directionNode = node.OwnerDocument.CreateElement("Direction");
      directionNode.InnerText = Direction.ToString();
      node.AppendChild(directionNode);

      XmlElement aggregationNode = node.OwnerDocument.CreateElement("AssociationType");
      aggregationNode.InnerText = AssociationType.ToString();
      node.AppendChild(aggregationNode);

      if (StartRole != null)
      {
        XmlElement roleNode = node.OwnerDocument.CreateElement("StartRole");
        roleNode.InnerText = StartRole.ToString();
        node.AppendChild(roleNode);
      }
      if (EndRole != null)
      {
        XmlElement roleNode = node.OwnerDocument.CreateElement("EndRole");
        roleNode.InnerText = EndRole.ToString();
        node.AppendChild(roleNode);
      }
      if (StartMultiplicity != null)
      {
        XmlElement multiplicityNode = node.OwnerDocument.CreateElement("StartMultiplicity");
        multiplicityNode.InnerText = StartMultiplicity.ToString();
        node.AppendChild(multiplicityNode);
      }
      if (EndMultiplicity != null)
      {
        XmlElement multiplicityNode = node.OwnerDocument.CreateElement("EndMultiplicity");
        multiplicityNode.InnerText = EndMultiplicity.ToString();
        node.AppendChild(multiplicityNode);
      }
    }

    public override void Deserialize(XmlElement node)
    {
      base.Deserialize(node);

      XmlElement child = node["Direction"];

      RaisePreChangedEvent = RaiseChangedEvent = false;
      if (child != null)
      {
        if (child.InnerText == "Unidirectional")
          Direction = Direction.Unidirectional;
        else
          Direction = Direction.Bidirectional;
      }

      try
      {
        child = node["AssociationType"];
        if (child != null)
        {
          if (child.InnerText == "Aggregation")
            _associationType = AssociationType.Aggregation;
          else if (child.InnerText == "Composition")
            _associationType = AssociationType.Composition;
          else
            _associationType = AssociationType.Association;
        }

        child = node["StartRole"];
        if (child != null)
          _startRole = child.InnerText;

        child = node["EndRole"];
        if (child != null)
          _endRole = child.InnerText;

        child = node["StartMultiplicity"];
        if (child != null)
          _startMultiplicity = child.InnerText;

        child = node["EndMultiplicity"];
        if (child != null)
          _endMultiplicity = child.InnerText;
      }
      catch (ArgumentException)
      {
        // Wrong format
      }
      RaisePreChangedEvent = RaiseChangedEvent = true;
    }

    private void OnReversed(EventArgs e)
    {
      Reversed?.Invoke(this, e);
    }

    public override string ToString()
    {
      StringBuilder builder = new StringBuilder(50);

      if (IsAggregation)
        builder.Append(Strings.Aggregation);
      else if (IsComposition)
        builder.Append(Strings.Composition);
      else
        builder.Append(Strings.Association);
      builder.Append(": ");
      builder.Append(First.Name);

      switch (Direction)
      {
        case Direction.Bidirectional:
          if (AssociationType == AssociationType.Association)
            builder.Append(" --- ");
          else
            builder.Append(" <>-- ");
          break;

        case Direction.Unidirectional:
          if (AssociationType == AssociationType.Association)
            builder.Append(" --> ");
          else
            builder.Append(" <>-> ");
          break;

        default:
          builder.Append(", ");
          break;
      }
      builder.Append(Second.Name);

      return builder.ToString();
    }
  }
}