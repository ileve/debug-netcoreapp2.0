/**********************************************************************************************************************************
 ********* utility which modifies our solution and project file appropriately *****************************************************
 ********* in order to be able to debug the asp.net core open source code. ********************************************************
 ********* it is based on my answer in stackoverflow: *****************************************************************************
 ********* https://stackoverflow.com/questions/41656571/debugging-asp-net-core-1-1-mvc-source-code-in-visual-studio-2017-rc *******
 **********************************************************************************************************************************/


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Xml;

namespace setsln
{
    class Program
    {
        class Package
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public string ID { get; set; }
        }
 
        static void Main(string[] args)
        {
            string OurSolutionFilePath = String.Empty;
            string OurProjectFilePath = String.Empty;

            //get the directory path of our solution where we want to modify the *.sln file
            using (StreamReader sr = new StreamReader(@"..\..\ourslnpath.txt"))
            {
                OurSolutionFilePath = sr.ReadLine();
                OurProjectFilePath = sr.ReadLine();
            }

            string TargetFramework = String.Empty;

            if(String.IsNullOrEmpty(OurProjectFilePath) || String.IsNullOrEmpty(OurSolutionFilePath))
            {
                Console.WriteLine("you must supply the full path of *.sln and *.csproj of your project");
                return;
            }


            if (Path.GetExtension(OurProjectFilePath) != ".csproj")
            {
                string temp = OurProjectFilePath;
                OurProjectFilePath = OurSolutionFilePath;
                OurSolutionFilePath = temp;
            }
 
            //open *.csproj file of the source code
            if (!File.Exists(OurProjectFilePath))
            {
                Console.WriteLine($"project file {OurProjectFilePath} was not found");
                return;
            }
            //we create a backup of our *.csproj file
            string ourcsproj = String.Empty;
            StringBuilder sbdest = new StringBuilder(OurProjectFilePath);

            sbdest.Append(".backup");

            if (!File.Exists(sbdest.ToString()))
            {
                File.Copy(OurProjectFilePath, sbdest.ToString());
            }

            //open our *.sln file 
            if (!File.Exists(OurSolutionFilePath))
            {
                Console.WriteLine($"no solution file {OurSolutionFilePath} was found");
                return;
            }

            //we create a backup of our *.sln file
            string oursln = String.Empty;
            sbdest = new StringBuilder(OurSolutionFilePath);

            sbdest.Append(".backup");

            if (!File.Exists(sbdest.ToString()))
            {
                File.Copy(OurSolutionFilePath, sbdest.ToString());
            }
            
            
            //get all the directory paths of solutions which contain open source projects
            List<string> slnpaths = new List<string>();

            using (StreamReader sr = new StreamReader(@"..\..\srcslnpaths.txt"))
            {
                while (!sr.EndOfStream)
                {
                    slnpaths.Add(sr.ReadLine());
                }
            }

            if(slnpaths.Count == 0)
            {
                Console.WriteLine("no open source solution file was supplied...");
                return;
            }

            bool FirstCalltoThis = true;

            foreach (string filepath in slnpaths)
            {
                if (!File.Exists(filepath))
                {
                    Console.WriteLine($"file {filepath} does not exist");
                    return;
                }

                List<Package> packages = new List<Package>(GetSrcPaths(filepath));

                if (!ModifyOurSolutionFile(OurSolutionFilePath, filepath, packages, FirstCalltoThis))
                {
                    Console.WriteLine("failed to modify successfully your solution file...");
                    return;
                }
                string srcpath = Path.GetDirectoryName(filepath);
                EditcsprojFiles(srcpath, OurProjectFilePath, packages, FirstCalltoThis);
                FirstCalltoThis = false;

            }

        }

        static List<Package> GetSrcPaths(string srcpath)
        {
            string sln;
            using (StreamReader read = new StreamReader(srcpath))
            {
                //read the *.sln file
                sln = read.ReadToEnd();
            }


            //retrieve all paths and package names inside \src folder
            int index = 0;
            List<Package> packages = new List<Package>();

            string Searchsrc = @"src\";
            string Searchcsproj = "csproj";
            while (index < sln.Length)
            {
                index = sln.IndexOf(Searchsrc, index);
                string temp = sln.Substring(index + 1, 3);
                if (index == -1)
                {
                    break;
                }
                Package package = new Package();
                package.Name = sln.Substring(index + Searchsrc.Length, sln.IndexOf("\\", index + Searchsrc.Length) - index - Searchsrc.Length);
                int nextindex = sln.IndexOf(Searchcsproj, index);
                package.Path = sln.Substring(index, nextindex + Searchcsproj.Length - index);
                packages.Add(package);
                index = nextindex;
            }

            return packages;
        }

