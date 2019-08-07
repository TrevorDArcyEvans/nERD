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
using NClass.EntityRelationshipDiagram;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;

namespace NClass.CodeGenerator
{
  internal sealed partial class Settings
  {

    public Settings()
    {
      SettingsLoaded += Settings_SettingsLoaded;
    }

    private readonly Dictionary<Language, StringCollection> _importLists = new Dictionary<Language, StringCollection>();
    public IDictionary<Language, StringCollection> ImportList
    {
      get { return _importLists; }
    }

    private void Settings_SettingsLoaded(object sender, SettingsLoadedEventArgs e)
    {
      if (CSharpImportList == null)
        CSharpImportList = new StringCollection();
      if (ErdImportList == null)
        ErdImportList = new StringCollection();

      ImportList.Clear();
      ImportList.Add(CSharpLanguage.Instance, CSharpImportList);
      ImportList.Add(ErdLanguage.Instance, ErdImportList);

      if (string.IsNullOrEmpty(DestinationPath))
      {
        string myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        DestinationPath = Path.Combine(myDocuments, "NClass Generated Projects");
      }
    }
  }
}
