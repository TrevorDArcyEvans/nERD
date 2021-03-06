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
using System.IO;
using System.Windows.Forms;

namespace NClass.CodeGenerator
{
  internal sealed class CSharpProjectGenerator : ProjectGenerator
  {
    public CSharpProjectGenerator(Model model) :
      base(model)
    {
    }

    public override string RelativeProjectFileName
    {
      get
      {
        var fileName = ProjectName + ".csproj";
        var directoryName = ProjectName;

        return Path.Combine(directoryName, fileName);
      }
    }

    protected override SourceFileGenerator CreateSourceFileGenerator(TypeBase type)
    {
      return new CSharpSourceFileGenerator(type, RootNamespace);
    }

    protected override bool GenerateProjectFiles(string location)
    {
      try
      {
        var templateDir = Path.Combine(Application.StartupPath, "Templates");
        var templateFile = Path.Combine(templateDir, "csproj.template");
        var projectFile = Path.Combine(location, RelativeProjectFileName);

        using (StreamReader reader = new StreamReader(templateFile))
        {
          using (StreamWriter writer = new StreamWriter(projectFile, false, reader.CurrentEncoding))
          {
            while (!reader.EndOfStream)
            {
              var line = reader.ReadLine();

              line = line.Replace("${RootNamespace}", RootNamespace);
              line = line.Replace("${AssemblyName}", ProjectName);

              if (line.Contains("${SourceFile}"))
              {
                foreach (var fileName in FileNames)
                {
                  var newLine = line.Replace("${SourceFile}", fileName);
                  writer.WriteLine(newLine);
                }
              }
              else
              {
                writer.WriteLine(line);
              }
            }
          }
        }

        return true;
      }
      catch
      {
        return false;
      }
    }
  }
}
