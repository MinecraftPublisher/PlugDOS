# PlugDOS
 A Semi-OS optimised for the command line.

## What does it do?
Everything...? ( ~~Except filesystem access~~ half done )

## Where to find it?
[WINDOWS ONLY] [Click here!](https://github.com/MinecraftPublisher/PlugDOS/releases/latest)

## The programming language name: PlugDOSLang (PDL)

## Available commands:
 - `# TEXT` => A comment!
 - `clear` => Clears the console
 - `dump PATH` => Dumps a file's data on command line
 - `error STRING` => Drop an error to the console 
 - `ask REGISTERNAME` => Ask the user for input, And store it in the registry
 - `register REGISTERNAME DATA MORE DATA` => Write data to the registry
 - `wait TIME` => Delay for a specified amount of seconds
 - `if STATEMENT1 STATEMENT2 REGISTERTORUN` => Run a register value if statement1 and statement2 are equal
 - Define a function (could be called later by calling it as a command, Example: `testFUNCTION`):
```DEF testFUNCTION
# Code goes here...
END testFUNCTION
```
 - `write FILENAME CONTENT` => Write some content to a file
 - `remove FILENAME` => Delete a file
 - `append FILENAME CONTENT` => Appends content to a file (string-only)
 - `import FILENAME` => Import a file as a PDL file
 - `exec REGISTERNAME` => Execute a PDL script from register
 ## The essentials:
  - `load` => Loads the filesystem from disk, Reverting any changes made without saving
  - `save` => Saves the filesystem to the disk
  - `wipe` => Wipes the PlugDOSFS drive, Be careful! We also prompt you at any time
