using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using System.IO;
using System.Linq;
using System.Xml;
using static System.Int32;

namespace MergeEnvironments
{
    class Program
    {
        public static async Task<int> Main() =>
            await new CliApplicationBuilder()
                .AddCommandsFromThisAssembly()
                .Build()
                .RunAsync();
    }
    
    [Command]
    public class HelloWorldCommand : ICommand
    {
        [CommandParameter(0, Description = "Environment 1 to merge")]
        public string Environment1 { get; set; }
        
        [CommandParameter(1, Description = "Environment 2 to merge")]
        public string Environment2 { get; set; }
        
        [CommandOption("out-dir", Description = "Output directory for where merged environment will be written to")]
        public string OutputDirectoryPath { get; set; }
        
        public ValueTask ExecuteAsync(IConsole console)
        {

            bool dir1 = Directory.Exists(Environment1);
            bool dir2 = Directory.Exists(Environment2);
            
            if (!dir1 || !dir2)
            {
                if (!dir1)
                {
                    console.Output.WriteLine("Environment1 does not exist on path:");
                    console.Output.WriteLine(Environment1);
                }

                if (!dir2)
                {
                    console.Output.WriteLine("Environment2 does not exist on path:");
                    console.Output.WriteLine(Environment2);
                }

                return default;
            }

            DirectoryInfo outputDirectory = null;
            
            if (OutputDirectoryPath != null)
            {
                try
                {

                    if (Directory.Exists(OutputDirectoryPath))
                    {
                        var outputDirectoryInfo = new DirectoryInfo(OutputDirectoryPath);

                        var sameNameDirectories = Directory.GetDirectories(OutputDirectoryPath.Replace(outputDirectoryInfo.Name, ""), outputDirectoryInfo.Name + "*");

                        OutputDirectoryPath += sameNameDirectories.Length;
                    }
                    
                    outputDirectory = Directory.CreateDirectory(OutputDirectoryPath);

                    console.Output.WriteLine("Merge path created at:");
                    console.Output.WriteLine(outputDirectory.FullName);

                }
                catch (Exception e)
                {
                    console.Output.WriteLine(e.Message);
                    return default;
                }
            }

            outputDirectory ??= Directory.CreateDirectory(Guid.NewGuid().ToString());

            var environmentLocations = new EnvironmentLocations(Environment1, Environment2, outputDirectory.FullName);
            
            MergeResources(environmentLocations, console);


            // Return empty task because our command executes synchronously
            return default;
        }


        private void MergeResources(EnvironmentLocations environmentLocations, IConsole console)
        {
            if (!Directory.Exists(environmentLocations.Environment1Path + @"\Resources") &&
                !Directory.Exists(environmentLocations.Environment2Path + @"\Resources"))
            {
                console.Output.WriteLine("Could not find Resources folder in both environments, skipping");
                return;
            }

            var env1ResourcesDir =  new DirectoryInfo(environmentLocations.Environment1Path + @"\Resources");
            var env2ResourcesDir =  new DirectoryInfo(environmentLocations.Environment2Path + @"\Resources");
            
            var env1ResourcesSubDirs = env1ResourcesDir.GetDirectories();
            var env2ResourcesSubDirs = env2ResourcesDir.GetDirectories().ToList();

            foreach (var env1SubDir in env1ResourcesSubDirs)
            {
                console.Output.WriteLine("Merging " + env1SubDir.Name);
                
                var subfolderIndex =  env2ResourcesSubDirs.FindIndex(dir => dir.Name == env1SubDir.Name);

                if (subfolderIndex > -1)
                {
                    env2ResourcesSubDirs.RemoveAt(subfolderIndex);
                    MergeResourceFolder(environmentLocations, env1SubDir.Name, console);
                }
                else
                {
                    
                }

            }

            console.Output.WriteLine("------------------------------------------");
            console.Output.WriteLine("Merge Is Done");
            console.Output.WriteLine("I am pretty sure everything went okay, like at least 82% sure");
            


            // var env1RootFiles = env1Resources.GetFiles();
            //
            // foreach (var env1File in env1RootFiles)
            // {
            //     if (env1File.Extension == ".xml")
            //     {
            //         console.Output.WriteLine(env1File.Name);
            //     }
            // }

        }

        private static readonly  string[] fileBasedFolders = new string[] {"Assets", "Documents", "Fonts", "Workspaces", "ViewPreferences", "ThreeDModels", "FoldingSettings"};
        
