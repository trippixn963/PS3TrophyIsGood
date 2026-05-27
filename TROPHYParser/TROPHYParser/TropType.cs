using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TROPHYParser
{
    /// <summary>
    /// Trophy rank, using the same numeric codes the PS3 stores on disk
    /// (1 = highest rank). Platinum is the "all other trophies earned" award.
    /// </summary>
    public enum TropType
    {
        Platinum = 1,
        Gold = 2,
        Silver = 3,
        Bronze = 4
    }

    /// <summary>
    /// Point value awarded for each trophy rank, matching PSN's level-progress
    /// weighting (Platinum 180, Gold 90, Silver 30, Bronze 15).
    /// </summary>
    public enum TropGrade
    {
        Platinum = 180,
        Gold = 90,
        Silver = 30,
        Bronze = 15
    }
}
