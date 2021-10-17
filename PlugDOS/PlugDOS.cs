using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Threading;

namespace PlugDOS
{
    class PlugDOS
    {
        public string version = "v1.3";
        public FileSystem filesystem = new FileSystem();
        public Dictionary<string, LoadedFile> loadedFunctions = new Dictionary<string, LoadedFile>();
        public Dictionary<string, LoadedFile> variables = new Dictionary<string, LoadedFile>();

        public void Boot()
        {
            PlugDOS.WriteLine("| PlugDOS " + this.version + " |\n", ConsoleColor.Blue);
            File boot = filesystem.ReadFile("/boot.pd");
            if (boot.Path == "EMPTY")
            {
                WriteLine("ERROR: Could not find a bootable file.");
            }
            else if (boot.FileType != "PLAINTEXT".GetType())
            {
                WriteLine("ERROR: Could not boot from the bootable file. Reason: Filetype is not PLAINTEXT.");
            }
            else
            {
                WriteLine("Booting from filesystem...");
                ExecASM(boot.Data.ToString(), boot.Path);
            }
        }

        public static void WriteLine(string text, ConsoleColor color = ConsoleColor.Yellow)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public bool checkArgs(string[] args, int size)
        {
            if (args.Length >= size)
                return true;
            else
            {
                PlugDOS.WriteLine("ERROR: Not enough arguments", ConsoleColor.Red);
                return false;
            }
        }

        public LoadedFile findRegister(string key)
        {
            if (variables.ContainsKey(key))
                return variables[key];
            else
            {
                PlugDOS.WriteLine("Register missing: " + key, ConsoleColor.Red);
                return new LoadedFile("null");
            }
        }
        bool terminated = false;

