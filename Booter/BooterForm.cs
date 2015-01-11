using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Booter
{
    public enum RegFindMode : uint
    {
        ALL = 0x11,
        KEY = 0x01,
        VALUE = 0x10
    }

    public partial class BooterForm : Form
    {
        public ImageList imgLst = new ImageList();
        public int iconcount = 0;

        public delegate string RegNodeChange(RegNode tr);

        public string GetParentName(RegNode tr)
        {
            if (tr != null && tr.Parent != null)
            {
                return tr.Parent.Name.Trim();
            }
            return "";
        }

        public string ObjectToString(object o)
        {
            string result = "";
            try
            {
                result = o as string;
            }
            catch { }
            return result;
        }

        public BooterForm()
        {
            InitializeComponent();
            listView.SmallImageList = imgLst;
            listView.LargeImageList = imgLst;
            this.listView.BeginUpdate();
            GetListView();
            this.listView.EndUpdate();
        }

        public void GetListView()
        {
            #region Logon
            {
                string subkey = @"System\CurrentControlSet\Control\Terminal Server\Wds\rdpwd";
                ListViewGroup group = new ListViewGroup(Registry.LocalMachine.Name + @"\" + subkey);
                listView.Groups.Add(group);
                Dictionary<string, object> names = MyReg.GetValues(
                                                       Registry.LocalMachine,
                                                       subkey);
                if (names.ContainsKey("StartupPrograms"))
                {
                    string rdpclip = "";
                    try
                    {
                        rdpclip = (names["StartupPrograms"] as string) + ".exe";
                        MyFile file = MyFileHelper.GetFileInfo(rdpclip, null, Environment.GetFolderPath(Environment.SpecialFolder.System));
                        if (file != null)
                        {
                            ListViewItem item = new ListViewItem(group);
                            item.Checked = true;
                            item.ToolTipText = group.Header;
                            item.Text = " " + names["StartupPrograms"];
                            FileToItem(file, ref item);
                            this.listView.Items.Add(item);
                        }
                    }
                    catch { }
                }
            }
            this.FindAllFiles(
                Registry.LocalMachine,
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon",
                new string[] { "Userinit" },
                new string[] { "exe" },
                new RegNodeChange((RegNode tr) =>
                {
                    return this.ObjectToString(tr.Value);
                }));
            this.FindAllFiles(
                Registry.LocalMachine,
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon",
                new string[] { "Shell" },
                new string[] { "exe" },
                new RegNodeChange((RegNode tr) =>
                {
                    return this.ObjectToString(tr.Value);
                }),
                RegFindMode.VALUE,
                Environment.GetFolderPath(Environment.SpecialFolder.Windows));
            this.FindFiles(
                Registry.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
            if (Environment.Is64BitOperatingSystem)
            {
                this.FindFiles(
                    Registry.LocalMachine,
                    @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Run");
            }
            this.FindFiles(
                Registry.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Run");
            #endregion
            #region Explore
            this.FindAllFiles(
                Registry.LocalMachine
                , @"SOFTWARE\Classes\Protocols\Filter",
                new string[] { "CLSID" },
                null,
                new RegNodeChange(GetParentName),
                RegFindMode.VALUE,
                Environment.GetFolderPath(Environment.SpecialFolder.System));
            this.FindAllFiles(
                Registry.LocalMachine
                , @"SOFTWARE\Classes\Protocols\Handler",
                new string[] { "CLSID" },
                null,
                new RegNodeChange(GetParentName),
                RegFindMode.VALUE,
                Environment.GetFolderPath(Environment.SpecialFolder.System));
            this.FindAllFiles(
                Registry.LocalMachine,
                @"SOFTWARE\Microsoft\Active Setup\Installed Components",
                new string[] { "StubPath" },
                null,
                new RegNodeChange((RegNode tr) =>
                {
                    if (tr.Parent != null)
                    {
                        foreach (RegNode node in tr.Parent.Nodes)
                        {
                            if (node.Name == "")
                            {
                                return this.ObjectToString(node.Value);
                            }
                        }
                    }
                    return "";
                }),
                RegFindMode.VALUE,
                Environment.GetFolderPath(Environment.SpecialFolder.System));
            if (Environment.Is64BitOperatingSystem)
            {
                this.FindAllFiles(
                    Registry.LocalMachine,
                    @"SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components",
                    new string[] { "StubPath" },
                    null,
                    new RegNodeChange((RegNode tr) =>
                    {
                        foreach (RegNode node in tr.Parent.Nodes)
                        {
                            if (node.Name == "")
                            {
                                return this.ObjectToString(node.Value);
                            }
                        }
                        return "";
                    }),
                    RegFindMode.VALUE,
                    Environment.GetFolderPath(Environment.SpecialFolder.System));
            }
            this.FindFiles(
                Registry.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\ShellServiceObjectDelayLoad");
            if (Environment.Is64BitOperatingSystem)
            {
                this.FindFiles(
                    Registry.LocalMachine,
                    @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\ShellServiceObjectDelayLoad");
            }
            this.FindAllFiles(
                Registry.LocalMachine,
                @"Software\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved",
                null,
                null,
                new RegNodeChange((RegNode tr) =>
                {
                    return this.ObjectToString(tr.Value);
                }),
                RegFindMode.KEY);
            if (Environment.Is64BitOperatingSystem)
            {
                this.FindAllFiles(
                    Registry.LocalMachine,
                    @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved",
                    null,
                    null,
                    new RegNodeChange((RegNode tr) =>
                    {
                        return this.ObjectToString(tr.Value);
                    }),
                    RegFindMode.KEY);
            }
            this.FindFolderComFile(
                Registry.LocalMachine,
                @"Software\Classes\Folder\Shellex\ColumnHandlers");
            if (Environment.Is64BitOperatingSystem)
            {
                this.FindFolderComFile(
                    Registry.LocalMachine,
                    @"Software\Wow6432Node\Classes\Folder\Shellex\ColumnHandlers");
            }
            #endregion
            #region Internet Explorer
            this.FindFolderComFile(
                Registry.LocalMachine,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects");
            if (Environment.Is64BitOperatingSystem)
            {
                this.FindFolderComFile(
                    Registry.LocalMachine,
                    @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects");
            }
            this.FindFiles(
                Registry.CurrentUser,
                @"Software\Microsoft\Internet Explorer\UrlSearchHooks",
                null,
                RegFindMode.KEY);
            #endregion
            #region service
            this.FindAllFiles(
                Registry.LocalMachine,
                @"System\CurrentControlSet\Services",
                new string[] { "ImagePath" },
                new string[] { "exe", "dll" },
                new RegNodeChange(GetParentName),
                RegFindMode.VALUE,
                Environment.GetFolderPath(Environment.SpecialFolder.System) + Path.DirectorySeparatorChar + "drivers");
            #endregion
            #region Drivers
            this.FindAllFiles(
                Registry.LocalMachine,
                @"System\CurrentControlSet\Services",
                new string[] { "ImagePath" },
                new string[] { "sys" },
                new RegNodeChange(GetParentName),
                RegFindMode.VALUE,
                Environment.GetFolderPath(Environment.SpecialFolder.System) + Path.DirectorySeparatorChar + "drivers");
            #endregion
            #region KnownDlls
            this.FindFiles(
                Registry.LocalMachine,
                @"System\CurrentControlSet\Control\Session Manager\KnownDlls",
                new string[] { "dll" },
                RegFindMode.VALUE,
                Environment.GetFolderPath(Environment.SpecialFolder.System));
            if (Environment.Is64BitOperatingSystem)
            {
                this.FindFiles(
                    Registry.LocalMachine,
                    @"System\CurrentControlSet\Control\Session Manager\KnownDlls",
                    new string[] { "dll" },
                    RegFindMode.VALUE,
                    Environment.GetFolderPath(Environment.SpecialFolder.SystemX86));
            }
            #endregion
            #region Winsock Providers
            {
                RegistryKey root = Registry.LocalMachine;
                string subkey = @"System\CurrentControlSet\Services\WinSock2\Parameters\Protocol_Catalog9";
                ListViewGroup group = new ListViewGroup(root.Name + @"\" + subkey);
                listView.Groups.Add(group);
                RegNode rootnode = new RegNode();
                List<RegNode> list = MyReg.FindAllValues(
                                         root,
                                         subkey,
                                         new string[] { "PackedCatalogItem" },
                                         ref rootnode);
                try
                {
                    Process p = new Process();
                    p.StartInfo.FileName = "cmd.exe";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardInput = true;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.RedirectStandardError = true;
                    p.StartInfo.CreateNoWindow = true;
                    p.Start();
                    p.StandardInput.AutoFlush = true;
                    p.StandardInput.WriteLine("netsh winsock show catalog");
                    p.StandardInput.WriteLine("exit");
                    string cmdstr = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    p.Close();
                    Regex reg = new Regex("(:|：)[^:：\n]+[\n]", RegexOptions.IgnoreCase);
                    foreach (RegNode node in list)
                    {
                        try
                        {
                            // reflection
                            FieldInfo maxPathField = typeof(Path).GetField("MaxPath",
                                BindingFlags.Static |
                                BindingFlags.GetField |
                                BindingFlags.NonPublic);

                            // invoke the field gettor, which returns 260
                            int MaxPathLength = (int)maxPathField.GetValue(null);
                            byte[] word = node.Value as byte[];
                            byte[] guid = new byte[16];
                            for (int i = 0; i < 16; i++)
                            {
                                guid[i] = word[20 + MaxPathLength + i];
                            }
                            Guid _guid = new Guid(guid);
                            int match = cmdstr.IndexOf(_guid.ToString(), StringComparison.OrdinalIgnoreCase);
                            if (match != -1)
                            {
                                MatchCollection collection = reg.Matches(cmdstr.Substring(0, match));
                                if (collection.Count > 0)
                                {
                                    Match lastmatch = collection[collection.Count - 1];
                                    string mainfile = Encoding.Default.GetString(word, 0, MaxPathLength).Trim();
                                    string nameinfo = lastmatch.Value.Trim(new char[] { ' ', ':', ':', '\r', '\n' });
                                    string info = Encoding.Unicode.GetString(word, MaxPathLength + 116, 512);
                                    MyFile file = MyFileHelper.GetFileInfo(mainfile);
                                    if (file != null)
                                    {
                                        ListViewItem item = new ListViewItem(group);
                                        item.Checked = true;
                                        item.ToolTipText = group.Header;
                                        item.Text = " " + nameinfo;
                                        FileToItem(file, ref item);
                                        item.SubItems[1].Text += " -- " + info.Trim();
                                        this.listView.Items.Add(item);
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }
            #endregion
            #region Print Monitors
            this.FindAllFiles(
                Registry.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\Print\Monitors",
                new string[] { "Driver" },
                null,
                new RegNodeChange(GetParentName),
                RegFindMode.VALUE,
                Environment.GetFolderPath(Environment.SpecialFolder.System));
            #endregion
            #region LSA Providers
            this.FindAllFiles(
                Registry.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\SecurityProviders",
                new string[] { "SecurityProviders" },
                null,
                new RegNodeChange((RegNode tr) =>
                {
                    return tr.Name;
                }),
                RegFindMode.VALUE,
                Environment.GetFolderPath(Environment.SpecialFolder.System));
            if (Environment.Is64BitOperatingSystem)
            {
                this.FindAllFiles(
                    Registry.LocalMachine,
                    @"SYSTEM\CurrentControlSet\Control\SecurityProviders",
                    new string[] { "SecurityProviders" },
                    null,
                    new RegNodeChange((RegNode tr) =>
                    {
                        return tr.Name;
                    }),
                    RegFindMode.VALUE,
                    Environment.GetFolderPath(Environment.SpecialFolder.SystemX86));
            }
            {
                RegistryKey root = Registry.LocalMachine;
                string subkey = @"SYSTEM\CurrentControlSet\Control\Lsa";
                Dictionary<string, object> result = MyReg.GetValues(root, subkey);
                if (result.ContainsKey("Authentication Packages"))
                {
                    ListViewGroup group = new ListViewGroup(root.Name + @"\" + subkey + @"\Authentication Packages");
                    listView.Groups.Add(group);
                    try
                    {
                        string[] value = result["Authentication Packages"] as string[];
                        foreach (string file in value)
                        {
                            MyFile myfile = MyFileHelper.GetFileInfo(Environment.GetFolderPath(Environment.SpecialFolder.System) + Path.DirectorySeparatorChar + file + ".dll");
                            if (file != null)
                            {
                                ListViewItem item = new ListViewItem(group);
                                item.Checked = true;
                                item.ToolTipText = group.Header;
                                item.Text = " " + file;
                                FileToItem(myfile, ref item);
                                this.listView.Items.Add(item);
                            }
                        }
                    }
                    catch { }
                }
                if (result.ContainsKey("Notification Packages"))
                {
                    ListViewGroup group = new ListViewGroup(root.Name + @"\" + subkey + @"\Notification Packages");
                    listView.Groups.Add(group);
                    try
                    {
                        string[] value = result["Notification Packages"] as string[];
                        foreach (string file in value)
                        {
                            MyFile myfile = MyFileHelper.GetFileInfo(Environment.GetFolderPath(Environment.SpecialFolder.System) + Path.DirectorySeparatorChar + file + ".dll");
                            if (file != null)
                            {
                                ListViewItem item = new ListViewItem(group);
                                item.Checked = true;
                                item.ToolTipText = group.Header;
                                item.Text = " " + file;
                                FileToItem(myfile, ref item);
                                this.listView.Items.Add(item);
                            }
                        }
                    }
                    catch { }
                }
                if (result.ContainsKey("Security Packages"))
                {
                    ListViewGroup group = new ListViewGroup(root.Name + @"\SYSTEM\CurrentControlSet\Control\Lsa\Security Packages");
                    listView.Groups.Add(group);
                    try
                    {
                        string[] value = result["Security Packages"] as string[];
                        foreach (string file in value)
                        {
                            MyFile myfile = MyFileHelper.GetFileInfo(Environment.GetFolderPath(Environment.SpecialFolder.System) + Path.DirectorySeparatorChar + file + ".dll");
                            if (file != null)
                            {
                                ListViewItem item = new ListViewItem(group);
                                item.Checked = true;
                                item.ToolTipText = group.Header;
                                item.Text = " " + file;
                                FileToItem(myfile, ref item);
                                this.listView.Items.Add(item);
                            }
                        }
                    }
                    catch { }
                }
            }
            this.FindFolderComFile(
                Registry.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers");
            this.FindFolderComFile(
                Registry.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Provider Filters");
            this.FindFolderComFile(
                Registry.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\PLAP Providers");
            #endregion
            #region Network Providers
            {
                RegistryKey root = Registry.LocalMachine;
                string subkey = @"SYSTEM\CurrentControlSet\Control\NetworkProvider\Order";
                ListViewGroup group = new ListViewGroup(root.Name + @"\" + subkey);
                listView.Groups.Add(group);
                Dictionary<string, object> names = MyReg.GetValues(Registry.LocalMachine, subkey);
                if (names.ContainsKey("ProviderOrder"))
                {
                    string origin_str = this.ObjectToString(names["ProviderOrder"]);
                    if (origin_str != null)
                    {
                        string[] providers = origin_str.Trim().Split(',');
                        foreach (string provider in providers)
                        {
                            Dictionary<string, object> values = MyReg.GetValues(root, @"SYSTEM\CurrentControlSet\Services\" + provider.Trim() + @"\NetworkProvider");
                            if (values.ContainsKey("ProviderPath"))
                            {
                                string origin_path = this.ObjectToString(values["ProviderPath"]);
                                if (origin_path != null)
                                {
                                    MyFile file = MyFileHelper.GetFileInfo(origin_path);
                                    if (file != null)
                                    {
                                        ListViewItem item = new ListViewItem(group);
                                        item.Checked = true;
                                        item.ToolTipText = root.Name +  @"\SYSTEM\CurrentControlSet\Services\" + provider.Trim();
                                        item.Text = " " + provider.Trim();
                                        FileToItem(file, ref item);
                                        if (values.ContainsKey("Name"))
                                        {
                                            item.SubItems[2].Text = values["Name"] + " ( " + item.SubItems[2].Text + " ) ";
                                        }
                                        this.listView.Items.Add(item);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            #endregion
            #region Sschiduled Tasks
            {
                ListViewGroup group = new ListViewGroup("Scheduled Tasks");
                listView.Groups.Add(group);
                ScheduledTasks st = new ScheduledTasks();
                string[] taskNames = st.GetTaskNames();
                foreach (string name in taskNames)
                {
                    try
                    {
                        Task t = st.OpenTask(name);
                        if (t != null)
                        {
                            MyFile myfile = MyFileHelper.GetFileInfo(t.ApplicationName);
                            ListViewItem item = new ListViewItem(group);
                            item.Checked = true;
                            item.ToolTipText = null;
                            item.Text = " " + t.Name;
                            FileToItem(myfile, ref item);
                            this.listView.Items.Add(item);
                            t.Close();
                        }
                    }
                    catch { }
                }
            }
            #endregion
        }

        #region 所有使用到的函数

        ///
        /// <summary>获取某一注册表下所有值的文件列表</summary>
        ///<param name="root">根注册表项</param>
        ///<param name="subkey">索要访问的子键路径</param>
        ///<param name="filter">文件名的过滤</param>
        ///<param name="defaultpath">默认文件路径</param>
        ///<returns>
        /// 返回值始终为空.</returns>
        ///
        public void FindFiles(RegistryKey root, string subkey, string[] filter = null, RegFindMode findmode = RegFindMode.VALUE, string defaultpath = "")
        {
            ListViewGroup group = new ListViewGroup(root.Name + @"\" + subkey);
            listView.Groups.Add(group);
            Dictionary<string, object> names = MyReg.GetValues(root, subkey);
            foreach (KeyValuePair<string, object> pair in names)
            {
                string origin_str = "";
                try
                {
                    origin_str = (pair.Value as string).Trim();
                }
                catch { continue; }
                if (((uint)RegFindMode.VALUE & (uint)findmode) == (uint)RegFindMode.VALUE)
                {
                    MyFile file = MyFileHelper.GetFileInfo(origin_str, filter, defaultpath);
                    if (file != null)
                    {
                        ListViewItem item = new ListViewItem(group);
                        item.Checked = true;
                        item.ToolTipText = group.Header;
                        item.Text = " " + ((file.IsCOM && file.fileversioninfo != null) ? file.comdescription : pair.Key.Trim());
                        FileToItem(file, ref item);
                        this.listView.Items.Add(item);
                    }
                }
                if (((uint)RegFindMode.KEY & (uint)findmode) == (uint)RegFindMode.KEY)
                {
                    MyFile file = MyFileHelper.GetFileInfo(pair.Key.Trim(), filter, defaultpath);
                    if (file != null)
                    {
                        ListViewItem item = new ListViewItem(group);
                        item.Checked = true;
                        item.ToolTipText = group.Header;
                        item.Text = " " + ((file.IsCOM && file.fileversioninfo != null) ? file.comdescription : origin_str);
                        FileToItem(file, ref item);
                        this.listView.Items.Add(item);
                    }
                }
            }
        }

        public void FindFolderComFile(RegistryKey root, string subkey, RegNodeChange _delegete = null, string defaultpath = "")
        {
            ListViewGroup group = new ListViewGroup(root.Name + @"\" + subkey);
            listView.Groups.Add(group);
            RegNode roonode = new RegNode();
            foreach (RegNode node in MyReg.FindAllCOMFolders(root, subkey, ref roonode))
            {
                MyFile file = MyFileHelper.GetComFileInfo(node.Name.Trim(), defaultpath);
                ListViewItem item = new ListViewItem(group);
                item.Checked = true;
                item.ToolTipText = group.Header + @"\" + node.Name.Trim();
                item.Text = " " + ((_delegete == null) ? ((file.IsCOM && file.fileversioninfo != null) ? file.comdescription : node.Name) : _delegete(node));
                FileToItem(file, ref item);
                this.listView.Items.Add(item);
            }
        }

        ///
        /// <summary>获取某一注册表下某一键名的文件列表</summary>
        ///<param name="root">根注册表项</param>
        ///<param name="subkey">索要访问的子键路径</param>
        ///<param name="search">需要查找的键名</param>
        ///<param name="filter">文件名的过滤</param>
        ///<param name="_delegete">行标题的显示委托函数</param>
        ///<param name="defaultpath">默认文件路径</param>
        ///<returns>
        /// 返回值始终为空.</returns>
        ///
        public void FindAllFiles(RegistryKey root, string subkey, string[] search, string[] filter = null, RegNodeChange _delegete = null, RegFindMode findmode = RegFindMode.VALUE, string defaultpath = "")
        {
            ListViewGroup group = new ListViewGroup(root.Name + @"\" + subkey);
            listView.Groups.Add(group);
            RegNode tr = new RegNode();
            List<RegNode> names = MyReg.FindAllValues(root, subkey, search, ref tr);
            foreach (RegNode tritem in names)
            {
                string origin_str = "";
                try
                {
                    origin_str = (tritem.Value as string).Trim();
                }
                catch { continue; }
                if (((uint)RegFindMode.VALUE & (uint)findmode) == (uint)RegFindMode.VALUE)
                {
                    MyFile file = MyFileHelper.GetFileInfo(origin_str, filter, defaultpath);
                    if (file != null)
                    {
                        ListViewItem item = new ListViewItem(group);
                        item.Checked = true;
                        item.ToolTipText = group.Header;
                        item.Text = " " + ((_delegete == null) ? ((file.IsCOM && file.fileversioninfo != null) ? file.comdescription : tritem.Name) : _delegete(tritem));
                        FileToItem(file, ref item);
                        this.listView.Items.Add(item);
                    }
                }
                if (((uint)RegFindMode.KEY & (uint)findmode) == (uint)RegFindMode.KEY)
                {
                    MyFile file = MyFileHelper.GetFileInfo(tritem.Name.Trim(), filter, defaultpath);
                    if (file != null)
                    {
                        ListViewItem item = new ListViewItem(group);
                        item.Checked = true;
                        item.ToolTipText = group.Header;
                        item.Text = " " + ((_delegete == null) ? ((file.IsCOM && file.fileversioninfo != null) ? file.comdescription : origin_str) : _delegete(tritem));
                        FileToItem(file, ref item);
                        
                        this.listView.Items.Add(item);
                    }
                }
            }
        }

        /// <summary>
        /// 将文件信息变为列表的一项
        /// </summary>
        /// <param name="file">文件结构体<see cref="MyFile"/></param>
        /// <param name="item"></param>
        public void FileToItem(MyFile file, ref ListViewItem item)
        {
            item.SubItems.Add(file.fullpath);
            if (file.fullpath.StartsWith("File not found !"))
            {
                item.SubItems[0].BackColor = Color.Gold;
            }
            if (file.fileversioninfo != null)
            {
                try
                {
                    Icon myicon = Icon.ExtractAssociatedIcon(file.fullpath);
                    this.imgLst.Images.Add(myicon);
                    item.ImageIndex = this.iconcount++;
                }
                catch { }
                item.SubItems.Add(file.fileversioninfo.FileDescription);
                item.SubItems.Add(file.fileversioninfo.LegalCopyright);
                item.SubItems.Add(file.fileversioninfo.Language);
                item.SubItems.Add(file.fileversioninfo.CompanyName);
                item.SubItems.Add(file.fileversioninfo.FileVersion);
            }
            else
            {
                item.SubItems.Add("");
                item.SubItems.Add("");
                item.SubItems.Add("");
                item.SubItems.Add("");
                item.SubItems.Add("");
            }
        }
        #endregion

        /// <summary>Opens RegEdit to the provided key
        /// <para><example>@"HKEY_CURRENT_USER\Software\MyCompanyName\MyProgramName\"</example></para>
        /// </summary>
        private void listView_ItemDoubleClick(object sender, EventArgs e)
        {
            if (this.listView.SelectedItems.Count == 0)
                return;

            //前提，listview禁止多选
            ListViewItem currentRow = listView.SelectedItems[0];
            if (currentRow.ToolTipText != null)
            {
                RegistryKey rKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Applets\Regedit", true);
                rKey.SetValue("LastKey", currentRow.ToolTipText);
                Process.Start("regedit.exe");
            }
        }

        private void 另存为ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFile = new SaveFileDialog();
            saveFile.AddExtension = true;
            saveFile.Filter = "文本文件|*.txt|网页文件|*.html";
            saveFile.DefaultExt = "txt";
            saveFile.FileName = "启动项";
            saveFile.CheckPathExists = true;
            //string a="";
            if (saveFile.ShowDialog() == DialogResult.OK)
            {
                if (!string.IsNullOrEmpty(saveFile.FileName))
                {
                    try
                    {
                        FileStream filStream = new FileStream(saveFile.FileName, FileMode.Create);
                        StreamWriter strWriter = new StreamWriter(filStream, Encoding.UTF8);
                        if (Path.GetExtension(saveFile.FileName).ToLower() == ".html")
                        {
                            string header = "<html lang='en'><head><meta charset='utf-8'>";
                            header += "<meta http-equiv='X-UA-Compatible' content='IE=edge'>";
                            header += "<meta name='viewport' content='width=device-width, initial-scale=1'>";
                            header += "<title>启动项列表</title>";
                            header += "<link href='http://cdn.bootcss.com/bootstrap/3.0.0/css/bootstrap.min.css' rel='stylesheet'>";
                            header += "</head><html><body><div class='container main' style='margin-top: 70px;'><div class='panel panel-default row'>";
                            header += "<table class='table table-striped table-bordered table-hover table-responsive'><thead><tr>";
                            header += "<th>图标</th><th>描述</th><th>路径信息</th><th>版权</th>";
                            header += "<th>语言</th><th>公司</th><th>版本</th></tr></thead><tbody><tbody></table>";
                            strWriter.Write(header);
                            foreach (ListViewGroup group in this.listView.Groups)
                            {
                                string html = "<h4 class='group'>" + group.Header + "</h4>";
                                html += "<table class='table table-striped table-bordered table-hover table-responsive'><thead></thead><tbody>";
                                foreach (ListViewItem item in group.Items)
                                {
                                    try
                                    {
                                        if (!item.Checked) { continue; }
                                        Image image = this.imgLst.Images[item.ImageIndex];
                                        string strbaser64 = "";
                                        Bitmap bmp = new Bitmap(image);
                                        MemoryStream ms = new MemoryStream();
                                        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                        byte[] arr = new byte[ms.Length];
                                        ms.Position = 0;
                                        ms.Read(arr, 0, (int)ms.Length);
                                        ms.Close();
                                        strbaser64 = Convert.ToBase64String(arr);
                                        html += @"<tr><td class='image'><img src='data:image/png;base64," + strbaser64 + @"'/></td>";
                                        html += "<td class='text'>" + item.Text + "</td>";
                                        html += "<td class='path'>" + item.SubItems[1].Text + "</td>";
                                        html += "<td class='description'>" + item.SubItems[2].Text + "</td>";
                                        html += "<td class='copyright'>" + item.SubItems[3].Text + "</td>";
                                        html += "<td class='language'>" + item.SubItems[4].Text + "</td>";
                                        html += "<td class='corporation'>" + item.SubItems[5].Text + "</td>";
                                        html += "<td class='version'>" + item.SubItems[6].Text + "</td></tr>";
                                    }
                                    catch { }
                                }
                                html += "</tbody></table>";
                                strWriter.Write(html);
                            }
                            strWriter.Write("</div></div><script src='http://cdn.bootcss.com/jquery/1.11.0/jquery.min.js'></script>"
                                + "<script src='http://cdn.bootcss.com/bootstrap/3.0.0/js/bootstrap.min.js'></script></body></html>");
                        }
                        else if (Path.GetExtension(saveFile.FileName).ToLower() == ".txt")
                        {
                            foreach (ListViewGroup group in this.listView.Groups)
                            {
                                string html = group.Header + "\r\n";
                                foreach (ListViewItem item in group.Items)
                                {
                                    try
                                    {
                                        if (!item.Checked) { continue; }
                                        html += "+ " + item.Text + "\t" + item.SubItems[2].Text + "\t" + item.SubItems[5].Text + "\t" + item.SubItems[1].Text + "\r\n";
                                    }
                                    catch { }
                                }
                                strWriter.Write(html);
                            }
                        }
                        else
                        {
                            strWriter.Close();
                            filStream.Close();
                            MessageBox.Show("未知保存类型！", "提示");
                            return;
                        }
                        strWriter.Close();
                        filStream.Close();
                        MessageBox.Show("保存成功！", "提示");
                    }
                    catch (Exception _e)
                    {
                        MessageBox.Show("保存成失败:" + _e.ToString(), "提示");
                    }
                }
            }
        }
    }
}
