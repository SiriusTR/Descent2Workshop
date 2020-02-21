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
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Text;
using LibDescent.Data;
using LibDescent.Edit;
using Descent2Workshop.EditorPanels;

namespace Descent2Workshop
{
    public partial class HAMEditor : Form
    {
        private static HAMType[] typeTable = { HAMType.TMAPInfo, HAMType.VClip, HAMType.EClip, HAMType.WClip, HAMType.Robot, HAMType.Weapon,
            HAMType.Model, HAMType.Sound, HAMType.Reactor, HAMType.Powerup, HAMType.Ship, HAMType.Gauge, HAMType.Cockpit, HAMType.XLAT };
        public int[] texturelist;
        public EditorHAMFile datafile;
        public StandardUI host;
        public bool isLocked = false;
        public bool glContextCreated = false;
        private ModelRenderer modelRenderer;
        private bool noPMView = false;
        private string currentFilename;

        private int ElementNumber { get { return (int)nudElementNum.Value; } }
        private int PageNumber { get { return EditorTabs.SelectedIndex; } }

        //I still don't get the VS toolbox. Ugh
        TMAPInfoPanel texturePanel;
        VClipPanel vclipPanel;
        EClipPanel eclipPanel;
        RobotPanel robotPanel;
        WeaponPanel weaponPanel;
        
        public HAMEditor(EditorHAMFile data, StandardUI host, string filename)
        {
            InitializeComponent();
            this.glControl1 = new OpenTK.GLControl();
            this.glControl1.BackColor = System.Drawing.Color.Black;
            this.glControl1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.glControl1.Location = new System.Drawing.Point(452, 61);
            this.glControl1.Name = "glControl1";
            this.glControl1.Size = new System.Drawing.Size(256, 256);
            this.glControl1.TabIndex = 0;
            this.glControl1.VSync = false;
            this.glControl1.Load += new System.EventHandler(this.glControl1_Load);
            this.glControl1.Paint += new System.Windows.Forms.PaintEventHandler(this.glControl1_Paint);

            texturePanel = new TMAPInfoPanel(); components.Add(texturePanel);
            texturePanel.Dock = DockStyle.Fill;
            vclipPanel = new VClipPanel(); components.Add(vclipPanel);
            vclipPanel.Dock = DockStyle.Fill;
            eclipPanel = new EClipPanel(); components.Add(eclipPanel);
            eclipPanel.Dock = DockStyle.Fill;
            robotPanel = new RobotPanel(); components.Add(robotPanel);
            robotPanel.Dock = DockStyle.Fill;
            weaponPanel = new WeaponPanel(); components.Add(weaponPanel);
            weaponPanel.Dock = DockStyle.Fill;
            TextureTabPage.Controls.Add(texturePanel);
            VClipTabPage.Controls.Add(vclipPanel);
            EffectsTabPage.Controls.Add(eclipPanel);
            RobotTabPage.Controls.Add(robotPanel);
            WeaponTabPage.Controls.Add(weaponPanel);

            if (!noPMView)
                this.ModelTabPage.Controls.Add(this.glControl1);
            datafile = data;
            this.host = host;
            modelRenderer = new ModelRenderer(datafile, host.DefaultPigFile);
            currentFilename = filename;
            this.Text = string.Format("{0} - HAM Editor", currentFilename);
        }

        private void HAMEditor2_Load(object sender, EventArgs e)
        {
            isLocked = true;
            ElementListInit();
            FillOutCurrentPanel(0, 0);
            isLocked = false;
        }


        //---------------------------------------------------------------------
        // UI MANAGEMENT AND PANEL MANAGEMENT
        //---------------------------------------------------------------------

        private void SetElementControl(bool status, bool listable)
        {
            btnInsertElem.Enabled = btnDeleteElem.Enabled = status;
            btnList.Enabled = listable;
        }

