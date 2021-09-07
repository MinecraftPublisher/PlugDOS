using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Jint;

class PlugDOS {
    public string version = "0.1";
    public FileSystem filesystem;

    public PlugDOS() {
        Console.WriteLine("Loading PlugDOS...");
        filesystem = new FileSystem();
        File boot = filesystem.GetFile("/boot.pd");
        if (boot.Path == "EMPTY") {
            Console.WriteLine("ERROR: Could not find a bootable file.");
        } else if (boot.FileType != "".GetType()) {
            Console.WriteLine("ERROR: Could not boot from the bootable file. Reason: Filetype is not PLAINTEXT.");
        } else {
            Console.WriteLine("Booting from filesystem...");

			// START Jint code, For later use
			// var engine = new Engine().SetValue("log", new Action<object>(Console.WriteLine));
			// engine.Execute("log('hewo')");
            // END Jint code, For later use

			int index = 0;
            bool BEGUN = false;
            bool ENDED = false;
            List<string> bootFile = boot.Data.ToString().Split('\n').ToList();
            for (index = 0; index < bootFile.Count; index++) {
                string line = bootFile[index];
                string command = line.Split(' ')[0].ToLower();
                string data;
				if(line.ToLower() == command) {
					data = line.Substring(command.Length);
				}
				else {
					data = line.Substring(command.Length + 1);
				}
                if (command == "start" && !BEGUN) {
                    BEGUN = true;
                } else if (command == "start" && BEGUN) {
                    // Handle duplicate START.
                } else if (command != "start" && command != "end" && BEGUN && !ENDED) {
                    // Parse the command.
					if(command == "echo") {
						Console.WriteLine(data);
					} else if(command == "wait") {
						int delay = 0;
						if(Int32.TryParse(data, out delay)) {
							Thread.Sleep(delay * 1000);
						}
						else {
							Console.WriteLine("ERROR: Invalid wait time");
						}
					}
                } else if (command == "end" && BEGUN && !ENDED) {
                    ENDED = true;
                } else if (command == "end" && BEGUN && ENDED) {
                    // Handle duplicate END.
                } else if (command == "end" && !BEGUN) {
                    Console.WriteLine("Error: Could not launch any commands before starting.");
                } else {
                    Console.WriteLine("Error: Undefined situation.");
                }
            }

            while (!ENDED) {
                // Handle non-ending loop.
            }
        }
    }
}

class FileSystem {
    /// <summary>
    /// Holds the files of the FileSystem in memory.
    /// </summary>
    public List < File > files = new List < File > ();
    /// <summary>
    /// Holds the path to save the filesystem to.
    /// </summary>
    public string FileSystemPath = "PlugDOS.fs.bin";

    public FileSystem() {
        this.NewFileSystem();
        this.LoadFileSystem(this.FileSystemPath);
        this.SaveFileSystem(this.FileSystemPath);
    }

    /// <summary>
    /// Saves the filesystem onto a binary file.
    /// </summary>
    /// <param name="path">The path to save the filesystem</param>
    public void SaveFileSystem(string path) {
        using(BinaryWriter writer = new BinaryWriter(System.IO.File.OpenWrite(path))) {
            writer.Write(JsonConvert.SerializeObject(this.files));
            writer.Close();
            Console.WriteLine("Saved the filesystem!");
        }
    }

    /// <summary>
    /// Loads the filesystem if any are available.
    /// </summary>
    /// <param name="path">The path to load the filesystem</param>
    public void LoadFileSystem(string path) {
        if (System.IO.File.Exists(path)) {
            using(BinaryReader reader = new BinaryReader(System.IO.File.Open(path, FileMode.Open))) {
                this.files = JsonConvert.DeserializeObject < List < File >> (reader.ReadString());
                reader.Close();
                Console.WriteLine("Loaded the filesystem!");
            }
        } else {
            Console.WriteLine("Failed to load data - Save file does not exist");
        }
    }

    /// <summary>
    /// Creates a new filesystem and boots it into the FS class.
    /// </summary>
    public void NewFileSystem() {
        files.Add(new File("/boot.pd",
            "START\n" +
            "ECHO Booting...\n" +
            "WAIT 5\n" +
            "ECHO ERROR: Boot procedure not specified\n" +
            "END"));
        files.Add(new File("/sys/tools.pd",
            "START\n" +
            "REGISTER CMD help\n" +
            "WRITE CMD help /sys/help.pd\n" +
            "END"));
        files.Add(new File("/sys/help.pd",
            "START\n" +
            "ECHO --- PlugDOS help ---\n" +
            "ECHO ERROR: No commands specified\n" +
            "ECHO --------------------\n" +
            "END\n"));
    }

    public File GetFile(string Path) {
        foreach(File file in this.files) {
            if (file.Path == Path) {
                return file;
            }
        }

        return new File();
    }
}