        public void ExecASM(string input, string filename, bool dependencied = false)
        {
            int index = 0;
            List<string> bootFile = input.Split('\n').ToList();
            for (index = 0; index < bootFile.Count && !terminated; index++)
            {
                variables["dos-version"] = new LoadedFile(this.version);
                variables["base-version"] = new LoadedFile(this.filesystem.baseVersion);
                variables["found-version"] = new LoadedFile(this.filesystem.foundVersion);
                string line = bootFile[index];
                foreach (Match match in Regex.Matches(line, @"\$\[[^\]]+\]"))
                {
                    string RegisterName = match.Value.Substring(2, match.Value.Length - 3);
                    if (variables.ContainsKey(RegisterName))
                        line = Regex.Replace(line, @"\$\[" + RegisterName + @"\]", (string)variables[RegisterName].Data);
                    else
                        line = Regex.Replace(line, @"\$\[" + RegisterName + @"\]", "NOT-FOUND");
                }
                string command = line.Split(' ')[0].ToLower();
                string data;
                try { data = line.Substring(command.Length + 1); } catch { data = line.Substring(command.Length); }
                string[] args = data.Split(' ');
                if (command == "#" || command == "") { } // A comment
                else if (command == "echo")
                {
                    if (checkArgs(args, 1))
                        PlugDOS.WriteLine(data, ConsoleColor.DarkCyan);
                }
                else if (command == "clear")
                    Console.Clear();
                else if (command == "dump")
                {
                    if (checkArgs(args, 1))
                    {
                        if (filesystem.ReadFile(args[0]).Path != "EMPTY")
                        {
                            PlugDOS.WriteLine("\n-----------------\n" + filesystem.ReadFile(args[0]).Data + "\n-----------------\n");
                        }
                        else
                        {
                            PlugDOS.WriteLine("ERROR: Missing file", ConsoleColor.Red);
                        }
                    }
                }
                else if (command == "error") // Drop an error and crash the program
                {
                    if (!checkArgs(args, 1)) break;
                    PlugDOS.WriteLine("ERROR: " + data, ConsoleColor.Red);
                    terminated = true;
                    break;
                }
                else if (command == "ask") // Ask for an input and store the input in the variables dictionary.
                {
                    if (!checkArgs(args, 1)) break;
                    Console.Write("" + filesystem.directory + " ~ $ ");
                    string ASKinput = Console.ReadLine();
                    variables[args[0]] = new LoadedFile(ASKinput);
                }
                else if (command == "register")
                {
                    if (!checkArgs(args, 2)) break;
                    string registerName = args[0];
                    string registerContent = data.Substring(registerName.Length + 1);
                    variables[registerName] = new LoadedFile(registerContent);
                }
                else if (command == "wait") // Wait for a few seconds
                {
                    if (!checkArgs(args, 1)) break;
                    int delay = 0;
                    if (Int32.TryParse(data, out delay))
                        Thread.Sleep(delay * 1000);
                    else
                        PlugDOS.WriteLine("ERROR: Unable to parse delay.", ConsoleColor.Red);
                }
                else if (command == "if")
                {
                    string state1 = args[0];
                    string state2 = args[1];
                    string state3 = args[2];
                    if (findRegister(state1).Data.ToString() == findRegister(state2).Data.ToString())
                        this.ExecASM((string)findRegister(state3).Data, "runtime", true);
                }
                else if (command == "ifnot")
                {
                    string state1 = args[0];
                    string state2 = args[1];
                    string state3 = args[2];
                    if (findRegister(state1).Data.ToString() != findRegister(state2).Data.ToString())
                        this.ExecASM((string)findRegister(state3).Data, "runtime", true);
                }
                else if (command == "def") // Read till the end of function, Or throw an error if none was found.
                {
                    if (!checkArgs(args, 1)) break;
                    string fnname = args[0].ToLower();
                    List<string> function = new List<string>
                    {
                        "# READ function \"" + fnname + "\""
                    };
                    bool endFound = false;
                    bool dontWrite = false;

                    index++;
                    while (!endFound)
                    {
                        if (index >= bootFile.Count)
                        {
                            endFound = true;
                            PlugDOS.WriteLine("Error: EOF detected, Function still booting.", ConsoleColor.Red);
                        }
                        else
                        {
                            string fnLine = bootFile[index];
                            string fnCMD = fnLine.Split(' ')[0].ToLower();
                            string fnARGS;
                            try { fnARGS = fnLine.Substring(fnCMD.Length + 1); } catch { fnARGS = fnLine.Substring(fnCMD.Length); }
                            if (fnCMD == "end" && fnARGS.ToLower() == fnname)
                            {
                                endFound = true;
                            }
                            else
                            {
                                function.Add(fnLine);
                            }
                        }
                        index++;
                    }
                    if (!terminated && !dontWrite)
                    {
                        loadedFunctions[fnname] = new LoadedFile(String.Join("\n", function));
                    }
                }
                else if (command == "override") // Read until the override end, And write it
                {
                    if (!checkArgs(args, 1)) break;
                    if(!loadedFunctions.ContainsKey(args[0]))
                    {
                        terminated = true;
                        PlugDOS.WriteLine("ERROR: Unable to find function to override.");
                    }
                    string fnname = args[0].ToLower();
                    List<string> function = new List<string>
                    {
                        "# BEGIN Function OVERRIDE \"" + fnname + "\" from file \"" + filename + "\""
                    };
                    bool endFound = false;
                    bool dontWrite = false;

                    index++;
                    while (!endFound && !terminated)
                    {
                        if (index >= bootFile.Count)
                        {
                            endFound = true;
                            PlugDOS.WriteLine("Error: EOF detected, Function still booting.", ConsoleColor.Red);
                        }
                        else
                        {
                            string fnLine = bootFile[index];
                            string fnCMD = fnLine.Split(' ')[0].ToLower();
                            string fnARGS;
                            try { fnARGS = fnLine.Substring(fnCMD.Length + 1); } catch { fnARGS = fnLine.Substring(fnCMD.Length); }
                            if (fnCMD == "end" && fnARGS.ToLower() == fnname)
                            {
                                endFound = true;
                            }
                            else
                            {
                                function.Add(fnLine);
                            }
                        }
                        index++;
                    }
                    if (!terminated && !dontWrite)
                    {
                        function.Add("# END Function OVERRIDE \"" + fnname + "\" from file \"" + filename + "\"");
                        loadedFunctions[fnname] = new LoadedFile(loadedFunctions[fnname].Data + "\n" + String.Join("\n", function));
                    }
                }
                else if (command == "write")
                {
                    if (!checkArgs(args, 2)) break;
                    string fsName = args[0];
                    string content = data.Substring(fsName.Length + 1);
                    try { filesystem.files.Remove(filesystem.ReadFile(fsName)); } catch { }
                    filesystem.WriteFile(new File(fsName, content));
                    filesystem.SaveFileSystem(filesystem.FileSystemPath);
                }
                else if (command == "remove")
                {
                    if (!checkArgs(args, 1)) break;
                    try { filesystem.files.Remove(filesystem.ReadFile(args[0])); } catch { }
                    filesystem.SaveFileSystem(filesystem.FileSystemPath);
                }
                else if (command == "append")
                {
                    if (!checkArgs(args, 2)) break;
                    string fsName = args[0];
                    string content = data.Substring(fsName.Length + 1);
                    if (filesystem.ReadFile(fsName).Path == "EMPTY")
                    {
                        filesystem.WriteFile(new File(fsName, content));
                        filesystem.SaveFileSystem(filesystem.FileSystemPath);
                    }
                    else
                    {
                        File fsFile = filesystem.ReadFile(fsName);
                        if (fsFile.FileType == ("string").GetType())
                        {
                            filesystem.files.Remove(fsFile);
                            fsFile.Data += "\n" + content;
                            filesystem.WriteFile(fsFile);
                            filesystem.SaveFileSystem(filesystem.FileSystemPath);
                        }
                        else
                        {
                            PlugDOS.WriteLine("ERROR: Filetype unmergable", ConsoleColor.Red);
                        }
                    }
                }
                else if (command == "import")
                {
                    if (filesystem.ReadFile(data).Path == "EMPTY")
                    {
                        terminated = true;
                        PlugDOS.WriteLine("ERROR: Couldn't find file to import, Process terminated.", ConsoleColor.Red);
                    }
                    else
                    {
                        this.ExecASM((string)filesystem.ReadFile(data).Data, "runtime", true);
                    }
                }
                else if (command == "exec")
                {
                    this.ExecASM((string)findRegister(data).Data, "runtime", true);
                }
                else if (command == "save")
                    filesystem.SaveFileSystem(filesystem.FileSystemPath);
                else if (command == "load")
                    filesystem.LoadFileSystem(filesystem.FileSystemPath);
                else if (command == "wipe")
                {
                    PlugDOS.WriteLine("CAUTION: This action will wipe your whole disk, Meaning you will loose everything you have stored in PlugDOS!\nIf you want to continue, Type in \"agree\" into the box below and press enter.", ConsoleColor.Red);
                    Console.Write("Typing \"agree\" would delete ALL your files >> ");
                    if (Console.ReadLine() == "agree")
                    {
                        PlugDOS.WriteLine("WIPING DRIVE...");
                        filesystem.files = new List<File>();
                        filesystem.NewFileSystem();
                        filesystem.SaveFileSystem(filesystem.FileSystemPath);
                    }
                    else
                    {
                        terminated = true;
                        PlugDOS.WriteLine("[PROCESS TERMINATION] Reason: Trying to wipe the filesystem");
                    }
                }
                else if (command == "ls")
                {
                    PlugDOS.WriteLine("----------", ConsoleColor.Yellow);
                    foreach (File file in filesystem.GetFiles(filesystem.directory))
                    {
                        PlugDOS.WriteLine(file.Path, ConsoleColor.Yellow);
                    }
                    PlugDOS.WriteLine("----------", ConsoleColor.Yellow);
                }
                else if (command == "cd")
                {
                    if (!checkArgs(args, 1)) break;
                    if (!data.EndsWith("/"))
                        data += "/";
                    if (data.StartsWith("/"))
                        filesystem.directory = data;
                    else
                    {
                        filesystem.directory += data;
                        filesystem.directory = Regex.Replace(filesystem.directory, "//", "/");
                    }
                }
                else if (command == "mkdir")
                {
                    if (!checkArgs(args, 1)) break;
                    filesystem.MakeDirectory(args[0]);
                }
                else if (command == "rmdir")
                {
                    if (!checkArgs(args, 1)) break;
                    filesystem.DeleteDirectory(args[0]);
                }
                else if (command == "reg")
                {
                    if (!checkArgs(args, 3)) break;
                    string regCMD = args[0];
                    string path = args[1];
                    string reg = args[2];

                    if (regCMD == "write")
                    {
                        if ((string)findRegister(reg).Data == "null")
                        {
                            PlugDOS.WriteLine("ERROR: Registry field not found", ConsoleColor.Red);
                        }
                        else
                        {
                            filesystem.DeleteFile(path);
                            filesystem.WriteFile(new File(path, (string)findRegister(reg).Data));
                        }
                    }
                    else if (regCMD == "read")
                    {
                        File foundFile = filesystem.ReadFile(path);
                        variables[reg] = new LoadedFile((string)foundFile.Data);
                    }
                    else if (regCMD == "clone")
                    {
                        if ((string)findRegister(path).Data == "null")
                        {
                            PlugDOS.WriteLine("ERROR: Registry field not found", ConsoleColor.Red);
                        }
                        else
                        {
                            variables[reg] = findRegister(path);
                        }
                    }
                    filesystem.SaveFileSystem(filesystem.FileSystemPath);
                }
                else if (command == "base")
                {
                    PlugDOS.WriteLine("Updating system files...");
                    filesystem.NewFileSystem();
                }
                else if (command == "exit")
                {
                    PlugDOS.WriteLine("Shutting down safely...");
                    PlugDOS.WriteLine("Stopping all running PDBs...");
                    terminated = true;
                    PlugDOS.WriteLine("Saving the filesystem...");
                    filesystem.SaveFileSystem(filesystem.FileSystemPath);
                    PlugDOS.WriteLine("Freeing up ram space...");
                    variables = new Dictionary<string, LoadedFile>();
                    loadedFunctions = new Dictionary<string, LoadedFile>();
                    PlugDOS.WriteLine("Now you are able to safely close PlugDOS without any issues.");
                }



                // Throw an error if there is an unknown command, But don't terminate the process.
                else
                {
                    if (loadedFunctions.ContainsKey(command))
                    {
                        // Check the function's arguments
                        LoadedFile function = loadedFunctions[command];
                        this.ExecASM(function.Data.ToString(), "runtime", true);
                    }
                    else
                    {
                        // Whoops, Command not found!
                        PlugDOS.WriteLine("ERROR: Unknown command \"" + command.ToUpper() + "\"", ConsoleColor.Red);
                    }
                }
                
            }
        }
    }

