﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Screenote
{
    public partial class Screen : Form
    {
        Graphics graphicsScreen;
        Bitmap bitmapScreen;
        Point Start;
        Point End;
        Bitmap bitmapTarget;
        Graphics graphicsMagnifier;
        bool shot = false;
        long tick = DateTime.Now.Ticks;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool AnimateWindow(IntPtr hwnd, int dwTime, int dwFlags);

        public Screen()
        {
            InitializeComponent();
            magnifier.MouseMove += picture_MouseMove;
        }

        protected override void WndProc(ref Message message)
        {
            try
            {
                switch (message.WParam.ToInt64())
                {
                    case 936:
                        {
                            if (this.Visible == false)
                            {
                                this.Location = new Point(System.Windows.Forms.Screen.FromPoint(Cursor.Position).Bounds.X, System.Windows.Forms.Screen.FromPoint(Cursor.Position).Bounds.Y);
                                this.Width = System.Windows.Forms.Screen.FromPoint(Cursor.Position).Bounds.Width;
                                this.Height = System.Windows.Forms.Screen.FromPoint(Cursor.Position).Bounds.Height;

                                bitmapScreen = new Bitmap(this.Width, this.Height);
                                using (Graphics graphics = Graphics.FromImage(bitmapScreen as Image))
                                {
                                    graphics.CopyFromScreen(System.Windows.Forms.Screen.FromPoint(Cursor.Position).Bounds.X, System.Windows.Forms.Screen.FromPoint(Cursor.Position).Bounds.Y, 0, 0, this.Size);
                                }

                                picture.Image = bitmapScreen;
                                graphicsScreen = picture.CreateGraphics();

                                AnimateWindow(this.Handle, 8, 0x00000010 + 0x00080000 + 0x00020000);
                                SetForegroundWindow(this.Handle);
                                this.Visible = true;
                                magnifier.Visible = true;
                                Cursor.Hide();
                            }
                            else
                            {
                                this.Visible = false;
                                magnifier.Visible = false;
                                Cursor.Show();
                                GC.Collect();
                            }
                            break;
                        }
                    case 937:
                        {
                            List<Image> images = new List<Image>();
                            System.Collections.Specialized.StringCollection files = Clipboard.GetFileDropList();

                            if (files != null)
                            {
                                foreach (string file in files)
                                {
                                    try
                                    {
                                        images.Add(Image.FromFile(file));
                                    }
                                    catch
                                    { }
                                }
                            }

                            Image temp = Clipboard.GetImage();
                            if (temp != null)
                            {
                                images.Add(temp);
                            }

                            foreach (Image image in images)
                            {
                                if (image.Width > 15 && image.Height > 15)
                                {
                                    Rectangle region = new Rectangle(Cursor.Position.X - image.Width / 2, Cursor.Position.Y - image.Height / 2, image.Width, image.Height);
                                    Note note = new Note(new Bitmap(image), region.Location);
                                    note.Show();
                                }
                                image.Dispose();
                            }


                            GC.Collect();
                            break;
                        }
                }

                base.WndProc(ref message);
            }
            catch
            {
                base.WndProc(ref message);
                return;
            }
        }

        private void Screen_Shown(object sender, EventArgs e)
        {
            this.Visible = false;
        }

        private void picture_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Start = new Point(e.Location.X, e.Location.Y);
                shot = true;
            }
            if (e.Button == MouseButtons.Right)
            {
                this.Visible = false;
                magnifier.Visible = false;
                Cursor.Show();
                GC.Collect();
            }
            if (e.Button == MouseButtons.Middle)
            {
                Color pixel = bitmapScreen.GetPixel(Cursor.Position.X, Cursor.Position.Y);
                Clipboard.SetText(pixel.R.ToString("X2") + pixel.G.ToString("X2") + pixel.B.ToString("X2"));
                this.Visible = false;
                magnifier.Visible = false;
                Cursor.Show();
                GC.Collect();
            }
        }

        private void picture_MouseUp(object sender, MouseEventArgs e)
        {
            if (shot)
            {
                End = e.Location;
                shot = false;
                int width = Math.Abs(End.X - Start.X) + 1;
                int height = Math.Abs(End.Y - Start.Y) + 1;
                if (width > 15 && height > 15)
                {
                    Rectangle region = new Rectangle(Math.Min(Start.X, End.X), Math.Min(Start.Y, End.Y), width, height);
                    Note note = new Note(bitmapScreen.Clone(region, System.Drawing.Imaging.PixelFormat.Format24bppRgb), region.Location);
                    note.Show();
                }
                this.Visible = false;
                magnifier.Visible = false;
                Cursor.Show();
                GC.Collect();
            }
        }

        private void picture_MouseMove(object sender, MouseEventArgs e)
        {
            if (DateTime.Now.Ticks - tick < 400000)
            {
                return;
            }
            tick = DateTime.Now.Ticks;

            this.Refresh();
            int X = Cursor.Position.X, Y = Cursor.Position.Y;
            magnifier.Location = new Point(X + 150 > this.Width ? X - 150 : X + 50, Y + 150 > this.Height ? Y - 150 : Y + 50);

            int left = X - 12 < 0 ? 12 - X : 0;
            int right = X + 13 > this.Width ? (X + 13 - this.Width) : 0;
            int top = Y - 12 < 0 ? 12 - Y : 0;
            int bottom = Y + 13 > this.Height ? (Y + 13 - this.Height) : 0;

            Rectangle region = new Rectangle(X - 12 + left, Y - 12 + top, 25 - right, 25 - bottom);
            bitmapTarget = bitmapScreen.Clone(region, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Bitmap bitmapMagnifier = new Bitmap(100, 100);
            graphicsMagnifier = Graphics.FromImage(bitmapMagnifier);

            for (int y = 0; y < region.Height; y++)
            {
                for (int x = 0; x < region.Width; x++)
                {
                    graphicsMagnifier.FillRectangle(new SolidBrush(bitmapTarget.GetPixel(x, y)), (x + left) * 4, (y + top) * 4, 4, 4);
                }
            }

            bitmapTarget.Dispose();
            graphicsMagnifier.FillRectangles(new SolidBrush(Color.FromArgb(192, 192, 192)), new Rectangle[] { new Rectangle(48, 0, 4, 48), new Rectangle(48, 52, 4, 48), new Rectangle(0, 48, 48, 4), new Rectangle(52, 48, 48, 4) });
            magnifier.Image = bitmapMagnifier;
            graphicsScreen.FillRectangles(new SolidBrush(Color.FromArgb(192, 192, 192)), new Rectangle[] { new Rectangle(X, 0, 1, this.Height), new Rectangle(0, Y, this.Width, 1) });
            if (shot)
            {
                graphicsScreen.FillRectangles(new SolidBrush(Color.FromArgb(192, 192, 192)), new Rectangle[] { new Rectangle(Start.X, 0, 1, this.Height), new Rectangle(0, Start.Y, this.Width, 1) });
            }

        }

        private void Screen_KeyDown(object sender, KeyEventArgs e)
        {
            int shift = 1;
            if (e.Shift)
            {
                shift = 10;
            }
            switch (e.KeyCode)
            {
                case Keys.Left:
                    Cursor.Position = new Point(Cursor.Position.X - shift, Cursor.Position.Y);
                    break;
                case Keys.Right:
                    Cursor.Position = new Point(Cursor.Position.X + shift, Cursor.Position.Y);
                    break;
                case Keys.Up:
                    Cursor.Position = new Point(Cursor.Position.X, Cursor.Position.Y - shift);
                    break;
                case Keys.Down:
                    Cursor.Position = new Point(Cursor.Position.X, Cursor.Position.Y + shift);
                    break;
                case Keys.Space:
                    picture_MouseDown(this, new MouseEventArgs(MouseButtons.Middle, 0, Cursor.Position.X, Cursor.Position.Y, 0));
                    break;
                case Keys.Escape:
                    picture_MouseDown(this, new MouseEventArgs(MouseButtons.Right, 0, Cursor.Position.X, Cursor.Position.Y, 0));
                    break;
                case Keys.Enter:
                    if (shot)
                    {
                        picture_MouseUp(this, new MouseEventArgs(MouseButtons.Left, 0, Cursor.Position.X, Cursor.Position.Y, 0));
                    }
                    else
                    {
                        picture_MouseDown(this, new MouseEventArgs(MouseButtons.Left, 0, Cursor.Position.X, Cursor.Position.Y, 0));
                    }
                    break;
            }
        }
    }
}