        private void ElementListInit()
        {
            //Hacks aaaa
            vclipPanel.Stop();
            eclipPanel.Stop();
            switch (EditorTabs.SelectedIndex)
            {
                case 0:
                    nudElementNum.Maximum = datafile.TMapInfo.Count - 1;
                    InitTexturePanel();
                    break;
                case 1:
                    nudElementNum.Maximum = datafile.VClips.Count - 1;
                    InitVClipPanel();
                    break;
                case 2:
                    nudElementNum.Maximum = datafile.EClips.Count - 1;
                    InitEClipPanel();
                    break;
                case 3:
                    nudElementNum.Maximum = datafile.WClips.Count - 1;
                    InitWallPanel();
                    break;
                case 4:
                    nudElementNum.Maximum = datafile.Robots.Count - 1;
                    InitRobotPanel();
                    break;
                case 5:
                    nudElementNum.Maximum = datafile.Weapons.Count - 1;
                    InitWeaponPanel();
                    break;
                case 6:
                    nudElementNum.Maximum = datafile.Models.Count - 1;
                    InitModelPanel();
                    break;
                case 7:
                    nudElementNum.Maximum = datafile.Sounds.Count - 1;
                    InitSoundPanel();
                    break;
                case 8:
                    nudElementNum.Maximum = datafile.Reactors.Count - 1;
                    InitReactorPanel();
                    break;
                case 9:
                    nudElementNum.Maximum = datafile.Powerups.Count - 1;
                    InitPowerupPanel();
                    break;
                case 10:
                    nudElementNum.Maximum = 0;
                    SetElementControl(false, false);
                    break;
                case 11:
                    nudElementNum.Maximum = datafile.Gauges.Count - 1;
                    SetElementControl(false, false);
                    break;
                case 12:
                    nudElementNum.Maximum = datafile.Cockpits.Count - 1;
                    SetElementControl(true, false);
                    break;
                case 13:
                    nudElementNum.Maximum = 2619;
                    SetElementControl(false, false);
                    break;
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            isLocked = true;
            ElementListInit();
            nudElementNum.Value = 0;
            FillOutCurrentPanel(EditorTabs.SelectedIndex, 0);
            isLocked = false;
        }

        private void nudElementNum_ValueChanged(object sender, EventArgs e)
        {
            if (!isLocked)
            {
                isLocked = true;
                FillOutCurrentPanel(EditorTabs.SelectedIndex, (int)nudElementNum.Value);
                isLocked = false;
            }
        }

        private void FillOutCurrentPanel(int id, int val)
        {
            switch (id)
            {
                case 0:
                    UpdateTexturePanel(val);
                    break;
                case 1:
                    UpdateVClipPanel(val);
                    break;
                case 2:
                    UpdateEClipPanel(val);
                    break;
                case 3:
                    UpdateWClipPanel(val);
                    break;
                case 4:
                    UpdateRobotPanel(val);
                    break;
                case 5:
                    UpdateWeaponPanel(val);
                    break;
                case 6:
                    UpdateModelPanel(val);
                    break;
                case 7:
                    UpdateSoundPanel(val);
                    break;
                case 8:
                    Reactor reactor = datafile.Reactors[val];
                    cbReactorModel.SelectedIndex = reactor.model_id;
                    break;
                case 9:
                    UpdatePowerupPanel(val);
                    break;
                case 10:
                    UpdateShipPanel();
                    break;
                case 11:
                    UpdateGaguePanel(val);
                    break;
                case 12:
                    UpdateCockpitPanel(val);
                    break;
                case 13:
                    UpdateXLATPanel(val);
                    break;
            }
        }

        private void InsertElem_Click(object sender, EventArgs e)
        {
            HAMType type = typeTable[EditorTabs.SelectedIndex];
            int newID = datafile.AddElement(type);
            if (newID != -1)
            {
                //Update the maximum of the numeric up/down control and ensure that any comboboxes that need to be regenerated for the current element are
                ElementListInit();
                nudElementNum.Value = newID;
            }
        }

        private void DeleteElem_Click(object sender, EventArgs e)
        {
            HAMType type = typeTable[EditorTabs.SelectedIndex];
            int returnv = datafile.DeleteElement(type, ElementNumber);
            if (returnv >= 0)
            {
                //Update the maximum of the numeric up/down control and ensure that any comboboxes that need to be regenerated for the current element are
                ElementListInit();
                isLocked = true;
                if (nudElementNum.Value >= returnv)
                    nudElementNum.Value = returnv - 1;
                FillOutCurrentPanel(EditorTabs.SelectedIndex, ElementNumber);
                isLocked = false;
            }
            else
            {
                statusBar1.Text = "Can't delete last element: It is being referenced by other elements";
            }
        }

        private void ListElem_Click(object sender, EventArgs e)
        {
            HAMType type = typeTable[PageNumber];
            ElementList elementList = new ElementList(datafile, type);
            if (elementList.ShowDialog() == DialogResult.OK)
            {
                if (elementList.ElementNumber != -1)
                {
                    nudElementNum.Value = elementList.ElementNumber;
                }
            }
            elementList.Dispose();
        }

        private void ElemName_TextChanged(object sender, EventArgs e)
        {
            if (isLocked) return;
            datafile.UpdateName(typeTable[EditorTabs.SelectedIndex], ElementNumber, txtElemName.Text);
        }

        //---------------------------------------------------------------------
        // PANEL INITALIZATION
        // The contents of these boxes can change if the amount of elements is changed,
        // so they must be updated each time before display. 
        //---------------------------------------------------------------------

        private void InitTexturePanel()
        {
            SetElementControl(true, false);
            texturePanel.Init(datafile.EClipNames);
        }

        private void InitVClipPanel()
        {
            SetElementControl(true, true);
            vclipPanel.Init(datafile.SoundNames);
        }

        private void InitEClipPanel()
        {
            SetElementControl(true, true);
            eclipPanel.Init(datafile.VClipNames, datafile.EClipNames, datafile.SoundNames);
        }

        private void InitWallPanel()
        {
            SetElementControl(true, false);
            cbWallCloseSound.Items.Clear(); cbWallCloseSound.Items.Add("None");
            cbWallOpenSound.Items.Clear(); cbWallOpenSound.Items.Add("None");
            cbWallOpenSound.Items.AddRange(datafile.SoundNames.ToArray());
            cbWallCloseSound.Items.AddRange(datafile.SoundNames.ToArray());
        }

        private void InitWeaponPanel()
        {
            SetElementControl(true, true);
            weaponPanel.Init(datafile.SoundNames, datafile.VClipNames, datafile.WeaponNames, datafile.ModelNames, host.DefaultPigFile);
        }

        private void InitRobotPanel()
        {
            SetElementControl(true, true);
            robotPanel.Init(datafile.VClipNames, datafile.SoundNames, datafile.RobotNames, datafile.WeaponNames, datafile.PowerupNames, datafile.ModelNames);
        }

        private void InitSoundPanel()
        {
            SetElementControl(false, true);
            cbSoundSNDid.Items.Clear();
            cbLowMemSound.Items.Clear();
            cbSoundSNDid.Items.Add("None");
            cbLowMemSound.Items.Add("None");
            cbLowMemSound.Items.AddRange(datafile.SoundNames.ToArray());
            foreach (SoundData sound in host.DefaultSoundFile.sounds)
                cbSoundSNDid.Items.Add(sound.name);
        }

        private void InitPowerupPanel()
        {
            SetElementControl(true, true);
            cbPowerupPickupSound.Items.Clear();
            cbPowerupSprite.Items.Clear();
            cbPowerupPickupSound.Items.AddRange(datafile.SoundNames.ToArray());
            cbPowerupSprite.Items.AddRange(datafile.VClipNames.ToArray());
        }

        private void InitModelPanel()
        {
            SetElementControl(true, true);
            cbModelLowDetail.Items.Clear(); cbModelLowDetail.Items.Add("None");
            cbModelDyingModel.Items.Clear(); cbModelDyingModel.Items.Add("None");
            cbModelDeadModel.Items.Clear(); cbModelDeadModel.Items.Add("None");
            cbModelLowDetail.Items.AddRange(datafile.ModelNames.ToArray());
            cbModelDyingModel.Items.AddRange(datafile.ModelNames.ToArray());
            cbModelDeadModel.Items.AddRange(datafile.ModelNames.ToArray());
        }

        private void InitReactorPanel()
        {
            SetElementControl(true, true);
            cbReactorModel.Items.Clear();
            cbReactorModel.Items.AddRange(datafile.ModelNames.ToArray());
        }

        //---------------------------------------------------------------------
        // PANEL CREATORS
        //---------------------------------------------------------------------

        private void UpdatePictureBox(Image img, PictureBox pictureBox)
        {
            if (pictureBox.Image != null)
            {
                Image oldImg = pictureBox.Image;
                pictureBox.Image = null;
                oldImg.Dispose();
            }
            pictureBox.Image = img;
        }

        public void UpdateTexturePanel(int num)
        {
            TMAPInfo texture = datafile.TMapInfo[num];
            //TODO: This may need to take a reference to the EditorHAMFile isntead
            texturePanel.Update(datafile.BaseFile, datafile.piggyFile, num, texture);
        }

        public void UpdateGaguePanel(int num)
        {
            ushort gague = datafile.Gauges[num];
            ushort hiresgague = datafile.GaugesHires[num];

            txtGagueLores.Text = gague.ToString();
            txtGagueHires.Text = hiresgague.ToString();

            if (pbGagueLores.Image != null)
            {
                Bitmap temp = (Bitmap)pbGagueLores.Image;
                pbGagueLores.Image = null;
                temp.Dispose();
            }
            pbGagueLores.Image = PiggyBitmapConverter.GetBitmap(datafile.piggyFile, gague);

            if (pbGagueHires.Image != null)
            {
                Bitmap temp = (Bitmap)pbGagueHires.Image;
                pbGagueHires.Image = null;
                temp.Dispose();
            }
            pbGagueHires.Image = PiggyBitmapConverter.GetBitmap(datafile.piggyFile, hiresgague);
        }

        public void UpdateVClipPanel(int num)
        {
            vclipPanel.Stop();
            VClip clip = datafile.VClips[num];
            vclipPanel.Update(clip, datafile.piggyFile);
            txtElemName.Text = datafile.VClipNames[num];
        }

        public void UpdateEClipPanel(int num)
        {
            eclipPanel.Stop();
            EClip clip = datafile.EClips[num];
            eclipPanel.Update(clip, datafile.piggyFile);
            txtElemName.Text = datafile.EClipNames[num];
        }

        public void UpdateWClipPanel(int num)
        {
            WClip animation = datafile.WClips[num];
            txtWallTotalTime.Text = animation.play_time.ToString();
            cbWallOpenSound.SelectedIndex = animation.open_sound + 1;
            cbWallCloseSound.SelectedIndex = animation.close_sound + 1;
            txtWallFilename.Text = new string(animation.filename);
            txtWallFrames.Text = animation.num_frames.ToString();

            cbWallExplodeOpen.Checked = (animation.flags & 1) != 0;
            cbWallShootable.Checked = (animation.flags & 2) != 0;
            cbWallOnPrimaryTMAP.Checked = (animation.flags & 4) != 0;
            cbWallHidden.Checked = (animation.flags & 8) != 0;

            nudWFrame.Value = 0;
            UpdateWallFrame(0);

            txtElemName.Text = "<unnamed>";
        }

        private void nudWFrame_ValueChanged(object sender, EventArgs e)
        {
            if (!isLocked)
            {
                isLocked = true;
                UpdateWallFrame((int)nudWFrame.Value);
                isLocked = false;
            }
        }

        public void UpdateRobotPanel(int num)
        {
            Robot robot = datafile.Robots[num];
            robotPanel.Update(robot);
            txtElemName.Text = datafile.RobotNames[num];
        }

        public void UpdateWeaponPanel(int num)
        {
            Weapon weapon = datafile.Weapons[num];
            weaponPanel.Update(weapon);
            txtElemName.Text = datafile.WeaponNames[num];
        }

        private void UpdateModelPanel(int num)
        {
            Polymodel model = datafile.Models[num];
            txtModelNumModels.Text = model.n_models.ToString();
            txtModelDataSize.Text = model.model_data_size.ToString();
            txtModelRadius.Text = model.rad.ToString();
            txtModelTextureCount.Text = model.n_textures.ToString();
            cbModelLowDetail.SelectedIndex = model.simpler_model;
            cbModelDyingModel.SelectedIndex = model.DyingModelnum + 1;
            cbModelDeadModel.SelectedIndex = model.DeadModelnum + 1;

            txtModelMinX.Text = model.mins.x.ToString();
            txtModelMinY.Text = model.mins.y.ToString();
            txtModelMinZ.Text = model.mins.z.ToString();
            txtModelMaxX.Text = model.maxs.x.ToString();
            txtModelMaxY.Text = model.maxs.y.ToString();
            txtModelMaxZ.Text = model.maxs.z.ToString();

            txtElemName.Text = datafile.ModelNames[num];
            if (!noPMView)
            {
                Polymodel mainmodel = datafile.Models[(int)nudElementNum.Value];
                modelRenderer.SetModel(mainmodel);
                glControl1.Invalidate();
            }
        }

        private void UpdatePowerupPanel(int num)
        {
            Powerup powerup = datafile.Powerups[num];
            cbPowerupPickupSound.SelectedIndex = powerup.hit_sound;
            cbPowerupSprite.SelectedIndex = powerup.vclip_num;
            txtPowerupSize.Text = powerup.size.ToString();
            txtPowerupLight.Text = powerup.light.ToString();
            txtElemName.Text = datafile.PowerupNames[num];
        }

        private void UpdateShipPanel()
        {
            Ship ship = datafile.PlayerShip;
            cbPlayerExplosion.Items.Clear();
            for (int i = 0; i < datafile.VClips.Count; i++)
                cbPlayerExplosion.Items.Add(datafile.VClipNames[i]);
            cbPlayerModel.Items.Clear(); cbMarkerModel.Items.Clear();
            for (int i = 0; i < datafile.Models.Count; i++)
            {
                cbPlayerModel.Items.Add(datafile.ModelNames[i]);
                cbMarkerModel.Items.Add(datafile.ModelNames[i]);
            }

            txtShipBrakes.Text = ship.brakes.ToString();
            txtShipDrag.Text = ship.drag.ToString();
            txtShipMass.Text = ship.mass.ToString();
            txtShipMaxRotThrust.Text = ship.max_rotthrust.ToString();
            txtShipRevThrust.Text = ship.reverse_thrust.ToString();
            txtShipThrust.Text = ship.max_thrust.ToString();
            txtShipWiggle.Text = ship.wiggle.ToString();
            nudShipTextures.Value = nudShipTextures.Minimum;
            UpdateShipTextures(0);
            //This can thereoetically null, but it never will except on deformed data that descent itself probably wouldn't like
            cbPlayerExplosion.SelectedIndex = ship.expl_vclip_num;
            cbMarkerModel.SelectedIndex = ship.markerModel;
            cbPlayerModel.SelectedIndex = ship.model_num;

            txtElemName.Text = "Ship";
        }

        private void nudShipTextures_ValueChanged(object sender, EventArgs e)
        {
            UpdateShipTextures((int)nudShipTextures.Value - 2);
        }

        private void UpdateShipTextures(int id)
        {
            UpdatePictureBox(PiggyBitmapConverter.GetBitmap(datafile.piggyFile, datafile.multiplayerBitmaps[id * 2]), pbWeaponTexture);
            UpdatePictureBox(PiggyBitmapConverter.GetBitmap(datafile.piggyFile, datafile.multiplayerBitmaps[id * 2 + 1]), pbWingTex);
        }

        private void UpdateWallFrame(int frame)
        {
            WClip animation = datafile.WClips[(int)nudElementNum.Value];
            txtWallCurrentFrame.Text = animation.frames[frame].ToString();

            if (pbWallAnimPreview.Image != null)
            {
                Bitmap temp = (Bitmap)pbWallAnimPreview.Image;
                pbWallAnimPreview.Image = null;
                temp.Dispose();
            }
            pbWallAnimPreview.Image = PiggyBitmapConverter.GetBitmap(datafile.piggyFile, datafile.Textures[animation.frames[frame]]);
        }

        private void UpdateXLATPanel(int num)
        {
            ushort dst = datafile.BitmapXLATData[num];
            txtXLATDest.Text = dst.ToString();

            if (pbBitmapSrc.Image != null)
            {
                Bitmap temp = (Bitmap)pbBitmapSrc.Image;
                pbBitmapSrc.Image = null;
                temp.Dispose();
            }
            pbBitmapSrc.Image = PiggyBitmapConverter.GetBitmap(datafile.piggyFile, num);

            if (pbBitmapDest.Image != null)
            {
                Bitmap temp = (Bitmap)pbBitmapDest.Image;
                pbBitmapDest.Image = null;
                temp.Dispose();
            }
            pbBitmapDest.Image = PiggyBitmapConverter.GetBitmap(datafile.piggyFile, dst);
        }

        private void UpdateCockpitPanel(int num)
        {
            ushort cockpit = datafile.Cockpits[num];
            txtCockpitID.Text = cockpit.ToString();

            if (pbCockpit.Image != null)
            {
                Bitmap temp = (Bitmap)pbCockpit.Image;
                pbCockpit.Image = null;
                temp.Dispose();
            }
            pbCockpit.Image = PiggyBitmapConverter.GetBitmap(datafile.piggyFile, cockpit);
        }

        private void UpdateSoundPanel(int num)
        {
            //txtSoundID.Text = datafile.Sounds[num].ToString();
            if (datafile.Sounds[num] == 255)
                cbSoundSNDid.SelectedIndex = 0;
            else
                cbSoundSNDid.SelectedIndex = datafile.Sounds[num] + 1;
            if (datafile.AltSounds[num] == 255)
                cbLowMemSound.SelectedIndex = 0;
            else
                cbLowMemSound.SelectedIndex = datafile.AltSounds[num] + 1;
            txtElemName.Text = datafile.SoundNames[num];
        }

        //---------------------------------------------------------------------
        // UTILITIES
        //---------------------------------------------------------------------
        

        public double GetFloatFromFixed88(short fixedvalue)
        {
            return (double)fixedvalue / 256D;
        }

        //---------------------------------------------------------------------
        // SHARED UPDATORS
        //---------------------------------------------------------------------

        private void RemapSingleImage_Click(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            ImageSelector selector = new ImageSelector(host.DefaultPigFile, false);
            if (selector.ShowDialog() == DialogResult.OK)
            {
                isLocked = true;
                int value = selector.Selection;
                int shipvalue = 0;
                switch (button.Tag)
                {
                    case "5":
                        shipvalue = (int)nudShipTextures.Value - 2;
                        datafile.multiplayerBitmaps[shipvalue * 2 + 1] = (ushort)(value);
                        UpdateShipTextures(shipvalue);
                        break;
                    case "6":
                        shipvalue = (int)nudShipTextures.Value - 2;
                        datafile.multiplayerBitmaps[shipvalue * 2] = (ushort)(value);
                        UpdateShipTextures(shipvalue);
                        break;
                    case "7":
                        txtGagueLores.Text = (value).ToString();
                        datafile.Gauges[ElementNumber] = (ushort)(value);
                        UpdatePictureBox(PiggyBitmapConverter.GetBitmap(datafile.piggyFile, value), pbGagueLores);
                        break;
                    case "8":
                        txtGagueHires.Text = (value).ToString();
                        datafile.GaugesHires[ElementNumber] = (ushort)(value);
                        UpdatePictureBox(PiggyBitmapConverter.GetBitmap(datafile.piggyFile, value), pbGagueHires);
                        break;
                    case "9":
                        txtCockpitID.Text = (value).ToString();
                        datafile.Cockpits[ElementNumber] = (ushort)(value);
                        UpdatePictureBox(PiggyBitmapConverter.GetBitmap(datafile.piggyFile, value), pbCockpit);
                        break;
                }
                isLocked = false;
            }
        }

        //---------------------------------------------------------------------
        // WALL UPDATORS
        //---------------------------------------------------------------------

        private void WallComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isLocked)
                return;
            ComboBox comboBox = (ComboBox)sender;
            WClip clip = datafile.WClips[ElementNumber];
            switch (comboBox.Tag)
            {
                case "1":
                    clip.open_sound = (short)(comboBox.SelectedIndex - 1);
                    break;
                case "2":
                    clip.close_sound = (short)(comboBox.SelectedIndex - 1);
                    break;
            }
        }