    class FileSystem
    {
        /// <summary>
        /// Holds the files of the FileSystem in memory.
        /// </summary>
        public List<File> files = new List<File>();
        public readonly string baseVersion = "1.0-2";
        public string foundVersion = "BASE";
        public string directory = "/";
        /// <summary>
        /// Holds the path to save the filesystem to.
        /// </summary>
        public string FileSystemPath = "PlugDOS.fs.bin";

        public FileSystem()
        {
            this.LoadFileSystem(this.FileSystemPath);
            this.SaveFileSystem(this.FileSystemPath);
        }

        /// <summary>
        /// Serialize an object to Binary
        /// </summary>
        /// <typeparam name="T">Le type</typeparam>
        /// <param name="item">Mama miba! Itemiba! Spiiin!</param>
        /// <returns>The byte array for the file</returns>
        public static byte[] SerializeToBytes<T>(T item)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, item);
                stream.Seek(0, SeekOrigin.Begin);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Deserialize an object from Binary
        /// </summary>
        /// <param name="bytes">The bytes for le file</param>
        /// <returns>The object i guess</returns>
        public static object DeserializeFromBytes(byte[] bytes)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream(bytes))
            {
                return formatter.Deserialize(stream);
            }
        }

        /// <summary>
        /// Saves the filesystem onto a binary file.
        /// </summary>
        /// <param name="path">The path to save the filesystem</param>
        public void SaveFileSystem(string path)
        {
            using (BinaryWriter writer = new BinaryWriter(System.IO.File.OpenWrite(path)))
            {
                byte[] bytes = SerializeToBytes(this.files);
                writer.Write(this.foundVersion);
                writer.Write(true);
                writer.Write(Convert.ToBase64String(bytes));
                writer.Close();
            }
        }

        /// <summary>
        /// Loads the filesystem if any are available.
        /// </summary>
        /// <param name="path">The path to load the filesystem</param>
        public void LoadFileSystem(string path)
        {
            if (System.IO.File.Exists(path))
            {
                using (BinaryReader reader = new BinaryReader(System.IO.File.Open(path, FileMode.Open)))
                {
                    try
                    {
                        this.foundVersion = reader.ReadString();
                    }
                    catch
                    {
                        this.NewFileSystem();
                    }
                    finally
                    {
                        bool dummy = reader.ReadBoolean();
                        this.files = (List<File>)DeserializeFromBytes(Convert.FromBase64String(reader.ReadString()));
                    }
                    reader.Close();
                }
            }
            else
            {
                PlugDOS.WriteLine("Failed to load data - Save file does not exist");
                this.NewFileSystem();
            }
        }

        /// <summary>
        /// Creates a new filesystem and boots it into the FS class.
        /// </summary>
        public void NewFileSystem()
        {
            this.foundVersion = this.baseVersion;
            WriteFile(new File("/boot.pd",
                "# The initial boot procedure for PlugDOS that loads up all system files and runs through them.\n" + 
                "ECHO Booting...\n" +
                "WAIT 1\n" +
                "IMPORT /sys/version.pd\n" +
                "WAIT 2\n" +
                "IMPORT /sys/load.pd"
                ));
            WriteFile(new File("/sys/version.pd",
                "# The file to check the current base and found version.\n" +
                "DEF payload9287\n" +
                "ECHO ERROR: Your filesystem version is not up-to-date, Try running the \"base\" command to update it.\n" +
                "ECHO Current version: $[found-version] | Latest version: $[base-version]\n" +
                "END payload9287\n\n" +
                "REGISTER version-error payload9287\n" +
                "REGISTER up-to-date ECHO Your filesystem version is up-to-date.\n" +
                "WAIT 2\n" +
                "IF base-version found-version up-to-date\n" +
                "IFNOT base-version found-version version-error"));
            WriteFile(new File("/sys/load.pd",
                "# The load procedure for all CMD commands and the PROMPT loop.\n" +
                "IMPORT /sys/prompt.pd\n" +
                "IMPORT /sys/help.pd\n" +
                "PROMPT"));
            WriteFile(new File("/sys/help.pd",
                "# The help command!\n" +
                "DEF help\n" +
                "ECHO --- PlugDOS help ---\n" +
                "ECHO \"# DATA\" or an empty line => A comment\n" +
                "ECHO \"ECHO string\" => Writes text to the console\n" +
                "ECHO \"CLEAR\" => Cleares the console\n" +
                "ECHO \"DUMP string\" => Dumps a file's data into console\n" +
                "ECHO \"ERROR string\" => Throws an error to the console and terminates the process\n" +
                "ECHO \"ASK registry string\" => Asks the user for input and puts it into a register\n" +
                "ECHO \"REGISTER key content\" => Registers a value with a certain key\n" +
                "ECHO \"WAIT seconds\" => Pauses the program for a specific amount of time\n" +
                "ECHO \"IFNOT statement1 statement2 registry1\" => If statement1 and 2 from the registry are NOT equal, registry1 will be executed as PDL\n" +
                "ECHO \"IF statement1 statement2 registry1\" => If statement1 and 2 from the registry are equal, registry1 will be executed as PDL\n" +
                "ECHO \"DEF functionname\" and \"END functionname\" => Define a function that could be called globally\n" +
                "ECHO \"OVERRIDE functionname\" and \"END functionname\" => Add PDL scripts to a function\n" +
                "ECHO \"WRITE text\" => Write text to a file\n" +
                "ECHO \"REMOVE filename\" => Removes a file from disk\n" +
                "ECHO \"APPEND filename string\" => Adds a string to a file on a new line\n" +
                "ECHO \"IMPORT filename\" => Imports a file as a PDL\n" +
                "ECHO \"EXEC registry1\" => Executes a registry spot as a PDL\n" +
                "ECHO \"SAVE\" => Saves the filesystem\n" +
                "ECHO \"LOAD\" => Loads the filesystem\n" +
                "ECHO \"WIPE\" => Removes all data on your PlugDOS disk.\n" +
                "ECHO \"LS\" => List files and directories in the current directory\n" +
                "ECHO \"CD path\" => Change the current directory path\n" +
                "ECHO \"MKDIR dirpath\" => Creates a directory at the given path\n" +
                "ECHO \"RMDIR path\" => Deletes a directory at the given path\n" +
                "ECHO \"REG\" => This command has a few parts, Which will be explained in the REGHELP command, Please refer to there\n" +
                "ECHO \"UPDATE\" => Updates the system files from the current executable" +
                "ECHO And finally, To call a registered function, You just need to type its name in the EXEC command or your PDL script\n" +
                "ECHO --- END help ---\n" +
                "END help\n" +
                "\n" +
                "DEF reghelp\n" +
                "ECHO --- REG command help ---\n" +
                "ECHO \"REG WRITE filepath registry1\" => Pulls data from the registry and writes it to the disk\n" +
                "ECHO \"REG READ filepath registry1\" => Pulls data form a file and registers it on the registry\n" +
                "ECHO \"REG clone registry1 registry2\" => Writes registry1 to registry2\n" +
                "ECHO --- END REG command help ---\n" +
                "END reghelp\n"));
            WriteFile(new File("/sys/prompt.pd",
                "# The command PROMPT for PlugDOS.\n" +
                "DEF PROMPT\n" +
                "ECHO PlugDOS terminal\n" +
                "PROMPT2\n" +
                "END PROMPT\n" +
                "\n" +
                "DEF PROMPT2\n" +
                "ASK cmd\n" +
                "EXEC cmd\n" +
                "PROMPT2\n" +
                "END PROMPT2\n"));
            this.SaveFileSystem(this.FileSystemPath);
        }

        public void WriteFile(File file)
        {
            File file2 = file;
            if(file2.Path.EndsWith(".RESERVE"))
            {
                PlugDOS.WriteLine("ERROR: Denied permission to reserved file/path", ConsoleColor.Red);
            }
            if(!file2.Path.StartsWith("/"))
                file2.Path = "/" + file2.Path;
            if (ReadFile(file2.Path).Path == "EMPTY")
                files.Add(file2);
            else
            {
                files.Remove(ReadFile(file2.Path));
                files.Add(file2);
            }
            SaveFileSystem(FileSystemPath);
        }

        /// <summary>
        /// Gets a file from a path.
        /// </summary>
        /// <param name="Path">The file path</param>
        /// <returns>A filled file, Otherwise a file with an "EMPTY" path.</returns>
        public File ReadFile(string Path)
        {
            string Path2 = Path;
            if (!Path2.StartsWith("/"))
                Path2 = directory + Path2;
            foreach (File file in this.files)
            {
                if ((file.Path == Path2 || directory + file.Path == Path2) && !file.Path.Contains(".RESERVE"))
                {
                    return file;
                }
            }

            SaveFileSystem(FileSystemPath);
            return new File("EMPTY", "");
        }

        public bool DeleteFile(string path)
        {
            if(this.ReadFile(path).Path == "EMPTY")
            {
                return false;
            }
            else
            {
                this.files.Remove(this.ReadFile(path));
                SaveFileSystem(this.FileSystemPath);
                return true;
            }
        }

        public void MakeDirectory(string path)
        {
            string workingPath = directory + path;
            if (!workingPath.EndsWith("/"))
                workingPath += "/";
            if(workingPath.Contains(".RESERVE"))
            {
                PlugDOS.WriteLine("ERROR: Denied permission to reserved file/path", ConsoleColor.Red);
            }
            else
            {
                files.Add(new File(workingPath + ".RESERVE", ""));
            }
            SaveFileSystem(this.FileSystemPath);
        }

        public void DeleteDirectory(string path)
        {
            string workingPath = directory + path;
            if (!workingPath.EndsWith("/"))
                workingPath += "/";
            if (workingPath.Contains(".RESERVE"))
            {
                PlugDOS.WriteLine("ERROR: Denied permission to reserved file/path", ConsoleColor.Red);
            }
            else
            {
                workingPath += ".RESERVE";
                foreach (File file in this.files)
                {
                    if (file.Path == workingPath || file.Path.StartsWith(workingPath))
                    {
                        files.Remove(file);
                        return;
                    }
                }
                PlugDOS.WriteLine("WARNING: Directory not found", ConsoleColor.DarkYellow);
            }
            SaveFileSystem(this.FileSystemPath);
        }

        public File[] GetFiles(string path)
        {
            if (path.EndsWith("/"))
            {
                List<File> files = new List<File>();
                foreach (File file in this.files)
                {
                    if(file.Path.StartsWith(path))
                    {
                        string filename = file.Path.Substring(path.Length);
                        if(filename.IndexOf('/') == -1)
                        {
                            files.Add(file);
                        }
                        else
                        {
                            string dirpath = path + filename.Split('/')[0] + "/";
                            bool isAdded = false;
                            foreach(File file2 in files)
                            {
                                if (file2.Path == dirpath)
                                    isAdded = true;
                            }
                            if (file.Path.EndsWith(".RESERVE"))
                            {
                                if (!isAdded)
                                    files.Add(new File(dirpath.Substring(0, file.Path.Length - ".RESERVE".Length), "EMPTY"));
                            }
                            else
                            {
                                if (!isAdded)
                                    files.Add(new File(dirpath, "EMPTY"));
                            }
                        }
                    }
                }

                return files.ToArray();
            }
            else
            {
                return new List<File>().ToArray();
            }
        }
    }

    [Serializable]
    struct File
    {
        public string Path;
        public object Data;
        public Type FileType;

        public File(string Path = "EMPTY", object Data = null)
        {
            this.Path = Path;
            this.Data = Data;
            this.FileType = Data.GetType();
        }
    }

    [Serializable]
    struct LoadedFile
    {
        public object Data;
        public Type FileType;

        public LoadedFile(object Data = null)
        {
            this.Data = Data;
            this.FileType = Data.GetType();
        }
    }
}