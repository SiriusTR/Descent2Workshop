﻿/*
    Copyright (c) 2019 SaladBadger

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

using System;
using System.Windows.Forms;
using System.IO;
using OpenTK.Graphics;
using LibDescent.Data;
using LibDescent.Edit;

namespace Descent2Workshop
{
    public partial class StandardUI : Form
    {
        private HOGFile defaultHogFile;
        private PIGFile defaultPigFile;
        private SNDFile defaultSoundFile;
        public static UserOptions options = new UserOptions();

        public bool readyForUse = false;

        public SNDFile DefaultSoundFile { get => defaultSoundFile; }
        public PIGFile DefaultPigFile { get => defaultPigFile; }
        public Palette DefaultPalette { get; private set; }

        public StandardUI()
        {
            InitializeComponent();

            options.openOptionsFile(Application.StartupPath + "/default.cfg");
            tbLog.Text += "\r\n-----------------------------\r\n";

            string defaultHOG = options.GetOption("HOGFile", "");
            string defaultPIG = options.GetOption("PIGFile", "");
            string defaultSound = options.GetOption("SNDFile", "");
            if (defaultHOG != "")
            {
                defaultHogFile = LoadDefaultHOG(defaultHOG);
                if (defaultHogFile != null)
                {
                    //Piggy is dependent on HOG palettes, so we need a HOG first. 
                    if (defaultPIG != "")
                    {
                        defaultPigFile = LoadDefaultPig(defaultPIG, defaultHogFile);
                    }
                }
            }

            if (defaultSound != "")
            {
                defaultSoundFile = LoadDefaultSND(defaultSound);
            }

            readyForUse = CheckReadyForUse();

            if (!readyForUse)
            {
                MessageBox.Show("It seems you haven't configured your default .HOG, .PIG, and .SXX files yet." +
                    "\nThis must be done before you can use D2 Workshop" +
                    "\nYou can do this in the Options menu");
            }
            //GraphicsContext.ShareContexts = false;

#if DEBUG == false
            mainMenu1.MenuItems.Remove(DebugMenu);
#endif
        }

        public void AppendConsole(string msg)
        {
            tbLog.Text += msg;
            tbLog.SelectionStart = tbLog.Text.Length - 1;
            tbLog.ScrollToCaret();
        }

        private void menuItem11_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = ".HOG files|*.HOG";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                HOGFile archive = new HOGFile();
                archive.Read(openFileDialog1.FileName);
                HOGEditor archiveEditor = new HOGEditor(archive, this);
                archiveEditor.Show();
            }
        }

        private void menuItem12_Click(object sender, EventArgs e)
        {
            HOGFile archive = new HOGFile();
            HOGEditor archiveEditor = new HOGEditor(archive, this);
            archiveEditor.Show();
        }

        private void LoadPIGMenu_Click(object sender, EventArgs e)
        {
            if (defaultHogFile == null)
            {
                MessageBox.Show("A default HOG file must be loaded before loading the default PIG file");
                return;
            }
            openFileDialog1.Filter = ".PIG files|*.PIG";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string paletteName = Path.GetFileNameWithoutExtension(openFileDialog1.FileName) + ".256";
                Palette newPalette; int lumpIndex;

                lumpIndex = defaultHogFile.GetLumpNum(paletteName);
                if (lumpIndex != -1) newPalette = new Palette(defaultHogFile.GetLumpData(lumpIndex));
                else newPalette = new Palette(); //If the palette couldn't be located, make a default grayscale palette

                PIGFile archive = new PIGFile();
                string statusMsg;
                if (FileUtilities.LoadDataFile(openFileDialog1.FileName, archive, out statusMsg))
                {
                    PIGEditor archiveEditor = new PIGEditor(archive, newPalette, openFileDialog1.FileName);
                    archiveEditor.host = this;
                    archiveEditor.Show();
                }
                else
                {
                    MessageBox.Show(statusMsg, "Error loading PIG file.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void DefaultHOGMenu_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = ".HOG files|*.HOG";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                defaultHogFile = LoadDefaultHOG(openFileDialog1.FileName);
            }
        }

        private void StandardUI_FormClosing(object sender, FormClosingEventArgs e)
        {
            options.WriteOptions(Application.StartupPath + "/default.cfg");
        }

        private void DefaultPIGMenu_Click(object sender, EventArgs e)
        {
            if (defaultHogFile == null)
            {
                AppendConsole("A default HOG file must be loaded before loading the default PIG file.");
                return;
            }
            openFileDialog1.Filter = ".PIG files|*.PIG";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                defaultPigFile = LoadDefaultPig(openFileDialog1.FileName, defaultHogFile);
            }
        }

        /// <summary>
        /// Helper function for loading a HAM file from a file.
        /// </summary>
        /// <param name="filename">The filename to load from.</param>
        /// <param name="dataFile">The HAM file to load data into.</param>
        /// <returns>True if no error, false if an error occurred.</returns>
        private bool LoadHAMFile(string filename, EditorHAMFile dataFile)
        {
            string statusMsg;
            if (!FileUtilities.LoadDataFile(filename, dataFile, out statusMsg))
            {
                MessageBox.Show(statusMsg, "Error loading HAM file.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Helper function for loading a HAM file from a file.
        /// </summary>
        /// <param name="filename">The filename to load from.</param>
        /// <param name="dataFile">The HAM file to load data into.</param>
        /// <returns>True if no error, false if an error occurred.</returns>
        private bool LoadHXMFile(string filename, EditorHXMFile dataFile)
        {
            string statusMsg;
            if (!FileUtilities.LoadDataFile(filename, dataFile, out statusMsg))
            {
                MessageBox.Show(statusMsg, "Error loading HXM file.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Helper function for loading a V-HAM file from a file.
        /// </summary>
        /// <param name="filename">The filename to load from.</param>
        /// <param name="dataFile">The HAM file to load data into.</param>
        /// <returns>True if no error, false if an error occurred.</returns>
        private bool LoadVHAMFile(string filename, EditorVHAMFile dataFile)
        {
            string statusMsg;
            if (!FileUtilities.LoadDataFile(filename, dataFile, out statusMsg))
            {
                MessageBox.Show(statusMsg, "Error loading V-HAM file.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        private void menuItem4_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = ".HAM files|*.HAM";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                EditorHAMFile archive = new EditorHAMFile(defaultPigFile);
                if (LoadHAMFile(openFileDialog1.FileName, archive))
                {
                    HAMEditor archiveEditor = new HAMEditor(archive, this, defaultPigFile, DefaultPalette, openFileDialog1.FileName);
                    archiveEditor.Show();
                }
            }
        }

        private void menuItem23_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = ".POF files|*.POF";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string traceto = "";
                if (bool.Parse(StandardUI.options.GetOption("TraceModels", bool.FalseString)))
                {
                    string bareFilename = Path.GetFileName(openFileDialog1.FileName);
                    traceto = StandardUI.options.GetOption("TraceDir", ".") + Path.DirectorySeparatorChar + Path.ChangeExtension(bareFilename, "txt");
                }

                Polymodel model = POFReader.ReadPOFFile(openFileDialog1.FileName, traceto);
                PolymodelPreviewer archiveEditor = new PolymodelPreviewer(model, this);
                archiveEditor.Show();
            }
        }

        private void NewHXMFileMenu_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = ".HAM files|*.HAM";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                EditorHAMFile archive = new EditorHAMFile(defaultPigFile);
                if (LoadHAMFile(openFileDialog1.FileName, archive))
                {
                    EditorHXMFile hxm = new EditorHXMFile(archive);

                    HXMEditor editor = new HXMEditor(hxm, this, "");
                    editor.Show();
                }
            }
        }

        private void menuItem24_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = ".S** files|*.S22;*.S11";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                bool closeOnExit = true;
                SNDFile archive;

                if (defaultSoundFile != null && Path.GetFullPath(openFileDialog1.FileName) == Path.GetFullPath(options.GetOption("SNDFile", "")))
                {
                    AppendConsole("Loading internal SND file for editing.");
                    archive = defaultSoundFile;
                    closeOnExit = false;
                }
                archive = new SNDFile();
                archive.LoadDataFile(openFileDialog1.FileName);
                SXXEditor archiveEditor = new SXXEditor(this, archive);
                if (Path.GetExtension(openFileDialog1.FileName).Equals(".S11", StringComparison.OrdinalIgnoreCase) ||
                    Path.GetExtension(openFileDialog1.FileName).Equals(".HAM", StringComparison.OrdinalIgnoreCase))
                {
                    archiveEditor.isLowFi = true;
                }
                archiveEditor.closeOnExit = closeOnExit;
                archiveEditor.Show();
            }
        }

        private void menuItem25_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = ".S** files|*.S22;*.S11";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                defaultSoundFile = new SNDFile();
                defaultSoundFile.LoadDataFile(openFileDialog1.FileName);
                options.SetOption("SNDFile", openFileDialog1.FileName);
                tbLog.Text += "Loaded .SXX!\r\n";
            }
        }

        private void OptionsMenu_Click(object sender, EventArgs e)
        {
            ConfigDialog config = new ConfigDialog();
            if (config.ShowDialog() == DialogResult.OK)
            {
                options.SetOption("HOGFile", config.HogFilename);
                options.SetOption("PIGFile", config.PigFilename);
                options.SetOption("SNDFile", config.SndFilename);
                options.SetOption("CompatObjBitmaps", config.NoPMView.ToString());
                options.SetOption("TraceModels", config.Traces.ToString());
                options.SetOption("TraceDir", config.TraceDir);
                options.SetOption("PMVersion", config.PofVer.ToString());

                defaultHogFile = LoadDefaultHOG(config.HogFilename);
                if (defaultHogFile != null)
                    defaultPigFile = LoadDefaultPig(config.PigFilename, defaultHogFile);
                else
                    AppendConsole("Could not load default PIG, since the default HOG is not loaded");
                defaultSoundFile = LoadDefaultSND(config.SndFilename);
            }
        }

        private void MnuAbout_Click(object sender, EventArgs e)
        {
#if DEBUG
            Descent1PIGFile piggyFile = new Descent1PIGFile(false);
            PSXDatFile datFile = new PSXDatFile();
            Stream stream = File.Open("C:/dev/whyamidoingthis/ChocolateDescent/build/data_d1_ed/descent.pig", FileMode.Open);
            piggyFile.Read(stream);
            stream.Close();
            stream.Dispose();

            /*stream = File.Open("C:/dev/psxdescent/DESCENT.DAT", FileMode.Open);
            datFile.Read(stream);
            stream.Close();
            stream.Dispose();*/
            Palette newPalette; int lumpIndex;

            lumpIndex = defaultHogFile.GetLumpNum("DEFAULT.256");
            if (lumpIndex != -1) newPalette = new Palette(defaultHogFile.GetLumpData(lumpIndex));
            else newPalette = new Palette(); //If the palette couldn't be located, make a default grayscale palette
            PIGFile test = new PIGFile();
            for (int i = 1; i < piggyFile.Bitmaps.Count; i++)
            {
                test.Bitmaps.Add(piggyFile.Bitmaps[i]);
            }
            PIGEditor editor = new PIGEditor(test, newPalette, "ara ara");
            editor.Show();
            EditorHAMFile hack = DebugUtil.TranslatePIGToHam(piggyFile, test);
            HAMEditor hameditor = new HAMEditor(hack, this, test, newPalette, "ara ara sayonara");
            hameditor.Show();
