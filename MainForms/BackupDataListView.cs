﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
#region tab backup
class BackupDataListView :ListView {
    private ContextMenuStrip cMenu;
    private BackupDataListViewItem selectedItem = null;
    private ColumnHeader clmnBackupTime; // 'バックアップ日時' 列ヘッダ
    private ColumnHeader clmnAffiliationWorldName;  // '所属ワールド名' 列ヘッダ
    private ColumnHeader clmnWorldAffiliationDir;  // '所属ディレクトリ' 列ヘッダ
    public BackupDataListView(World worldObj) {
        FullRowSelect = true;
        MultiSelect = false;
        Anchor = AnchorStyles.Left | AnchorStyles.Right;
        Scrollable = false;
        View = View.Details;
        Margin = new Padding(0);
        BorderStyle = BorderStyle.None;
        Dock = DockStyle.Fill;
        Scrollable = false;
        Sorting = SortOrder.Descending;


        cMenu = new ContextMenuStrip();

        #region contextmenu

        cMenu.Opening += new CancelEventHandler(Menu_Opening);
        ToolStripMenuItem returnBackup = new ToolStripMenuItem() {
            Text = "このバックアップから復元する(&B)"
        };
        returnBackup.Click += new EventHandler(ReturnBackup_Click);
        cMenu.Items.Add(returnBackup);

        ToolStripMenuItem openInExplorer = new ToolStripMenuItem() {
            Text = "エクスプローラーで開く(&X)"
        };

        openInExplorer.Click += new EventHandler(OpenInExplorer_Click);
        if (worldObj.isAlive)
            cMenu.Items.Add(openInExplorer);

        this.ContextMenuStrip = cMenu;
        #endregion

        clmnBackupTime = new ColumnHeader() { Text = "バックアップ日時" };
        clmnAffiliationWorldName = new ColumnHeader() { Text = "バックアップ元" };
        clmnWorldAffiliationDir = new ColumnHeader() { Text = "バックアップ元の所属ディレクトリ" };

        Columns.AddRange(new ColumnHeader[]{
            clmnBackupTime,clmnAffiliationWorldName,clmnWorldAffiliationDir
        });

        foreach (ColumnHeader ch in this.Columns) {
            ch.Width = -2;
        }

        Load(worldObj);
        if (Items.Count > 0) {
            Logger.Debug(Height);
            Height /= 4;
            Height *= Items.Count + 1;
        }
    }
    void Load(World worldObj) {
        Logger.Info("" + worldObj.WName + "の一覧以下のパスからロードします");
        Logger.Info($"path:{worldObj.WPath}");
        try {
            List<string> backupFolders = new List<string>(GetBackups(worldObj));
            Logger.Info($"backupFolderCount[{backupFolders.Count()}]");
            foreach (string backupFolder in backupFolders) {
                if (Path.GetExtension(backupFolder) == "zip") {
                    backupFolder.Substring(0, backupFolder.Length - 4);
                    Logger.Debug("a::::::::::::" + backupFolder);
                }
                DateTime time = DateTime.ParseExact((Path.GetFileName(backupFolder)).Substring(0, 12), "yyyyMMddHHmm", null);
                this.Items.Add(new BackupDataListViewItem(new string[] { time.ToString("yyyy-MM-dd HH:mm"), worldObj.WName, worldObj.WDir }, worldObj));
            }
        }
        catch (DirectoryNotFoundException) {
            Logger.Warn("バックアップが存在しません");
        }
    }

    private List<string> GetBackups(World w) {
        return Directory.GetFileSystemEntries(AppConfig.BackupPath + "\\" + w.WDir + "\\" + w.WName).ToList();
    }

    void Menu_Opening(object sender, CancelEventArgs e) {
        Logger.Info("call:Menu_Opening");
        Point p = this.PointToClient(Cursor.Position);
        BackupDataListViewItem item = this.HitTest(p).Item as BackupDataListViewItem;
        if (item == null) {
            e.Cancel = true;
        }
        else if (item.Bounds.Contains(p)) {
            ContextMenuStrip menu = sender as ContextMenuStrip;
            if (menu != null) {
                Logger.Info($"{item}");
                Logger.Info(item.world.WName);
                Logger.Info(item.world.isAlive);
                selectedItem = item;
            }
        }
        else {
            e.Cancel = true;
        }
    }

    private void ReturnBackup_Click(object sender, EventArgs e) {
        Logger.Info("call:ReturnBackup_Click");
        ContextMenuStrip menu = sender as ContextMenuStrip;
        if (selectedItem != null) {
            Logger.Info(selectedItem.world.isAlive);
            if (!selectedItem.world.isAlive) {
                //バックアップ元ワールドが死んでいる場合messageBox出現
                Logger.Info(" 死亡済みワールドのバックアップが選択されました");
                MessageBox.Show("同名の別ワールドがゲームディレクトリ内に存在しています", "Minecraft Auto Backup", buttons: MessageBoxButtons.OK);
            }
            DateTime dt = DateTime.ParseExact(selectedItem.SubItems[0].Text, "yyyy-MM-dd HH:mm", null);
            string fileName = dt.ToString("yyyyMMddHHmm");
            string src;
            if (File.Exists($"{AppConfig.BackupPath}\\{selectedItem.SubItems[2].Text}\\{selectedItem.world.WName}\\{fileName}.zip")) {
                // バックアップがzipだった場合
                src = $"{AppConfig.BackupPath}\\{selectedItem.SubItems[2].Text}\\{selectedItem.world.WName}\\{fileName}.zip";
            }
            else {
                // バックアップがzipじゃなかった場合
                src = $"{AppConfig.BackupPath}\\{selectedItem.SubItems[2].Text}\\{selectedItem.world.WName}\\{fileName}";
            }

            World tar = selectedItem.world;
            RestoreFromBackupForm restoreFrom = new RestoreFromBackupForm(src, tar);
            restoreFrom.Show();
        }
    }

    private void OpenInExplorer_Click(object sender, EventArgs e) {
        ContextMenuStrip menu = sender as ContextMenuStrip;
        System.Diagnostics.Process.Start("EXPLORER.EXE", Util.makePathToWorld(selectedItem.SubItems[1].Text, selectedItem.SubItems[2].Text));
    }

}
#endregion