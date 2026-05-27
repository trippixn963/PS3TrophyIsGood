using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace TROPHYParser
{
    /// <summary>
    /// Reads <c>TROPCONF.SFM</c>, the trophy set's definition file. This is an XML
    /// document describing the game (npcommid, title, parental level) and every
    /// trophy in the set (id, rank, hidden flag, name, detail). On a real PS3 the
    /// XML is preceded by a 64-byte (0x40) binary header; RPCS3 dumps omit it.
    /// This class is read-only — it never writes the file back.
    /// </summary>
    public class TROPCONF
    {
        private const string TROPCONF_FILE_NAME = "TROPCONF.SFM";

        string path;
        string trophyconf_version;
        public string npcommid;
        public string trophyset_version;
        public string parental_level;
        public string title_name;
        public string title_detail;
        private bool _hasplat;
        public List<Trophy> trophys;

        public bool HasPlatinium { get => _hasplat; }
        public int Count
        {
            get
            {
                return trophys.Count;
            }
        }
        public Trophy this[int index]
        {
            get
            {
                return trophys[index];
            }
        }
        public TROPCONF(string path, bool isRpcs3Format)
        {
            if (path == null || path.Trim() == string.Empty)
                throw new Exception("Path cannot be null!");

            string fileName = Path.Combine(path, TROPCONF_FILE_NAME);

            if (!File.Exists(fileName))
                throw new FileNotFoundException("File not found", TROPCONF_FILE_NAME);

            this.path = path;

            // Skip the 64-byte binary header on real PS3 files; RPCS3 dumps start
            // straight at the XML, so there is nothing to skip there.
            byte[] data = File.ReadAllBytes(fileName);
            int startByte = isRpcs3Format ? 0x00 : 0x40;
            data = data.SubArray(startByte, data.Length - startByte);

            // The payload is UTF-8 XML, often null-padded at the end; trim the
            // padding before handing it to the XML parser.
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.LoadXml(Encoding.UTF8.GetString(data).Trim('\0'));

            // Fixed Data
            trophyconf_version = xmldoc.DocumentElement.Attributes["version"].Value;
            npcommid = xmldoc.GetElementsByTagName("npcommid")[0].InnerText;
            trophyset_version = xmldoc.GetElementsByTagName("trophyset-version")[0].InnerText;
            parental_level = xmldoc.GetElementsByTagName("parental-level")[0].InnerText;
            title_name = xmldoc.GetElementsByTagName("title-name")[0].InnerText;
            title_detail = xmldoc.GetElementsByTagName("title-detail")[0].InnerText;

            // Trophys
            XmlNodeList trophysXML = xmldoc.GetElementsByTagName("trophy");
            trophys = new List<Trophy>();
            foreach (XmlNode trophy in trophysXML)
            {
                Trophy item = new Trophy(
                    int.Parse(trophy.Attributes["id"].Value),
                    trophy.Attributes["hidden"].Value,
                    trophy.Attributes["ttype"].Value,
                    int.Parse(trophy.Attributes["pid"].Value),
                    trophy["name"].InnerText,
                    trophy["detail"].InnerText,
                    int.Parse(trophy.Attributes["gid"]?.Value ?? "0")
                    );


                trophys.Add(item);
            }
            // By convention the platinum, when present, is always trophy 0.
            _hasplat = trophys[0].ttype.Equals("P");
        }

        public void PrintState()
        {
            Console.WriteLine(trophyconf_version);
            Console.WriteLine(npcommid);
            Console.WriteLine(trophyset_version);
            Console.WriteLine(parental_level);
            Console.WriteLine(title_name);
            Console.WriteLine(title_detail);

            foreach (Trophy t in trophys)
            {
                Console.WriteLine(t);
            }
        }
        /// <summary>
        /// One trophy definition as declared in TROPCONF.SFM (metadata only — no
        /// earned/locked state, which lives in TROPUSR.DAT / TROPTRNS.DAT).
        /// </summary>
        public struct Trophy
        {
            public int id;

            /// <summary>"yes" if the trophy is hidden until earned, otherwise "no".</summary>
            public string hidden;

            /// <summary>
            /// Single-letter trophy rank: P = Platinum, G = Gold, S = Silver, B = Bronze.
            /// </summary>
            public string ttype;

            public int pid;
            public string name;
            public string detail;
            public int gid;
            public Trophy(int id, string hidden, string ttype, int pid, string name, string detail, int gid)
            {
                this.id = id;
                this.hidden = hidden;
                this.ttype = ttype;
                this.pid = pid;
                this.name = name;
                this.detail = detail;
                this.gid = gid;

            }
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("[").Append(id).Append(",");
                sb.Append(hidden).Append(",");
                sb.Append(ttype).Append(",");
                sb.Append(pid).Append(",");
                sb.Append(name).Append(",");
                sb.Append(detail).Append("]");
                return sb.ToString();
            }
        }
    }
}