#else
            AppendConsole("Descent II Workshop by ISB... heh\n");
#endif
        }

        private void menuItem6_Click_1(object sender, EventArgs e)
        {
            VHAMLoadDialog dialog = new VHAMLoadDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                EditorHAMFile dataFile = new EditorHAMFile(defaultPigFile);
                if (!LoadHAMFile(dialog.HAMFilename, dataFile))
                {
                    AppendConsole("VHAM load aborted.\r\n");
                    return;
                }
                EditorVHAMFile hxmFile = new EditorVHAMFile(dataFile);
                if (LoadVHAMFile(dialog.HXMFilename, hxmFile))
                {
                    VHAMEditor editor = new VHAMEditor(hxmFile, this);
                    editor.Show();
                }
            }
        }

        private HOGFile LoadDefaultHOG(string filename)
        {
            HOGFile defaultHOG = new HOGFile();
            try
            {
                defaultHOG.Read(filename);
                AppendConsole("Loaded default HOG file!\r\n");
                options.SetOption("HOGFile", filename);
            }
            catch (Exception exc)
            {
                AppendConsole("Failed to load default HOG file!\r\n");
                FileExceptionHandler(exc, "HOG file");
                defaultHOG = null;
            }
            return defaultHOG;
        }

        private PIGFile LoadDefaultPig(string filename, HOGFile hogFile)
        {
            string paletteName = Path.GetFileNameWithoutExtension(filename) + ".256";
            Palette newPalette; int lumpIndex;

            lumpIndex = defaultHogFile.GetLumpNum(paletteName);
            if (lumpIndex != -1) newPalette = new Palette(defaultHogFile.GetLumpData(lumpIndex));
            else newPalette = new Palette(); //If the palette couldn't be located, make a default grayscale palette
            DefaultPalette = newPalette;

            PIGFile defaultPIG = new PIGFile();
            try
            {
                /*defaultPIG.Read(filename);
                AppendConsole("Loaded default PIG file!\r\n");*/
                string statusMsg;
                if (FileUtilities.LoadDataFile(filename, defaultPIG, out statusMsg))
                {
                    options.SetOption("PIGFile", filename);
                    AppendConsole("Loaded default PIG file!\r\n");
                }
                else
                {
                    AppendConsole("Failed to load default PIG file:\r\n");
                    AppendConsole(statusMsg);
                    defaultPIG = null;
                }
                
            }
            catch (Exception)
            {
                AppendConsole("Failed to load default PIG file!\r\n");
                defaultPIG = null;
            }
            return defaultPIG;
        }

        private SNDFile LoadDefaultSND(string filename)
        {
            SNDFile defaultSND = new SNDFile();
            try
            {
                defaultSND.LoadDataFile(filename);
                AppendConsole("Loaded default SND file!\r\n");
                if (defaultSoundFile != null)
                    defaultSoundFile.CloseDataFile();
                options.SetOption("SNDFile", filename);
            }
            catch (Exception exc)
            {
                AppendConsole("Failed to load default SND file!\r\n");
                FileExceptionHandler(exc, "SND file");
                defaultSND = null;
            }
            return defaultSND;
        }

        private void FileExceptionHandler(Exception error, string context)
        {
            if (error is FileNotFoundException)
            {
                AppendConsole(String.Format("The specified {0} was not found.\r\n", context));
            }
            else if (error is UnauthorizedAccessException)
            {
                AppendConsole(String.Format("You do not have permission to access the specified {0}.\r\n", context));
            }
            else
            {
                AppendConsole(String.Format("Unhandled error loading {0}: {1}.\r\n", context, error.Message));
            }
        }

        private bool CheckReadyForUse()
        {
            return (defaultHogFile != null) && (defaultPigFile != null) && (defaultSoundFile != null);
        }

        private void OpenHXMMenu_Click(object sender, EventArgs e)
        {
            HXMLoadDialog dialog = new HXMLoadDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                EditorHAMFile dataFile = new EditorHAMFile(defaultPigFile);
                EditorVHAMFile augmentFile = null;
                if (!LoadHAMFile(dialog.HAMFilename, dataFile))
                {
                    AppendConsole("HXM load aborted.\r\n");
                    return;
                }
                if (dialog.VHAMFilename != "")
                {
                    augmentFile = new EditorVHAMFile(dataFile);
                    if (!LoadVHAMFile(dialog.VHAMFilename, augmentFile))
                    {
                        AppendConsole("HXM load aborted.\r\n");
                        return;
                    }
                }
                EditorHXMFile hxmFile = new EditorHXMFile(dataFile);
                if (augmentFile != null)
                    hxmFile.augmentFile = augmentFile;

                if (LoadHXMFile(dialog.HXMFilename, hxmFile))
                {
                    HXMEditor editor = new HXMEditor(hxmFile, this, dialog.HXMFilename);
                    editor.Show();
                }
            }
        }

        private void ViewFNTMenu_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Font files|*.FNT";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Font font = new Font();
                font.LoadFont(openFileDialog1.FileName);
                FontViewer viewer = new FontViewer(font);
                viewer.Show();
            }
        }

        private void OpenPOGMenu_Click(object sender, EventArgs e)
        {
            if (defaultHogFile == null)
            {
                MessageBox.Show("A default HOG file must be loaded before loading a POG file");
                return;
            }
            openFileDialog1.Filter = ".POG files|*.POG";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                POGFile archive = new POGFile();
                string statusMsg;
                if (FileUtilities.LoadDataFile(openFileDialog1.FileName, archive, out statusMsg))
                {
                    POGEditor archiveEditor = new POGEditor(archive, defaultHogFile, openFileDialog1.FileName);
                    archiveEditor.host = this;
                    archiveEditor.Show();
                }
                else
                {
                    MessageBox.Show(statusMsg, "Error loading POG file.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void LoadLevelMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void DumpDescent1PigMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = ".PIG files|*.PIG";
            Descent1PIGFile piggyFile = new Descent1PIGFile(false);
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Stream stream = File.Open(openFileDialog1.FileName, FileMode.Open);
                piggyFile.Read(stream);
                stream.Close();
                stream.Dispose();
            }
            saveFileDialog1.FileName = "ignored";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Palette newPalette;
                int lumpIndex = defaultHogFile.GetLumpNum("DEFAULT.256");
                if (lumpIndex != -1) newPalette = new Palette(defaultHogFile.GetLumpData(lumpIndex));
                else newPalette = new Palette(); //If the palette couldn't be located, make a default grayscale palette
                DebugUtil.DumpDescent1PIGToBBM(piggyFile, newPalette, saveFileDialog1.FileName);
            }
        }

        private void DumpDescent2PigMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = ".PIG files|*.PIG";
            PIGFile piggyFile = new PIGFile();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Stream stream = File.Open(openFileDialog1.FileName, FileMode.Open);
                piggyFile.Read(stream);
                stream.Close();
                stream.Dispose();
            }
            saveFileDialog1.FileName = "ignored";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Palette newPalette;
                string paletteName = Path.GetFileNameWithoutExtension(openFileDialog1.FileName) + ".256";
                int lumpIndex = defaultHogFile.GetLumpNum(paletteName);
                if (lumpIndex != -1) newPalette = new Palette(defaultHogFile.GetLumpData(lumpIndex));
                else newPalette = new Palette(); //If the palette couldn't be located, make a default grayscale palette
                DebugUtil.DumpPIGToBBM(piggyFile, newPalette, saveFileDialog1.FileName);
            }
        }
    }
}
