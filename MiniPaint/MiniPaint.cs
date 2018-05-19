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

            Hide();
            MyInitializeComponent();
            InitializeForm();
            Show();
        }

        void MyInitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MiniPaint));
            
            foreach (Control c in Controls)
            {
                resources.ApplyResources(c, c.Name);
                if (c is ToolStrip t)
                {
                    foreach (ToolStripItem tt in t.Items)
                    {
                        resources.ApplyResources(tt, tt.Name);
                    }
                }
                else
                {
                    foreach (Control cc in c.Controls)
                    {
                        resources.ApplyResources(cc, cc.Name);
                    }
                }
            }
        }
    }
}
