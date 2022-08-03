using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IniParser;
using IniParser.Model;

namespace ColorsModManager
{
    public static class ModParser
    {
        public static ModInfo GetModInfo(string folderPath)
        {
            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile(Path.Combine(folderPath, "mod.ini"));
            ModInfo mod = new ModInfo();

            mod.Title = data["Main"]["Title"];
            mod.Author = data["Main"]["Author"];
            mod.Version = data["Main"]["Version"];
            mod.Description = data["Main"]["Description"];
            mod.Date = data["Main"]["Date"];
            mod.AuthorURL = data["Main"]["AuthorURL"];
            if (mod.Title == null)
                mod.Title = Path.GetFileNameWithoutExtension(folderPath);
            if (mod.Author  == null)
                mod.Author = "Unknown";
            if (mod.Version == null)
                mod.Version = "0.0";

            return mod;
        }
    }
    public class ModInfo
    {
        public string Author { get; set; }
        public string Title { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string Date { get; set; }
        public string AuthorURL { get; set; }
    }
}
