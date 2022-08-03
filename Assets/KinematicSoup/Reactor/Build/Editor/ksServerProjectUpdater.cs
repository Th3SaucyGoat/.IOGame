/*
KINEMATICSOUP CONFIDENTIAL
 Copyright(c) 2014-2022 KinematicSoup Technologies Incorporated 
 All Rights Reserved.

NOTICE:  All information contained herein is, and remains the property of 
KinematicSoup Technologies Incorporated and its suppliers, if any. The 
intellectual and technical concepts contained herein are proprietary to 
KinematicSoup Technologies Incorporated and its suppliers and may be covered by
U.S. and Foreign Patents, patents in process, and are protected by trade secret
or copyright law. Dissemination of this information or reproduction of this
material is strictly forbidden unless prior written permission is obtained from
KinematicSoup Technologies Incorporated.
*/

using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using KS.Unity.Editor;

namespace KS.Reactor.Client.Unity.Editor
{
    /// <summary>Provides methods for updating and syncing the KSServerRuntime project with the file system.</summary>
    public class ksServerProjectUpdater 
    {
        /// <summary>Singleton instance</summary>
        public static ksServerProjectUpdater Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new ksServerProjectUpdater();
                }
                return m_instance;
            }
        }
        private static ksServerProjectUpdater m_instance;

        /// <summary>Singleton constructor</summary>
        private ksServerProjectUpdater()
        {
        }

        /// <summary>
        /// Updates the contents of the KSServerRuntime project with any unreferenced .cs and dll files and
        /// removes references to missing files.
        /// </summary>
        public void UpdateIncludes()
        {
            if (!GenerateMissingFiles(true))
            {
                return;
            }
            try
            {
                XmlDocument projectXml = LoadProject();
                if (projectXml == null)
                {
                    return;
                }

                if (UpdateCompileIncludes(projectXml) || 
                    UpdateReferenceIncludes(projectXml) ||
                    UpdateDefineSymbols(projectXml))
                {
                    SaveProject(projectXml);
                }
            }
            catch (Exception e)
            {
                ksLog.Error(this, "Error updating KSServerRuntime project includes", e);
            }
        }

        /// <summary>Edits the KSServerRuntime project so that output path builds to the installation path.</summary>
        public void UpdateOutputPath()
        {
            if (!GenerateMissingFiles(false))
            {
                return;
            }
            try
            {
                XmlDocument projectXml = LoadProject();
                if (projectXml == null)
                {
                    return;
                }
                XmlNode root = projectXml.DocumentElement;
                XmlElement rootElement = root as XmlElement;
                XmlNodeList nodeList = rootElement.SelectNodes("//*");
                string outputPath;
                string proxyPath;
                GetRelativePaths(out outputPath, out proxyPath);
                foreach (XmlNode node in nodeList)
                {
                    switch (node.Name)
                    {
                        case "OutputPath":
                            node.InnerText = outputPath + "KSServerRuntime/";
                            break;
                        case "HintPath":
                            if (node.InnerText.EndsWith("KSReactor.dll"))
                            {
                                node.InnerText = outputPath + "KSReactor.dll";
                            }
                            else if (node.InnerText.EndsWith("KSCommon.dll"))
                            {
                                node.InnerText = outputPath + "KSCommon.dll";
                            }
                            else if (node.InnerText.EndsWith("KSLZMA.dll"))
                            {
                                node.InnerText = outputPath + "KSLZMA.dll";
                            }
                            break;
                        case "PostBuildEvent":
                            node.InnerText = GetProxyCommand(proxyPath);
                            break;
                    }
                }
                SaveProject(projectXml);
            }
            catch (Exception e)
            {
                ksLog.Error(this, "Error updating KSServerRuntime project output path", e);
            }
        }

        /// <summary>Adds a file to the server runtime project's includes.</summary>
        /// <param name="fileName">Name of file to add.</param>
        public void AddFileToProject(string fileName)
        {
            if (!GenerateMissingFiles(true))
            {
                return;
            }
            fileName = fileName.Replace('/', '\\');
            try
            {
                XmlDocument projectXml = LoadProject();
                if (!IsFileIncluded(projectXml, fileName))
                {
                    HashSet<string> fileHash = new HashSet<string>();
                    fileHash.Add(fileName);
                    AddFilesToProject(projectXml, fileHash);
                    SaveProject(projectXml);
                }
            }
            catch (Exception e)
            {
                ksLog.Error(this, "Error adding " + fileName + " to KSServerRuntime project", e);
            }
        }

        /// <summary>
        /// Generates the server runtime solution and/or project if the they are missing and a valid
        /// installation path is set in the project settings.
        /// </summary>
        /// <param name="projectGeneratedReturnValue">Value to return if the project is generated.</param>
        /// <returns>
        /// true if the server runtime project already existed. False if the project does not exist and we were unable
        /// to generate it. Returns projectGeneratedReturnValue if the project was generated.
        /// </returns>
        private bool GenerateMissingFiles(bool projectGeneratedReturnValue)
        {
            if (!Directory.Exists(ksReactorConfig.Instance.Server.ServerPath))
            {
                return false;
            }

            if (!ksPathUtils.Create(ksPaths.ServerRuntime, true))
            {
                return false;
            }

            if (!ksPathUtils.Create(ksPaths.Proxies, true))
            {
                return false;
            }

            if (!File.Exists(ksPaths.ServerRuntimeSolution))
            {
                GenerateSolution();
            }

            if (!File.Exists(ksPaths.ServerRuntimeProject))
            {
                GenerateProject();
                return projectGeneratedReturnValue;
            }
            return true;
        }

        /// <summary>Loads the server runtime project.</summary>
        /// <returns>Server runtime XML project configuration</returns>
        private XmlDocument LoadProject()
        {
            XmlDocument projectXml = new XmlDocument();
            try
            {
                projectXml.Load(ksPaths.ServerRuntimeProject);
            }
            catch (Exception e)
            {
                ksLog.Error(this, "Error loading " + ksPaths.ServerRuntimeProject, e);
                return null;
            }
            return projectXml;
        }

        /// <summary>Saves the server runtime project.</summary>
        /// <param name="projectXml">Server runtime XML project configuration data to save.</param>
        private void SaveProject(XmlDocument projectXml)
        {
            try
            {
                projectXml.Save(ksPaths.ServerRuntimeProject);
            }
            catch (Exception e)
            {
                ksLog.Error(this, "Unable to write to " + ksPaths.ServerRuntimeProject, e);
            }
        }

        /// <summary>Adds files to a .csproj's includes.</summary>
        /// <param name="projectXml">Server runtime XML project configuration data to update.</param>
        /// <param name="fileNames">Files to add.</param>
        private void AddFilesToProject(XmlDocument projectXml, HashSet<string> fileNames)
        {
            XmlNode root = projectXml.DocumentElement;
            XmlNode node = FindNode(root, "Compile");
            if (node == null)
            {
                node = projectXml.CreateElement("ItemGroup", root.NamespaceURI);
                root.AppendChild(node);
            }
            else
            {
                node = node.ParentNode;
            }
            foreach (string fileName in fileNames)
            {
                XmlElement element = projectXml.CreateElement("Compile", root.NamespaceURI);
                element.SetAttribute("Include", fileName);
                node.AppendChild(element);
            }
        }

        /// <summary>Returns a fileHash set of .cs files from the KSServerRuntime project folder.</summary>
        /// <returns>File names found.</returns>
        private HashSet<string> GetServerFiles()
        {
            HashSet<string> filesInDirectory = new HashSet<string>();
            GetFilesInFolder(ksPaths.ServerRuntime, "cs", filesInDirectory);
            return filesInDirectory;
        }

        /// <summary>
        /// Adds references to unreferenced .cs files in the server runtime folder, and removes references to missing
        /// files.
        /// </summary>
        /// <param name="projectXml">Server runtime XML project configuration data to update.</param>
        /// <returns>True if a reference was added or removed.</returns>
        private bool UpdateCompileIncludes(XmlDocument projectXml)
        {
            HashSet<string> files = GetServerFiles();
            List<XmlNode> removeList = new List<XmlNode>();
            XmlNodeList compileNodes = FindNodes(projectXml.DocumentElement, "Compile");
            bool changed = false;
            foreach (XmlNode node in compileNodes)
            {
                XmlNode includeAttribute = node.Attributes.GetNamedItem("Include");
                if (includeAttribute == null)
                {
                    continue;
                }
                string path = includeAttribute.InnerText.Replace('/', '\\');
                if (!path.EndsWith("*.cs") && !files.Remove(path))// * is not a wild card
                {
                    removeList.Add(node);
                    changed = true;
                }
            }
            foreach (XmlNode node in removeList)
            {
                RemoveNode(node);
            }
            if (files.Count > 0)
            {
                AddFilesToProject(projectXml, files);
                changed = true;
            }
            return changed;
        }

        /// <summary>Update the project file with server define symbols.</summary>
        /// 
        /// <param name="projectXml">Server runtime XML project configuration data to update.</param>
        /// <returns>True if the project was updated.</returns>
        private bool UpdateDefineSymbols(XmlDocument projectXml)
        {
            bool updated = false;
            string symbols = ksReactorConfig.Instance.Server.DefineSymbols.Trim();

            //replace commas, and spaces with semi-colons
            symbols = symbols.Replace(',', ';');
            symbols = symbols.Replace(' ', ';');
            if (symbols.Length > 0)
            {
                symbols += ";";
            }

            XmlNode root = projectXml.DocumentElement;
            XmlNodeList defineNodes = FindNodes(root, "DefineConstants");

            foreach (XmlNode node in defineNodes)
            {
                if (node != null)
                {
                    string newValue = symbols;

                    // REACTOR_SERVER is always the first built-in define symbol.
                    int index = node.InnerText.LastIndexOf("REACTOR_SERVER");
                    // Append build-in define symbols to the custom define symbols.
                    if (index >= 0)
                    {
                        newValue = symbols + node.InnerText.Substring(index);
                    }
                    else
                    {
                        newValue = symbols;
                    }

                    if (node.InnerText != newValue)
                    {
                        node.InnerText = newValue;
                        updated = true;
                    }
                }
            }

            return updated;
        }

        /// <summary>Checks if a .cs file is included in a project.</summary>
        /// <param name="projectXml">Server runtime XML project configuration data to check.</param>
        /// <param name="fileName">File name to check for.</param>
        /// <returns>True if the file is included in the project.</returns>
        private bool IsFileIncluded(XmlDocument projectXml, string fileName)
        {
            XmlNode root = projectXml.DocumentElement;
            XmlNodeList compileNodes = FindNodes(root, "Compile");
            foreach (XmlNode node in compileNodes)
            {
                XmlNode includeAttribute = node.Attributes.GetNamedItem("Include", root.NamespaceURI);
                if (includeAttribute != null && includeAttribute.InnerText.Replace('/', '\\').Equals(fileName))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Adds libraries to a project's references.</summary>
        /// <param name="projectXml">Server runtime XML project configuration data to update.</param>
        /// <param name="libraries">Libraries to add.</param>
        private void AddLibrariesToProject(XmlDocument projectXml, HashSet<string> libraries)
        {
            XmlNode root = projectXml.DocumentElement;
            XmlNode node = FindNode(root, "Reference");
            if (node == null)
            {
                node = projectXml.CreateElement("ItemGroup", root.NamespaceURI);
                root.AppendChild(node);
            }
            else
            {
                node = node.ParentNode;
            }
            foreach (string library in libraries)
            {
                string name = library;
                int index = name.LastIndexOf('.');
                if (index >= 0)
                {
                    name = name.Substring(0, index);
                }
                index = name.LastIndexOf('/');
                if (index >= 0)
                {
                    name = name.Substring(index + 1);
                }
                XmlElement element = projectXml.CreateElement("Reference", root.NamespaceURI);
                element.SetAttribute("Include", name);
                XmlElement hintPath = projectXml.CreateElement("HintPath", root.NamespaceURI);
                hintPath.InnerText = library;
                element.AppendChild(hintPath);
                node.AppendChild(element);
            }
        }

        /// <returns>
        /// DLLs in the server runtime and common folders, with paths relative to the server runtime folder.
        /// </returns>
        private HashSet<string> GetLibraries()
        {
            HashSet<string> libraries = new HashSet<string>();
            GetFilesInFolder(ksPaths.ServerRuntime, "dll", libraries);
            GetFilesInFolder(ksPaths.CommonScripts, "dll", libraries, ksPaths.ServerRuntime);
            return libraries;
        }

        /// <summary>
        /// Adds references to unreferenced dlls in the server runtime folder,
        /// and removes references to missing dlls.
        /// </summary>
        /// <param name="projectXml">Server runtime XML project configuration data to update.</param>
        /// <returns>True if a reference was added or removed.</returns>
        private bool UpdateReferenceIncludes(XmlDocument projectXml)
        {
            HashSet<string> libraries = GetLibraries();
            List<XmlNode> removeList = new List<XmlNode>();
            XmlNodeList compileNodes = FindNodes(projectXml.DocumentElement, "HintPath");
            bool changed = false;
            foreach (XmlNode node in compileNodes)
            {
                if (node.InnerText.EndsWith("KSReactor.dll") || node.InnerText.EndsWith("KSCommon.dll") || node.InnerText.EndsWith("KSLZMA.dll"))
                {
                    continue;
                }
                string path = node.InnerText.Replace('/', '\\');
                if (!libraries.Remove(path))
                {
                    removeList.Add(node.ParentNode);
                    changed = true;
                }
            }
            foreach (XmlNode node in removeList)
            {
                RemoveNode(node);
            }
            if (libraries.Count > 0)
            {
                AddLibrariesToProject(projectXml, libraries);
                changed = true;
            }
            return changed;
        }

        /// <summary>Fills a hashset with a list of files in a folder and its subfolders.</summary>
        /// <param name="path">Path to search for files.</param>
        /// <param name="extension">Extension of files to find.</param>
        /// <param name="outFiles">Hash set to put file names in.</param>
        /// <param name="relativeTo">
        /// If provided, file names will be relative to this folder. If null, they are relative to <paramref name="path"/>.
        /// </param>
        private void GetFilesInFolder(string path, string extension, HashSet<string> outFiles, string relativeTo = null)
        {
            string relativePath = "";
            if (relativeTo != null)
            {
                Uri root = new Uri(ksPaths.ProjectRoot);
                Uri relativeToUri = new Uri(root, relativeTo);
                relativePath = relativeToUri.MakeRelativeUri(new Uri(root, path)).ToString();
            }
            try
            {
                if (!Directory.Exists(path))
                {
                    return;
                }
                foreach (string fileName in Directory.GetFiles(path, "*." + extension, SearchOption.AllDirectories))
                {
                    string name = fileName.Substring(path.Length).Replace('/', '\\');
                    outFiles.Add(relativePath + name);
                }
            }
            catch (Exception e)
            {
                ksLog.Error(this, "Error loading " + extension + " files in " + path, e);
            }
        }

        /// <summary>Removes an XML node. Remove's the node's parent if the parent has no other children.</summary>
        /// <param name="node">Node to remove.</param>
        private void RemoveNode(XmlNode node)
        {
            if (node.ParentNode.ChildNodes.Count == 1)
            {
                node.ParentNode.ParentNode.RemoveChild(node.ParentNode);
            }
            else
            {
                node.ParentNode.RemoveChild(node);
            }
        }

        /// <summary>Finds all nodes of a given type in an XML document.</summary>
        /// <param name="node">Node with the namespace to search in.</param>
        /// <param name="type">Type of node to look for.</param>
        /// <returns>Nodes of the given type.</returns>
        private XmlNodeList FindNodes(XmlNode node, string type)
        {
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(node.OwnerDocument.NameTable);
            namespaceManager.AddNamespace("ns", node.NamespaceURI);
            return node.SelectNodes("//ns:" + type, namespaceManager);
        }

        /// <summary>Finds the first nodes of a given type in an XML document.</summary>
        /// <param name="node">Node with the namespace to search in.</param>
        /// <param name="type">Type of node to look for.</param>
        /// <returns>First node of the given type, or null if no nodes of that type are found.</returns>
        private XmlNode FindNode(XmlNode node, string type)
        {
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(node.OwnerDocument.NameTable);
            namespaceManager.AddNamespace("ns", node.NamespaceURI);
            return node.SelectSingleNode("//ns:" + type, namespaceManager);
        }

        /// <summary>Gets output and proxy paths.</summary>
        /// <param name="outputPath">Path to build to relative to the server runtime project directory.</param>
        /// <param name="proxyPath">Path to put proxy scripts in relative to the reflection tool.</param>
        private void GetRelativePaths(out string outputPath, out string proxyPath)
        {
            Uri root = new Uri(ksPaths.ProjectRoot);
            Uri installationPathUri = null;
            try
            {
                installationPathUri = new Uri(ksReactorConfig.Instance.Server.ServerPath);
            }
            catch (UriFormatException)
            {
                installationPathUri = new Uri(root, ksReactorConfig.Instance.Server.ServerPath);
            }
            Uri reflectionToolPathUri = new Uri(installationPathUri, "KSServerRuntime/");
            Uri serverRuntimeUri = new Uri(root, ksPaths.ServerRuntime);
            outputPath = serverRuntimeUri.MakeRelativeUri(installationPathUri).ToString();
            proxyPath = reflectionToolPathUri.MakeRelativeUri(new Uri(root, ksPaths.Proxies)).ToString();
#if !UNITY_2019_3_OR_NEWER
            if (EditorApplication.scriptingRuntimeVersion != ScriptingRuntimeVersion.Latest)
            {
                outputPath = "../" + outputPath;
                proxyPath = "../" + proxyPath;
            }
#endif
        }

        /// <summary>Gets the command to generate proxy scripts.</summary>
        /// <param name="proxyPath">Path to put proxy scripts in.</param>
        /// <returns>Command to generate proxy scripts.</returns>
        private string GetProxyCommand(string proxyPath)
        {
            switch (ksPaths.OperatingSystem)
            {
                case "Unix":
                    return "mono KSReflectionTool.exe \"" + proxyPath + "\"";
                default:
                    return "KSReflectionTool.exe \"" + proxyPath + "\"";
            }
        }

        /// <summary>Generates the KSServerRuntime solution.</summary>
        private void GenerateSolution()
        {
            string template = @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 15
VisualStudioVersion = 15.0.27004.2005
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{%PROJECT_GUID%}"") = ""KSServerRuntime"", ""KSServerRuntime.csproj"", ""{%CONFIG_GUID%}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		LocalDebug|Any CPU = LocalDebug|Any CPU
		LocalRelease|Any CPU = LocalRelease|Any CPU
		OnlineDebug|Any CPU = OnlineDebug|Any CPU
        OnlineRelease|Any CPU = OnlineRelease|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{%CONFIG_GUID%}.LocalDebug|Any CPU.ActiveCfg = LocalDebug|Any CPU
		{%CONFIG_GUID%}.LocalDebug|Any CPU.Build.0 = LocalDebug|Any CPU
        {%CONFIG_GUID%}.LocalRelease|Any CPU.ActiveCfg = LocalRelease|Any CPU
		{%CONFIG_GUID%}.LocalRelease|Any CPU.Build.0 = LocalRelease|Any CPU
		{%CONFIG_GUID%}.OnlineDebug|Any CPU.ActiveCfg = OnlineDebug|Any CPU
		{%CONFIG_GUID%}.OnlineDebug|Any CPU.Build.0 = OnlineDebug|Any CPU
		{%CONFIG_GUID%}.OnlineRelease|Any CPU.ActiveCfg = OnlineRelease|Any CPU
		{%CONFIG_GUID%}.OnlineRelease|Any CPU.Build.0 = OnlineRelease|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal";

            template = template.Replace("%PROJECT_GUID%", Guid.NewGuid().ToString());
            template = template.Replace("%CONFIG_GUID%", Guid.NewGuid().ToString());
            Write(ksPaths.ServerRuntimeSolution, template);
        }

        /// <summary>Generates the KSServerRuntime project.</summary>
        private void GenerateProject()
        {
            string outputPath;
            string proxyPath;
            GetRelativePaths(out outputPath, out proxyPath);
            string template = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""$(MSBuildExtensionsPath)/$(MSBuildToolsVersion)/Microsoft.Common.props"" Condition=""Exists('$(MSBuildExtensionsPath)/$(MSBuildToolsVersion)/Microsoft.Common.props')"" />
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">LocalDebug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProjectGuid>{320C10B1-2EC3-4A18-A2BB-BE17FE126BA0}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>KSServerRuntime</RootNamespace>
    <TargetFrameworkVersion>v4.5</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
    <TargetFrameworkProfile />
    <BaseIntermediateOutputPath>../../../../KSServerRuntime/obj/</BaseIntermediateOutputPath>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'LocalDebug|AnyCPU' "">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <AssemblyName>KSServerRuntime.Local</AssemblyName>
    <OutputPath>" + outputPath + @"KSServerRuntime/</OutputPath>
    <DefineConstants>REACTOR_SERVER;REACTOR_LOCAL_SERVER;DEBUG;TRACE;</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
    <PlatformTarget>x64</PlatformTarget>
    <RunPostBuildEvent>OnOutputUpdated</RunPostBuildEvent>
    <PostBuildEvent>" + GetProxyCommand(proxyPath) + @"</PostBuildEvent>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'LocalRelease|AnyCPU' "">
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <AssemblyName>KSServerRuntime.Local</AssemblyName>
    <OutputPath>" + outputPath + @"KSServerRuntime/</OutputPath>
    <DefineConstants>REACTOR_SERVER;REACTOR_LOCAL_SERVER;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
    <PlatformTarget>x64</PlatformTarget>
    <RunPostBuildEvent>OnOutputUpdated</RunPostBuildEvent>
    <PostBuildEvent>" + GetProxyCommand(proxyPath) + @"</PostBuildEvent>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'OnlineDebug|AnyCPU' "">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <AssemblyName>KSServerRuntime</AssemblyName>
    <OutputPath>" + outputPath + @"KSServerRuntime/</OutputPath>
    <DefineConstants>REACTOR_SERVER;DEBUG;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
    <PlatformTarget>x64</PlatformTarget>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'OnlineRelease|AnyCPU' "">
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <AssemblyName>KSServerRuntime</AssemblyName>
    <OutputPath>" + outputPath + @"KSServerRuntime/</OutputPath>
    <DefineConstants>REACTOR_SERVER;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
    <PlatformTarget>x64</PlatformTarget>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
    <Reference Include=""System.Core"" />
    <Reference Include=""System.Xml.Linq"" />
    <Reference Include=""System.Data.DataSetExtensions"" />
    <Reference Include=""System.Data"" />
    <Reference Include=""System.Xml"" />
    <Reference Include=""KSCommon"">
      <HintPath>" + outputPath + @"KSCommon.dll</HintPath>
    </Reference>
    <Reference Include=""KSReactor"">
      <HintPath>" + outputPath + @"KSReactor.dll</HintPath>
    </Reference>
    <Reference Include=""KSLZMA"">
      <HintPath>" + outputPath + @"KSLZMA.dll</HintPath>
    </Reference>
  </ItemGroup>
  <ItemGroup>
    <Compile Include=""..\..\Common\**\*.cs"">
      <Link>%(RecursiveDir)%(FileName)</Link>
    </Compile>
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)/Microsoft.CSharp.targets"" />
</Project>";
            Write(ksPaths.ServerRuntimeProject, template);
        }

        /// <summary>Writes to a file.</summary>
        /// <param name="path">Path to write to.</param>
        /// <param name="data">Data to write.</param>
        private void Write(string path, string data)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(path))
                {
                    writer.Write(data);
                    writer.Close();
                }
            }
            catch (Exception e)
            {
                ksLog.Error(this, "Error writing to " + path, e);
            }
        }
    }
}