        static bool ModifyOurSolutionFile(string ourslnFile, string srcslnfile, List<Package> packages, bool FirstCall)
        {
            string slnFilePath = ourslnFile, directory = Path.GetDirectoryName(ourslnFile); ;
            if (FirstCall)
            {
                //get file name
                string Filename = Path.GetFileName(ourslnFile);
                Filename = Filename + ".backup";
                slnFilePath = directory + "\\" + Filename; 
            }
            //now we open *.sln file
            StringBuilder sb = new StringBuilder(String.Empty);
 
            string guid = String.Empty;
            string uniqueguid = String.Empty;

            using (StreamReader read = new StreamReader(slnFilePath))
            {
                sb.Append(read.ReadToEnd());
            }

            string Searchproj = "Project(\"{";
            string Searchcsproj = "csproj";
            string Searchendproj = "EndProject";

            int idx1 = sb.ToString().LastIndexOf(Searchendproj);
            string RelativePath = FindRelativePath(Path.GetDirectoryName(srcslnfile), directory);
            if (String.IsNullOrEmpty(RelativePath))
            {
                Console.WriteLine("---bug--- failed to find folder hierarchy");
                return false;
            }


            //get unique guid
            int csprojindex = sb.ToString().IndexOf(Searchcsproj);
            int endcsprojindex = sb.ToString().IndexOf("}\"\r\n", csprojindex);
            uniqueguid = sb.ToString()
                .Substring(csprojindex + Searchcsproj.Length + 5, endcsprojindex - csprojindex - Searchcsproj.Length - 5);
            //get guid of the project
            int projidx = sb.ToString().IndexOf(Searchproj);
            if (projidx == -1)
            {
                Console.WriteLine("*.sln file is corrupted");
                return false;
            }
            guid = sb.ToString().Substring(projidx + Searchproj.Length,
                    sb.ToString().IndexOf("}", projidx + Searchproj.Length) - projidx - Searchproj.Length);

            //now we can append the package data collection
            StringBuilder sbpaths = new StringBuilder(String.Empty);
            sbpaths.AppendLine();

            //append relative paths of the source code packages
            foreach (Package package in packages)
            {
                StringBuilder filenamePath = new StringBuilder(Path.GetDirectoryName(srcslnfile));
                filenamePath.Append("\\");
                filenamePath.Append(package.Path);
                if(!File.Exists(filenamePath.ToString()))
                {
                    Console.WriteLine($"filename {filenamePath.ToString()} does not exist");
                    return false;
                }
                sbpaths.Append("Project(\"{");
                sbpaths.Append(guid);
                sbpaths.Append("}\") = \"");
                sbpaths.Append(package.Name);
                sbpaths.Append("\", \"");
                sbpaths.Append(RelativePath);
                sbpaths.Append(package.Path);
                sbpaths.Append("\", \"{");
                package.ID = Guid.NewGuid().ToString(); //toUpper ????????????????????
                sbpaths.Append(package.ID);
                Package temp = packages.FirstOrDefault(p => p.ID == package.ID);

                sbpaths.AppendLine("}\"");
                sbpaths.AppendLine("EndProject");
            }

            int endLF = sbpaths.ToString().IndexOf((char)0x0a, sbpaths.ToString().LastIndexOf(Searchendproj));
            sb.Insert(idx1 + Searchendproj.Length, sbpaths.ToString().Substring(0, endLF - 1));
            sbpaths.Clear();
            sbpaths = null;

            //get build attributes templates
            string searchconfs = "GlobalSection(ProjectConfigurationPlatforms) = postSolution";
            int index = sb.ToString().IndexOf(searchconfs);
            int guidindex = sb.ToString().IndexOf(uniqueguid, index);

            List<string> templates = new List<string>();

            while (guidindex != -1)
            {
                int endtempidx = sb.ToString().IndexOf((char)0x0a, guidindex);  //detect new line feed
                templates.Add(sb.ToString()
                    .Substring(guidindex + uniqueguid.Length + 1, endtempidx - guidindex - uniqueguid.Length - 2));
                guidindex = sb.ToString().IndexOf(uniqueguid, guidindex + uniqueguid.Length);
            }

            //append configuration templates in every guid of the source package and then append it
            StringBuilder sbconfig = new StringBuilder(String.Empty);
            sbconfig.AppendLine();
            foreach(Package package in packages)
            {
                foreach (string template in templates)
                {
                    sbconfig.Append("\t\t{");
                    sbconfig.Append(package.ID);    //to uppercase?????????????
                    sbconfig.Append("}");
                    sbconfig.AppendLine(template);
                }
            }

            sb.Insert(index + searchconfs.Length, sbconfig.ToString().Substring(0, sbconfig.Length - 2)); //we exclude last LF
            sbconfig.Clear();
            sbconfig = null;

            //write sb to our *.sln file
            using (StreamWriter writer = new StreamWriter(ourslnFile))
            {
                writer.Write(sb.ToString());
            }

            return true;
        }

