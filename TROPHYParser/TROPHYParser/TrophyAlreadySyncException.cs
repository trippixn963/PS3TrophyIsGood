using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TROPHYParser
{
    /// <summary>
    /// Thrown when an edit (lock / time change / delete) is attempted on a trophy
    /// that has already been synced to PSN. Synced trophies are frozen, because
    /// changing them would put the local save out of step with the server.
    /// </summary>
    class TrophyAlreadySyncException : Exception
    {
        public TrophyAlreadySyncException(string message) : base(message) { }
        public TrophyAlreadySyncException() : base("Trophy already synchronized. Can't be modified.") { }
    }
}
