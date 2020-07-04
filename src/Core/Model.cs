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

using NClass.Translations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace NClass.Core
{
  public class Model : IProjectItem
  {
    public event EventHandler BeginUndoableOperation;
    public event EventHandler Modified;
    public event EventHandler Renamed;
    public event EventHandler Closing;
    public event EntityEventHandler EntityAdded;
    public event EntityEventHandler EntityRemoved;
    public event RelationshipEventHandler RelationAdded;
    public event RelationshipEventHandler RelationRemoved;
    public event SerializeEventHandler Serializing;
    public event SerializeEventHandler Deserializing;

    protected Stack<XmlElement> UndoModels = new Stack<XmlElement>();

    // required for XML deserialisation
    protected Model()
    {
      _name = Strings.Untitled;
      Language = null;
    }

    public Model(Language language) :
      this(null, language)
    {
    }

    public Model(string name, Language language)
    {
      if (name != null && name.Length == 0)
      {
        throw new ArgumentException("Name cannot empty string.");
      }

      _name = name;
      Language = language ?? throw new ArgumentNullException("language");
    }

    private string _name;
    public string Name
    {
      get
      {
        if (_name == null)
        {
          return Strings.Untitled;
        }
        else
        {
          return _name;
        }
      }
      set
      {
        if (_name != value && value != null)
        {
          OnBeginUndoableOperation(this, EventArgs.Empty);
          _name = value;
          OnRenamed(EventArgs.Empty);
          OnModified(EventArgs.Empty);
        }
      }
    }

    public Language Language { get; private set; }

    public Project Project { get; set; } = null;

    public bool IsUntitled
    {
      get
      {
        return (_name == null);
      }
    }

    public bool IsDirty { get; private set; } = false;

    protected bool Loading { get; private set; } = false;

    public bool IsEmpty
    {
      get
      {
        return (_entities.Count == 0 && _relationships.Count == 0);
      }
    }

    public void Clean()
    {
      IsDirty = false;
    }

    public void Close()
    {
      OnClosing(EventArgs.Empty);
    }

    private readonly List<IEntity> _entities = new List<IEntity>();
    public IEnumerable<IEntity> Entities
    {
      get { return _entities; }
    }

    private readonly List<Relationship> _relationships = new List<Relationship>();
    public IEnumerable<Relationship> Relationships
    {
      get { return _relationships; }
    }

    private void ElementChanged(object sender, EventArgs e)
    {
      ResolveDuplicateEntityName(sender);

      OnModified(e);
    }

    #region Entity
    private void AddEntity(IEntity entity)
    {
      ResolveDuplicateEntityName(entity);

      OnBeginUndoableOperation(this, EventArgs.Empty);
      _entities.Add(entity);
      entity.BeginUndoableOperation += new EventHandler(OnBeginUndoableOperation);
      entity.Modified += new EventHandler(ElementChanged);
      OnEntityAdded(new EntityEventArgs(entity));
    }

    private void ResolveDuplicateEntityName(object obj)
    {
      var entity = obj as TypeBase;

      if (!Loading &&
        entity is TypeBase &&
        _entities
          .OfType<TypeBase>()
          .Except(new[] { entity })
          .Any(x => x.Name == entity?.Name))
      {
        entity.Name += "1";
        ResolveDuplicateEntityName(entity);
      }
    }
    #endregion

    #region Class
    public ClassType AddClass()
    {
      ClassType newClass = Language.CreateClass();
      AddClass(newClass);
      return newClass;
    }

    protected virtual void AddClass(ClassType newClass)
    {
      AddEntity(newClass);
    }

    public bool InsertClass(ClassType newClass)
    {
      if (newClass != null && !_entities.Contains(newClass) && newClass.Language == Language)
      {
        AddClass(newClass);
        return true;
      }
      else
      {
        return false;
      }
    }
    #endregion

    #region Structure
    public StructureType AddStructure()
    {
      StructureType structure = Language.CreateStructure();
      AddStructure(structure);
      return structure;
    }

    protected virtual void AddStructure(StructureType structure)
    {
      AddEntity(structure);
    }

    public bool InsertStructure(StructureType structure)
    {
      if (structure != null && !_entities.Contains(structure) &&
        structure.Language == Language)
      {
        AddStructure(structure);
        return true;
      }
      else
      {
        return false;
      }
    }
    #endregion

    #region Interface
    public InterfaceType AddInterface()
    {
      InterfaceType newInterface = Language.CreateInterface();
      AddInterface(newInterface);
      return newInterface;
    }

    protected virtual void AddInterface(InterfaceType newInterface)
    {
      AddEntity(newInterface);
    }

    public bool InsertInterface(InterfaceType newInterface)
    {
      if (newInterface != null && !_entities.Contains(newInterface) &&
        newInterface.Language == Language)
      {
        AddInterface(newInterface);
        return true;
      }
      else
      {
        return false;
      }
    }
    #endregion

    #region Enum
    public EnumType AddEnum()
    {
      EnumType newEnum = Language.CreateEnum();
      AddEnum(newEnum);
      return newEnum;
    }

    protected virtual void AddEnum(EnumType newEnum)
    {
      AddEntity(newEnum);
    }

    public bool InsertEnum(EnumType newEnum)
    {
      if (newEnum != null && !_entities.Contains(newEnum) &&
        newEnum.Language == Language)
      {
        AddEnum(newEnum);
        return true;
      }
      else
      {
        return false;
      }
    }
    #endregion

    #region Delegate
    public DelegateType AddDelegate()
    {
      DelegateType newDelegate = Language.CreateDelegate();
      AddDelegate(newDelegate);
      return newDelegate;
    }

    protected virtual void AddDelegate(DelegateType newDelegate)
    {
      AddEntity(newDelegate);
    }

    public bool InsertDelegate(DelegateType newDelegate)
    {
      if (newDelegate != null && !_entities.Contains(newDelegate) &&
        newDelegate.Language == Language)
      {
        AddDelegate(newDelegate);
        return true;
      }
      else
      {
        return false;
      }
    }
    #endregion

    #region Comment
    public Comment AddComment()
    {
      Comment comment = new Comment();
      AddComment(comment);
      return comment;
    }

    protected virtual void AddComment(Comment comment)
    {
      AddEntity(comment);
    }

    public bool InsertComment(Comment comment)
    {
      if (comment != null && !_entities.Contains(comment))
      {
        AddComment(comment);
        return true;
      }
      else
      {
        return false;
      }
    }
    #endregion

    #region State
    public State AddState()
    {
      var state = new State(Strings.State);
      AddState(state);
      return state;
    }

    protected virtual void AddState(State state)
    {
      AddEntity(state);
    }

    public bool InsertState(State state)
    {
      if (state != null && !_entities.Contains(state))
      {
        AddState(state);
        return true;
      }
      else
      {
        return false;
      }
    }
    #endregion

    #region Relationship
    private void AddRelationship(Relationship relationship)
    {
      OnBeginUndoableOperation(this, EventArgs.Empty);
      _relationships.Add(relationship);
      relationship.BeginUndoableOperation += new EventHandler(OnBeginUndoableOperation);
      relationship.Modified += new EventHandler(ElementChanged);
      OnRelationAdded(new RelationshipEventArgs(relationship));
    }
    #endregion

    #region Association
    public AssociationRelationship AddAssociation(TypeBase first, TypeBase second)
    {
      AssociationRelationship association = new AssociationRelationship(first, second);
      AddAssociation(association);
      return association;
    }

    protected virtual void AddAssociation(AssociationRelationship association)
    {
      AddRelationship(association);
    }

    public bool InsertAssociation(AssociationRelationship associaton)
    {
      if (associaton != null && !_relationships.Contains(associaton) &&
        _entities.Contains(associaton.First) && _entities.Contains(associaton.Second))
      {
        AddAssociation(associaton);
        return true;
      }
      else
      {
        return false;
      }
    }
    #endregion

    #region Composition
    public AssociationRelationship AddComposition(TypeBase first, TypeBase second)
    {
      AssociationRelationship composition = new AssociationRelationship(first, second, AssociationType.Composition);

      AddAssociation(composition);
      return composition;
    }
    #endregion

    #region Aggregation
    public AssociationRelationship AddAggregation(TypeBase first, TypeBase second)
    {
      AssociationRelationship aggregation = new AssociationRelationship(first, second, AssociationType.Aggregation);

      AddAssociation(aggregation);
      return aggregation;
    }
    #endregion

    #region Generalization
    /// <exception cref="RelationshipException">
    /// Cannot create relationship between the two types.
    /// </exception>
    public GeneralizationRelationship AddGeneralization(CompositeType derivedType,
      CompositeType baseType)
    {
      GeneralizationRelationship generalization = new GeneralizationRelationship(derivedType, baseType);

      AddGeneralization(generalization);
      return generalization;
    }

    protected virtual void AddGeneralization(GeneralizationRelationship generalization)
    {
      AddRelationship(generalization);
    }

    public bool InsertGeneralization(GeneralizationRelationship generalization)
    {
      if (generalization != null && !_relationships.Contains(generalization) &&
        _entities.Contains(generalization.First) && _entities.Contains(generalization.Second))
      {
        AddGeneralization(generalization);
        return true;
      }
      else
      {
        return false;
      }
    }
    #endregion

    #region Realization
    /// <exception cref="RelationshipException">
    /// Cannot create relationship between the two types.
    /// </exception>
    public RealizationRelationship AddRealization(TypeBase implementer, InterfaceType baseType)
    {
      RealizationRelationship realization = new RealizationRelationship(implementer, baseType);

      AddRealization(realization);
      return realization;
    }

    protected virtual void AddRealization(RealizationRelationship realization)
    {
      AddRelationship(realization);
    }

    public bool InsertRealization(RealizationRelationship realization)
    {
      if (realization != null && !_relationships.Contains(realization) &&
        _entities.Contains(realization.First) && _entities.Contains(realization.Second))
      {
        AddRealization(realization);
        return true;
      }
      else
      {
        return false;
      }
    }
    #endregion

    #region Dependency
    public DependencyRelationship AddDependency(TypeBase first, TypeBase second)
    {
      DependencyRelationship dependency = new DependencyRelationship(first, second);

      AddDependency(dependency);
      return dependency;
    }

    protected virtual void AddDependency(DependencyRelationship dependency)
    {
      AddRelationship(dependency);
    }

    public bool InsertDependency(DependencyRelationship dependency)
    {
      if (dependency != null &&
        !_relationships.Contains(dependency) &&
        _entities.Contains(dependency.First) &&
        _entities.Contains(dependency.Second))
      {
        AddDependency(dependency);
        return true;
      }
      else
      {
        return false;
      }
    }
    #endregion

    #region EntityRelationship
    public EntityRelationship AddEntityRelationship(ClassType first, ClassType second)
    {
      var dependency = new EntityRelationship(first, second);

      AddEntityRelationship(dependency);
      return dependency;
    }

    protected virtual void AddEntityRelationship(EntityRelationship dependency)
    {
      AddRelationship(dependency);
    }

    public bool InsertEntityRelationship(EntityRelationship dependency)
    {
      if (dependency != null && !_relationships.Contains(dependency) &&
        _entities.Contains(dependency.First) && _entities.Contains(dependency.Second))
      {
        AddEntityRelationship(dependency);
        return true;
      }
      else
      {
        return false;
      }
    }
    #endregion

    #region Transition
    public Transition AddTransitionRelationship(State first, State second)
    {
      var trans = new Transition(first, second);

      AddTransitionRelationship(trans);
      return trans;
    }

    protected virtual void AddTransitionRelationship(Transition trans)
    {
      AddRelationship(trans);
    }

    public bool InsertTransitionRelationship(Transition trans)
    {
      if (trans != null && !_relationships.Contains(trans) &&
        _entities.Contains(trans.First) && _entities.Contains(trans.Second))
      {
        AddTransitionRelationship(trans);
        return true;
      }
      else
      {
        return false;
      }
    }
    #endregion

    #region SourceSinkRelationship
    public SourceSinkRelationship AddSourceSinkRelationship(ClassType first, ClassType second)
    {
      var dependency = new SourceSinkRelationship(first, second);

      AddSourceSinkRelationship(dependency);
      return dependency;
    }

    protected virtual void AddSourceSinkRelationship(SourceSinkRelationship sourceSink)
    {
      AddRelationship(sourceSink);
    }

    public bool InsertSourceSinkRelationship(SourceSinkRelationship sourceSink)
    {
      if (sourceSink != null && !_relationships.Contains(sourceSink) &&
        _entities.Contains(sourceSink.First) && _entities.Contains(sourceSink.Second))
      {
        AddSourceSinkRelationship(sourceSink);
        return true;
      }
      else
      {
        return false;
      }
    }

    #endregion

    #region Nesting
    /// <exception cref="RelationshipException">
    /// Cannot create relationship between the two types.
    /// </exception>
    public NestingRelationship AddNesting(CompositeType parentType, TypeBase innerType)
    {
      NestingRelationship nesting = new NestingRelationship(parentType, innerType);

      AddNesting(nesting);
      return nesting;
    }

    protected virtual void AddNesting(NestingRelationship nesting)
    {
      AddRelationship(nesting);
    }

    public bool InsertNesting(NestingRelationship nesting)
    {
      if (nesting != null && !_relationships.Contains(nesting) &&
        _entities.Contains(nesting.First) && _entities.Contains(nesting.Second))
      {
        AddNesting(nesting);
        return true;
      }
      else
      {
        return false;
      }
    }
    #endregion

    #region Comment
    public virtual CommentRelationship AddCommentRelationship(Comment comment, IEntity entity)
    {
      CommentRelationship commentRelationship = new CommentRelationship(comment, entity);

      AddCommentRelationship(commentRelationship);
      return commentRelationship;
    }

    protected virtual void AddCommentRelationship(CommentRelationship commentRelationship)
    {
      AddRelationship(commentRelationship);
    }

    public bool InsertCommentRelationship(CommentRelationship commentRelationship)
    {
      if (commentRelationship != null && !_relationships.Contains(commentRelationship) &&
        _entities.Contains(commentRelationship.First) && _entities.Contains(commentRelationship.Second))
      {
        AddCommentRelationship(commentRelationship);
        return true;
      }
      else
      {
        return false;
      }
    }
    #endregion

    #region Remove
    public void RemoveEntity(IEntity entity)
    {
      RemoveRelationships(entity);

      OnBeginUndoableOperation(this, EventArgs.Empty);
      if (_entities.Remove(entity))
      {
        entity.BeginUndoableOperation -= new EventHandler(OnBeginUndoableOperation);
        entity.Modified -= new EventHandler(ElementChanged);
        OnEntityRemoved(new EntityEventArgs(entity));
      }
    }

    private void RemoveRelationships(IEntity entity)
    {
      for (int i = 0; i < _relationships.Count; i++)
      {
        Relationship relationship = _relationships[i];
        if (relationship.First == entity || relationship.Second == entity)
        {
          OnBeginUndoableOperation(this, EventArgs.Empty);
          relationship.Detach();
          relationship.BeginUndoableOperation -= new EventHandler(OnBeginUndoableOperation);
          relationship.Modified -= new EventHandler(ElementChanged);
          _relationships.RemoveAt(i--);
          OnRelationRemoved(new RelationshipEventArgs(relationship));
        }
      }
    }

    public void RemoveRelationship(Relationship relationship)
    {
      if (_relationships.Contains(relationship))
      {
        OnBeginUndoableOperation(this, EventArgs.Empty);
        relationship.Detach();
        relationship.BeginUndoableOperation -= new EventHandler(OnBeginUndoableOperation);
        relationship.Modified -= new EventHandler(ElementChanged);
        _relationships.Remove(relationship);
        OnRelationRemoved(new RelationshipEventArgs(relationship));
      }
    }
    #endregion

    #region Serialize/Deserialize
    public void Serialize(XmlElement node)
    {
      if (node == null)
      {
        throw new ArgumentNullException("root");
      }

      XmlElement nameElement = node.OwnerDocument.CreateElement("Name");
      nameElement.InnerText = Name;
      node.AppendChild(nameElement);

      XmlElement languageElement = node.OwnerDocument.CreateElement("Language");
      languageElement.InnerText = Language.AssemblyName;
      node.AppendChild(languageElement);

      SaveEntitites(node);
      SaveRelationships(node);

      OnSerializing(new SerializeEventArgs(node));
    }

    /// <exception cref="InvalidDataException">
    /// The save format is corrupt and could not be loaded.
    /// </exception>
    public void Deserialize(XmlElement node)
    {
      if (node == null)
      {
        throw new ArgumentNullException("root");
      }
      Loading = true;

      XmlElement nameElement = node["Name"];
      if (nameElement == null || nameElement.InnerText == "")
      {
        _name = null;
      }
      else
      {
        _name = nameElement.InnerText;
      }

      XmlElement languageElement = node["Language"];
      try
      {
        Language language = Language.GetLanguage(languageElement.InnerText);
        Language = language ?? throw new InvalidDataException("Invalid project language.");
      }
      catch (Exception ex)
      {
        throw new InvalidDataException("Invalid project language.", ex);
      }

      LoadEntitites(node);
      LoadRelationships(node);

      OnDeserializing(new SerializeEventArgs(node));
      Loading = false;
    }

    /// <exception cref="InvalidDataException">
    /// The save format is corrupt and could not be loaded.
    /// </exception>
    private void LoadEntitites(XmlNode root)
    {
      if (root == null)
      {
        throw new ArgumentNullException("root");
      }

      XmlNodeList nodeList = root.SelectNodes("Entities/Entity");

      foreach (XmlElement node in nodeList)
      {
        try
        {
          string type = node.GetAttribute("type");

          IEntity entity = GetEntity(type);
          entity.Deserialize(node);
        }
        catch (BadSyntaxException ex)
        {
          throw new InvalidDataException("Invalid entity.", ex);
        }
      }
    }

    private IEntity GetEntity(string type)
    {
      switch (type)
      {
        case "Class":
          return AddClass();

        case "Structure":
          return AddStructure();

        case "Interface":
          return AddInterface();

        case "Enum":
          return AddEnum();

        case "Delegate":
          return AddDelegate();

        case "Comment":
          return AddComment();

        case "State":
          return AddState();

        default:
          throw new InvalidDataException("Invalid entity type: " + type);
      }
    }

    /// <exception cref="InvalidDataException">
    /// The save format is corrupt and could not be loaded.
    /// </exception>
    private void LoadRelationships(XmlNode root)
    {
      if (root == null)
      {
        throw new ArgumentNullException("root");
      }

      XmlNodeList nodeList = root.SelectNodes("Relationships/Relationship");

      foreach (XmlElement node in nodeList)
      {
        string type = node.GetAttribute("type");
        string firstString = node.GetAttribute("first");
        string secondString = node.GetAttribute("second");
        int firstIndex, secondIndex;

        if (!int.TryParse(firstString, out firstIndex) ||
          !int.TryParse(secondString, out secondIndex))
        {
          throw new InvalidDataException(Strings.ErrorCorruptSaveFormat);
        }
        if (firstIndex < 0 || firstIndex >= _entities.Count ||
          secondIndex < 0 || secondIndex >= _entities.Count)
        {
          throw new InvalidDataException(Strings.ErrorCorruptSaveFormat);
        }

        try
        {
          IEntity first = _entities[firstIndex];
          IEntity second = _entities[secondIndex];
          Relationship relationship;

          switch (type)
          {
            case "Association":
              relationship = AddAssociation(first as TypeBase, second as TypeBase);
              break;

            case "Generalization":
              relationship = AddGeneralization(
                first as CompositeType, second as CompositeType);
              break;

            case "Realization":
              relationship = AddRealization(first as TypeBase, second as InterfaceType);
              break;

            case "Dependency":
              relationship = AddDependency(first as TypeBase, second as TypeBase);
              break;

            case "Nesting":
              relationship = AddNesting(first as CompositeType, second as TypeBase);
              break;

            case "Comment":
              if (first is Comment)
                relationship = AddCommentRelationship(first as Comment, second);
              else
                relationship = AddCommentRelationship(second as Comment, first);
              break;

            case "EntityRelationship":
              relationship = AddEntityRelationship(first as ClassType, second as ClassType);
              break;

            case "Transition":
              relationship = AddTransitionRelationship(first as State, second as State);
              break;

            case "SourceSink":
              relationship = AddSourceSinkRelationship(first as ClassType, second as ClassType);
              break;

            default:
              throw new InvalidDataException(Strings.ErrorCorruptSaveFormat);
          }
          relationship.Deserialize(node);
        }
        catch (ArgumentNullException ex)
        {
          throw new InvalidDataException("Invalid relationship.", ex);
        }
        catch (RelationshipException ex)
        {
          throw new InvalidDataException("Invalid relationship.", ex);
        }
      }
    }

    private void SaveEntitites(XmlElement node)
    {
      if (node == null)
      {
        throw new ArgumentNullException("root");
      }

      XmlElement entitiesChild = node.OwnerDocument.CreateElement("Entities");

      foreach (IEntity entity in _entities)
      {
        XmlElement child = node.OwnerDocument.CreateElement("Entity");

        entity.Serialize(child);
        child.SetAttribute("type", entity.EntityType.ToString());
        entitiesChild.AppendChild(child);
      }
      node.AppendChild(entitiesChild);
    }

    private void SaveRelationships(XmlNode root)
    {
      if (root == null)
      {
        throw new ArgumentNullException("root");
      }

      XmlElement relationsChild = root.OwnerDocument.CreateElement("Relationships");

      foreach (Relationship relationship in _relationships)
      {
        XmlElement child = root.OwnerDocument.CreateElement("Relationship");

        int firstIndex = _entities.IndexOf(relationship.First);
        int secondIndex = _entities.IndexOf(relationship.Second);

        relationship.Serialize(child);
        child.SetAttribute("type", relationship.RelationshipType.ToString());
        child.SetAttribute("first", firstIndex.ToString());
        child.SetAttribute("second", secondIndex.ToString());
        relationsChild.AppendChild(child);
      }
      root.AppendChild(relationsChild);
    }
    #endregion

    #region Events
    protected virtual void OnEntityAdded(EntityEventArgs e)
    {
      EntityAdded?.Invoke(this, e);
      OnModified(EventArgs.Empty);
    }

    protected virtual void OnEntityRemoved(EntityEventArgs e)
    {
      EntityRemoved?.Invoke(this, e);
      OnModified(EventArgs.Empty);
    }

    protected virtual void OnRelationAdded(RelationshipEventArgs e)
    {
      RelationAdded?.Invoke(this, e);
      OnModified(EventArgs.Empty);
    }

    protected virtual void OnRelationRemoved(RelationshipEventArgs e)
    {
      RelationRemoved?.Invoke(this, e);
      OnModified(EventArgs.Empty);
    }

    protected virtual void OnSerializing(SerializeEventArgs e)
    {
      Serializing?.Invoke(this, e);
    }

    protected virtual void OnDeserializing(SerializeEventArgs e)
    {
      Deserializing?.Invoke(this, e);
      OnModified(EventArgs.Empty);
    }

    protected void OnBeginUndoableOperation(object sender, EventArgs e)
    {
      if (Loading)
      {
        return;
      }

      var xmlElem = new XmlDocument().CreateElement("Undo");
      Serialize(xmlElem);
      UndoModels.Push(xmlElem);

      OnBeginUndoableOperation(EventArgs.Empty);
    }

    protected void OnBeginUndoableOperation(EventArgs e)
    {
      BeginUndoableOperation?.Invoke(this, e);
    }

    protected virtual void OnModified(EventArgs e)
    {
      IsDirty = true;
      Modified?.Invoke(this, e);
    }

    protected virtual void OnRenamed(EventArgs e)
    {
      Renamed?.Invoke(this, e);
    }

    protected virtual void OnClosing(EventArgs e)
    {
      Closing?.Invoke(this, e);
    }
    #endregion

    protected virtual void Reset()
    {
      _relationships.Clear();
      _entities.Clear();
    }

    public override string ToString()
    {
      if (IsDirty)
      {
        return Name + "*";
      }
      else
      {
        return Name;
      }
    }
  }
}