        static string FindRelativePath(string OpensrcPath, string OurSlnPath)
        {
            if (String.IsNullOrEmpty(OpensrcPath) || String.IsNullOrEmpty(OurSlnPath))
            {
                Console.WriteLine("---bug---: Could not find the relative path, because one of the directory paths was empty...");
                return String.Empty;
            }
            StringBuilder refpath= new StringBuilder(OurSlnPath);

            StringBuilder sbhierarchy = new StringBuilder(String.Empty);

            int i = 0;

            while (OpensrcPath.IndexOf(refpath.ToString()) == -1 && i < 30)
            {
                DirectoryInfo folderInfo = Directory.GetParent(refpath.ToString());

                if (folderInfo == null)
                {
                    break;  //different hard disk
                }

                refpath.Clear();
                refpath.Append(folderInfo.FullName);
                sbhierarchy.Append("..\\");
                ++i;
            } 

            if(i == 30)
            {
                Console.WriteLine("folder hierarchy was not found");
                return String.Empty;
            }

            string relativepath = OpensrcPath.Substring(refpath.ToString().Length + 1);
            if (refpath.ToString().Length == 3)
            {
                //if directory pruning ends up to the root disk directory (C:\, E:\, etc)
                //the absolute path is returned
                relativepath = OpensrcPath;
                sbhierarchy.Clear();
            }

            sbhierarchy.Append(relativepath);
            sbhierarchy.Append("\\");
            return sbhierarchy.ToString();
        }
        
        static void EditcsprojFiles(string OpensrcPath, string OurcsprojFilePath, List<Package> packages, bool FirstCall, string TargetFramework = "xxx")
        {
            string OurProjectPath = Path.GetDirectoryName(OurcsprojFilePath);
            string RelativePath = FindRelativePath(OpensrcPath, OurProjectPath);

            //open *.csproj file in our project
            if (!File.Exists(OurcsprojFilePath))
            {
                Console.WriteLine($"filename {OurcsprojFilePath} does not exist");
                return;
            }
            //create a backup of our *.csproj file
            string projFilePath = OurcsprojFilePath;//, directory = String.Empty;
            if (FirstCall)
            {
                //get file name
                string Filename = Path.GetFileName(OurcsprojFilePath);
                Filename = Filename + ".backup";
                projFilePath = OurProjectPath + "\\" + Filename;
            }
            XmlDocument xmlOurSln = new XmlDocument();
            xmlOurSln.Load(projFilePath);
            XmlNode ourslnnode = xmlOurSln.SelectSingleNode("//TargetFramework");

            if (ourslnnode != null && TargetFramework == "xxx")
            {
                TargetFramework = ourslnnode.InnerText;
            }
            else
            {
                Console.WriteLine($"xml project file {OurcsprojFilePath} is corrupted");
            }
            XmlNode ItemGroupNode = xmlOurSln.CreateElement("ItemGroup");
            //open *.csproj file in src\*
            XmlDocument xmlSrcSln = new XmlDocument();
            
            foreach (Package package in packages)
            {
                StringBuilder csprojPath = new StringBuilder(OpensrcPath);

                csprojPath.Append("\\");
                csprojPath.Append(package.Path);
                if(!File.Exists(csprojPath.ToString()))
                {
                    Console.WriteLine($"filename {csprojPath} does not exist");
                    return;
                }
                xmlSrcSln.Load(csprojPath.ToString());
                XmlNode node = xmlSrcSln.SelectSingleNode("//TargetFramework"); 

                if (node != null)
                {
                    if (TargetFramework != node.InnerText)
                    {
                        node.InnerText = TargetFramework;
                        xmlSrcSln.Save(csprojPath.ToString());
                    }
                }
                else
                {
                    Console.WriteLine($"xml project file {csprojPath} was not edited");
                }
                //add project reference to OurcsprojFilePath

                XmlNode ProjRefNode = xmlOurSln.CreateElement("ProjectReference");
                XmlAttribute IncludeAttr = xmlOurSln.CreateAttribute("Include");
                StringBuilder sbrefprojPath = new StringBuilder(RelativePath);
                sbrefprojPath.Append(package.Path);
                IncludeAttr.Value = sbrefprojPath.ToString();
                ProjRefNode.Attributes.Append(IncludeAttr);
                ItemGroupNode.AppendChild(ProjRefNode);

            }
            xmlOurSln.DocumentElement.AppendChild(ItemGroupNode);
            xmlOurSln.Save(OurcsprojFilePath);
        }
    }
}
