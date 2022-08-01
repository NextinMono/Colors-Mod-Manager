using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Newtonsoft.Json;
public class Program
{
    private void Program_Load(object sender, EventArgs e)
    {
        AllocConsole();
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool AllocConsole();
    static string? ProgramPath
    {
        get
        {
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string exeDir = System.IO.Path.GetDirectoryName(exePath);
            return exeDir;
        }
    }
    static string? ModsPath
    {
        get
        {
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string exeDir = System.IO.Path.GetDirectoryName(exePath);
            return Path.Combine(exeDir, "mods");
        }
    }
    static string? CPKPath;   
    static string[] ModsAvailable;
    static string[] ModsInstalled;
    static string ColorsExtractedLoc;
    static int ModChosen;
    [STAThread]
    static void Main(string[] args)
    {
        if(!Directory.Exists(ModsPath))
            Directory.CreateDirectory(ModsPath);
        ModsAvailable = Directory.GetDirectories(ModsPath);
        Console.WriteLine("Colors Modding Test");
        //Check if mod folder has a mod.ini file, if not, delete the option (yes its a bit sloppy but eh)
        for (int i = 0; i < ModsAvailable.Length; i++)
        {
            if (File.Exists(Path.Combine(ModsAvailable[i], "mod.ini")))
            {
                Console.WriteLine($"{i}. {Path.GetFileNameWithoutExtension(ModsAvailable[i])}");
            }
            else
            {
                ModsAvailable.ToList().RemoveAt(i);
            }
        }
        Console.WriteLine();
        Console.Write("Type the number of the mod that you want to add:");
        ModChosen = int.Parse(Console.ReadLine());

        var e = LoadCPKConfig();
        if(string.IsNullOrEmpty(e.ColorsPath))
        {
            Console.WriteLine("Paste the path of your extracted Colors installation (the root folder, not files or sys)");
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
            {
                ColorsExtractedLoc = folderBrowserDialog.SelectedPath;
            }
        }
        else
        {
            ColorsExtractedLoc = e.ColorsPath;
        }
       
        
        ApplyModTest(ModsAvailable[ModChosen]);
    }

    private static void ApplyModTest(string path)
    {
        ExtractCPK();
        string extractedCPK = $"{ProgramPath}\\sonic2010_0";
        string[] fileArray = Directory.GetFiles(path: Path.Combine(path, "sonic2010_0"), searchPattern: "*.*", searchOption: SearchOption.AllDirectories);
        var o = LoadCPKConfig();
        //Check if CPK configuration actually has the file in the file list (a.k.a check if its stock)
        for (int i = 0; i < fileArray.Length; i++)
        {
            var actualPath = fileArray[i].Replace($"{path}\\sonic2010_0\\", "");
            actualPath = actualPath.Replace(@"\\sonic2010_0\\", "");
            //If it is actually real, copy the old one to a cache and move the new one in its place
            if (o.Files.Contains(actualPath))
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
                File.Copy(fileArray[i], pathOg);
                #endregion

            }
        }
        RepackCPK();
        Console.WriteLine("Check if it worked!");


    }

    private static CPKSettings LoadCPKConfig()
    {
        using (FileStream fileStream = File.OpenRead(Path.Combine(ProgramPath, "cpkfiles.json")))
        {
            byte[] buffer = new byte[new FileInfo(Path.Combine(ProgramPath, "cpkfiles.json")).Length];
            fileStream.Read(buffer, 0, buffer.Length);

            string json = Encoding.Unicode.GetString(buffer);
            return JsonConvert.DeserializeObject<CPKSettings>(json);
        }
    }

            

    static void ExtractCPK()
    {
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = @Path.Combine(ProgramPath, "PackCpk.exe");
        startInfo.Arguments = $"\"{@Path.Combine(ColorsExtractedLoc, "files", "sonic2010_0.cpk")}\" \"{ProgramPath}\"";
        Process? processExtractTXD = Process.Start(startInfo);
        processExtractTXD.WaitForExit();
        //Set it after, since if it cant be found, it'll be a bit of a pain
        CPKPath = $"{@Path.Combine(ColorsExtractedLoc, "files")}";
        if (!File.Exists(Path.Combine(ProgramPath, "cpkfiles.json")))
        {
            SaveCPKFilesToJson();
        }
    }
    static void RepackCPK()
    {
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = @Path.Combine(ProgramPath, "PackCpk.exe");
        startInfo.Arguments = $"\"{@Path.Combine(ProgramPath, "sonic2010_0")}\" \"{Path.Combine(ColorsExtractedLoc, "files", "sonic2010_0")}\"";
        Process? processExtractTXD = Process.Start(startInfo);
        processExtractTXD.WaitForExit();
    }

    private static void SaveCPKFilesToJson()
    {
        string[] fileArray = Directory.GetFiles(path:$"{Path.Combine(ProgramPath, "sonic2010_0")}", searchPattern: "*.*", searchOption: SearchOption.AllDirectories);
        for (int i = 0; i < fileArray.Length; i++)
        {
            string e = fileArray[i];
            fileArray[i] = e.Replace($"{ProgramPath}\\sonic2010_0\\", "");
        }
        using (FileStream fileStream = File.Create(Path.Combine(ProgramPath, "cpkfiles.json")))
        {
            string jsonSerialized = JsonConvert.SerializeObject(new CPKSettings(CPKPath, ColorsExtractedLoc, fileArray), Formatting.Indented);

            byte[] buffer = Encoding.Unicode.GetBytes(jsonSerialized);
            fileStream.Write(buffer, 0, buffer.Length);
        }

    }
}

public class CPKSettings
{
    public string ColorsPath { get; set; }
    public string CPKPath { get; set; }
    public string[] ModsActiveNames { get; set; }
    public string[] Files { get; set; }

    public CPKSettings(string path, string colorsPath, string[] files)
    {
        CPKPath = path;
        ColorsPath = colorsPath;
        Files = files;
    }
}