using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
//using System.Text.RegularExpressions;
//using WinForms = System.Windows.Forms;
using Newtonsoft.Json;
namespace ColorsModManager
{
    public class Program
    {
        #region Launch Console
        private void Program_Load(object sender, EventArgs e)
        {
            AllocConsole();
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
        #endregion
        public static string? ProgramPath
        {
            get
            {
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string exeDir = System.IO.Path.GetDirectoryName(exePath);
                return exeDir;
            }
        }
        public static string? ModsPath
        {
            get
            {
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string exeDir = System.IO.Path.GetDirectoryName(exePath);
                return Path.Combine(exeDir, "mods");
            }
        }
        public static string? CPKPath, ExtractedCPKPath;
        public static string[] ModsAvailable;
        public static List<string> ModsInstalled = new List<string>();
        public static string ColorsExtractedLoc;
        public static int ModChosen;
        public static CPKSettings CPKSettingsFile;

        [STAThread]
        static void Main(string[] args)
        {
            CPKSettingsFile = LoadCPKConfig();
            ConfigCheck(false);
            ModsInstalled = CPKSettingsFile.ModsActiveNames;
            MainMenu();
        }
        /// <summary>
        /// Checks for configuration information, if it can't find it, it prompts the user to set the config.
        /// </summary>
        /// <param name="showMain">Show MainMenu after operation is complete.</param>
        static void ConfigCheck(bool showMain = false)
        {
            //Check if configuration has been completed before
            if (string.IsNullOrEmpty(CPKSettingsFile.ColorsPath) || !File.Exists(Path.Combine(ProgramPath, "cpkfiles.json")))
            {
                ConsoleC.WriteLineColors("Configuring Colours Mod Manager", ConsoleColor.White, ConsoleColor.Red);
                //Configure CPKPath
                ConsoleC.WriteLineColors("Welcome to the Colours Mod Manager! To use the mod manager, please extract your copy of Sonic Colours using wit(wit.wiimm.de), then browse to the extracted folder, and press Select Folder. (the root folder, not files or sys or disc)", ConsoleColor.White, ConsoleColor.Black);
                FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
                folderBrowserDialog.UseDescriptionForTitle = true;
                folderBrowserDialog.Description = "Navigate to your extracted Sonic Colours installation, and press Select Folder in the root folder.";
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
                {
                    ColorsExtractedLoc = folderBrowserDialog.SelectedPath;

                    CPKSettingsFile = new CPKSettings(CPKPath, ColorsExtractedLoc, new string[0], ModsInstalled, null);
                }

                //Configure Dolphin path
                ConsoleC.WriteLineColors("Now, navigate to Dolphin's Load directory and press Select Folder.", ConsoleColor.White, ConsoleColor.Black);
                folderBrowserDialog.Description = "Navigate to Dolphin's Load\\Textures directory and press Select Folder (usually this is in Documents)";
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
                {
                    CPKSettingsFile = new CPKSettings(CPKPath, ColorsExtractedLoc, new string[0], ModsInstalled, folderBrowserDialog.SelectedPath);
                    SaveCPKFilesToJson(CPKSettingsFile);
                }
            }
            else
            {
                ColorsExtractedLoc = CPKSettingsFile.ColorsPath;
            }
            if (showMain)
                MainMenu();
        }
        static void MainMenu()
        {
            Console.Clear();
            bool doneInstalling = true;
            bool install = true;
            while (doneInstalling)
            {
                ConsoleC.WriteLineColors("Sonic Colours Mod Manager\n", ConsoleColor.White, ConsoleColor.Black);
                //Check if mod folder has a mod.ini file, if not, delete the option (yes its a bit sloppy but eh)
                GetModsList();
                Console.Write("\n");
                ConsoleC.WriteColors("Type the number of the mod that you want to add, and enter I, U or C to Install, Uninstall and Configure: ", ConsoleColor.DarkCyan, ConsoleColor.Black);
                var consoleval = Console.ReadLine();
                try
                {
                    ModChosen = int.Parse(consoleval) - 1;
                    string modname = Path.GetFileNameWithoutExtension(ModsAvailable[ModChosen]);
                    if (!ModsInstalled.Contains(modname))
                        ModsInstalled.Add(modname);
                    else
                        ModsInstalled.Remove(modname);
                    Console.Clear();
                    CPKSettingsFile.ModsActiveNames = ModsInstalled;
                    SaveCPKFilesToJson(CPKSettingsFile);
                }
                catch
                {
                    switch (consoleval)
                    {
                        case "I":
                            {
                                doneInstalling = false;
                                Install();
                                break;
                            }
                        case "U":
                            {
                                doneInstalling = false;
                                Uninstall();
                                break;
                            }
                        case "C":
                            {
                                doneInstalling = false;
                                File.Delete(Path.Combine(ProgramPath, "cpkfiles.json"));
                                ConfigCheck(true);
                                break;
                            }
                        default:
                            {
                                Console.Clear();
                                break;
                            }
                    }
                }
            }
        }
        /// <summary>
        /// Saves string array by formatting it without the actual path, only the relative one.
        /// </summary>
        /// <param name="files">Files array</param>
        static void SaveFileList(string[] files)
        {
            List<string> filesList = new List<string>();
            for (int i = 0; i < files.Length; i++)
            {
                filesList.Add(files[i].Replace(Path.Combine(ProgramPath, "sonic2010_0") + "\\", ""));
            }
            CPKSettingsFile.Files = filesList.ToArray();

        }
        /// <summary>
        /// Installs all mods selected
        /// </summary>
        private static void Install()
        {
            //TODO: implement actually checking if mods change and remove those that are unchecked
            bool haveToExtract = false, alreadyExtracted = false;
            string extractedCPK = $"{ProgramPath}\\sonic2010_0";

            //Go through all mods and install their content
            for (int a = 0; a < ModsInstalled.Count; a++)
            {

                var modConfig = ModParser.GetModInfo(Path.Combine(ProgramPath, "mods", ModsInstalled[a]));
                bool dolphin = modConfig.DolphinMod;
                bool hasLoad = false;
                string modPathCPK = Path.Combine(ProgramPath, "mods", ModsInstalled[a], "sonic2010_0");
                string modPathLoad = Path.Combine(ProgramPath, "mods", ModsInstalled[a], "Load");

                if (!dolphin)
                {
                    haveToExtract = true;
                    if (Directory.Exists(modPathLoad))
                        hasLoad = true;
                }
                else
                {
                    hasLoad = true;
                    //Disable CPK loading.
                    modPathCPK = "";
                }

                

                if (!Directory.Exists(modPathCPK) && !Directory.Exists(modPathLoad))
                {
                    ConsoleC.WriteLineColors($"Mod {ModsInstalled[a]} doesn't have any files to replace, skipping...", ConsoleColor.Black, ConsoleColor.White);
                    if (a == ModsInstalled.Count - 1)
                        return;
                    else
                        continue;
                }
                ConsoleC.WriteLineColors($"Installing {ModsInstalled[a]}...", ConsoleColor.DarkGray, ConsoleColor.Black);
                //Only extract CPK if a mod requires it, otherwise don't
                if (haveToExtract && !alreadyExtracted)
                {
                    alreadyExtracted = true;
                    ExtractCPK();
                    if (CPKSettingsFile.Files.Length == 0)
                        SaveFileList(Directory.GetFiles(path: Path.Combine(ProgramPath, "sonic2010_0"), searchPattern: "*.*", searchOption: SearchOption.AllDirectories));
                }
                string[] fileArrayLoad = hasLoad ? Directory.GetFiles(path: modPathLoad, searchPattern: "*.*", searchOption: SearchOption.AllDirectories) : new string[0];
                string[] fileArrayCPK = !dolphin ? Directory.GetFiles(path: modPathCPK, searchPattern: "*.*", searchOption: SearchOption.AllDirectories) : new string[0];
               
                //Go through all of the mods' files  and copy the original to cache if they werent cached and move the new files in
                //(CPK)
                for (int b = 0; b < fileArrayCPK.Length; b++)
                {                    
                    string actualPath = fileArrayCPK[b].Replace($"{Path.Combine(ProgramPath, "mods", ModsInstalled[a])}\\sonic2010_0\\", "");
                    actualPath = actualPath.Replace(@"\\sonic2010_0\\", "");
                    //If it is actually real, copy the old one to a cache and move the new one in its place
                    if (CPKSettingsFile.Files.Contains(actualPath))
                    {
                        string pathOg = Path.Combine(extractedCPK, actualPath);
                        string newPath = Path.Combine(extractedCPK, "cache-do-not-delete", actualPath);
                        string cachePath = Path.Combine(extractedCPK, "cache-do-not-delete", actualPath.Split("\\").First());

                        #region Copy original file to cache
                        //Make checks to see if the directory and file already exist. If directory doesnt exist, create, if file already exists, delete.
                        if (!Directory.Exists(cachePath))
                            Directory.CreateDirectory(cachePath);

                        if (!File.Exists(newPath))
                        {                           
                            File.Move(@pathOg, @newPath);
                        }
                        else
                            ConsoleC.WriteLineColors($"File {actualPath} already cached", ConsoleColor.DarkGray, ConsoleColor.Black);

                        #endregion
                        #region Copy new file in original place
                        if (File.Exists(pathOg))
                            File.Delete(pathOg);
                        File.Copy(fileArrayCPK[b], pathOg);
                        #endregion


                    }
                }
                //(Load)
                for (int c = 0; c < fileArrayLoad.Length; c++)
                {
                    string folderName = "SNC";
                    string relativeP = fileArrayLoad[c].Replace($"{Path.Combine(ProgramPath, "mods", ModsInstalled[a])}\\Load\\", "");
                    string newPath = Path.Combine(CPKSettingsFile.DolphinLoadPath, folderName, ModsInstalled[a], relativeP);
                    string newPathParent = Directory.GetParent(newPath).FullName;

                    //If dolphin path for the textures doesnt exist ("Load\Textures\SNC\(modname)") then make it
                    if (!Directory.Exists(Path.Combine(CPKSettingsFile.DolphinLoadPath, folderName, ModsInstalled[a])))
                        Directory.CreateDirectory(Path.Combine(CPKSettingsFile.DolphinLoadPath, folderName, ModsInstalled[a]));

                    //If parent path of texture doesn't exist, make it. Ideally File.Copy would just create it but it doesn't
                    if (!Directory.Exists(newPathParent))
                        Directory.CreateDirectory(newPathParent);

                   
                    File.Copy(fileArrayLoad[c], newPath, true);
                }
            }

            //Check if CPK configuration actually has the file in the file list (a.k.a check if its stock)
            if (alreadyExtracted)
                RepackCPK();
            Console.WriteLine("Check if it worked!");
        }
        /// <summary>
        /// Uninstalls mods (for now, all of them)
        /// </summary>
        private static void Uninstall()
        {
            bool alreadyExtracted = false;

            if (CPKSettingsFile.ModsActiveNames.Count == 0)
            {
                ConsoleC.WriteLineColors("No mods have been installed!", ConsoleColor.White, ConsoleColor.DarkGreen);
                Console.WriteLine("Press any key to go back to the main menu.");
                Console.ReadKey();
                Console.Clear();
                MainMenu();
                return;
            }

            string cache = Path.Combine($"{ProgramPath}\\sonic2010_0", "cache-do-not-delete");
            string load = Path.Combine(CPKSettingsFile.DolphinLoadPath, "SNC");
            string[] cacheArray = Directory.GetFiles(path: cache, searchPattern: "*.*", searchOption: SearchOption.AllDirectories);
            string[] loadArray = Directory.GetDirectories(path: load, searchPattern: "*.*", searchOption: SearchOption.TopDirectoryOnly);

            string[] fileArrayCPK = Directory.GetFiles(path: Path.Combine(ProgramPath, "sonic2010_0"), searchPattern: "*.*", searchOption: SearchOption.AllDirectories);
            if (CPKSettingsFile.Files.Length == 0)
            { SaveFileList(fileArrayCPK); }

            if(cacheArray.Length != 0)
            {
                ExtractCPK();
                alreadyExtracted = true;
            }  
            
            for (int i = 0; i < cacheArray.Length; i++)
            {
                var cacheFilePath = cacheArray[i].Replace($"{cache}\\", "");
                //If it is actually real, copy the old one to a cache and move the new one in its place
                if (CPKSettingsFile.Files.Contains(cacheFilePath))
                {
                    string pathOg = Path.Combine(cache, cacheFilePath);
                    string newPath = Path.Combine(ProgramPath, "sonic2010_0", cacheFilePath);

                    #region Copy original file back
                    if (File.Exists(newPath))
                    { File.Delete(newPath); }

                    ConsoleC.WriteLineColors($"Uninstalling file {cacheArray[i]}...",ConsoleColor.DarkGray, ConsoleColor.Black);

                    File.Move(@pathOg, @newPath);
                    File.Delete(pathOg);
                    #endregion
                }
            }
            for (int i = 0; i < loadArray.Length; i++)
            {
                ConsoleC.WriteLineColors($"Removing texture {loadArray[i]}...", ConsoleColor.DarkGray, ConsoleColor.Black);
                if (loadArray[i].Replace(load, "") != "User")
                {
                    Directory.Delete(loadArray[i], true);
                }

            }
            CPKSettingsFile.ModsActiveNames.Clear();
            SaveCPKFilesToJson(CPKSettingsFile);
            if(alreadyExtracted)
            RepackCPK();

        }
        static void ExtractCPK()
        {
            ConsoleC.WriteLineColors("Extracting sonic2010_0.cpk. Do NOT close the program now, or the game will become unbootable.", ConsoleColor.DarkGray, ConsoleColor.Black);
            var a = @Path.Combine(ColorsExtractedLoc, "files", "sonic2010_0.cpk");
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @Path.Combine(ProgramPath, "PackCpk.exe");
            startInfo.Arguments = $"\"{@Path.Combine(ColorsExtractedLoc, "files", "sonic2010_0.cpk")}\" \"{ProgramPath}\"";
            Process? processExtractTXD = Process.Start(startInfo);
            processExtractTXD.WaitForExit();
            //Set it after, since if it cant be found, it'll be a bit of a pain
            CPKPath = @Path.Combine(ColorsExtractedLoc, "files");
            ExtractedCPKPath = @Path.Combine(ProgramPath, "sonic2010_0");
            if (!File.Exists(Path.Combine(ProgramPath, "cpkfiles.json")))
            {
                SaveCPKFilesToJson(CPKSettingsFile);
            }
        }
        static void RepackCPK()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @Path.Combine(ProgramPath, "PackCpk.exe");
            startInfo.Arguments = $"\"{ExtractedCPKPath}\" \"{Path.Combine(CPKPath, "sonic2010_0")}\"";
            Process? processExtractTXD = Process.Start(startInfo);
            processExtractTXD.WaitForExit();
        }
        private static CPKSettings LoadCPKConfig()
        {
            try
            {
                using (FileStream fileStream = File.OpenRead(Path.Combine(ProgramPath, "cpkfiles.json")))
                {
                    byte[] buffer = new byte[new FileInfo(Path.Combine(ProgramPath, "cpkfiles.json")).Length];
                    fileStream.Read(buffer, 0, buffer.Length);

                    string json = Encoding.Unicode.GetString(buffer);
                    return JsonConvert.DeserializeObject<CPKSettings>(json);
                }
            }
            catch
            {
                return new CPKSettings("", "", new string[0], new List<string>(), "");
            }

        }
        private static void SaveCPKFilesToJson(CPKSettings saveobj)
        {
            using (FileStream fileStream = File.Create(Path.Combine(ProgramPath, "cpkfiles.json")))
            {
                string jsonSerialized = JsonConvert.SerializeObject(saveobj, Formatting.Indented);

                byte[] buffer = Encoding.Unicode.GetBytes(jsonSerialized);
                fileStream.Write(buffer, 0, buffer.Length);
            }

        }
        /// <summary>
        /// Displays a mods list and returns an array with all the names of them.
        /// </summary>
        /// <returns></returns>
        public static string[] GetModsList()
        {
            if (!Directory.Exists(ModsPath))
            {
                Directory.CreateDirectory(ModsPath);
            }
            if (ModsInstalled == null) ModsInstalled = new List<string>();
            string[]? list = Directory.GetDirectories(ModsPath);
            Separator();
            Console.WriteLine("\nList:");
            if (list.Length == 0)
            {
                ConsoleC.WriteLineColors("No mods found!", ConsoleColor.Black, ConsoleColor.White);
            }
            for (int i = 0; i < list.Length; i++)
            {
                bool modInstalledBefore = ModsInstalled.Contains(Path.GetFileNameWithoutExtension(list[i]));
                string checkbox = modInstalledBefore ? "[✓]" : "[ ]";
                if (File.Exists(Path.Combine(list[i], "mod.ini")))
                {
                    ModInfo? m = ModParser.GetModInfo(list[i]);
                    ConsoleC.WriteColors(checkbox, ConsoleColor.White, ConsoleColor.Black);
                    ConsoleC.WriteColors($"{i + 1}.", ConsoleColor.White, ConsoleColor.Black);
                    ConsoleC.WriteColors(m.Title, ConsoleColor.Gray, ConsoleColor.Black);
                    ConsoleC.WriteLineColors($" by {m.Author} ", ConsoleColor.White, ConsoleColor.Black);
                }
                else
                {
                    list.ToList().RemoveAt(i);
                }
            }
            Console.WriteLine("\n\n");
            Separator();
            ModsAvailable = list.ToArray();
            return ModsAvailable;
        }
        /// <summary>
        /// Decorative separator
        /// </summary>
        static void Separator()
        {
            string separator = new String(' ', Console.WindowWidth);
            ConsoleC.WriteLineColors(separator, ConsoleColor.Black, ConsoleColor.Blue);
        }
    }

}


public class CPKSettings
{
    public string ColorsPath { get; set; }
    public string CPKPath { get; set; }
    public string DolphinLoadPath { get; set; }
    public List<string> ModsActiveNames { get; set; } = new List<string>();
    public string[] Files { get; set; }

    public CPKSettings(string path, string colorsPath, string[] files, List<string> modsactive, string dolphinLoadPath)
    {
        CPKPath = path;
        ColorsPath = colorsPath;
        Files = files;
        ModsActiveNames = modsactive;
        DolphinLoadPath = dolphinLoadPath;
    }
}