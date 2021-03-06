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
using NClass.CSharp;
using NClass.Translations;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NClass.CodeGenerator
{
  internal sealed class SqlProjectGenerator : ProjectGenerator
  {
    private static readonly string[] SupportedLoopForeignKeys = new[]
    {
      "Next",
      "Previous",
      "Parent",
      "Child"
    };

    public SqlProjectGenerator(Model model) :
      base(model)
    {
    }

    public override string RelativeProjectFileName
    {
      get { return null; }
    }

    protected override SourceFileGenerator CreateSourceFileGenerator(TypeBase type)
    {
      return null;
    }

    protected override bool GenerateProjectFiles(string location)
    {
      var sb = new StringBuilder();
      try
      {
        var entities = Model.Entities.OfType<CSharpClass>();
        var links = Model.Relationships.OfType<EntityRelationship>();

        #region Pre-flight Checks

        // check for any unsupported .NET types
        var fieldTypeNames = entities.SelectMany(ent => ent.Fields.OfType<CSharpField>()).Select(field => field.Type);
        var propTypeNames = entities.SelectMany(ent => ent.Operations.OfType<CSharpProperty>()).Select(op => op.Type);
        if (fieldTypeNames.Any(fieldTypeName => !NetToSqlTypeMap.TryGetValue(fieldTypeName.ToLowerInvariant(), out _)) ||
          propTypeNames.Any(propTypeName => !NetToSqlTypeMap.TryGetValue(propTypeName.ToLowerInvariant(), out _)))
        {
          sb.AppendLine($"-- {Strings.SqlGenError_UnsupportedType}");
          return false;
        }

        // check for loop relationships - only support 'NextId' or 'PreviousId'
        foreach (var link in links)
        {
          if (link.First.Id == link.Second.Id)
          {
            var canWriteLoop = false;
            // Note:  GetForeignKeyMember will append 'Id'
            foreach (var suppLoopFk in SupportedLoopForeignKeys)
            {
              if (GetForeignKeyMember((CSharpClass)link.First, suppLoopFk.ToLowerInvariant()) != null ||
              GetForeignKeyMember((CSharpClass)link.Second, suppLoopFk.ToLowerInvariant()) != null)
              {
                canWriteLoop = true;
                continue;
              }
            }
            if (canWriteLoop)
            {
              continue;
            }

            sb.AppendLine($"-- {Strings.SqlGenError_LoopRelationship}");
            sb.AppendLine($"-- [{link.First.Name}] <---> [{link.Second.Name}]");
            return false;
          }
        }

        // check for two links between two entities - BAD
        foreach (var link in links)
        {
          var otherLinks = links.Except(new[] { link });
          if (otherLinks.Any(otherLink =>
            (otherLink.First.Id == link.First.Id && otherLink.Second.Id == link.Second.Id) ||
            (otherLink.First.Id == link.Second.Id && otherLink.Second.Id == link.First.Id)))
          {
            sb.AppendLine($"-- {Strings.SqlGenError_TwoLinks}");
            return false;
          }
        }

        #endregion

        #region Delete

        sb.AppendLine("------------ Delete");

        // delete all links aka foreign keys
        foreach (var link in links)
        {
          DeleteForeignKey(sb, link);
        }
        sb.AppendLine();

        // delete all entities aka tables
        foreach (var entity in entities)
        {
          DeleteTable(sb, entity);
        }
        sb.AppendLine();

        #endregion

        #region Create

        sb.AppendLine("------------ Create");

        // create all entities aka tables
        foreach (var entity in entities)
        {
          CreateTable(sb, entity);
        }
        sb.AppendLine();

        // write primary key
        foreach (var entity in entities)
        {
          CreatePrimaryKey(sb, entity);
        }
        sb.AppendLine();

        // create all links aka foreign keys
        foreach (var link in links)
        {
          CreateForeignKey(sb, link);
        }
        sb.AppendLine();

        #endregion

        return true;
      }
      finally
      {
        var fileName = Path.ChangeExtension(Model.Name, ".sql");
        var filePath = Path.Combine(location, fileName);
        File.WriteAllText(filePath, sb.ToString());
      }
    }

    private static void DeleteTable(StringBuilder sb, CSharpClass type)
    {
      sb.AppendLine($"DROP TABLE {type.Name};");
    }

    private static void CreateTable(StringBuilder sb, CSharpClass type)
    {
      var pk = GetPrimaryKeyMember(type);

      sb.AppendLine($"CREATE TABLE {type.Name}");
      sb.AppendLine($"(");

      foreach (var field in type.Fields.OfType<CSharpField>())
      {
        var pkField = field.Name == pk?.Name ? "NOT NULL" : string.Empty;
        sb.AppendLine($"  {field.Name} {NetToSqlTypeMap[field.Type.ToLowerInvariant()]} {pkField},");
      }

      foreach (var op in type.Operations.OfType<CSharpProperty>())
      {
        var pkOp = op.Name == pk?.Name ? "NOT NULL" : string.Empty;
        sb.AppendLine($"  {op.Name} {NetToSqlTypeMap[op.Type.ToLowerInvariant()]} {pkOp},");
      }

      sb.AppendLine($");");
      sb.AppendLine();
    }

    private static string GetPrimaryKeyName(CSharpClass type)
    {
      return $"PK_{type.Name}";
    }

    private static void CreatePrimaryKey(StringBuilder sb, CSharpClass type)
    {
      var pk = GetPrimaryKeyMember(type);
      if (pk != null)
      {
        sb.AppendLine($"ALTER TABLE {type.Name} ADD CONSTRAINT {GetPrimaryKeyName(type)} PRIMARY KEY({pk.Name});");
      }
    }

    private static string GetForeignKeyName(IEntity first, IEntity second)
    {
      return $"FK_{first.Name}_{second.Name}";
    }

    private static void DeleteForeignKey(
      StringBuilder sb,
      string tableName,
      IEntity first,
      IEntity second,
      string firstPk,
      string secondPk)
    {
      sb.AppendLine($"ALTER TABLE {tableName} DROP CONSTRAINT {GetForeignKeyName(first, second)};");
    }

    private static void CreateForeignKey(
      StringBuilder sb,
      string tableName,
      IEntity first,
      IEntity second,
      string firstPk,
      string secondPk)
    {
      sb.AppendLine($"ALTER TABLE {tableName} ADD CONSTRAINT {GetForeignKeyName(first, second)} FOREIGN KEY({firstPk}) REFERENCES {first.Name}({secondPk});");
    }

    private void DeleteForeignKey(StringBuilder sb, EntityRelationship link)
    {
      // loop relationship
      if (link.First.Id == link.Second.Id)
      {
        // Note:  GetForeignKeyMember will append 'Id'
        foreach (var suppFk in SupportedLoopForeignKeys)
        {
          var fkLoop = GetForeignKeyMember((CSharpClass)link.First, suppFk.ToLowerInvariant());
          if (fkLoop != null)
          {
            var pk = GetPrimaryKeyMember((CSharpClass)link.First);
            DeleteForeignKey(sb, link.First.Name, link.First, link.First, fkLoop.Name, pk.Name);
            return;
          }
        }

        // should never get here as should have passed pre-flight checks
        var fileName = Path.ChangeExtension(Model.Name, ".sql");
        var errMsg = $"Unknown error deleting loop relationship:  [{link.First.Name}] <---> [{link.Second.Name}]";
        sb.AppendLine($"-- {errMsg}");
        throw new FileGenerationException(fileName, errMsg);
      }

      // [First] --> [Second]
      var fk1 = GetForeignKeyMember((CSharpClass)link.Second, link.First.Name);
      if (fk1 != null)
      {
        var pk1 = GetPrimaryKeyMember((CSharpClass)link.First);
        DeleteForeignKey(sb, link.Second.Name, link.First, link.Second, fk1.Name, pk1.Name);
      }

      // [Second] --> [First]
      var fk2 = GetForeignKeyMember((CSharpClass)link.First, link.Second.Name);
      if (fk2 != null)
      {
        var pk2 = GetPrimaryKeyMember((CSharpClass)link.Second);
        DeleteForeignKey(sb, link.First.Name, link.Second, link.First, fk2.Name, pk2.Name);
      }

      // create link tables
      if ((link.StartMultiplicity == MultiplicityType.ZeroOrMany || link.StartMultiplicity == MultiplicityType.OneOrMany) &&
      (link.EndMultiplicity == MultiplicityType.ZeroOrMany || link.EndMultiplicity == MultiplicityType.OneOrMany))
      {
        sb.AppendLine();
        sb.AppendLine($"-- delete link table: [{link.First.Name}] >+--+< [{link.Second.Name}]");
        DeleteLinkTable(sb, link);
        sb.AppendLine();
        return;
      }
    }

    private void CreateForeignKey(StringBuilder sb, EntityRelationship link)
    {
      // loop relationship
      if (link.First.Id == link.Second.Id)
      {
        // Note:  GetForeignKeyMember will append 'Id'
        foreach (var suppFk in SupportedLoopForeignKeys)
        {
          var fkLoop = GetForeignKeyMember((CSharpClass)link.First, suppFk.ToLowerInvariant());
          if (fkLoop != null)
          {
            var pk = GetPrimaryKeyMember((CSharpClass)link.First);
            CreateForeignKey(sb, link.First.Name, link.First, link.First, fkLoop.Name, pk.Name);
            return;
          }
        }

        // should never get here as should have passed pre-flight checks
        var fileName = Path.ChangeExtension(Model.Name, ".sql");
        var errMsg = $"Unknown error generating loop relationship:  [{link.First.Name}] <---> [{link.Second.Name}]";
        sb.AppendLine($"-- {errMsg}");
        throw new FileGenerationException(fileName, errMsg);
      }

      // create link tables
      if ((link.StartMultiplicity == MultiplicityType.ZeroOrMany || link.StartMultiplicity == MultiplicityType.OneOrMany) &&
      (link.EndMultiplicity == MultiplicityType.ZeroOrMany || link.EndMultiplicity == MultiplicityType.OneOrMany))
      {
        sb.AppendLine();
        sb.AppendLine($"-- generate link table: [{link.First.Name}] >+--+< [{link.Second.Name}]");
        CreateLinkTable(sb, link);
        sb.AppendLine();
        return;
      }

      // [First] --> [Second]
      var fk1 = GetForeignKeyMember((CSharpClass)link.Second, link.First.Name);
      if (fk1 != null)
      {
        var pk1 = GetPrimaryKeyMember((CSharpClass)link.First);
        CreateForeignKey(sb, link.Second.Name, link.First, link.Second, fk1.Name, pk1.Name);
      }

      // [Second] --> [First]
      var fk2 = GetForeignKeyMember((CSharpClass)link.First, link.Second.Name);
      if (fk2 != null)
      {
        var pk2 = GetPrimaryKeyMember((CSharpClass)link.Second);
        CreateForeignKey(sb, link.First.Name, link.Second, link.First, fk2.Name, pk2.Name);
      }
    }

    private static void DeleteLinkTable(StringBuilder sb, EntityRelationship link)
    {
      var linkTable = new CSharpClass
      {
        Name = $"{link.First.Name}_{link.Second.Name}"
      };

      DeleteTable(sb, linkTable);
    }

    private static void CreateLinkTable(StringBuilder sb, EntityRelationship link)
    {
      var linkTable = new CSharpClass
      {
        Name = $"{link.First.Name}_{link.Second.Name}"
      };

      var pk1 = GetPrimaryKeyMember((CSharpClass)link.First);
      var firstId = linkTable.AddProperty();
      firstId.Name = $"{link.First.Name}Id";
      firstId.Type = pk1.Type;

      var pk2 = GetPrimaryKeyMember((CSharpClass)link.Second);
      var secondId = linkTable.AddProperty();
      secondId.Name = $"{link.Second.Name}Id";
      secondId.Type = pk2.Type;

      CreateTable(sb, linkTable);

      CreateForeignKey(sb, linkTable.Name, link.First, link.Second, firstId.Name, pk1.Name);
      CreateForeignKey(sb, linkTable.Name, link.Second, link.First, secondId.Name, pk2.Name);
    }

    private static Member GetPrimaryKeyMember(CSharpClass type)
    {
      var pk = GetForeignKeyMember(type, string.Empty);
      return pk;
    }

    private static Member GetForeignKeyMember(CSharpClass type, string otherTypeName)
    {
      // give preference to Property over Field
      var fk = type.Operations.OfType<CSharpProperty>().SingleOrDefault(op => op.Name.ToLowerInvariant() == $"{otherTypeName.ToLowerInvariant()}id") as Member ??
                type.Fields.OfType<CSharpField>().SingleOrDefault(field => field.Name.ToLowerInvariant() == $"{otherTypeName.ToLowerInvariant()}id");
      return fk;
    }

    // [.NET type (lowercase)] --> [MS SQL type]
    private readonly static Dictionary<string, string> NetToSqlTypeMap = new Dictionary<string, string>
    {
      { "int", "int" },
      { "string", "nvarchar(max)" },
      { "bool", "bit" },
      { "datetime", "datetime2" },
      { "decimal", "decimal" },
      { "float", "float" },
      { "double", "float" },
      { "guid", "uniqueidentifier" }
    };
  }
}