        private void WallFlag_CheckedChanged(object sender, EventArgs e)
        {
            if (isLocked)
                return;
            CheckBox checkBox = (CheckBox)sender;
            WClip clip = datafile.WClips[ElementNumber];
            int bit = int.Parse((string)checkBox.Tag);
            if ((clip.flags & bit) != 0)
                clip.flags -= (short)bit;
            else
                clip.flags |= (short)bit;
        }

        private void WallProperty_TextChanged(object sender, EventArgs e)
        {
            if (isLocked)
                return;
            TextBox textBox = (TextBox)sender;
            WClip clip = datafile.WClips[ElementNumber];
            double value;
            if (double.TryParse(textBox.Text, out value))
            {
                switch (textBox.Tag)
                {
                    case "1":
                        int totalTimeFix = (int)(value * 65536);
                        clip.play_time = Fix.FromRawValue(totalTimeFix);
                        break;
                    case "2":
                        clip.filename = textBox.Text.ToCharArray();
                        break;
                }
            }
        }

        //---------------------------------------------------------------------
        // SOUND UPDATORS
        //---------------------------------------------------------------------

        private void cbSoundSNDid_SelectedIndexChanged(object sender, EventArgs e)
        {
            int value = cbSoundSNDid.SelectedIndex;
            if (value == 0)
                datafile.Sounds[ElementNumber] = 255;
            else
                datafile.Sounds[ElementNumber] = (byte)(value - 1);
        }

