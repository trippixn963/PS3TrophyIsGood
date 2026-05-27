using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace TROPHYParser
{
    /// <summary>Thrown by <see cref="TROPTRNS.PutTrophy"/> when the trophy is already in the table.</summary>
    public class TropTrnsAlreadyGotException : Exception
    {
        public TropTrnsAlreadyGotException(string message) : base(message) { }
        public TropTrnsAlreadyGotException() : base("Trophy already got.") { }
    }

    /// <summary>
    /// Thrown when a requested earned-time would land before a trophy that is already
    /// synced to PSN. Carries that trophy's sync time so the caller can warn the user.
    /// </summary>
    public class TropTrnsPsnSyncTimeException : Exception
    {
        private DateTime psnSyncTime = new DateTime(0);
        public DateTime PsnSyncTime
        {
            get { return psnSyncTime; }
        }

        public TropTrnsPsnSyncTimeException(string message, DateTime psnSyncTime) : base(message)
        {
            this.psnSyncTime = psnSyncTime;
        }
        public TropTrnsPsnSyncTimeException(DateTime psnSyncTime) : base(string.Format("The last trophy synchronized with PSN has the following date: {0:dd/MM/yyyy HH:mm:ss}. Select a date greater than this.", psnSyncTime)) { }
    }

    /// <summary>Thrown when an operation references a trophy id that isn't in the table.</summary>
    public class TropTrnsTrophyNotFound : Exception
    {
        public TropTrnsTrophyNotFound(string message) : base(message) { }
        public TropTrnsTrophyNotFound() : base("Trophy ID not found.") { }
    }

    /// <summary>
    /// Reads and writes <c>TROPTRNS.DAT</c>, the trophy "transaction" log — the
    /// ordered list of earned trophies that the PS3 syncs to PSN. It overlaps with
    /// TROPUSR.DAT but is kept time-ordered and carries each trophy's sync flag.
    ///
    /// Same on-disk shape as TROPUSR: big-endian <see cref="Header"/>, a
    /// <see cref="TypeRecord"/> directory, then blocks. The trophy entries live in
    /// block type 4 as a time-sorted list of <see cref="TrophyInfo"/>.
    ///
    /// RPCS3 dumps don't have this file, so when <c>isRpcs3Format</c> is set the
    /// parser and <see cref="Save"/> become no-ops. Edits keep the list sorted by
    /// time and refuse to disturb entries already synced to PSN.
    /// </summary>
    public class TROPTRNS
    {

        private const string TROPTRNS_FILE_NAME = "TROPTRNS.DAT";
        private bool isRpcs3Format;

        string path;
        Header header;

        // Block-type directory: block type id -> on-disk location/size.
        Dictionary<int, TypeRecord> typeRecordTable;

        // Earned trophies, kept sorted by earned time (oldest first).
        List<TrophyInfo> trophyInfoTable = new List<TrophyInfo>();
        public string account_id;
        public string trophy_id;
        int u1;
        int AllGetTrophysCount;
        int AllSyncPSNTrophyCount;

        /// <summary>Latest earned-time among already-synced trophies, or 2008-01-01 if none.</summary>
        public DateTime LastSyncTime
        {
            get
            {
                DateTime aux = new DateTime(2008, 1, 1);
                foreach (TrophyInfo tropInfo in trophyInfoTable)
                {
                    if (tropInfo.IsSync && DateTime.Compare(tropInfo.Time, aux) > 0)
                    {
                        aux = tropInfo.Time;
                    }
                }
                return aux;
            }
        }

        /// <summary>Latest earned-time across all trophies (synced or not), or 2008-01-01 if none.</summary>
        public DateTime LastTrophyTime
        {
            get
            {
                DateTime aux = new DateTime(2008, 1, 1);
                foreach (TrophyInfo tropInfo in trophyInfoTable)
                {
                    if (DateTime.Compare(tropInfo.Time, aux) > 0)
                    {
                        aux = tropInfo.Time;
                    }
                }
                return aux;
            }
        }


        TrophyInitTime trophyInitTime;

        /// <summary>
        /// Parses <c>TROPTRNS.DAT</c> under <paramref name="path"/>: reads the account
        /// id (block 2), the trophy id and totals (block 3), then the time-sorted
        /// trophy list (block 4). For RPCS3 there is no file, so this returns early
        /// and the object stays empty. Throws on a missing file or bad magic number.
        /// </summary>
        public TROPTRNS(string path, bool isRpcs3Format)
        {
            if (isRpcs3Format)
            {
                this.isRpcs3Format = isRpcs3Format;
                return;
            }
            if (path == null || path.Trim() == string.Empty)
                throw new Exception("Path cannot be null!");

            string fileName = Path.Combine(path, TROPTRNS_FILE_NAME);

            if (!File.Exists(fileName))
                throw new FileNotFoundException("File not found", fileName);

            this.path = path;

            using (var fileStream = new FileStream(fileName, FileMode.Open))
            using (var TROPTRNSReader = new BigEndianBinaryReader(fileStream))
            {
                header = TROPTRNSReader.ReadBytes(Marshal.SizeOf(typeof(Header))).ToStruct<Header>();
                if (header.Magic != 0x0000000100ad548f81)
                    throw new InvalidTrophyFileException(TROPTRNS_FILE_NAME);

                typeRecordTable = new Dictionary<int, TypeRecord>();
                for (int i = 0; i < header.UnknowCount; i++)
                {
                    TypeRecord TypeRecordTmp = TROPTRNSReader.ReadBytes(Marshal.SizeOf(typeof(TypeRecord))).ToStruct<TypeRecord>();
                    typeRecordTable.Add(TypeRecordTmp.ID, TypeRecordTmp);
                }

                // Block type 2: account id (sits 32 bytes past the record offset).
                TypeRecord account_id_Record = typeRecordTable[2];
                TROPTRNSReader.BaseStream.Position = account_id_Record.Offset + 32; // skip block header
                account_id = Encoding.UTF8.GetString(TROPTRNSReader.ReadBytes(16));

                // Block type 3: trophy id followed by the earned/synced totals.
                TypeRecord trophy_id_Record = typeRecordTable[3];
                TROPTRNSReader.BaseStream.Position = trophy_id_Record.Offset + 16; // skip block header
                trophy_id = Encoding.UTF8.GetString(TROPTRNSReader.ReadBytes(16)).Trim('\0');
                u1 = TROPTRNSReader.ReadInt32(); // always 00000090
                AllGetTrophysCount = TROPTRNSReader.ReadInt32();
                AllSyncPSNTrophyCount = TROPTRNSReader.ReadInt32();

                // Block type 4: the time-sorted trophy list. The first entry is the
                // list's "init time" marker; the rest are the actual earned trophies.
                TypeRecord TrophyInfoRecord = typeRecordTable[4];
                TROPTRNSReader.BaseStream.Position = TrophyInfoRecord.Offset; // start of the block
                int type = TROPTRNSReader.ReadInt32();
                int blocksize = TROPTRNSReader.ReadInt32();
                int sequenceNumber = TROPTRNSReader.ReadInt32(); // distinguishes multiple blocks of the same type
                int unknow = TROPTRNSReader.ReadInt32();
                byte[] blockdata = TROPTRNSReader.ReadBytes(blocksize);
                trophyInitTime = blockdata.ToStruct<TrophyInitTime>();


                // Read the remaining entries (the init-time marker took one slot).
                for (int i = 0; i < (AllGetTrophysCount - 1); i++)
                {
                    TROPTRNSReader.BaseStream.Position += 16; // step over this entry's header
                    TrophyInfo ti = TROPTRNSReader.ReadBytes(blocksize).ToStruct<TrophyInfo>();
                    trophyInfoTable.Add(ti);
                }
            }
        }

        /// <summary>Dumps the parsed contents to the console; for debugging only.</summary>
        public void PrintState()
        {
            Console.WriteLine("AllGetTrophysCount:{0}", AllGetTrophysCount);
            Console.WriteLine("Counter: {0}", header.UnknowCount);
            Console.WriteLine("Padding:{0}", header.padding.ToHexString());
            foreach (KeyValuePair<int, TypeRecord> fk in typeRecordTable)
            {
                Console.WriteLine(fk.Value);
            }
            Console.WriteLine("account_id:{0}", account_id);
            Console.WriteLine("trophy_id:{0}", trophy_id);

            Console.WriteLine("Geted Trophys:{0} Sync Trophys:{1} ", AllGetTrophysCount, AllSyncPSNTrophyCount);


            for (int i = 0; i < trophyInfoTable.Count; i++)
            {
                Console.WriteLine("SN:{0}, Trophy ID:{1}, Type:{2}, Exists:{3}, Time:{4}, Sync:{5} ",
                    trophyInfoTable[i].SequenceNumber, trophyInfoTable[i].TrophyID,
                    trophyInfoTable[i].TrophyType, trophyInfoTable[i].IsExist, trophyInfoTable[i].Time,
                    trophyInfoTable[i].IsSync
                   );
            }

        }

        /// <summary>
        /// Writes the in-memory trophy list back into <c>TROPTRNS.DAT</c> in place
        /// (no-op for RPCS3). Recomputes the earned and synced totals, renumbers the
        /// entries, writes each one, and zero-fills the unused slots up to the block's
        /// capacity. As in TROPUSR, <c>Position += 16</c> hops over each entry's header.
        /// </summary>
        public void Save()
        {
            if (isRpcs3Format) return;
            using (var fileStream = new FileStream(Path.Combine(path, TROPTRNS_FILE_NAME), FileMode.Open))
            using (var TROPTRNSWriter = new BigEndianBinaryWriter(fileStream))
            {
                TROPTRNSWriter.Write(header.StructToBytes());
                TypeRecord account_id_Record = typeRecordTable[2];
                TROPTRNSWriter.BaseStream.Position = account_id_Record.Offset + 32; // skip block header
                TROPTRNSWriter.Write(account_id.ToCharArray());

                TypeRecord trophy_id_Record = typeRecordTable[3];
                TROPTRNSWriter.BaseStream.Position = trophy_id_Record.Offset + 16; // skip block header
                TROPTRNSWriter.Write(trophy_id.ToCharArray());
                TROPTRNSWriter.BaseStream.Position = trophy_id_Record.Offset + 32; // trophy id length varies; jump to the fixed fields
                TROPTRNSWriter.Write(u1);
                Console.WriteLine(trophyInfoTable.Count);
                TROPTRNSWriter.Write(trophyInfoTable.Count + 1); // AllGetTrophysCount
                AllSyncPSNTrophyCount = 0;
                for (int i = 0; i < trophyInfoTable.Count; i++)
                {
                    if (trophyInfoTable[i].IsSync)
                    {
                        AllSyncPSNTrophyCount++;
                    }
                }
                // AllSyncPSNTrophyCount++;
                TROPTRNSWriter.Write(AllSyncPSNTrophyCount + 1);

                // Type 4
                TypeRecord TrophyType_Record = typeRecordTable[4];
                TROPTRNSWriter.BaseStream.Position = TrophyType_Record.Offset;
                TROPTRNSWriter.BaseStream.Position += 16;
                TROPTRNSWriter.Write(trophyInitTime.StructToBytes());


                for (int i = 0; i < trophyInfoTable.Count; i++)
                {
                    TROPTRNSWriter.BaseStream.Position += 16; // step over this entry's header
                    TrophyInfo ti = trophyInfoTable[i];
                    ti.SequenceNumber = i + 1; // renumber sequentially after any reorder
                    // Earlier experiment with _unknowInt2 left in place for reference:
                    //if (i == 0) {
                    //    ti._unknowInt2 = 0x100000;
                    //} else {
                    //    ti._unknowInt2 = 0x100000;
                    //}
                    ti._unknowInt3 = 0;

                    trophyInfoTable[i] = ti;
                    TROPTRNSWriter.Write(trophyInfoTable[i].StructToBytes());
                }

                // Zero-fill the rest of the block's capacity so stale entries from a
                // previously longer list don't linger on disk.
                byte[] emptyStruct = new byte[Marshal.SizeOf(typeof(TrophyInfo))];
                Array.Clear(emptyStruct, 0, emptyStruct.Length);
                TrophyInfo emptyTrophyInfo = emptyStruct.ToStruct<TrophyInfo>();
                for (int i = trophyInfoTable.Count; i < TrophyType_Record.Size; i++)
                {
                    TROPTRNSWriter.BaseStream.Position += 16;
                    emptyTrophyInfo.SequenceNumber = i + 1;
                    TROPTRNSWriter.Write(emptyTrophyInfo.StructToBytes());
                }
            }
        }

        /// <summary>
        /// Inserts a newly earned trophy at the position that keeps the list sorted by
        /// time. Throws <see cref="TropTrnsAlreadyGotException"/> if it's already present,
        /// or <see cref="TropTrnsPsnSyncTimeException"/> if the chosen time would push it
        /// ahead of a trophy already synced to PSN.
        /// </summary>
        public void PutTrophy(int id, int TrophyType, DateTime dt)
        {
            TrophyInfo ti = new TrophyInfo(id, TrophyType, dt);
            foreach (TrophyInfo titmp in trophyInfoTable)
            {
                if (titmp.TrophyID == id)
                {
                    throw new TropTrnsAlreadyGotException();
                }
            }

            // Find the first entry later than dt; that's where the new one slots in.
            int insertPoint;
            for (insertPoint = 0; insertPoint < trophyInfoTable.Count; insertPoint++)
            {
                var trophyTime = trophyInfoTable[insertPoint].Time;
                if (DateTime.Compare(trophyTime, dt) > 0)
                {
                    if (trophyInfoTable[insertPoint].IsSync)
                    {
                        throw new TropTrnsPsnSyncTimeException(trophyTime);
                    }
                    break;
                }
            }
            trophyInfoTable.Insert(insertPoint, ti);
            // Console.WriteLine(trophyInfoTable.Count);
            AllGetTrophysCount++;
        }

        /// <summary>
        /// Removes and returns the most recently earned trophy, or null if the list is
        /// empty or its last entry is already synced (synced trophies can't be removed).
        /// </summary>
        public TrophyInfo? PopTrophy()
        {
            if (trophyInfoTable.Count == 0)
                return null;
            TrophyInfo pop = trophyInfoTable[trophyInfoTable.Count - 1];
            if (pop.IsSync)
            {
                return null;
            }
            else
            {
                trophyInfoTable.RemoveAt(trophyInfoTable.Count - 1);
                AllGetTrophysCount--;
            }
            return pop;
        }

        /// <summary>
        /// Moves trophy <paramref name="id"/> to a new earned time, re-sorting it into
        /// the right position. Throws if the trophy isn't found, is already synced, or
        /// if the new time would precede a synced trophy (in which case the entry is put
        /// back where it was before throwing).
        /// </summary>
        public void ChangeTime(int id, DateTime dt)
        {
            TrophyInfo? ti = null;
            int originalIndex;
            for (originalIndex = 0; originalIndex < trophyInfoTable.Count; originalIndex++)
            {
                if (trophyInfoTable[originalIndex].TrophyID == id)
                {
                    ti = trophyInfoTable[originalIndex];
                    break;
                }
            }

            if (ti == null)
                throw new TropTrnsTrophyNotFound();

            if (ti.Value.IsSync)
                throw new TrophyAlreadySyncException();

            trophyInfoTable.RemoveAt(originalIndex);

            TrophyInfo trophyInfo = (TrophyInfo)ti;
            bool inserted = false;
            for (int i = 0; i < trophyInfoTable.Count; i++)
            {
                var trophyTime = trophyInfoTable[i].Time;
                if (DateTime.Compare(trophyTime, dt) > 0)
                {
                    if (trophyInfoTable[i].IsSync)
                    {
                        trophyInfoTable.Insert(originalIndex, trophyInfo);
                        throw new TropTrnsPsnSyncTimeException(trophyTime);
                    }
                    trophyInfo.Time = dt;
                    trophyInfoTable.Insert(i, trophyInfo);
                    inserted = true;
                    break;
                }
            } // Insert Into Table

            if (!inserted)
            {
                trophyInfo.Time = dt;
                trophyInfoTable.Add(trophyInfo);
            }

        }

        /// <summary>
        /// Removes the trophy with the given id. Throws <see cref="TrophyAlreadySyncException"/>
        /// if it has been synced to PSN. No-op if the id isn't present.
        /// </summary>
        public void DeleteTrophyByID(int id)
        {
            for (int i = 0; i < trophyInfoTable.Count; i++)
            {
                if (trophyInfoTable[i].TrophyID == id)
                {
                    if (trophyInfoTable[i].IsSync)
                        throw new TrophyAlreadySyncException();
                    trophyInfoTable.RemoveAt(i);
                    AllGetTrophysCount--;
                }
            }
        }

        /// <summary>Looks up a trophy by its id, or null if it isn't in the list.</summary>
        public TrophyInfo? this[int TrophyID]
        {
            get
            {
                TrophyInfo? ret = null;
                for (int i = 0; i < trophyInfoTable.Count; i++)
                {
                    if (trophyInfoTable[i].TrophyID == TrophyID)
                    {
                        ret = trophyInfoTable[i];
                        break;
                    }
                }
                return ret;
            }
        }
        // Structs below mirror the on-disk byte layout exactly: [StructLayout(Sequential)]
        // plus fixed-size MarshalAs byte arrays let a raw block be cast straight into a
        // struct. DO NOT reorder fields, change their types, or alter array sizes — that
        // would shift every offset and corrupt parsing. Multi-byte numbers are big-endian,
        // so the public accessors run the raw `_field` through ChangeEndian(). The leading
        // "/// <type>" notes record each field's raw on-disk width.
        #region Structs

        /// <summary>File header: magic number, block count, and reserved padding.</summary>
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct Header
        {

            /// long
            public ulong Magic;

            /// int
            public int _unknowCount;
            public int UnknowCount
            {
                get
                {
                    return _unknowCount.ChangeEndian();
                }
            }


            /// byte[36]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 36, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
            public byte[] padding;
        }

        /// <summary>
        /// Directory entry for one block type: its id, payload size, how many times it
        /// occurs, and the byte offset where its block(s) begin in the file.
        /// </summary>
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct TypeRecord
        {

            /// int
            private int _id;
            public int ID
            {
                get
                {
                    return _id.ChangeEndian();
                }
            }

            /// int
            private int _size;
            public int Size
            {
                get
                {
                    return _size.ChangeEndian();
                }
            }

            /// int
            public int _unknow3;
            public int unknow3
            {
                get
                {
                    return _unknow3.ChangeEndian();
                }
            }

            /// int
            private int _usedTimes;
            public int UsedTimes
            {
                get
                {
                    return _usedTimes.ChangeEndian();
                }
            }

            /// int
            public long _offset;
            public long Offset
            {
                get
                {
                    return _offset.ChangeEndian();
                }
            }

            /// int
            public long _unknow6;
            public long unknow6
            {
                get
                {
                    return _unknow6.ChangeEndian();
                }
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("{ID:").Append(ID).Append(", ");
                sb.Append("Size:").Append(Size).Append(", ");
                sb.Append("u3:").Append(unknow3).Append(", ");
                sb.Append("UsedTimes:").Append(UsedTimes).Append(", ");
                sb.Append("Offset:").Append(Offset).Append(", ");
                sb.Append("u6:").Append(unknow6).Append("}");
                return sb.ToString();
            }
        }

        /// <summary>
        /// One entry in the trophy transaction list: which trophy, its rank, whether it
        /// exists/has been synced, and when it was earned (<see cref="Time"/>). Note the
        /// time encoding here matches TROPUSR but has no "ticks == 0 means unset" branch
        /// on read.
        /// </summary>
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct TrophyInfo
        {

            /// int
            private int _sequenceNumber;
            public int SequenceNumber
            {
                get
                {
                    return _sequenceNumber.ChangeEndian();
                }
                set
                {
                    _sequenceNumber = value.ChangeEndian();
                }
            }

            /// byte[4]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
            private byte[] _isExist;

            /// <summary>True if this slot holds a real trophy (flag byte == 2). Lives in the last byte of the 4-byte field.</summary>
            public bool IsExist
            {
                set
                {
                    _isExist[3] = (byte)((value) ? 2 : 0);
                }
                get
                {
                    return (_isExist[3] == 2) ? true : false;
                }
            }


            /// byte[4]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
            private byte[] _syncState;

            /// <summary>True once this trophy has been synced to PSN. Flag lives in the last byte of the 4-byte field.</summary>
            public bool IsSync
            {
                set
                {
                    _syncState[3] = (byte)((value) ? 1 : 0);
                }
                get
                {
                    return (_syncState[3] == 0) ? false : true;
                }
            }

            /// int
            public int _unknowInt1;

            /// byte[16]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 16, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
            public byte[] padding;


            /// int
            private int _trophyID;
            public int TrophyID
            {
                get
                {
                    return _trophyID.ChangeEndian();
                }
                set
                {
                    _trophyID = value.ChangeEndian();
                }
            }

            /// int
            private int _trophyType;
            public int TrophyType
            {
                get
                {
                    return _trophyType.ChangeEndian();
                }
                set
                {
                    _trophyType = value.ChangeEndian();
                }
            }


            /// int
            public int _unknowInt2;

            /// int
            public int _unknowInt3;

            /// byte[16]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 16, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
            private byte[] _getTime;

            /// <summary>
            /// Earned time, encoded as in <see cref="TROPUSR.TrophyListInfo.ListCreateTime"/>
            /// (big-endian microseconds, local time, stored twice in the 16-byte slot).
            /// </summary>
            public DateTime Time
            {
                get
                {
                    DateTime dt = new DateTime(BitConverter.ToInt64(_getTime, 0).ChangeEndian() * 10);
                    return dt.AddHours(TimeZoneInfo.Local.BaseUtcOffset.Hours);
                }
                set
                {
                    if (value.Ticks == 0)
                    {
                        Array.Clear(_getTime, 0, 16);
                    }
                    else
                    {
                        long tmp = value.AddHours(-TimeZoneInfo.Local.BaseUtcOffset.Hours).Ticks;
                        Array.Copy(BitConverter.GetBytes((tmp / 10).ChangeEndian()), 0, _getTime, 0, 8);
                        Array.Copy(BitConverter.GetBytes((tmp / 10).ChangeEndian()), 0, _getTime, 8, 8);
                    }
                }
            }

            /// byte[96]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 96, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
            public byte[] padding2;

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("[").Append("SequenceNumber:").Append(SequenceNumber).Append(", ");
                sb.Append("GetState:").Append(IsExist).Append("]");
                return sb.ToString();
            }

            /// <summary>
            /// Builds a fresh, earned-but-unsynced entry for trophy <paramref name="id"/>.
            /// Allocates the fixed-size byte arrays the marshaller expects, byte-swaps the
            /// numeric fields, sets the earned time, and marks the slot as existing.
            /// </summary>
            public TrophyInfo(int id, int TrophyType, DateTime dt)
            {
                _sequenceNumber = 0;
                _isExist = new byte[4];
                _getTime = new byte[16];
                _syncState = new byte[4];
                _trophyID = id.ChangeEndian();
                _trophyType = TrophyType.ChangeEndian();
                _unknowInt1 = 0;
                _unknowInt2 = 0x00100000;
                _unknowInt3 = 0;
                padding = new byte[16];
                padding2 = new byte[96];
                Time = dt;
                IsExist = true;
            }
        }

        /// <summary>
        /// The first entry in the block-4 trophy list: a marker record holding the
        /// list's initialisation time. Mostly unknown fields plus a 16-byte time slot.
        /// </summary>
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct TrophyInitTime
        {

            /// int
            public int u1;

            /// int
            public int u2;

            /// int
            public int u3;

            /// int
            public int u4;

            /// byte[16]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 16, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
            public byte[] padding;

            /// byte[16]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 16, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
            public byte[] _initTime;

            /// byte[112]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 112, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
            public byte[] padding2;
        }

        #endregion
    }
}
