using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Globalization;
using System.IO;

namespace MiniPaint
{
    public partial class MiniPaint : Form
    {
        Color penColor = Color.Black;
        float penSize = 2;

        Point lastPoint;
        Point currentPoint;
        Rectangle rect;

        bool brushActive = false;
        bool rectActive = false;
        bool ellipseActive = false;
        bool drawMode = false;

        Bitmap image;

        int? selectedColorIndexBackup;
        int? selectedSizeBackup;

        public MiniPaint()
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
            InitializeComponent();
            InitializeForm();
            aLanguage_Click(this, new EventArgs());
        }

        private void InitializeForm()
        {
            chosenColor.Image = new Bitmap(20, 20);

            IniatializeGroupBox();
            InitializeImage();

            if (selectedSizeBackup.HasValue)
                sizeSelector.SelectedIndex = selectedSizeBackup.Value;
            else
                sizeSelector.SelectedIndex = 1;

            brushButton.Checked = brushActive;
            rectButton.Checked = rectActive;
            ellipseButton.Checked = ellipseActive;
        }

        private void ChooseColor(Color c)
        {
            using (Graphics g = Graphics.FromImage(chosenColor.Image))
            using (Brush b = new SolidBrush(c))
            {
                g.FillRectangle(b, new Rectangle(0, 0, 20, 20));
                Refresh();
            }
        }

        private void InitializeImage()
        {
            if (image == null)
            {
                image = new Bitmap(pictureBox.Width, pictureBox.Height);
                using (Graphics g = Graphics.FromImage(image))
                    g.Clear(Color.White);
            }
            pictureBox.Image = image;
        }

        void IniatializeGroupBox()
        {
            foreach (KnownColor x in Enum.GetValues(typeof(KnownColor)))
            {
                PictureBox box = new PictureBox();
                box.Size = new Size(25, 25);
                colorSelector.Controls.Add(box);
                box.BackColor = Color.FromKnownColor(x);
                box.Click += ChoosePenColor;
            }
            if (selectedColorIndexBackup.HasValue)
            {
                ChoosePenColor(colorSelector.Controls[selectedColorIndexBackup.Value], new EventArgs());
            }
            else
            {
                ChooseColor(Color.Black);
            }
        }

        void ChoosePenColor(object sender, EventArgs e)
        {
            PictureBox box = (sender as PictureBox);

            if (selectedColorIndexBackup.HasValue)
            {
                (colorSelector.Controls[selectedColorIndexBackup.Value] as PictureBox).Image?.Dispose();
                (colorSelector.Controls[selectedColorIndexBackup.Value] as PictureBox).Image = null;
            }

            selectedColorIndexBackup = colorSelector.Controls.IndexOf(box);
            penColor = box.BackColor;
            ChooseColor(penColor);

            box.Image = new Bitmap(box.Width, box.Height);

            using (Pen p = new Pen(Color.FromArgb(255 - penColor.R, 255 - penColor.B, 255 - penColor.B), 4))
            using (Graphics g = Graphics.FromImage(box.Image))
            {
                p.DashPattern = new float[] { 1F, 1F };
                g.DrawRectangle(p, new Rectangle(0, 0, (sender as PictureBox).Width, (sender as PictureBox).Height));
            }
        }

        private void pictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (!brushActive && !rectActive && !ellipseActive) return;

            if (e.Button == MouseButtons.Left)
            {
                lastPoint = e.Location;
                BeginDrawing();
            }

