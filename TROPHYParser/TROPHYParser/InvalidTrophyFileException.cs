using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TROPHYParser
{
    /// <summary>
    /// Thrown when a trophy data file fails its sanity check (e.g. a wrong magic
    /// number), meaning the file is missing, corrupt, or not a PS3 trophy file.
    /// </summary>
    class InvalidTrophyFileException : Exception
    {
        private string fileName;

        /// <summary>Name of the file that failed to parse.</summary>
        public string FileName
        {
            get { return fileName; }
        }

        public InvalidTrophyFileException(string message, string fileName) : base(message)
        {
            this.fileName = fileName;
        }

        public InvalidTrophyFileException(string fileName) : base(string.Format("Not a valid {0}.", fileName)) { }
    }
}
