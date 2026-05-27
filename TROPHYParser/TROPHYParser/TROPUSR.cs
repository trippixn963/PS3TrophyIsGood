using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace TROPHYParser
{
    /// <summary>
    /// Reads and writes <c>TROPUSR.DAT</c>, the per-account trophy state file.
    ///
    /// The file is a big-endian binary blob: a <see cref="Header"/>, a table of
    /// <see cref="TypeRecord"/> entries (each saying where a numbered block lives
    /// and how big it is), then a sequence of variable blocks. Each block has a
    /// 16-byte block header (type, size, sequence number, unknown) followed by its
    /// payload, which is marshalled directly into one of the structs below.
    ///
    /// Block types: 1 unknown, 2 account_id, 3 trophy_id + achievement bitmap,
    /// 4 per-trophy <see cref="TrophyType"/>, 5 <see cref="TrophyListInfo"/> totals,
    /// 6 per-trophy <see cref="TrophyTimeInfo"/> (earned time + sync state),
    /// 7 <see cref="UnknowType7"/> counts, 8 hash, 9/10 unknown/padding.
    ///
    /// This is the file that records WHICH trophies are earned and WHEN, so it is
    /// what <see cref="UnlockTrophy"/> / <see cref="LockTrophy"/> mutate.
    /// </summary>
    public class TROPUSR
    {
        private const string TROPUSR_FILE_NAME = "TROPUSR.DAT";
        private bool isRpcs3Format;

        string path;
        Header header;

        // Block-type table: maps a block type id to its on-disk location/size.
        Dictionary<int, TypeRecord> typeRecordTable;

        public List<TrophyType> trophyTypeTable = new List<TrophyType>();
        public List<TrophyTimeInfo> trophyTimeInfoTable = new List<TrophyTimeInfo>();
        public TrophyListInfo trophyListInfo;
        public string account_id;
        public string trophy_id;
        public int all_trophy_number;
        byte[] unknowHash;
        uint[] AchievementRate = new uint[4];
        UnknowType7 unknowType7;

        /// <summary>
        /// Latest earned-time among trophies already synced to PSN, or 2008-01-01
        /// (a floor predating PS3 launch) if none are synced. Used as the earliest
        /// date a newly edited trophy may legally take.
        /// </summary>
        public DateTime LastSyncTime
        {
            get
            {
                DateTime aux = new DateTime(2008, 1, 1);
                foreach (TrophyTimeInfo tropInfo in trophyTimeInfoTable)
                {
                    if (tropInfo.IsSync && DateTime.Compare(tropInfo.Time, aux) > 0)
                    {
                        aux = tropInfo.Time;
                    }
                }
                return aux;
            }
        }

        /// <summary>
        /// Latest earned-time across all trophies (synced or not), or 2008-01-01
        /// if none have been earned.
        /// </summary>
        public DateTime LastTrophyTime
        {
            get
            {
                DateTime aux = new DateTime(2008, 1, 1);
                foreach (TrophyTimeInfo tropInfo in trophyTimeInfoTable)
                {
                    if (DateTime.Compare(tropInfo.Time, aux) > 0)
                    {
                        aux = tropInfo.Time;
                    }
                }
                return aux;
            }
        }

        /// <summary>
        /// Opens <c>TROPUSR.DAT</c> under <paramref name="path"/> and parses every
        /// block into the tables above. Throws if the file is missing or its magic
        /// number doesn't match. For RPCS3 dumps the list-info block is rebuilt
        /// from zeros, since RPCS3 lays the file out differently.
        /// </summary>
        public TROPUSR(string path, bool isRpcs3Format)
        {
            if (path == null || path.Trim() == string.Empty)
                throw new Exception("Path cannot be null!");

            string fileName = Path.Combine(path, TROPUSR_FILE_NAME);

            if (!File.Exists(fileName))
                throw new FileNotFoundException("File not found", fileName);

            this.path = path;
            this.isRpcs3Format = isRpcs3Format;

            using (var fileStream = new FileStream(fileName, FileMode.Open))
            using (var TROPUSRReader = new BigEndianBinaryReader(fileStream))
            {
                header = TROPUSRReader.ReadBytes(Marshal.SizeOf(typeof(Header))).ToStruct<Header>();
                if (header.Magic != 0x0000000100ad548f81)
                    throw new InvalidTrophyFileException(TROPUSR_FILE_NAME);

                typeRecordTable = new Dictionary<int, TypeRecord>();
                for (int i = 0; i < header.UnknowCount; i++)
                {
                    TypeRecord TypeRecordTmp = TROPUSRReader.ReadBytes(Marshal.SizeOf(typeof(TypeRecord))).ToStruct<TypeRecord>();
                    typeRecordTable.Add(TypeRecordTmp.ID, TypeRecordTmp);
                }

                // Walk the file block by block until we run out of bytes. Every
                // block starts with a 16-byte header (type, payload size, sequence
                // number, one unknown int) followed by `blocksize` payload bytes.
                do
                {
                    int type = TROPUSRReader.ReadInt32();
                    int blocksize = TROPUSRReader.ReadInt32();
                    int sequenceNumber = TROPUSRReader.ReadInt32(); // distinguishes multiple blocks of the same type
                    int unknow = TROPUSRReader.ReadInt32();
                    byte[] blockdata = TROPUSRReader.ReadBytes(blocksize);
                    switch (type)
                    {
                        case 1: // unknown
                            break;
                        case 2: // account_id sits 16 bytes into the payload
                            account_id = Encoding.UTF8.GetString(blockdata, 16, 16);
                            break;
                        case 3: // trophy_id, a few unknown shorts, the total count and the achievement bitmap
                            trophy_id = Encoding.UTF8.GetString(blockdata, 0, 16).Trim('\0');
                            short u1 = BitConverter.ToInt16(blockdata, 16).ChangeEndian();
                            short u2 = BitConverter.ToInt16(blockdata, 18).ChangeEndian();
                            short u3 = BitConverter.ToInt16(blockdata, 20).ChangeEndian();
                            short u4 = BitConverter.ToInt16(blockdata, 22).ChangeEndian();
                            all_trophy_number = BitConverter.ToInt32(blockdata, 24).ChangeEndian();
                            int u5 = BitConverter.ToInt32(blockdata, 28).ChangeEndian();
                            AchievementRate[0] = BitConverter.ToUInt32(blockdata, 64);
                            AchievementRate[1] = BitConverter.ToUInt32(blockdata, 68);
                            AchievementRate[2] = BitConverter.ToUInt32(blockdata, 72);
                            AchievementRate[3] = BitConverter.ToUInt32(blockdata, 76);
                            break;
                        case 4: // one trophy's rank/type record (one block per trophy)
                            trophyTypeTable.Add(blockdata.ToStruct<TrophyType>());
                            break;
                        case 5: // list-wide totals (counts, timestamps, achievement rate)
                            trophyListInfo = blockdata.ToStruct<TrophyListInfo>();
                            break;
                        case 6: // one trophy's earned time + sync state (one block per trophy)
                            trophyTimeInfoTable.Add(blockdata.ToStruct<TrophyTimeInfo>());
                            break;
                        case 7: // unknown counts
                            unknowType7 = blockdata.ToStruct<UnknowType7>();
                            break;
                        case 8: // SHA-1 style hash (first 20 bytes)
                            unknowHash = blockdata.SubArray(0, 20);
                            break;
                        case 9: // usually some numbers related to the platinum trophy; purpose unknown
                            break;
                        case 10: // appears to be pure padding
                            break;
                    }

                } while (TROPUSRReader.BaseStream.Position < TROPUSRReader.BaseStream.Length);

                // RPCS3 stores the totals block differently, so rebuild it from a
                // zeroed buffer rather than trusting what we read.
                if (this.isRpcs3Format)
                    trophyListInfo = new byte[208].ToStruct<TrophyListInfo>();
                trophyListInfo.ListLastUpdateTime = DateTime.Now;
            }
        }

        /// <summary>Dumps the parsed contents to the console; for debugging only.</summary>
        public void PrintState()
        {
            Console.WriteLine("Counter: {0}", header.UnknowCount);
            Console.WriteLine("Padding:{0}", header.padding.ToHexString());
            foreach (KeyValuePair<int, TypeRecord> fk in typeRecordTable)
            {
                Console.WriteLine(fk.Value);
            }
            Console.WriteLine("account_id:{0}", account_id);
            Console.WriteLine("List Create Time:{0}", trophyListInfo.ListCreateTime);
            Console.WriteLine("Last Trophy Earned Time:{0}", trophyListInfo.ListLastGetTrophyTime);
            Console.WriteLine("Last Update Time:{0}", trophyListInfo.ListLastUpdateTime);
            Console.WriteLine("Earned Trophy Count:{0}", trophyListInfo.GetTrophyNumber);
            Console.WriteLine("Achievement Rate (raw):{0}", trophyListInfo.AchievementRate[0]);

            for (int i = 0; i < trophyTypeTable.Count; i++)
            {
                Console.WriteLine("SN:{0}, Type:{1}, Got:{2}, Time:{3} ", trophyTypeTable[i].SequenceNumber,
                    trophyTypeTable[i].Type, trophyTimeInfoTable[i].IsGet, trophyTimeInfoTable[i].Time);
            }
        }

        /// <summary>
        /// Writes the in-memory tables back into <c>TROPUSR.DAT</c> in place, seeking
        /// to each block's recorded offset (via <see cref="TypeRecord.Offset"/>) and
        /// overwriting only the fields we manage. RPCS3 dumps skip the account/id and
        /// totals blocks. Note the per-record <c>Position += 16</c> hops over each
        /// block's 16-byte header before its payload.
        /// </summary>
        public void Save()
        {
            using (var fileStream = new FileStream(Path.Combine(path, TROPUSR_FILE_NAME), FileMode.Open))
            using (var TROPUSRWriter = new BigEndianBinaryWriter(fileStream))
            {
                TROPUSRWriter.Write(header.StructToBytes());
                if (!isRpcs3Format)
                {
                    TypeRecord account_id_Record = typeRecordTable[2];
                    TROPUSRWriter.BaseStream.Position = account_id_Record.Offset + 32; // skip to the account-id field
                    TROPUSRWriter.Write(account_id.ToCharArray()); // account id

                    TypeRecord trophy_id_Record = typeRecordTable[3];
                    TROPUSRWriter.BaseStream.Position = trophy_id_Record.Offset + 16;
                    TROPUSRWriter.Write(trophy_id.ToCharArray()); // trophy id
                    TROPUSRWriter.BaseStream.Position = trophy_id_Record.Offset + 80;
                }

                TypeRecord TrophyType_Record = typeRecordTable[4];
                TROPUSRWriter.BaseStream.Position = TrophyType_Record.Offset;
                foreach (TrophyType type in trophyTypeTable)
                {
                    TROPUSRWriter.BaseStream.Position += 16; // step over this block's header
                    TROPUSRWriter.Write(type.StructToBytes());
                }

                if (!isRpcs3Format)
                {
                    TypeRecord trophyListInfo_Record = typeRecordTable[5];
                    TROPUSRWriter.BaseStream.Position = trophyListInfo_Record.Offset + 16;
                    TROPUSRWriter.Write(trophyListInfo.StructToBytes());
                }

                TypeRecord TrophyTimeInfo_Record = typeRecordTable[6];
                TROPUSRWriter.BaseStream.Position = TrophyTimeInfo_Record.Offset;
                foreach (TrophyTimeInfo time in trophyTimeInfoTable)
                {
                    TROPUSRWriter.BaseStream.Position += 16;
                    TROPUSRWriter.Write(time.StructToBytes());
                }

                if (!isRpcs3Format)
                {
                    TypeRecord unknowType7_Record = typeRecordTable[7];
                    TROPUSRWriter.BaseStream.Position = unknowType7_Record.Offset + 16;
                    TROPUSRWriter.Write(unknowType7.StructToBytes());
                }

                TROPUSRWriter.Flush();
                TROPUSRWriter.Close();
            }
        }

        /// <summary>
        /// Marks trophy <paramref name="id"/> as earned at <paramref name="dt"/>:
        /// stamps the time, bumps the earned counts (only if it wasn't already got),
        /// sets the trophy's bit in the achievement bitmap, flags it earned-but-not-
        /// synced, and advances the "last earned" timestamp if this is the newest.
        /// </summary>
        public void UnlockTrophy(int id, DateTime dt)
        {
            TrophyTimeInfo tti = trophyTimeInfoTable[id];
            tti.Time = dt;
            if (!tti.IsGet)
            {
                trophyListInfo.GetTrophyNumber = trophyListInfo.GetTrophyNumber + 1;
                unknowType7.TrophyCount = unknowType7.TrophyCount + 1;
            }
            // Set this trophy's bit in the achievement bitmap (32 trophies per word).
            // The on-disk copy is byte-swapped; the in-memory mirror is not.
            trophyListInfo.AchievementRate[id / 32] |= (uint)(1 << id).ChangeEndian();
            AchievementRate[id / 32] |= (uint)(1 << id);
            tti.IsGet = true;
            tti.SyncState = (int)TropSyncState.NotSync; // earned locally; 0x100100 would mean already synced
            trophyTimeInfoTable[id] = tti;
            if (dt > trophyListInfo.ListLastGetTrophyTime)
            {
                trophyListInfo.ListLastGetTrophyTime = dt;
            }
        }

        /// <summary>
        /// Reverts trophy <paramref name="id"/> to un-earned: clears its time, lowers
        /// the earned counts, clears its achievement-bitmap bit, and resets sync state.
        /// Throws <see cref="TrophyAlreadySyncException"/> if the trophy is already
        /// synced to PSN (it can no longer be safely changed).
        /// </summary>
        public void LockTrophy(int id)
        {
            TrophyTimeInfo tti = trophyTimeInfoTable[id];
            if (tti.IsSync)
                throw new TrophyAlreadySyncException();

            tti.Time = new DateTime(0);
            if (tti.IsGet)
            {
                trophyListInfo.GetTrophyNumber = trophyListInfo.GetTrophyNumber - 1;
                unknowType7.TrophyCount = unknowType7.TrophyCount - 1;
            }
            // Clear this trophy's achievement-bitmap bit (XOR mask), on both the
            // byte-swapped on-disk copy and the plain in-memory mirror.
            trophyListInfo.AchievementRate[id / 32] &= 0xFFFFFFFF ^ (uint)(1 << id).ChangeEndian();
            AchievementRate[id / 32] &= 0xFFFFFFFF ^ (uint)(1 << id);
            tti.IsGet = false;
            tti.SyncState = 0;
            trophyTimeInfoTable[id] = tti;
        }

        // Structs below mirror the on-disk byte layout exactly: [StructLayout(Sequential)]
        // plus fixed-size MarshalAs byte arrays let a raw block be cast straight into a
        // struct. DO NOT reorder fields, change their types, or alter array sizes — that
        // would shift every offset and corrupt parsing. The PS3 stores multi-byte numbers
        // big-endian, so the public accessors run the raw `_field` through ChangeEndian().
        // The leading "/// <type>" notes record each field's raw on-disk width.
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
        /// Directory entry for one block type: its id, payload size, how many times
        /// it occurs, and the byte offset where its block(s) begin in the file.
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
        /// Per-trophy rank record (block type 4): the trophy's sequence number and
        /// its rank code (see <see cref="TropType"/>), plus unknown/padding bytes.
        /// </summary>
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct TrophyType
        {

            /// int
            private int _sequenceNumber;
            public int SequenceNumber
            {
                get
                {
                    return _sequenceNumber.ChangeEndian();
                }
            }

            /// int
            private int _type;
            public int Type
            {
                get
                {
                    return _type.ChangeEndian();
                }
                set
                {
                    _type = value.ChangeEndian();
                }
            }

            /// byte[16]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 16, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
            public byte[] unknow;

            /// byte[56]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 56, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
            public byte[] padding;

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("[").Append("SequenceNumber:").Append(SequenceNumber).Append(", ");
                sb.Append("Type:").Append(Type).Append("]");
                return sb.ToString();
            }
        }

        /// <summary>
        /// List-wide totals block (type 5): creation/update/last-earned timestamps,
        /// the earned-trophy count, and the 128-bit achievement bitmap.
        ///
        /// Timestamp fields all follow the same convention (see <see cref="ListCreateTime"/>):
        /// stored big-endian as .NET-ticks/10 (i.e. microseconds), in local time, with
        /// a value of 0 meaning "never". Each 16-byte time slot holds the 8-byte value
        /// twice.
        /// </summary>
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct TrophyListInfo
        {

            /// byte[16]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 16, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
            public byte[] padding;

            /// byte[16]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 16, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
            private byte[] _listCreateTime;

            /// <summary>
            /// When the trophy list was created. Decoding: read the first 8 bytes as a
            /// big-endian long, multiply by 10 to turn microseconds into .NET ticks, and
            /// add the local UTC offset (the PS3 stores wall-clock local time). Ticks == 0
            /// means unset. Encoding reverses this and writes the value into both 8-byte
            /// halves of the 16-byte slot.
            /// </summary>
            public DateTime ListCreateTime
            {
                get
                {
                    DateTime realDateTime = new DateTime(BitConverter.ToInt64(_listCreateTime, 0).ChangeEndian() * 10);
                    if (realDateTime.Ticks == 0)
                    {
                        return realDateTime;
                    }
                    else
                    {
                        return realDateTime.AddHours(TimeZoneInfo.Local.BaseUtcOffset.Hours);
                    }
                }
                set
                {
                    if (value.Ticks == 0)
                    {
                        Array.Clear(_listCreateTime, 0, 16);
                    }
                    else
                    {
                        long temp = value.AddHours(-TimeZoneInfo.Local.BaseUtcOffset.Hours).Ticks;
                        Array.Copy(BitConverter.GetBytes((temp / 10).ChangeEndian()), 0, _listCreateTime, 0, 8);
                        Array.Copy(BitConverter.GetBytes((temp / 10).ChangeEndian()), 0, _listCreateTime, 8, 8);
                    }
                }
            }

            /// byte[16]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 16, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
            private byte[] _listLastUpdateTime;
            public DateTime ListLastUpdateTime
            {
                get
                {
                    DateTime realDateTime = new DateTime(BitConverter.ToInt64(_listLastUpdateTime, 0).ChangeEndian() * 10);
                    if (realDateTime.Ticks == 0)
                    {
                        return realDateTime;
                    }
                    else
                    {
                        return realDateTime.AddHours(TimeZoneInfo.Local.BaseUtcOffset.Hours);
                    }
                }
                set
                {
                    if (value.Ticks == 0)
                    {
                        Array.Clear(_listLastUpdateTime, 0, 16);
                    }
                    else
                    {
                        long temp = value.AddHours(-TimeZoneInfo.Local.BaseUtcOffset.Hours).Ticks;
                        Array.Copy(BitConverter.GetBytes((temp / 10).ChangeEndian()), 0, _listLastUpdateTime, 0, 8);
                        Array.Copy(BitConverter.GetBytes((temp / 10).ChangeEndian()), 0, _listLastUpdateTime, 8, 8);
                    }
                }
            }

            /// byte[16]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 16, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
            public byte[] padding2;

            /// byte[16]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 16, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
            private byte[] _listLastGetTrophyTime;
            public DateTime ListLastGetTrophyTime
            {
                get
                {
                    DateTime realDateTime = new DateTime(BitConverter.ToInt64(_listLastGetTrophyTime, 0).ChangeEndian() * 10);
                    if (realDateTime.Ticks == 0)
                    {
                        return realDateTime;
                    }
                    else
                    {
                        return realDateTime.AddHours(TimeZoneInfo.Local.BaseUtcOffset.Hours);
                    }
                }
                set
                {
                    if (value.Ticks == 0)
                    {
                        Array.Clear(_listLastGetTrophyTime, 0, 16);
                    }
                    else
                    {
                        long temp = value.AddHours(-TimeZoneInfo.Local.BaseUtcOffset.Hours).Ticks;
                        Array.Copy(BitConverter.GetBytes((temp / 10).ChangeEndian()), 0, _listLastGetTrophyTime, 0, 8);
                        Array.Copy(BitConverter.GetBytes((temp / 10).ChangeEndian()), 0, _listLastGetTrophyTime, 8, 8);
                    }
                }
            }

            /// byte[32]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 32, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
            public byte[] padding3;

            /// int
            private int _getTrophyNumber;
            public int GetTrophyNumber
            {
                get
                {
                    return _getTrophyNumber.ChangeEndian();
                }
                set
                {
                    _getTrophyNumber = value.ChangeEndian();
                }
            }

            /// byte[12]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 12, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
            public byte[] padding4;

            /// uint[4]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I4)]
            public uint[] AchievementRate;

            /// byte[16]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 16, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
            public byte[] padding5;

            /// byte[16]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 16, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
            public byte[] hash;

            /// byte[32]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 32, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
            public byte[] padding6;
        }

        /// <summary>
        /// Per-trophy earned-state record (block type 6): whether the trophy is earned
        /// (<see cref="IsGet"/>), its sync state, and when it was earned (<see cref="Time"/>,
        /// same encoding as <see cref="TrophyListInfo.ListCreateTime"/>).
        /// </summary>
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct TrophyTimeInfo
        {

            /// int
            private int _sequenceNumber;
            public int SequenceNumber
            {
                get
                {
                    return _sequenceNumber.ChangeEndian();
                }
            }

            /// byte[4]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
            private byte[] _getOrNot;

            /// <summary>True if the trophy is earned. The flag lives in the last byte of the 4-byte field.</summary>
            public bool IsGet
            {
                get
                {
                    return (_getOrNot[3] != 0) ? true : false;
                }
                set
                {
                    _getOrNot[3] = (byte)((value) ? 1 : 0);
                }
            }

            /// int
            public int SyncState;

            /// <summary>True once the <see cref="TropSyncState.Sync"/> bit is set, i.e. the trophy has been synced to PSN.</summary>
            public bool IsSync => (SyncState & (int)TropSyncState.Sync) == (int)TropSyncState.Sync;

            /// int
            public int unknow2;

            /// byte[16]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 16, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
            private byte[] _getTime;
            public DateTime Time
            {
                get
                {
                    DateTime realDateTime = new DateTime(BitConverter.ToInt64(_getTime, 0).ChangeEndian() * 10);
                    if (realDateTime.Ticks == 0)
                    {
                        return realDateTime;
                    }
                    else
                    {
                        return realDateTime.AddHours(TimeZoneInfo.Local.BaseUtcOffset.Hours);
                    }
                }
                set
                {
                    if (value.Ticks == 0)
                    {
                        Array.Clear(_getTime, 0, 16);
                    }
                    else
                    {
                        long temp = value.AddHours(-TimeZoneInfo.Local.BaseUtcOffset.Hours).Ticks;
                        Array.Copy(BitConverter.GetBytes((temp / 10).ChangeEndian()), 0, _getTime, 0, 8);
                        Array.Copy(BitConverter.GetBytes((temp / 10).ChangeEndian()), 0, _getTime, 8, 8);
                    }
                }
            }

            /// byte[64]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 64, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
            public byte[] padding;

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("[").Append("SequenceNumber:").Append(SequenceNumber).Append(", ");
                sb.Append("GetOrNot:").Append(IsGet).Append(", ");
                sb.Append("GetTime:").Append(Time).Append("]");

                return sb.ToString();
            }
        }

        /// <summary>
        /// Block type 7 (partly reverse-engineered): holds the earned-trophy count
        /// (kept in step by <see cref="UnlockTrophy"/>/<see cref="LockTrophy"/>), a
        /// synced count, a last-sync time, and several still-unknown fields.
        /// </summary>
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct UnknowType7
        {

            /// int
            private int _getTrophyCount;
            public int TrophyCount
            {
                get
                {
                    return _getTrophyCount.ChangeEndian();
                }
                set
                {
                    _getTrophyCount = value.ChangeEndian();
                }
            }

            /// int
            private int _syncTrophyCount;

            /// int
            public int u3;

            /// int
            public int u4;

            /// byte[8]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
            private byte[] _lastSyncTime;
            public DateTime ListSyncTime
            {
                get
                {
                    DateTime realDateTime = new DateTime(BitConverter.ToInt64(_lastSyncTime, 0).ChangeEndian() * 10);
                    if (realDateTime.Ticks == 0)
                    {
                        return realDateTime;
                    }
                    else
                    {
                        return realDateTime.AddHours(TimeZoneInfo.Local.BaseUtcOffset.Hours);
                    }
                }
                set
                {
                    if (value.Ticks == 0)
                    {
                        Array.Clear(_lastSyncTime, 0, 8);
                    }
                    else
                    {
                        long temp = value.AddHours(-TimeZoneInfo.Local.BaseUtcOffset.Hours).Ticks;
                        Array.Copy(BitConverter.GetBytes((temp / 10).ChangeEndian()), 0, _lastSyncTime, 0, 8);
                    }
                }
            }


            /// byte[8]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
            public byte[] padding;

            /// byte[48]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 48, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
            public byte[] padding2;
        }

        #endregion

    }
}
