using System;

namespace PlugDOS
{
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