        private void cbLowMemSound_SelectedIndexChanged(object sender, EventArgs e)
        {
            int value = cbLowMemSound.SelectedIndex;
            if (value == 0)
                datafile.AltSounds[ElementNumber] = 255;
            else
                datafile.AltSounds[ElementNumber] = (byte)(value - 1);
        }

        //---------------------------------------------------------------------
        // MODEL UPDATORS
        //---------------------------------------------------------------------

        private void btnImportModel_Click(object sender, EventArgs e)
        {
            ImportModel(datafile.Models[ElementNumber]);
        }

        private void btnExportModel_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "Parallax Object Files|*.pof";
            saveFileDialog1.FileName = string.Format("model_{0}.pof", ElementNumber);
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                BinaryWriter bw = new BinaryWriter(File.Open(saveFileDialog1.FileName, FileMode.Create));
                POFWriter.SerializePolymodel(bw, datafile.Models[ElementNumber], short.Parse(StandardUI.options.GetOption("PMVersion", "8")));
                bw.Close();
                bw.Dispose();
            }
        }

        private void ImportModel(Polymodel original)
        {
            int oldNumTextures = original.n_textures;

            List<string> newTextureNames = new List<string>();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string traceto = "";
                if (bool.Parse(StandardUI.options.GetOption("TraceModels", bool.FalseString)))
                {
                    string bareFilename = Path.GetFileName(openFileDialog1.FileName);
                    traceto = StandardUI.options.GetOption("TraceDir", ".") + Path.DirectorySeparatorChar + Path.ChangeExtension(bareFilename, "txt");
                }

                Polymodel model = POFReader.ReadPOFFile(openFileDialog1.FileName, traceto);
                model.ExpandSubmodels();
                //int numTextures = model.n_textures;
                //TODO: does this work?
                //datafile.ReplaceModel(ElementNumber, model);
                datafile.Models[ElementNumber] = model;
                UpdateModelPanel(ElementNumber);
            }
        }

        private void ModelComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isLocked)
                return;
            Polymodel model = datafile.Models[ElementNumber];
            ComboBox comboBox = (ComboBox)sender;
            switch (comboBox.Tag)
            {
                case "1":
                    model.simpler_model = (byte)comboBox.SelectedIndex;
                    break;
                case "2":
                    model.DyingModelnum = comboBox.SelectedIndex - 1;
                    break;
                case "3":
                    model.DeadModelnum = comboBox.SelectedIndex - 1;
                    break;
            }
        }

        //---------------------------------------------------------------------
        // POWERUP UPDATORS
        //---------------------------------------------------------------------

        private void PowerupComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isLocked)
                return;
            Powerup powerup = datafile.Powerups[ElementNumber];
            ComboBox comboBox = (ComboBox)sender;
            switch (comboBox.Tag)
            {
                case "1":
                    powerup.vclip_num = comboBox.SelectedIndex;
                    break;
                case "2":
                    powerup.hit_sound = comboBox.SelectedIndex;
                    break;
            }
        }

        private void PowerupTextBox_TextChanged(object sender, EventArgs e)
        {
            if (isLocked)
                return;
            Powerup powerup = datafile.Powerups[ElementNumber];
            TextBox textBox = (TextBox)sender;
            double value;
            if (double.TryParse(textBox.Text, out value))
            {
                switch (textBox.Tag)
                {
                    case "3":
                        powerup.size = Fix.FromRawValue((int)(value * 65536.0));
                        break;
                    case "4":
                        powerup.light = Fix.FromRawValue((int)(value * 65536.0));
                        break;
                }
            }
        }

        //---------------------------------------------------------------------
        // REACTOR UPDATOR
        //---------------------------------------------------------------------

        private void cbReactorModel_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isLocked) return;
            Reactor reactor = datafile.Reactors[ElementNumber];
            reactor.model_id = cbReactorModel.SelectedIndex;
        }

        //---------------------------------------------------------------------
        // SHIP UPDATORS
        //---------------------------------------------------------------------

        private void ShipProperty_TextChanged(object sender, EventArgs e)
        {
            if (isLocked)
            {
                return;
            }
            TextBox textBox = (TextBox)sender;
            int input;
            int tagValue = int.Parse((String)textBox.Tag);
            Ship ship = datafile.PlayerShip;
            if (tagValue == 1 || tagValue == 2)
            {
                if (int.TryParse(textBox.Text, out input))
                {
                    ship.UpdateShip(tagValue, input);
                }
            }
            else
            {
                double temp;
                if (double.TryParse(textBox.Text, out temp))
                {
                    input = (int)(temp * 65536);
                    ship.UpdateShip(tagValue, input);
                }
            }
        }

        private void ShipComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isLocked) return;
            ComboBox comboBox = (ComboBox)sender;
            switch (comboBox.Tag)
            {
                case "0":
                    datafile.PlayerShip.model_num = (comboBox.SelectedIndex);
                    break;
                case "1":
                    datafile.PlayerShip.expl_vclip_num = (comboBox.SelectedIndex);
                    break;
                case "2":
                    datafile.PlayerShip.markerModel = (comboBox.SelectedIndex);
                    break;
            }
        }

        //---------------------------------------------------------------------
        // GAUGE UPDATORS
        //---------------------------------------------------------------------

        private void GaugeProperty_TextChanged(object sender, EventArgs e)
        {
            if (isLocked) return;
            ushort value;
            TextBox textBox = (TextBox)sender;
            if (ushort.TryParse(textBox.Text, out value))
            {
                switch (textBox.Tag)
                {
                    case "1":
                        datafile.Gauges[ElementNumber] = value;
                        UpdatePictureBox(PiggyBitmapConverter.GetBitmap(datafile.piggyFile, value - 1), pbGagueLores);
                        break;
                    case "2":
                        datafile.GaugesHires[ElementNumber] = value;
                        UpdatePictureBox(PiggyBitmapConverter.GetBitmap(datafile.piggyFile, value - 1), pbGagueHires);
                        break;
                }
            }
        }

        //---------------------------------------------------------------------
        // SPECIAL FUNCTIONS
        //---------------------------------------------------------------------

        private void menuItem5_Click(object sender, EventArgs e)
        {
            ElementCopy copyForm = new ElementCopy();

            if (copyForm.ShowDialog() == DialogResult.OK)
            {
                int result = 1;
                int elementNumber = copyForm.elementValue;
                result = datafile.CopyElement(typeTable[EditorTabs.SelectedIndex], (int)nudElementNum.Value, elementNumber);
                if (result == -1)
                    MessageBox.Show("Cannot copy to an element that doesn't exist!");
                else if (result == 1)
                    MessageBox.Show("Element cannot be copied!");
            }
        }

        private void menuItem6_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "Bitmap table files|*.TBL";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                StreamWriter sw = new StreamWriter(File.Open(saveFileDialog1.FileName, FileMode.Create));
                sw.Write(BitmapTableFile.GenerateBitmapsTable(datafile, host.DefaultPigFile, host.DefaultSoundFile));
                sw.Flush();
                sw.Close();
                sw.Dispose();
                string modelPath = Path.GetDirectoryName(saveFileDialog1.FileName);
                Polymodel model;
                if (datafile.Models.Count >= 160)
                {
                    //for (int i = 0; i < datafile.PolygonModels.Count; i++)
                    foreach (int i in BitmapTableFile.pofIndicies)
                    {
                        model = datafile.Models[i];
                        BinaryWriter bw = new BinaryWriter(File.Open(String.Format("{0}{1}{2}", modelPath, Path.DirectorySeparatorChar, BitmapTableFile.pofNames[i]), FileMode.Create));
                        POFWriter.SerializePolymodel(bw, model, 8);
                        bw.Close();
                        bw.Dispose();
                    }
                }
            }
        }

        private void mnuFindRefs_Click(object sender, EventArgs e)
        {
            Console.WriteLine("mnuFindRefs_Click: STUB");
        }

        //---------------------------------------------------------------------
        // 3D VIEWER UTILITIES
        //---------------------------------------------------------------------

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            if (noPMView) return;
            SetupViewport();
            glControl1.Invalidate();
        }

        private void glControl1_Load(object sender, EventArgs e)
        {
            if (noPMView) return;
            if (!glContextCreated)
            {
                glControl1.Location = glControlStandin.Location;
                glControl1.Size = glControlStandin.Size;
                glControlStandin.Visible = false;
            }
            glContextCreated = true;
            modelRenderer.Init();
            SetupViewport();
        }

        private void glControl1_Paint(object sender, PaintEventArgs e)
        {
            if (noPMView) return;
            if (!glContextCreated)
                return; //can't do anything with this, heh
            glControl1.MakeCurrent();

            modelRenderer.Pitch = (trackBar3.Value - 8) * -22.5d;
            modelRenderer.Angle = (trackBar1.Value - 8) * -22.5d;
            modelRenderer.ShowBBs = chkShowBBs.Checked;
            modelRenderer.ShowNormals = chkNorm.Checked;
            modelRenderer.Wireframe = chkWireframe.Checked;
            modelRenderer.ShowRadius = chkRadius.Checked;
            modelRenderer.EmulateSoftware = chkSoftwareOverdraw.Checked;

            modelRenderer.Draw();
            glControl1.SwapBuffers();
        }

        private void SetupViewport()
        {
            if (noPMView) return;
            modelRenderer.SetupViewport(glControl1.Width, glControl1.Height, trackBar2.Value * 0.5d + 4.0d);
        }

        private void HAMEditor2_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (glContextCreated)
            {
            }
        }

        private void PMCheckBox_CheckChanged(object sender, EventArgs e)
        {
            glControl1.Invalidate();
        }

        //---------------------------------------------------------------------
        // BASIC MENU FUNCTIONS
        //---------------------------------------------------------------------

        /// <summary>
        /// Helper function to save a HAM file, with lots of dumb error handling that doesn't work probably.
        /// </summary>
        /// <param name="filename">The filename to save the file to.</param>
        private void SaveHAMFile(string filename)
        {
            //Get rid of any old backups
            try
            {
                File.Delete(Path.ChangeExtension(filename, "BAK"));
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { } //Discover this with our face to avoid a 1/1000000 race condition
            catch (UnauthorizedAccessException exc)
            {
                host.AppendConsole(String.Format("Cannot delete old backup file {0}: Permission denied.\r\nMsg: {1}\r\n", Path.ChangeExtension(filename, "BAK"), exc.Message));
            }
            catch (IOException exc)
            {
                host.AppendConsole(String.Format("Cannot delete old backup file {0}: IO error occurred.\r\nMsg: {1}\r\n", Path.ChangeExtension(filename, "BAK"), exc.Message));
            }
            //Move the current file into the backup slot
            try
            {
                File.Move(filename, Path.ChangeExtension(filename, "BAK"));
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { } //Discover this with our face to avoid a 1/1000000 race condition
            catch (UnauthorizedAccessException exc)
            {
                host.AppendConsole(String.Format("Cannot move old HAM file {0}: Permission denied.\r\nMsg: {1}\r\n", filename, exc.Message));
            }
            catch (IOException exc)
            {
                host.AppendConsole(String.Format("Cannot move old HAM file {0}: IO error occurred.\r\nMsg: {1}\r\n", filename, exc.Message));
            }
            //Finally write the new file
            FileStream stream;
            try
            {
                stream = File.Open(filename, FileMode.Create);
                datafile.Write(stream, false);
                stream.Close();
                stream.Dispose();
            }
            catch (Exception exc)
            {
                FileUtilities.FileExceptionHandler(exc, "HAM file");
            }
        }

        private void mnuSave_Click(object sender, EventArgs e)
        {
            bool compatObjBitmaps = (StandardUI.options.GetOption("CompatObjBitmaps", bool.FalseString) == bool.TrueString);
            SaveHAMFile(currentFilename);
        }

        private void mnuSaveAs_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "HAM Files|*.HAM";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                bool compatObjBitmaps = (StandardUI.options.GetOption("CompatObjBitmaps", bool.FalseString) == bool.TrueString);
                SaveHAMFile(saveFileDialog1.FileName);
            }
            this.Text = string.Format("{0} - HAM Editor", currentFilename);
        }
    }
}