        private void MergeResourceFolder(EnvironmentLocations environmentLocations, string folderName, IConsole console)
        {
            var env1FolderDir = new DirectoryInfo(environmentLocations.Environment1Path + @"\Resources\" + folderName);
            var env2FolderDir = new DirectoryInfo(environmentLocations.Environment2Path + @"\Resources\" + folderName);

            var merge2FolderDir =
                Directory.CreateDirectory(environmentLocations.MergePath + @"\Resources\" + folderName);

            var env1FolderFiles = env1FolderDir.GetFiles();
            var env2FolderFiles = env2FolderDir.GetFiles();

            var folderFilesCombined = new List<FileInfo>();
            folderFilesCombined.AddRange(env1FolderFiles);
            folderFilesCombined.AddRange(env2FolderFiles);

            var dataXmlsCombined = GetDataXmls(folderFilesCombined);

            if (fileBasedFolders.Contains(folderName))
            {

                for (int i = 0; i < dataXmlsCombined.Count; i++)
                {
                    var dataXml = dataXmlsCombined[i];
                    
                    File.Copy(dataXml.FileInfo.FullName, merge2FolderDir.FullName + "/data" + ((i == 0) ? "" :  $"{i + 1:0000000}") + ".xml");
                }

                // var noDataFiles =  folderFilesCombined .Where(file => !file.Name.Contains("data"));
                //
                // // Env 1 files will overwrite env 2
                // foreach (var fileInfo in noDataFiles)
                // {
                //     File.Copy(fileInfo.FullName, merge2FolderDir.FullName + "/" + fileInfo.Name);
                // }

                var filesData = new List<(FileInfo, string)>();

                GetAllFilesAndCreateDirectories(env1FolderDir, filesData, merge2FolderDir.FullName, true);
                GetAllFilesAndCreateDirectories(env2FolderDir, filesData, merge2FolderDir.FullName, true);

                var filesCount = filesData.Count;
                
                console.Output.WriteLine(filesCount + " file found");

                for (int i = 0; i < filesCount; i++)
                {
                    var fileData = filesData[i];
                    
                    if (File.Exists(fileData.Item2)) continue;
                    
                    File.Copy(fileData.Item1.FullName, fileData.Item2);
                    console.Output.WriteLine("\r" + i + "/" + filesCount);
                }

            }
            else
            {

                if (dataXmlsCombined.Count == 0) return;

                var mergeDataDoc = new XmlDocument();
                mergeDataDoc.Load(dataXmlsCombined[0].FileInfo.FullName);

                if (mergeDataDoc.FirstChild?.FirstChild == null) { console.Output.WriteLine("data.xml for " + folderName + " is wrong"); return;}
                
                var mergeItemsNode = mergeDataDoc.FirstChild.FirstChild;

                for (int i = 1; i < dataXmlsCombined.Count; i++)
                {
                    var dataXmlDoc = new XmlDocument();
                    dataXmlDoc.Load(dataXmlsCombined[i].FileInfo.FullName);
                    
                    if (dataXmlDoc.FirstChild?.FirstChild == null) { console.Output.WriteLine("data.xml for " + folderName + " is wrong"); continue;}

                    var dataItemsNode = dataXmlDoc.FirstChild.FirstChild;

                    foreach (XmlNode itemNode in dataItemsNode.ChildNodes)
                    {
                        var newItemNode = mergeDataDoc.ImportNode(itemNode, true);

                        mergeItemsNode.AppendChild(newItemNode);
                    }

                }

                var file = File.Create(environmentLocations.MergePath + @"\Resources\" + folderName + @"\data.xml");

                var writer = new StreamWriter(file);
                
                writer.Write(mergeDataDoc.OuterXml);
                
                writer.Close();
                file.Close();
            }
        }

        /* First file will overwrite second file of same name */
        private void GetAllFilesAndCreateDirectories(DirectoryInfo parentDirectoy, List<(FileInfo, string)> filesData, string curerntNewPath, bool removeDataXmls = false)
        {
            var files = parentDirectoy.GetFiles();
            var childDirs = parentDirectoy.GetDirectories();

            foreach (var file in files)
            {
                if (removeDataXmls)
                {
                    if (file.Name.Contains("data") && file.Name.Contains("xml"))
                    {
                        continue;
                    }
                }
                
                if (filesData.FindIndex(fileData => fileData.Item2 == curerntNewPath + "/" + file.Name) == -1)
                {
                    filesData.Add((file, curerntNewPath + "/" + file.Name));
                }
            }

            foreach (var childDir in childDirs)
            {
                var newPath = curerntNewPath + "/" + childDir.Name;

                if (!Directory.Exists(newPath))
                {
                    Directory.CreateDirectory(newPath);
                }
                
                GetAllFilesAndCreateDirectories(childDir, filesData, newPath);
            }

        }

        private List<DataXml> GetDataXmls(IEnumerable<FileInfo> files)
        {
            List<DataXml> dataXmlList = new List<DataXml>();
            
            var dataXmlFileInfoList =  files.Where(file => file.Extension == ".xml").Where(file => file.Name.Contains("data")).ToList();

            for (int i = dataXmlFileInfoList.Count - 1; i >= 0; i--)
            {
                var dataXmlFileInfo = dataXmlFileInfoList[i];

                var dataXmlIndexString = dataXmlFileInfo.Name.Replace("data", "").Replace(".xml", "");
                if (dataXmlIndexString == "")
                {
                    dataXmlList.Add(new DataXml(dataXmlFileInfo, 0));
                    continue;
                }

                TryParse(dataXmlIndexString, out var dataXmlIndex);

                if (dataXmlIndex > 0)
                {
                    dataXmlList.Add(new DataXml(dataXmlFileInfo, dataXmlIndex));
                }
            }

            return dataXmlList;
        }

        record DataXml(FileInfo FileInfo, int Index);
        record EnvironmentLocations(string Environment1Path, string Environment2Path, string MergePath);
        
        public bool DoesItExist(string path, IConsole console, bool directory = true)
        {
            if (directory)
            {
                if (Directory.Exists(path)) return true;
                console.Output.WriteLine("Path does not exist: " + path);
            }
            else
            {
                if (File.Exists(path)) return true;
                console.Output.WriteLine("Path does not exist: " + path);
            }

            return false;
        }
    }

}

