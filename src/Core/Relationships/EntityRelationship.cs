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

using System;
using System.Xml;
using NClass.Translations;

namespace NClass.Core
{
  public sealed class EntityRelationship : TypeRelationship
  {
    public MultiplicityType StartMultiplicity { get; set; }
    public MultiplicityType EndMultiplicity { get; set; }

    internal EntityRelationship(TypeBase first, TypeBase second) :
      base(first, second)
    {
      Attach();
    }

    public override RelationshipType RelationshipType
    {
      get { return RelationshipType.EntityRelationship; }
    }

    public override bool SupportsLabel
    {
      get { return true; }
    }
    protected internal override void Serialize(XmlElement node)
    {
      base.Serialize(node);

      var startMultNode = node.OwnerDocument.CreateElement("StartMultiplicity");
      startMultNode.InnerText = StartMultiplicity.ToString();
      node.AppendChild(startMultNode);

      var endMultNode = node.OwnerDocument.CreateElement("EndMultiplicity");
      endMultNode.InnerText = EndMultiplicity.ToString();
      node.AppendChild(endMultNode);
    }

    protected internal override void Deserialize(XmlElement node)
    {
      base.Deserialize(node);

      RaiseChangedEvent = false;

      StartMultiplicity = (MultiplicityType)Enum.Parse(typeof(MultiplicityType), node["StartMultiplicity"].InnerText);
      EndMultiplicity = (MultiplicityType)Enum.Parse(typeof(MultiplicityType), node["EndMultiplicity"].InnerText);

      RaiseChangedEvent = true;
    }

    public EntityRelationship Clone(TypeBase first, TypeBase second)
    {
      EntityRelationship dependency = new EntityRelationship(first, second);
      dependency.CopyFrom(this);
      return dependency;
    }

    public override string ToString()
    {
      return $"{Strings.EntityRelationship}: [{First.Name}]{StartMultiplicityAsString()}----{EndMultiplicityAsString()}[{Second.Name}]";
    }

    private string StartMultiplicityAsString()
    {
      switch (StartMultiplicity)
      {
        case MultiplicityType.ZeroOrOne:
          return "+o";
        case MultiplicityType.OneAndOnly:
          return "++";
        case MultiplicityType.ZeroOrMany:
          return "o<";
        case MultiplicityType.OneOrMany:
          return "+<";
        default:
          throw new ArgumentOutOfRangeException($"Unknown MultiplicityType: {StartMultiplicity}");
      }
    }

    private string EndMultiplicityAsString()
    {
      switch (EndMultiplicity)
      {
        case MultiplicityType.ZeroOrOne:
          return "o+";
        case MultiplicityType.OneAndOnly:
          return "++";
        case MultiplicityType.ZeroOrMany:
          return ">o";
        case MultiplicityType.OneOrMany:
          return ">+";
        default:
          throw new ArgumentOutOfRangeException($"Unknown MultiplicityType: {EndMultiplicity}");
      }
    }
  }
}
