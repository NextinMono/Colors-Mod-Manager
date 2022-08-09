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
            //Set defaults if they're missing
            data["Main"]["Title"] ??= Path.GetFileNameWithoutExtension(folderPath);
            data["Main"]["Author"] ??= "Unknown";
            data["Main"]["Version"] ??= "0.0";
            data["Main"]["Description"] ??= "";
            data["Main"]["Date"] ??= "";
            data["Main"]["AuthorURL"] ??= "";
            data["Main"]["IsDolphinMod"] ??= false.ToString();


            mod.Title = data["Main"]["Title"];
            mod.Author = data["Main"]["Author"];
            mod.Version = data["Main"]["Version"];
            mod.Description = data["Main"]["Description"];
            mod.Date = data["Main"]["Date"];
            mod.AuthorURL = data["Main"]["AuthorURL"];
            mod.DolphinMod = bool.Parse(data["Main"]["IsDolphinMod"]);
            parser.WriteFile(Path.Combine(folderPath, "mod.ini"), data);

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
        public bool DolphinMod { get; set; }
    }
}
