using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace PlugDOS
{
    class PlugDOS
    {
        public string version = "v1.0.5";
        public FileSystem filesystem = new FileSystem();
        public Dictionary<string, LoadedFile> loadedFunctions = new Dictionary<string, LoadedFile>();
        public Dictionary<string, LoadedFile> variables = new Dictionary<string, LoadedFile>();

        public void Boot()
        {
            PlugDOS.WriteLine("| PlugDOS " + this.version + " |\n", ConsoleColor.Blue);
            WriteLine("Loading PlugDOS...");
            File boot = filesystem.GetFile("/boot.pd");
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
                return new LoadedFile("null");
        }
        bool terminated = false;

        public void ExecASM(string input, string filename, bool dependencied = false)
        {
            int index = 0;
            List<string> bootFile = input.Split('\n').ToList();
            for (index = 0; index < bootFile.Count && !terminated; index++)
            {
                string line = bootFile[index];
                string command = line.Split(' ')[0].ToLower();
                string data;
                try { data = line.Substring(command.Length + 1); } catch { data = line.Substring(command.Length); }
                string[] args = data.Split(' ');
                if (command == "#" || command == "") ; // A comment
                else if (command == "echo" && checkArgs(args, 1))
                    PlugDOS.WriteLine(data, ConsoleColor.DarkCyan);
                else if (command == "clear")
                    Console.Clear();
                else if (command == "dump" && checkArgs(args, 1) && filesystem.GetFile(args[0]).Path != "EMPTY")
                    PlugDOS.WriteLine("\n-----------------\n" + filesystem.GetFile(args[0]).Data + "\n-----------------\n");
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
                    Console.Write(">> ");
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
                        PlugDOS.WriteLine("Error: Unable to parse delay.", ConsoleColor.Red);
                }
                else if (command == "if")
                {
                    string state1 = args[0];
                    string state2 = args[1];
                    string state3 = args[2];
                    if (findRegister(state1).Data.ToString() == findRegister(state2).Data.ToString())
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

                    index++;
                    while (!endFound)
                    {
                        if (index >= bootFile.Count)
                        {
                            terminated = true;
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
                    if (!terminated)
                    {
                        loadedFunctions[fnname] = new LoadedFile(String.Join("\n", function));
                    }
                }
                else if (command == "write")
                {
                    if (!checkArgs(args, 2)) break;
                    string fsName = args[0];
                    string content = data.Substring(fsName.Length + 1);
                    try { filesystem.files.Remove(filesystem.GetFile(fsName)); } catch { }
                    filesystem.files.Add(new File(fsName, content));
                }
                else if (command == "remove")
                {
                    if (!checkArgs(args, 1)) break;
                    try { filesystem.files.Remove(filesystem.GetFile(args[0])); } catch { }
                }
                else if (command == "append")
                {
                    if (!checkArgs(args, 2)) break;
                    string fsName = args[0];
                    string content = data.Substring(fsName.Length + 1);
                    if (filesystem.GetFile(fsName).Path == "EMPTY")
                        filesystem.files.Add(new File(fsName, content));
                    else
                    {
                        File fsFile = filesystem.GetFile(fsName);
                        if (fsFile.FileType == ("string").GetType())
                        {
                            filesystem.files.Remove(fsFile);
                            fsFile.Data += "\n" + content;
                            filesystem.files.Add(fsFile);
                        }
                        else
                        {
                            PlugDOS.WriteLine("ERROR: Filetype unmergable", ConsoleColor.Red);
                        }
                    }
                }
                else if (command == "import")
                {
                    if (filesystem.GetFile(data).Path == "EMPTY")
                    {
                        terminated = true;
                        PlugDOS.WriteLine("ERROR: Couldn't find file to import, Process terminated.", ConsoleColor.Red);
                    }
                    else
                    {
                        this.ExecASM((string)filesystem.GetFile(data).Data, "runtime", true);
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
                    if(Console.ReadLine() == "agree")
                    {
                        PlugDOS.WriteLine("WIPING DRIVE...");
                        filesystem.NewFileSystem();
                        filesystem.SaveFileSystem(filesystem.FileSystemPath);
                    }
                    else
                    {
                        terminated = true;
                        PlugDOS.WriteLine("[PROCESS TERMINATION] Reason: Trying to wipe the filesystem");
                    }
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
        /// <summary>
        /// Holds the path to save the filesystem to.
        /// </summary>
        public string FileSystemPath = "PlugDOS.fs.bin";

        public FileSystem()
        {
            this.NewFileSystem();
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
        /// <param name="bytes">The bytes for le fil</param>
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
                writer.Write(Convert.ToBase64String(bytes));
                writer.Close();
                PlugDOS.WriteLine("Saved the filesystem!");
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
                    this.files = (List<File>)DeserializeFromBytes(Convert.FromBase64String(reader.ReadString()));
                    reader.Close();
                    PlugDOS.WriteLine("Loaded the filesystem!");
                }
            }
            else
            {
                PlugDOS.WriteLine("Failed to load data - Save file does not exist");
            }
        }

        /// <summary>
        /// Creates a new filesystem and boots it into the FS class.
        /// </summary>
        public void NewFileSystem()
        {
            files = new List<File>();
            files.Add(new File("/boot.pd",
                "# The initial boot procedure for PlugDOS that loads up all system files and runs through them.\n" + 
                "ECHO Booting...\n" +
                "WAIT 2\n" +
                "IMPORT /sys/load.pd"
                ));
            files.Add(new File("/sys/load.pd",
                "# The load procedure for all CMD commands and the PROMPT loop.\n" +
                "IMPORT /sys/prompt.pd\n" +
                "IMPORT /sys/help.pd\n" +
                "PROMPT"));
            files.Add(new File("/sys/help.pd",
                "# The help command!\n" +
                "DEF help\n" +
                "ECHO --- PlugDOS help ---\n" +
                "ECHO Whoops!\n" +
                "ECHO the owner is  too lazy, So wait a while :)\n" +
                "ECHO --- END help lol ---\n" +
                "END help"));
            files.Add(new File("/sys/prompt.pd",
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
        }

        /// <summary>
        /// Gets a file from a path.
        /// </summary>
        /// <param name="Path">The file path</param>
        /// <returns>A filled file, Otherwise a file with an "EMPTY" path.</returns>
        public File GetFile(string Path)
        {
            foreach (File file in this.files)
            {
                if(file.Path == Path)
                {
                    return file;
                }
            }

            return new File("EMPTY", "");
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
}