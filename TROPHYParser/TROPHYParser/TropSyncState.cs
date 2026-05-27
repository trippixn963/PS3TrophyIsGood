using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TROPHYParser
{
    /// <summary>
    /// Bit flags stored in a trophy's sync-state field. <see cref="Sync"/> marks a
    /// trophy that has already been uploaded to PSN (and so must not be edited);
    /// <see cref="NotSync"/> marks one that has been earned locally but not yet synced.
    /// </summary>
    enum TropSyncState
    {
        Sync = 0x100,
        NotSync = 0x100000
    }
}
