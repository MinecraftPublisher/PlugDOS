/*
 * PlugDOS
 * C# to TS port
 * Original lines: 735
 * TS port lines: 529
 */
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var __generator = (this && this.__generator) || function (thisArg, body) {
    var _ = { label: 0, sent: function() { if (t[0] & 1) throw t[1]; return t[1]; }, trys: [], ops: [] }, f, y, t, g;
    return g = { next: verb(0), "throw": verb(1), "return": verb(2) }, typeof Symbol === "function" && (g[Symbol.iterator] = function() { return this; }), g;
    function verb(n) { return function (v) { return step([n, v]); }; }
    function step(op) {
        if (f) throw new TypeError("Generator is already executing.");
        while (_) try {
            if (f = 1, y && (t = op[0] & 2 ? y["return"] : op[0] ? y["throw"] || ((t = y["return"]) && t.call(y), 0) : y.next) && !(t = t.call(y, op[1])).done) return t;
            if (y = 0, t) op = [op[0] & 2, t.value];
            switch (op[0]) {
                case 0: case 1: t = op; break;
                case 4: _.label++; return { value: op[1], done: false };
                case 5: _.label++; y = op[1]; op = [0]; continue;
                case 7: op = _.ops.pop(); _.trys.pop(); continue;
                default:
                    if (!(t = _.trys, t = t.length > 0 && t[t.length - 1]) && (op[0] === 6 || op[0] === 2)) { _ = 0; continue; }
                    if (op[0] === 3 && (!t || (op[1] > t[0] && op[1] < t[3]))) { _.label = op[1]; break; }
                    if (op[0] === 6 && _.label < t[1]) { _.label = t[1]; t = op; break; }
                    if (t && _.label < t[2]) { _.label = t[2]; _.ops.push(op); break; }
                    if (t[2]) _.ops.pop();
                    _.trys.pop(); continue;
            }
            op = body.call(thisArg, _);
        } catch (e) { op = [6, e]; y = 0; } finally { f = t = 0; }
        if (op[0] & 5) throw op[1]; return { value: op[0] ? op[1] : void 0, done: true };
    }
};
var called = 0;
var outputs = [];
var files = [];
function stdin(input) {
    var result = prompt(outputs.join('\n') + '\n' + input);
    console.log(result);
    return result;
}
function stdout(output) {
    outputs.push(output);
}
function stdclear() {
    // outputs = [];
}
var PlugDOS = /** @class */ (function () {
    function PlugDOS() {
        this.version = "";
        this.filesystem = new PDFileSystem();
        this.loadedFunctions = {};
        this.variables = {};
        this.terminated = false;
    }
    PlugDOS.prototype.Boot = function () {
        var _this = this;
        this.terminated = false;
        stdout("| PlugDOS " + this.version + " |");
        stdout("Loading PlugDOS...");
        var boot = this.filesystem.ReadFile("/boot.pd");
        if (boot.Path === "EMPTY") {
            stdout("ERROR: Could not find a bootable file.");
            if (stdin('Do you want to reset the filesystem? (yes/no)') == "yes") {
                this.filesystem.files = [];
                this.filesystem.NewFileSystem().then(function () {
                    _this.Boot();
                });
            }
            else {
                stdout('Aborting...');
                stdin("");
            }
        }
        else if (boot.FileType != typeof "PLAINTEXT") {
            stdout("ERROR: Could not boot from the bootable file. Reason: Filetype is not PLAINTEXT.");
        }
        else {
            stdout("Booting from filesystem...");
            this.ExecASM((boot.Data || ""), boot.Path);
        }
    };
    PlugDOS.prototype.checkArgs = function (args, size) {
        if (args.length >= size)
            return true;
        else {
            stdout("ERROR: Not enough arguments");
            return false;
        }
    };
    PlugDOS.prototype.findRegister = function (key) {
        if (this.variables[key])
            return this.variables[key];
        else
            return new PDLoadedFile("UNFOUND");
    };
    PlugDOS.prototype.ExecASM = function (input, filename, dependencied) {
        var _this = this;
        if (dependencied === void 0) { dependencied = false; }
        var index = 0;
        var bootFile = input.split('\n');
        for (index = 0; index < bootFile.length && !this.terminated; index++) {
            var line = bootFile[index];
            var command = line.split(' ')[0].toLowerCase();
            var data = void 0;
            try {
                data = line.substring(command.length + 1);
            }
            catch (_a) {
                data = line.substring(command.length);
            }
            var args = data.split(' ');
            if (command === "#" || command === "") { } // A comment
            else if (command === "echo") { // Write text to stdout
                if (this.checkArgs(args, 1))
                    stdout(data);
            }
            else if (command === "clear") { // Clear stdout
                stdclear();
            }
            else if (command === "dump") { // Dump a file data to stdout
                if (this.checkArgs(args, 1)) {
                    if (this.filesystem.ReadFile(args[0]).Path != "EMPTY") {
                        stdout("\n-----------------\n" + this.filesystem.ReadFile(args[0]).Data + "\n-----------------\n");
                    }
                    else {
                        stdout("ERROR: File not found.");
                    }
                }
            }
            else if (command === "error") { // Drop an error and crash the program
                if (!this.checkArgs(args, 1))
                    break;
                stdout("ERROR: " + data);
                this.terminated = true;
                break;
            }
            else if (command === "ask") { // Ask for an input and store it in a variable.
                if (!this.checkArgs(args, 1))
                    break;
                var ASKinput = stdin(this.filesystem.directory + " ~ $ ");
                this.variables[args[0]] = new PDLoadedFile(ASKinput);
            }
            else if (command === "register") { // Set a variable slot to the given value
                if (!this.checkArgs(args, 2))
                    break;
                var registerName = args[0];
                var registerContent = data.substring(registerName.length + 1);
                this.variables[registerName] = new PDLoadedFile(registerContent);
            }
            else if (command === "wait") { // Wait for a few seconds, deprecated in TS-PlugDOS, However no warnings or errors are dropped.
                if (!this.checkArgs(args, 1))
                    break;
                var delay = 0;
                if (!isNaN(parseInt(data))) { } // Command deprecated in TS-PlugDOS
                else
                    stdout("ERROR: Unable to parse delay, Command deprecated.");
            }
            else if (command === "if") { // Check if two statements are equal, And then run a variable data from memory if they match.
                if (!this.checkArgs(args, 3))
                    break;
                var state1 = args[0];
                var state2 = args[1];
                var state3 = args[2];
                if ((this.findRegister(state1).Data || "") === (this.findRegister(state2).Data || ""))
                    this.ExecASM((this.findRegister(state3).Data || ""), "runtime", true);
            }
            else if (command === "def") { // Read until the end of the function, Or throw an error if the function has no ending.
                if (!this.checkArgs(args, 1))
                    break;
                var fnname = args[0].toLowerCase();
                var func = [
                    "# READ function \"" + fnname + "\""
                ];
                var endFound = false;
                index++;
                while (!endFound) {
                    if (index >= bootFile.length) {
                        endFound = true;
                        stdout("ERROR: EOF detected, Function still booting.");
                    }
                    else {
                        var fnLine = bootFile[index];
                        var fnCMD = fnLine.split(' ')[0].toLowerCase();
                        var fnARGS = void 0;
                        try {
                            fnARGS = fnLine.substring(fnCMD.length + 1);
                        }
                        catch (_b) {
                            fnARGS = fnLine.substring(fnCMD.length);
                        }
                        if (fnCMD === "end" && fnARGS.toLowerCase() === fnname) {
                            endFound = true;
                        }
                        else {
                            func.push(fnLine);
                        }
                        index++;
                    }
                }
                if (!this.terminated) {
                    this.loadedFunctions[fnname] = new PDLoadedFile(func.join("\n"));
                }
            }
            else if (command === "override") { // Read until the end of the override, Or throw an error if the override has no ending or the main function wasnt found.
                if (!this.checkArgs(args, 1))
                    break;
                if (!this.loadedFunctions[args[0]]) {
                    this.terminated = true;
                    stdout("ERROR: Unable to find function to override.");
                }
                var fnname = args[0].toLowerCase();
                var func = [
                    "# BEGIN Function OVERRIDE \"" + fnname + "\" from file \"" + filename + "\""
                ];
                var endFound = false;
                index++;
                while (!endFound) {
                    if (index >= bootFile.length) {
                        endFound = true;
                        stdout("ERROR: EOF detected, Function still booting.");
                    }
                    else {
                        var fnLine = bootFile[index];
                        var fnCMD = fnLine.split(' ')[0].toLowerCase();
                        var fnARGS = void 0;
                        try {
                            fnARGS = fnLine.substring(fnCMD.length + 1);
                        }
                        catch (_c) {
                            fnARGS = fnLine.substring(fnCMD.length);
                        }
                        if (fnCMD === "end" && fnARGS.toLowerCase() === fnname) {
                            endFound = true;
                        }
                        else {
                            func.push(fnLine);
                        }
                        index++;
                    }
                }
                if (!this.terminated) {
                    func.push("# END Function OVERRIDE \"" + fnname + "\" from file \"" + filename + "\"");
                    this.loadedFunctions = new PDLoadedFile(this.loadedFunctions[fnname].Data + "\n" + func.join("\n"));
                }
            }
            else if (command === "write") { // Write data to a file on the disk.
                if (!this.checkArgs(args, 2))
                    break;
                var fsName = args[0];
                var content = data.substring(fsName.length + 1);
                try {
                    this.filesystem.DeleteFile(fsName);
                }
                catch (_d) { }
                this.filesystem.WriteFile(new PDFile(fsName, content));
                this.filesystem.Save();
            }
            else if (command === "remove") { // Delete a file from disk.
                if (!this.checkArgs(args, 2))
                    break;
                try {
                    this.filesystem.DeleteFile(args[0]);
                }
                catch (_e) { }
                this.filesystem.Save();
            }
            else if (command === "append") { // Append data to the end of a file.
                if (!this.checkArgs(args, 2))
                    break;
                var fsName = args[0];
                var content = data.substring(fsName.length + 1);
                if (this.filesystem.ReadFile(fsName).Path === "EMPTY") {
                    this.filesystem.WriteFile(new PDFile(fsName, content));
                    this.filesystem.Save();
                }
                else {
                    var fsFile = this.filesystem.ReadFile(fsName);
                    if (fsFile.FileType === typeof "string") {
                        this.filesystem.DeleteFile(fsName);
                        fsFile.Data += "\n" + content;
                        this.filesystem.Save();
                    }
                    else {
                        stdout("ERROR: Filetype unmergeable");
                    }
                }
            }
            else if (command === "import") { // Import a module as a PDL.
                if (this.filesystem.ReadFile(data).Path === "EMPTY") {
                    // this.terminated = true;
                    stdout("ERROR: Couldn't find file to import, Process will keep running.");
                }
                else {
                    this.ExecASM((this.filesystem.ReadFile(data).Data || ""), "runtime", true);
                }
            }
            else if (command === "exec") { // Execute a PDL from a variable.
                this.ExecASM(this.findRegister(data).Data || "", "runtime", true);
            }
            else if (command === "save") { // Save the filesystem.
                this.filesystem.Save();
            }
            else if (command === "load") { // Load the filesystem.
                this.filesystem.Load();
            }
            else if (command === "wipe") { // Wipe the filesystem.
                stdout("CAUTION: This action will wipe your whole disk, Meaning you will loose everything you have stored in PlugDOS!\nIf you want to continue, Type in \"agree\" into the box below and press enter.");
                if (stdin("Typing \"agree\" would delete ALL your files >> ") === "agree") {
                    stdout("WIPING DRIVE...");
                    files = [];
                    this.filesystem.NewFileSystem().then(function () {
                        _this.filesystem.Save();
                    });
                }
                else {
                    this.terminated = true;
                    stdout("[PROCESS TERMINATION] Reason: trying to wipe the filesystem.");
                }
            }
            else if (command === "ls") { // List all files in the current directory.
                var output = "----------\n";
                for (var _i = 0, _f = this.filesystem.GetFiles(this.filesystem.directory); _i < _f.length; _i++) {
                    var file = _f[_i];
                    output += file.Path + "\n";
                }
                output += "----------";
                stdout(output);
            }
            else if (command === "cd") { // Switch to a directory
                if (!this.checkArgs(args, 1))
                    break;
                if (data === "." || data === "..") { }
                if (data === "")
                    this.filesystem.directory = "/";
                if (!data.endsWith("/"))
                    data += "/";
                if (data.startsWith("/"))
                    this.filesystem.directory = data;
                else {
                    this.filesystem.directory += data;
                    this.filesystem.directory = this.filesystem.directory.replace("//", "");
                }
            }
            else if (command === "mkdir") { // Create a directory.
                if (!this.checkArgs(args, 1))
                    break;
            }
            else if (command === "rmdir") { // Delete a directory.
                if (!this.checkArgs(args, 1))
                    break;
                this.filesystem.DeleteDirectory(args[0]);
            }
            else if (command === "reg") { // The ultimate registry tool
                if (!this.checkArgs(args, 3))
                    break;
                var regCMD = args[0];
                var path = args[1];
                var reg = args[2];
                if (regCMD === "write") { // Write value from the registry to a file.
                    if ((this.findRegister(reg).Data || "") === "null") {
                        stdout("ERROR: Registry field not found");
                    }
                    else {
                        this.filesystem.DeleteFile(path);
                        this.filesystem.WriteFile(new PDFile(path, (this.findRegister(reg).Data || "")));
                    }
                }
                else if (regCMD === "read") { // Read a value from a file and write it to a registry slot.
                    var foundFile = this.filesystem.ReadFile(path);
                    this.variables[reg] = new PDLoadedFile(foundFile.Data);
                }
                else if (regCMD === "clone") { // Clone a registry value.
                    if ((this.findRegister(path).Data || "") === "null") {
                        stdout("ERROR: Registry field not found.");
                    }
                    else {
                        this.variables[reg] = this.findRegister(path);
                    }
                }
                this.filesystem.Save();
            }
            else if (command === "base") {
                stdout("Updating system files...");
                this.filesystem.NewFileSystem();
            }
            else if (command === "exit") {
                stdout("Shutting down safely...");
                stdout("Stopping all running PDBs...");
                this.terminated = true;
                stdout("Saving the filesystem...");
                this.filesystem.Save();
                stdout("Freeing up ram space...");
                this.variables = [];
                this.loadedFunctions = [];
                stdout("Now you are able to safely close PlugDOS without any issues. Type in anything to close this window.");
                stdin("");
            }
            else { // Check if the user is calling a function, Throw an error if it is unknown, Or run the function.
                if (this.loadedFunctions[command]) {
                    var func = this.loadedFunctions[command];
                    this.ExecASM((func.Data || ""), "runtime", true);
                }
                else {
                    stdout("ERROR: Unknown command \"" + command.toUpperCase() + "\"");
                }
            }
        }
    };
    return PlugDOS;
}());
// Declares a file class.
var PDFile = /** @class */ (function () {
    function PDFile(Path, Data) {
        this.Path = Path;
        this.Data = Data;
        this.FileType = typeof Data;
    }
    return PDFile;
}());
// Declares a file that has been loaded into memory. Could either be a normal file, A function or a variable.
var PDLoadedFile = /** @class */ (function () {
    function PDLoadedFile(Data) {
        this.Data = Data;
        this.FileType = typeof Data;
    }
    return PDLoadedFile;
}());
// Declares the actual filesystem.
var PDFileSystem = /** @class */ (function () {
    function PDFileSystem() {
        this.files = [];
        this.directory = "/";
        this.fallback = JSON.stringify([new PDFile("/boot.pd", "ECHO We are sorry, But due to Javascript and Typescript problems we couldn't recover your filesystem. Try using the \"wipe\" command to reset the OS.\nDEF RECOVERY-PROMPT\nASK cmd\nEXEC cmd\nRECOVERY-PROMPT\nEND RECOVERY-PROMPT\nWAIT 2\nRECOVERY-PROMPT")]);
        this.Load();
    }
    PDFileSystem.prototype.Save = function () {
        var _this = this;
        if (typeof this.files == "undefined") {
            this.NewFileSystem().then(function () {
                _this.Save();
            });
        }
        else {
            localStorage.setItem('PlugDOS', JSON.stringify(this.files) || this.fallback);
        }
    };
    PDFileSystem.prototype.Load = function () {
        var _this = this;
        if (typeof this.files == "undefined") {
            this.NewFileSystem().then(function () {
                _this.Load();
            });
        }
        else {
            this.files = JSON.parse(localStorage.getItem('PlugDOS') || this.fallback);
        }
    };
    // Write a file to filesystem
    PDFileSystem.prototype.WriteFile = function (file) {
        var file2 = file;
        if (file2.Path.endsWith(".RESERVE")) {
            stdout("ERROR: Denied permission to reserved file/path");
        }
        if (!file2.Path.startsWith("/"))
            file2.Path = "/" + file2.Path;
        if (this.ReadFile(file2.Path).Path === "EMPTY") {
            this.files.push(file2);
        }
        else {
            this.files.push(file2);
            this.files.filter(function (file) { return file.Path !== file2.Path; });
            this.files.push(file2);
        }
        this.Save();
    };
    // Updates the system dependencies.
    PDFileSystem.prototype.NewFileSystem = function () {
        return __awaiter(this, void 0, void 0, function () {
            return __generator(this, function (_a) {
                this.WriteFile(new PDFile("/sys/prompt.pd", "# The command PROMPT for PlugDOS.\n" +
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
                this.WriteFile(new PDFile("/boot.pd", "# The initial boot procedure for PlugDOS that loads up all system files and runs through them.\n" +
                    "ECHO Booting...\n" +
                    "WAIT 2\n" +
                    "IMPORT /sys/load.pd"));
                this.WriteFile(new PDFile("/sys/load.pd", "# The load procedure for all CMD commands and the PROMPT loop.\n" +
                    "IMPORT /sys/prompt.pd\n" +
                    "IMPORT /sys/help.pd\n" +
                    "PROMPT"));
                this.WriteFile(new PDFile("/sys/help.pd", "# The help command!\n" +
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
                return [2 /*return*/];
            });
        });
    };
    // Reads a file off the filesystem.
    PDFileSystem.prototype.ReadFile = function (Path) {
        var _this = this;
        var Path2 = Path;
        if (!Path2.startsWith("/"))
            Path2 = this.directory + Path2;
        return this.files.filter(function (file) { return (file.Path === Path2 || _this.directory + file.Path === Path2) && (file.Path.indexOf(".RESERVE") === -1); })[0] || new PDFile("EMPTY", "");
    };
    // Deletes a file from the filesystem.
    PDFileSystem.prototype.DeleteFile = function (Path) {
        if (this.ReadFile(Path).Path === "EMPTY") {
            return false;
        }
        else {
            files = files.filter(function (file) { return file.Path !== Path; });
            this.Save();
            return true;
        }
    };
    // Creates a directory.
    PDFileSystem.prototype.MakeDirectory = function (Path) {
        var workingPath = this.directory + Path;
        if (!workingPath.endsWith("/"))
            workingPath += "/";
        if (workingPath.indexOf(".RESERVE") > -1) {
            stdout("ERROR: Denied permission to reserved file/path");
        }
        else {
            files.push(new PDFile(workingPath + ".RESERVE", ""));
        }
        this.Save();
    };
    // Deletes a directory.
    PDFileSystem.prototype.DeleteDirectory = function (Path) {
        var workingPath = this.directory + Path;
        if (!workingPath.endsWith("/"))
            workingPath += "/";
        if (workingPath.indexOf(".RESERVE") > -1) {
            stdout("ERROR: Denied permission to reserved file/path");
        }
        else {
            workingPath += ".RESERVE";
            files = files.filter(function (file) { return file.Path !== workingPath || file.Path.startsWith(workingPath); });
        }
        this.Save();
    };
    // Gets all the files and directories from a path.
    PDFileSystem.prototype.GetFiles = function (Path) {
        if (!Path.endsWith("/"))
            Path += "/";
        var fileList = [];
        for (var _i = 0, _a = this.files; _i < _a.length; _i++) {
            var file = _a[_i];
            if (file.Path.startsWith(Path)) {
                var filename = file.Path.substring(Path.length);
                if (filename.indexOf('/') === -1) {
                    fileList.push(file);
                }
                else {
                    var dirpath = Path + filename.split('/')[0] + "/";
                    var isAdded = false;
                    for (var _b = 0, fileList_1 = fileList; _b < fileList_1.length; _b++) {
                        var file2 = fileList_1[_b];
                        if (file2.Path) {
                            isAdded = true;
                        }
                    }
                    if (file.Path.endsWith(".RESERVE")) {
                        if (!isAdded) {
                            fileList.push(new PDFile(dirpath.substring(0, file.Path.length - ".RESERVE".length), "EMPTY"));
                        }
                    }
                    else {
                        if (!isAdded) {
                            fileList.push(new PDFile(dirpath, "EMPTY"));
                        }
                    }
                }
            }
        }
        return fileList;
    };
    return PDFileSystem;
}());
var dos = new PlugDOS();
dos.Boot();
