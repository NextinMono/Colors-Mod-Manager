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
                //Configure Savefile
                Console.WriteLine("Paste the path of your extracted Colors installation (the root folder, not files or sys)");
                FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
                folderBrowserDialog.UseDescriptionForTitle = true;
                folderBrowserDialog.Description = "Enter your extracted Sonic Colors installation, and press Enter in the root folder.";
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
                {
                    ColorsExtractedLoc = folderBrowserDialog.SelectedPath;

                    CPKSettingsFile = new CPKSettings(CPKPath, ColorsExtractedLoc, new string[0], ModsInstalled);
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
                    ModChosen = int.Parse(consoleval);
                    string modname = Path.GetFileNameWithoutExtension(ModsAvailable[ModChosen]);
                    if (!ModsInstalled.Contains(modname))
                        ModsInstalled.Add(modname);
                    else
                        ModsInstalled.Remove(modname);
                    Console.Clear();
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
            ExtractCPK();
            string extractedCPK = $"{ProgramPath}\\sonic2010_0";
            if (CPKSettingsFile.Files.Length == 0)
                SaveFileList(Directory.GetFiles(path: Path.Combine(ProgramPath, "sonic2010_0"), searchPattern: "*.*", searchOption: SearchOption.AllDirectories));

            //Go through all mods and install their content
            for (int a = 0; a < ModsInstalled.Count; a++)
            {
                string modPath = Path.Combine(ProgramPath, "mods", ModsInstalled[a], "sonic2010_0");
                if (!Directory.Exists(modPath))
                {
                    ConsoleC.WriteLineColors($"Mod {ModsInstalled[a]} doesn't have any files to replace, skipping...", ConsoleColor.Black, ConsoleColor.White);
                    if (a == ModsInstalled.Count - 1)
                        return;
                    else
                        continue;

                }
                string[] fileArray = Directory.GetFiles(path: modPath, searchPattern: "*.*", searchOption: SearchOption.AllDirectories);

                for (int b = 0; b < fileArray.Length; b++)
                {
                    var actualPath = fileArray[b].Replace($"{Path.Combine(ProgramPath, "mods", ModsInstalled[a])}\\sonic2010_0\\", "");
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
                            Console.WriteLine($"PathOg: {pathOg}\nPathNew: {newPath}");
                            Console.WriteLine($"Moving file {actualPath}");
                            File.Move(@pathOg, @newPath);
                        }
                        else
                            Console.WriteLine($"File {actualPath} already cached");

                        #endregion
                        #region Copy new file in original place
                        File.Copy(fileArray[b], pathOg);
                        #endregion

                    }
                }

            }

            //Check if CPK configuration actually has the file in the file list (a.k.a check if its stock)

            RepackCPK();
            Console.WriteLine("Check if it worked!");
        }
        /// <summary>
        /// Uninstalls mods (for now, all of them)
        /// </summary>
        private static void Uninstall()
        {
            if(CPKSettingsFile.ModsActiveNames.Count == 0)
            {
                ConsoleC.WriteLineColors("No mods have been installed!", ConsoleColor.White, ConsoleColor.DarkGreen);
                Console.WriteLine("Press any key to go back to the main menu.");
                Console.ReadKey();
                Console.Clear();
                MainMenu();
                return;
            }
            ExtractCPK();
            string cache = Path.Combine($"{ProgramPath}\\sonic2010_0", "cache-do-not-delete");
            string[] cacheArray = Directory.GetFiles(path: cache, searchPattern: "*.*", searchOption: SearchOption.AllDirectories);

            string[] fileArray = Directory.GetFiles(path: Path.Combine(ProgramPath, "sonic2010_0"), searchPattern: "*.*", searchOption: SearchOption.AllDirectories);
            if (CPKSettingsFile.Files.Length == 0)
            { SaveFileList(fileArray); }
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

                    Console.WriteLine($"PathOg: {pathOg}\nPathNew: {newPath}");
                    Console.WriteLine($"Moving file {cacheFilePath}");
                    File.Move(@pathOg, @newPath);
                    File.Delete(pathOg);
                    #endregion
                    //Directory.Move(fileArray[i], files);
                }
            }
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
                return new CPKSettings("", "", new string[0], new List<string>());
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
            if(ModsInstalled == null) ModsInstalled = new List<string>();
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
                    ConsoleC.WriteColors($"{i}.", ConsoleColor.White, ConsoleColor.Black);
                    ConsoleC.WriteColors(Path.GetFileNameWithoutExtension(list[i]), ConsoleColor.Gray, ConsoleColor.Black);
                    ConsoleC.WriteLineColors(checkbox, ConsoleColor.White, ConsoleColor.Black);
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
    public List<string> ModsActiveNames { get; set; } = new List<string>();
    public string[] Files { get; set; }

    public CPKSettings(string path, string colorsPath, string[] files, List<string> modsactive)
    {
        CPKPath = path;
        ColorsPath = colorsPath;
        Files = files;
        ModsActiveNames = modsactive;
    }
}