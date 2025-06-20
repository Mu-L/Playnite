﻿using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Markup;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Playnite SDK")]
[assembly: AssemblyDescription("Playnite Development Kit Library")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Playnite")]
[assembly: AssemblyCopyright("Copyright © Josef Nemec 2020")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("19bc9097-5705-4352-90e2-99f0c63230d0")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("6.12.0.0")]
[assembly: AssemblyFileVersion("6.12.0.0")]
[assembly: InternalsVisibleTo("Playnite.DesktopApp")]
[assembly: InternalsVisibleTo("Playnite.FullscreenApp")]
[assembly: InternalsVisibleTo("Playnite.Tests")]
[assembly: InternalsVisibleTo("Playnite.DesktopApp.Tests")]
[assembly: InternalsVisibleTo("Playnite.FullscreenApp.Tests")]
[assembly: InternalsVisibleTo("Playnite")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "Playnite.SDK.Models")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "Playnite.SDK.Controls")]