using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Booter
{
    public class MyFile 
    {
        public FileVersionInfo fileversioninfo;
        public string fullpath = "";
        public string filename ="";
        public string comdescription = null;
        public bool IsCOM = false;
    }

    public class MyFileHelper
    {
        [DllImport("shell32.dll")]
        static extern bool SHGetSpecialFolderPath(IntPtr hwndOwner, [Out] StringBuilder lpszPath, int nFolder, bool fCreate);

        public static string GetSpecialPath(int param)
        {
            StringBuilder path = new StringBuilder(260);
            if (SHGetSpecialFolderPath(IntPtr.Zero, path, param, false))
            {
                return path.ToString();
            }
            else
            {
                return null;
            }
        }

        [DllImport("Kernel32.Dll", EntryPoint = "Wow64DisableWow64FsRedirection")]
        public static extern bool Wow64DisableWow64FsRedirection(out bool oldvalue);

        [DllImport("Kernel32.Dll", EntryPoint = "Wow64DisableWow64FsRedirection")]
        public static extern bool Wow64RevertWow64FsRedirection(bool oldvalue);

        [DllImport("Kernel32.Dll", EntryPoint = "Wow64EnableWow64FsRedirection")]
        public static extern bool Wow64EnableWow64FsRedirection(bool oldvalue);

        [DllImport("shell32.dll", EntryPoint = "ExtractIconEx")]
        public static extern int ExtractIconEx(string lpszFile, int niconIndex, ref IntPtr phiconLarge, ref IntPtr phiconSmall, int nIcons);

        public static bool wow64oldstate;

        public static Regex[] regs = 
        { 
            new Regex(@"^(?<fpath>([a-zA-Z]:\\)([\s\.\-\w()_#]+\\)+)(?<fname>[\w\.]+.[\w]+)"),
            new Regex(@"^(?<fpath>([%\s\.\-\w()_#]+\\)+)(?<fname>[\w\.]+.[\w]+)"),
            new Regex(@"^(?<fname>[\w\.]+.[\w]+)"),
            new Regex(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}|[{][0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}[}]")
        };

        public static char[] trimcahrs = { '"', '\'', ' ', ',', ';', '/', '\\', '@' , '?'};

        public static MyFile GetFileInfo(string path, string [] filter = null, string defaultpath = "")
        {
            MyFile myfile = new MyFile();
            myfile.fullpath = path;
            string real_path = path.Trim().Trim(MyFileHelper.trimcahrs);
            int matchcase = 0;
            foreach (Regex reg in MyFileHelper.regs) 
            {
                Match result = reg.Match(real_path);
                if (result.Success)
                {
                    myfile.fullpath = result.Value;
                    myfile.filename = result.Result("${fname}");
                    break;
                }
                matchcase++;
            }
            if (matchcase == 4) 
            {
                return null;
            }
            else if (matchcase == 3) 
            {
                try
                {
                    Guid guid = new Guid(real_path);
                    return MyFileHelper.GetComFileInfo(path, defaultpath);
                }
                catch { }
                return null;
            }
            else if (matchcase == 2)
            {
                IDictionary environment = Environment.GetEnvironmentVariables();
                bool hasEnvironmentStr = false;
                foreach (string environmentKey in environment.Keys)
                {
                    int index = myfile.fullpath.ToUpper().IndexOf("%" + environmentKey.ToUpper() + "%");
                    if (index != -1)
                    {
                        myfile.fullpath = (environment[environmentKey] as string).Trim('\\') + @"\" +
                            myfile.fullpath.Substring(index + environmentKey.Length).Trim('\\');
                        if (myfile.fullpath.StartsWith(@"system32", StringComparison.OrdinalIgnoreCase)
                            || myfile.fullpath.StartsWith(@"syswowo64", StringComparison.OrdinalIgnoreCase))
                        {
                            myfile.fullpath = Environment.GetFolderPath(Environment.SpecialFolder.Windows) +
                                @"\" + myfile.filename;
                        }
                        hasEnvironmentStr = true;
                        break;
                    }
                }
                if (!hasEnvironmentStr)
                {
                    myfile.fullpath = defaultpath.Trim('\\') + @"\" + myfile.fullpath.Trim('\\');
                }
            }
            else if (matchcase == 1) 
            {
                if (myfile.fullpath.StartsWith(@"system32", StringComparison.OrdinalIgnoreCase)
                    || myfile.fullpath.StartsWith(@"syswowo64", StringComparison.OrdinalIgnoreCase))
                {
                    myfile.fullpath = Environment.GetFolderPath(Environment.SpecialFolder.Windows) +
                        @"\" + myfile.fullpath;
                }
                IDictionary environment = Environment.GetEnvironmentVariables();
                foreach (string environmentKey in environment.Keys)
                {
                    int index = myfile.fullpath.ToUpper().IndexOf("%" + environmentKey.ToUpper() + "%");
                    if (index != -1)
                    {
                        myfile.fullpath = (environment[environmentKey] as string).Trim('\\') + @"\" +
                            myfile.fullpath.Substring(index + environmentKey.Length + 2).Trim('\\');
                        if (myfile.fullpath.StartsWith(@"system32", StringComparison.OrdinalIgnoreCase)
                            || myfile.fullpath.StartsWith(@"syswowo64", StringComparison.OrdinalIgnoreCase))
                        {
                            myfile.fullpath = Environment.GetFolderPath(Environment.SpecialFolder.Windows) +
                                @"\" + myfile.filename;
                        }
                        break;
                    }
                    index = myfile.fullpath.IndexOf(environmentKey);
                    if (index != -1)
                    {
                        myfile.fullpath = (environment[environmentKey] as string).Trim('\\') + @"\" +
                            myfile.fullpath.Substring(index + environmentKey.Length).Trim('\\');
                        if (myfile.fullpath.StartsWith(@"system32", StringComparison.OrdinalIgnoreCase)
                            || myfile.fullpath.StartsWith(@"syswowo64", StringComparison.OrdinalIgnoreCase))
                        {
                            myfile.fullpath = Environment.GetFolderPath(Environment.SpecialFolder.Windows) +
                                @"\" + myfile.filename;
                        }
                        break;
                    }
                }
            }
            if (filter != null)
            {
                bool find = false;
                foreach (string extension in filter)
                {
                    if (Path.GetExtension(myfile.filename).ToLower() == "." + extension.ToLower())
                    {
                        find = true;
                        break;
                    }
                }
                if (!find) { return null; }
            }
            if (Environment.Is64BitOperatingSystem && !MyFileHelper.Wow64DisableWow64FsRedirection(out MyFileHelper.wow64oldstate))
            {
                if (File.Exists(myfile.fullpath))
                {
                    myfile.fileversioninfo = FileVersionInfo.GetVersionInfo(myfile.fullpath);
                }
                else
                {
                    myfile.fullpath = "File not found ! " + real_path;
                }
                return myfile;
            }
            if (File.Exists(myfile.fullpath))
            {
                myfile.fileversioninfo = FileVersionInfo.GetVersionInfo(myfile.fullpath);
            }
            else
            {
                myfile.fullpath = "File not found ! " + real_path;
            }
            if (Environment.Is64BitOperatingSystem)
            {
                MyFileHelper.Wow64RevertWow64FsRedirection(MyFileHelper.wow64oldstate);
                MyFileHelper.Wow64EnableWow64FsRedirection(true);
            }
            return myfile;
        }

        /// <summary>
        /// 获取一个COM文件的信息，需要提前检验。
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="defaultpath"></param>
        /// <returns>不会返回null</returns>
        public static MyFile GetComFileInfo(string guid, string defaultpath = "") 
        {
            RegistryKey root = MyReg.GetNonRedirectionKey(Registry.ClassesRoot);
            RegistryKey clsid = root.OpenSubKey(@"CLSID\" + guid.Trim('\\'));
            if (clsid != null) 
            {
                string description = clsid.GetValue("") as string;
                if (description != null)
                {
                    RegistryKey apppath = MyReg.GetNonRedirectionKey(clsid).OpenSubKey(@"InprocServer32");
                    if (apppath != null) 
                    {
                        string path = apppath.GetValue("") as string;
                        if (path != null) 
                        {
                            MyFile result = MyFileHelper.GetFileInfo(path, null, defaultpath);
                            if (result != null) 
                            {
                                result.comdescription = description;
                                result.IsCOM = true;
                                return result;
                            }
                        }
                    }
                }
            }
            if (Environment.Is64BitOperatingSystem) 
            {
                RegistryKey _root = MyReg.GetNonRedirectionKey(Registry.ClassesRoot);
                RegistryKey _clsid = root.OpenSubKey(@"Wow6432Node\CLSID\" + guid.Trim('\\'));
                if (_clsid != null)
                {
                    string description = _clsid.GetValue("") as string;
                    if (description != null)
                    {
                        RegistryKey apppath = MyReg.GetNonRedirectionKey(_clsid).OpenSubKey(@"InprocServer32");
                        if (apppath != null)
                        {
                            string path = apppath.GetValue("") as string;
                            if (path != null)
                            {
                                MyFile result = MyFileHelper.GetFileInfo(path, null, defaultpath);
                                if (result != null)
                                {
                                    result.comdescription = description;
                                    result.IsCOM = true;
                                    return result;
                                }
                            }
                        }
                    }
                }
            }
            RegistryKey typelib = root.OpenSubKey(@"TypeLib\" + guid.Trim('\\'));
            if(typelib != null)
            {
                List<RegNode> names = new List<RegNode>();
                RegNode tr = new RegNode();
                if (Environment.Is64BitOperatingSystem)
                {
                    names = MyReg.FindAllValues(
                        Registry.ClassesRoot,
                        @"TypeLib\" + guid,
                        new string[] { "win64" },
                        ref tr);
                }
                else 
                {
                    names = MyReg.FindAllValues(
                        Registry.ClassesRoot,
                        @"TypeLib\" + guid,
                        new string[] { "win32" },
                        ref tr);
                }
                foreach (RegNode item in names)
                {
                    string origin_str = "";
                    try
                    {
                        origin_str = item.Value as string;
                    }
                    catch { continue; }
                    MyFile myfile = MyFileHelper.GetFileInfo(origin_str);
                    if (myfile != null && item.Parent != null && item.Parent.Parent != null)
                    {
                        foreach (RegNode trnd in item.Parent.Parent.Nodes) 
                        {
                            if (trnd.Name == "") 
                            {
                                try
                                {
                                    myfile.comdescription = trnd.Value as string;
                                }
                                catch { continue; }
                            }
                        }
                        myfile.IsCOM = true;
                        return myfile;
                    }
                }
            }
            MyFile empty = new MyFile();
            empty.fullpath = "File not found ! " + guid;
            empty.IsCOM = true;
            return empty;
        }
    }
}