            if (e.Button == MouseButtons.Right)
            {
                CancelDrawing();
            }
        }
        
        private void pictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                CancelDrawing();
            }
        }

        private void BeginDrawing()
        {
            drawMode = true;
        }

        private void CancelDrawing()
        {
            pictureBox.Invalidate();
            drawMode = false;
            if (rectActive || ellipseActive)
            {
                using (Graphics g = Graphics.FromImage(image))
                using (Pen pen = new Pen(penColor, penSize))
                {
                    g.SmoothingMode =
                        System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    if (rectActive)
                        g.DrawRectangle(pen, rect);
                    if (ellipseActive)
                        g.DrawEllipse(pen, rect);
                }
            }
        }

        private void pictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (!drawMode) return;

            currentPoint = e.Location; 

            if (brushActive)
            {
                using (Pen pen = new Pen(penColor, penSize))
                using (Graphics g = Graphics.FromImage(image))
                {
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    g.SmoothingMode =
                        System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.DrawLine(pen, lastPoint, currentPoint);
                    lastPoint = currentPoint;
                }
            }
            else if (rectActive || ellipseActive)
            {
                int x = Math.Min(lastPoint.X, e.X);
                int y = Math.Min(lastPoint.Y, e.Y);
                
                int width = Math.Max(lastPoint.X, e.X) - Math.Min(lastPoint.X, e.X);
                
                int height = Math.Max(lastPoint.Y, e.Y) - Math.Min(lastPoint.Y, e.Y);
                rect = new Rectangle(x, y, width, height);
                pictureBox.Refresh();
            }
            pictureBox.Image = image;
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            ImageFormat format = ImageFormat.Bmp;
            image.Save(saveFileDialog.FileName, format);
        }

        private void pictureBox_SizeChanged(object sender, EventArgs e)
        {
            Bitmap newImage = new Bitmap(pictureBox.Width, pictureBox.Height);
            newImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
            using (Graphics g = Graphics.FromImage(newImage))
            {
                g.Clear(Color.White);
                g.DrawImageUnscaled(image, new Point(0, 0));
            }
            image.Dispose();
            image = newImage;
            pictureBox.Image = image;
        }

        private void loadButton_Click(object sender, EventArgs e)
        {
            pictureBox.CreateGraphics().Clear(Color.White);
            rect = new Rectangle();

            pictureBox.Invalidate();
            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;

            int oldImageHeight = pictureBox.Image.Height;
            int oldImageWidth = pictureBox.Image.Width;

            image?.Dispose();
            image = Image.FromFile(openFileDialog.FileName) as Bitmap;

            ClientSize = new Size((int)(ClientSize.Width - oldImageWidth * 1.11 + image.Width * 1.11),
                ClientSize.Height - oldImageHeight + image.Height);
            
            pictureBox.Image = image;
        }


        private void brushButton_Click(object sender, EventArgs e)
        {
            brushActive = brushButton.Checked = !brushActive;
            ellipseActive = ellipseButton.Checked = false;
            rectActive = rectButton.Checked = false;
        }

        private void rectButton_Click(object sender, EventArgs e)
        {
            brushActive = brushButton.Checked = false;
            ellipseActive = ellipseButton.Checked = false;
            rectActive = rectButton.Checked = !rectActive;
        }

        private void ellipseButton_Click(object sender, EventArgs e)
        {
            brushActive = brushButton.Checked = false;
            ellipseActive = ellipseButton.Checked = !ellipseActive;
            rectActive = rectButton.Checked = false;
        }

        private void pictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (!drawMode)
                return;

            e.Graphics.SmoothingMode =
                System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            using (Pen pen = new Pen(penColor, penSize))
            {
                pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                if (rectActive)
                    e.Graphics.DrawRectangle(pen, rect);
                if (ellipseActive)
                    e.Graphics.DrawEllipse(pen, rect);
            }
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            image?.Dispose();
            image = null;
            InitializeImage();
        }

        private void sizeSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (sizeSelector.SelectedIndex)
            {
                case 0:
                    penSize = 1;
                    break;
                case 1:
                    penSize = 3;
                    break;
                case 2:
                    penSize = 6;
                    break;
            }
        }

        private void aLanguage_Click(object sender, EventArgs e)
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en");
            RefreshAllControls();
            aLanguage.Checked = true;
            bLanguage.Checked = false;
        }

        private void bLanguage_Click(object sender, EventArgs e)
        {
            CultureInfo.CurrentUICulture = new CultureInfo("pl");
            RefreshAllControls();
            aLanguage.Checked = false;
            bLanguage.Checked = true;
        }

        private void RefreshAllControls()
        {
            selectedSizeBackup = sizeSelector.SelectedIndex;

            Controls.Clear();
            SuspendLayout();
            Enabled = false;
            MyInitializeComponent();
            colorSelector.SuspendLayout();
            InitializeForm();
            Enabled = true;
            ResumeLayout(true);
            
            foreach (Control c in Controls)
            {
                c.Show();
            }

            colorSelector.ResumeLayout(true);
        }


        void MyInitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MiniPaint));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            this.saveButton = new System.Windows.Forms.ToolStripButton();
            this.loadButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.brushButton = new System.Windows.Forms.ToolStripButton();
            this.rectButton = new System.Windows.Forms.ToolStripButton();
            this.ellipseButton = new System.Windows.Forms.ToolStripButton();
            this.clearButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripLabel3 = new System.Windows.Forms.ToolStripLabel();
            this.sizeSelector = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripLabel4 = new System.Windows.Forms.ToolStripLabel();
            this.chosenColor = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripLabel5 = new System.Windows.Forms.ToolStripLabel();
            this.aLanguage = new System.Windows.Forms.ToolStripButton();
            this.bLanguage = new System.Windows.Forms.ToolStripButton();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.groupBox = new System.Windows.Forms.GroupBox();
            this.colorSelector = new System.Windows.Forms.FlowLayoutPanel();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.toolStrip1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.groupBox.SuspendLayout();
            this.SuspendLayout();
            
            // 
            // toolStrip1
            // 
            resources.ApplyResources(this.toolStrip1, "toolStrip1");
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel2,
            this.saveButton,
            this.loadButton,
            this.toolStripSeparator1,
            this.toolStripLabel1,
            this.brushButton,
            this.rectButton,
            this.ellipseButton,
            this.clearButton,
            this.toolStripSeparator2,
            this.toolStripLabel3,
            this.sizeSelector,
            this.toolStripSeparator3,
            this.toolStripLabel4,
            this.chosenColor,
            this.toolStripSeparator4,
            this.toolStripLabel5,
            this.aLanguage,
            this.bLanguage});
            this.toolStrip1.Name = "toolStrip1";
            // 
            // toolStripLabel2
            // 
            resources.ApplyResources(this.toolStripLabel2, "toolStripLabel2");
            this.toolStripLabel2.Name = "toolStripLabel2";
            // 
            // saveButton
            // 
            resources.ApplyResources(this.saveButton, "saveButton");
            this.saveButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.saveButton.Image = global::MiniPaint.Properties.Resources.save;
            this.saveButton.Name = "saveButton";
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // loadButton
            // 
            resources.ApplyResources(this.loadButton, "loadButton");
            this.loadButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.loadButton.Image = global::MiniPaint.Properties.Resources.load;
            this.loadButton.Name = "loadButton";
            this.loadButton.Click += new System.EventHandler(this.loadButton_Click);
            // 
            // toolStripSeparator1
            // 
            resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            // 
            // toolStripLabel1
            // 
            resources.ApplyResources(this.toolStripLabel1, "toolStripLabel1");
            this.toolStripLabel1.Name = "toolStripLabel1";
            // 
            // brushButton
            // 
            resources.ApplyResources(this.brushButton, "brushButton");
            this.brushButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.brushButton.Image = global::MiniPaint.Properties.Resources.brush;
            this.brushButton.Name = "brushButton";
            this.brushButton.Click += new System.EventHandler(this.brushButton_Click);
            // 
            // rectButton
            // 
            resources.ApplyResources(this.rectButton, "rectButton");
            this.rectButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.rectButton.Image = global::MiniPaint.Properties.Resources.rect;
            this.rectButton.Name = "rectButton";
            this.rectButton.Click += new System.EventHandler(this.rectButton_Click);
            // 
            // ellipseButton
            // 
            resources.ApplyResources(this.ellipseButton, "ellipseButton");
            this.ellipseButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.ellipseButton.Image = global::MiniPaint.Properties.Resources.ellipse;
            this.ellipseButton.Name = "ellipseButton";
            this.ellipseButton.Click += new System.EventHandler(this.ellipseButton_Click);
            // 
            // clearButton
            // 
            resources.ApplyResources(this.clearButton, "clearButton");
            this.clearButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.clearButton.Image = global::MiniPaint.Properties.Resources.trash;
            this.clearButton.Name = "clearButton";
            this.clearButton.Click += new System.EventHandler(this.clearButton_Click);
            // 
            // toolStripSeparator2
            // 
            resources.ApplyResources(this.toolStripSeparator2, "toolStripSeparator2");
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            // 
            // toolStripLabel3
            // 
            resources.ApplyResources(this.toolStripLabel3, "toolStripLabel3");
            this.toolStripLabel3.Name = "toolStripLabel3";
            // 
            // sizeSelector
            // 
            resources.ApplyResources(this.sizeSelector, "sizeSelector");
            this.sizeSelector.Items.AddRange(new object[] {
            resources.GetString("sizeSelector.Items"),
            resources.GetString("sizeSelector.Items1"),
            resources.GetString("sizeSelector.Items2")});
            this.sizeSelector.Name = "sizeSelector";
            this.sizeSelector.SelectedIndexChanged += new System.EventHandler(this.sizeSelector_SelectedIndexChanged);
            // 
            // toolStripSeparator3
            // 
            resources.ApplyResources(this.toolStripSeparator3, "toolStripSeparator3");
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            // 
            // toolStripLabel4
            // 
            resources.ApplyResources(this.toolStripLabel4, "toolStripLabel4");
            this.toolStripLabel4.Name = "toolStripLabel4";
            // 
            // chosenColor
            // 
            resources.ApplyResources(this.chosenColor, "chosenColor");
            this.chosenColor.BackColor = System.Drawing.SystemColors.Control;
            this.chosenColor.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.chosenColor.Name = "chosenColor";
            // 
            // toolStripSeparator4
            // 
            resources.ApplyResources(this.toolStripSeparator4, "toolStripSeparator4");
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            // 
            // toolStripLabel5
            // 
            resources.ApplyResources(this.toolStripLabel5, "toolStripLabel5");
            this.toolStripLabel5.Name = "toolStripLabel5";
            // 
            // aLanguage
            // 
            resources.ApplyResources(this.aLanguage, "aLanguage");
            this.aLanguage.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.aLanguage.Image = global::MiniPaint.Properties.Resources.flag_gb;
            this.aLanguage.Name = "aLanguage";
            this.aLanguage.Click += new System.EventHandler(this.aLanguage_Click);
            // 
            // bLanguage
            // 
            resources.ApplyResources(this.bLanguage, "bLanguage");
            this.bLanguage.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bLanguage.Image = global::MiniPaint.Properties.Resources.flag_pl;
            this.bLanguage.Name = "bLanguage";
            this.bLanguage.Click += new System.EventHandler(this.bLanguage_Click);
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.pictureBox, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.groupBox, 1, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // pictureBox
            // 
            resources.ApplyResources(this.pictureBox, "pictureBox");
            this.pictureBox.BackColor = System.Drawing.Color.White;
            this.pictureBox.Cursor = System.Windows.Forms.Cursors.Cross;
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.TabStop = false;
            this.pictureBox.SizeChanged += new System.EventHandler(this.pictureBox_SizeChanged);
            this.pictureBox.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureBox_Paint);
            this.pictureBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBox_MouseDown);
            this.pictureBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBox_MouseMove);
            this.pictureBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBox_MouseUp);
            // 
            // groupBox
            // 
            resources.ApplyResources(this.groupBox, "groupBox");
            this.groupBox.Controls.Add(this.colorSelector);
            this.groupBox.Name = "groupBox";
            this.groupBox.TabStop = false;
            // 
            // colorSelector
            // 
            resources.ApplyResources(this.colorSelector, "colorSelector");
            this.colorSelector.Name = "colorSelector";
            // 
            // saveFileDialog
            // 
            resources.ApplyResources(this.saveFileDialog, "saveFileDialog");
            // 
            // openFileDialog
            // 
            resources.ApplyResources(this.openFileDialog, "openFileDialog");
            // 
            // MiniPaint
            // 
            //resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.toolStrip1);
            this.DoubleBuffered = true;
            this.Name = "MiniPaint";
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.groupBox.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
            foreach (Control c in Controls)
            {
                c.Hide();
            }
        }
    }
}
