using System;
using System.Diagnostics;
using System.Text;
using System.IO;
using DiscUtils;
using DiscUtils.Iso9660;

namespace MakeBoot
{
    class Program
    {
        private static ConsoleKeyInfo driveNumber;

        static void Main(string[] args)
        {
            Console.BackgroundColor = ConsoleColor.DarkCyan;
            Console.ForegroundColor = ConsoleColor.White;

            startPrompt();
        }

        private static void startPrompt()
        {
            Console.Clear();

            Console.WriteLine("MakeBoot by Wargog\nA console application that puts a Windows install disk on your flash drive");

            Console.Write("\n\nAre you ready to install? (Y/N): ");
            ConsoleKeyInfo confirmationInput = Console.ReadKey();

            switch (confirmationInput.Key)
            {
                case ConsoleKey.Y:
                    selectDriveNumber();
                    break;
                case ConsoleKey.N:
                    Console.WriteLine("Exiting...");
                    Environment.Exit(0);
                    break;
            }
        }

        private static void selectDriveNumber()
        {
            Console.WriteLine("");

            Process diskPartConsole = new Process();

            diskPartConsole.StartInfo.UseShellExecute = false;
            diskPartConsole.StartInfo.RedirectStandardInput = true;
            diskPartConsole.StartInfo.RedirectStandardOutput = true;
            diskPartConsole.StartInfo.FileName = @"C:\Windows\SysWOW64\diskpart.exe";
            diskPartConsole.StartInfo.CreateNoWindow = false;

            diskPartConsole.Start();

            diskPartConsole.StandardInput.WriteLine("list disk");
            diskPartConsole.StandardInput.WriteLine("exit");
            string output = diskPartConsole.StandardOutput.ReadToEnd();

            diskPartConsole.WaitForExit();

            Console.WriteLine(output);
            Console.Write("\n\nEnter the drive number of the drive you wish to use as you see it in DiskPart: ");

            driveNumber = Console.ReadKey();

            Console.Write("\n\nAre you sure you want to use drive {0}? This will completely wipe it! (Y/N): ", driveNumber.KeyChar);
            ConsoleKeyInfo confirmationInput = Console.ReadKey();

            switch (confirmationInput.Key)
            {
                case ConsoleKey.Y:
                    clearDrive();
                    break;
                case ConsoleKey.N:
                    Console.Write("\n\nSelect another drive? (Y/N): ");
                    ConsoleKeyInfo selectAnotherDrive = Console.ReadKey();

                    switch (selectAnotherDrive.Key)
                    {
                        case ConsoleKey.Y:
                            selectDriveNumber();
                            break;
                        case ConsoleKey.N:
                            Console.WriteLine("Exiting...");
                            Environment.Exit(0);
                            break;
                    }
                    break;
            }
        }

        private static void clearDrive()
        {
            Console.WriteLine("");

            Process diskPartConsole = new Process();

            diskPartConsole.StartInfo.UseShellExecute = false;
            diskPartConsole.StartInfo.RedirectStandardInput = true;
            diskPartConsole.StartInfo.RedirectStandardOutput = true;
            diskPartConsole.StartInfo.FileName = @"C:\Windows\System32\diskpart.exe";
            diskPartConsole.StartInfo.CreateNoWindow = false;

            diskPartConsole.Start();

            diskPartConsole.StandardInput.WriteLine("select disk " + driveNumber.KeyChar);
            diskPartConsole.StandardInput.WriteLine("online disk");
            diskPartConsole.StandardInput.WriteLine("attributes disk clear readonly");
            diskPartConsole.StandardInput.WriteLine("clean");
            diskPartConsole.StandardInput.WriteLine("create partition primary");
            diskPartConsole.StandardInput.WriteLine("select partition 1");
            diskPartConsole.StandardInput.WriteLine("active");
            diskPartConsole.StandardInput.WriteLine("format fs=fat32 quick");
            diskPartConsole.StandardInput.WriteLine("assign");
            diskPartConsole.StandardInput.WriteLine("exit");
            string output = diskPartConsole.StandardOutput.ReadToEnd();

            diskPartConsole.WaitForExit();

            Console.WriteLine(output);

            getDriveLetter();
        }

        private static void getDriveLetter()
        {
            Console.Write("\n\nEnter the letter Windows assigned your drive: ");
            ConsoleKeyInfo driveLetter = Console.ReadKey();

            Console.Write("\n\nAre you sure you want to use drive {0}? (Y/N): ", driveLetter.KeyChar);
            ConsoleKeyInfo confirmationInput = Console.ReadKey();

            switch (confirmationInput.Key)
            {
                case ConsoleKey.Y:
                    getISOLocationAndExtract(driveLetter);
                    break;
                case ConsoleKey.N:
                    getDriveLetter();
                    break;
            }
        }

        private static void getISOLocationAndExtract(ConsoleKeyInfo driveLetter)
        {
            Console.Write("\n\nEnter the full path to your Windows ISO file and hit enter: ");
            string isoPath = Console.ReadLine().ToString();

            Console.WriteLine("Extracting ISO to drive, please do not close the window, it will close automatically once the extraction has completed.");

            ExtractISO(isoPath, driveLetter.KeyChar + @":\");
        }

        private static void ExtractISO(string ISOName, string ExtractionPath)
        {
            using (FileStream ISOStream = File.Open(ISOName, FileMode.Open))
            {
                CDReader Reader = new CDReader(ISOStream, true, true);
                ExtractDirectory(Reader.Root, ExtractionPath, "");
                Reader.Dispose();
            }
        }
        private static void ExtractDirectory(DiscDirectoryInfo Dinfo, string RootPath, string PathinISO)
        {
            if (!string.IsNullOrWhiteSpace(PathinISO))
            {
                PathinISO += "\\" + Dinfo.Name;
            }
            RootPath += "\\" + Dinfo.Name;
            AppendDirectory(RootPath);
            foreach (DiscDirectoryInfo dinfo in Dinfo.GetDirectories())
            {
                ExtractDirectory(dinfo, RootPath, PathinISO);
            }
            foreach (DiscFileInfo finfo in Dinfo.GetFiles())
            {
                using (Stream FileStr = finfo.OpenRead())
                {
                    using (FileStream Fs = File.Create(RootPath + "\\" + finfo.Name))
                    {
                        FileStr.CopyTo(Fs, 4 * 1024);
                    }
                }
            }
        }
        private static void AppendDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (DirectoryNotFoundException Ex)
            {
                AppendDirectory(Path.GetDirectoryName(path));
            }
            catch (PathTooLongException Exx)
            {
                AppendDirectory(Path.GetDirectoryName(path));
            }
        }
    }
}
