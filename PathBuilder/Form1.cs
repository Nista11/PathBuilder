using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace PathBuilder
{
    public partial class Form1 : Form
    {
        Graphics gf;
        const double mmTileWidth = 578;
        const double mmFieldWidth = 6.0 * mmTileWidth; 
        int screenWidth, screenHeight, borderWidth, title_borderHeight, cursorWidth, cursorHeight;
        int robotWidth, robotHeight;
        bool setStartPos, setLastPos, pathLoaded, codeCoordinatesReversed;
        string generatedCode;
        List<Point> Pos;
        List<Point> Change;
        public Form1()
        {
            InitializeComponent(); 
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            screenWidth = Screen.PrimaryScreen.Bounds.Width;
            screenHeight = Screen.PrimaryScreen.Bounds.Height;
            this.Location = new Point(screenWidth / 2 - this.Width / 2, screenHeight / 2 - this.Height / 2);
            borderWidth = (this.Width - this.ClientSize.Width) / 2;
            title_borderHeight = this.Height - this.ClientSize.Height - 2 * borderWidth;
            cursorWidth = cursorHeight = 16;
            robotWidth = robotHeight = 0;
            setStartPos = setLastPos = pathLoaded = codeCoordinatesReversed = false;
            generatedCode = null;
            gf = pictureBox1.CreateGraphics();
            Pos = new List<Point>();
            Change = new List<Point>();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState != FormWindowState.Minimized)
                refreshGraphics();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (robotWidth > 0 && robotHeight > 0 && setStartPos && setLastPos)
            {
                int x = Cursor.Position.X - pictureBox1.Left - this.Location.X - borderWidth;
                int y = Cursor.Position.Y - cursorHeight / 2 - pictureBox1.Top - this.Location.Y - title_borderHeight;
                addCoordinates(sender, e, true, new Point(x - Pos.Last().X, y - Pos.Last().Y));               
            }

            else
                MessageBox.Show("Input data!");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            robotWidth = (int)numericUpDown1.Value;
            robotHeight = (int)numericUpDown2.Value;
            refreshGraphics();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Point startPos = new Point(mmToPixels((int)numericUpDown3.Value), mmToPixels((int)numericUpDown4.Value));
            if (Pos.Count == 0)
            {
                Pos.Add(startPos);
                setStartPos = setLastPos = true;
            }
            else
            {
                Pos[0] = startPos;
                for (int ind = 1; ind < Pos.Count; ++ind)
                    Pos[ind] = new Point(Pos[ind - 1].X + Change[ind - 1].X, Pos[ind - 1].Y + Change[ind - 1].Y);
            }
            refreshGraphics();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (Pos.Count > 1)
            {
                Pos.RemoveAt(Pos.Count - 1);
                Change.RemoveAt(Change.Count - 1);
                for (int i = 0; i < 6; ++i)
                    groupBox1.Controls.RemoveAt(groupBox1.Controls.Count - 1);
                groupBox1.Refresh();
                refreshGraphics();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (setStartPos && robotHeight > 0 && robotWidth > 0 && setLastPos)
                addCoordinates(sender, e, false, new Point(0, 0));

            else MessageBox.Show("Input data!");
        }

        private void addCoordinates(object sender, EventArgs e, bool drawing, Point p)
        {
            if (!drawing)
                setLastPos = false;
            Label index = new Label();
            index.Width = 40;
            index.Height = 23;
            index.Text = Pos.Count.ToString();
            index.Font = new Font("Microsoft Sans Serif", 13);
            index.Top = 10 + groupBox1.Top - groupBox1.Location.Y + (Pos.Count - 1) * 7 + (Pos.Count - 1) * index.Height - vScrollBar1.Value;
            groupBox1.Controls.Add(index);

            Label x = new Label();
            x.Width = 20;
            x.Height = index.Height;
            x.Text = "x";
            x.Font = new Font("Microsoft Sans Serif", 13);
            x.Top = index.Top;
            x.Left = index.Left + index.Width + 5;
            groupBox1.Controls.Add(x);

            NumericUpDown xVal = new NumericUpDown();
            xVal.Width = 57;
            xVal.Height = index.Height;
            xVal.Font = new Font("Microsoft Sans Serif", 13);
            xVal.Top = index.Top;
            xVal.Left = x.Left + x.Width + 5;
            xVal.Maximum = numericUpDown3.Maximum;
            xVal.Minimum = -xVal.Maximum;
            if (drawing)
                xVal.Value = pixelsToMm(p.X);
            groupBox1.Controls.Add(xVal);

            Label y = new Label();
            y.Width = 20;
            y.Height = index.Height;
            y.Text = "y";
            y.Font = new Font("Microsoft Sans Serif", 13);
            y.Top = index.Top;
            y.Left = xVal.Left + xVal.Width + 5;
            groupBox1.Controls.Add(y);

            NumericUpDown yVal = new NumericUpDown();
            yVal.Width = 57;
            yVal.Height = index.Height;
            yVal.Font = new Font("Microsoft Sans Serif", 13);
            yVal.Top = index.Top;
            yVal.Left = y.Left + y.Width + 5;
            yVal.Maximum = numericUpDown4.Maximum;
            yVal.Minimum = -yVal.Maximum;
            if (drawing)
                yVal.Value = pixelsToMm(p.Y);
            groupBox1.Controls.Add(yVal);

            Button b = new Button();
            b.Width = 57;
            b.Height = index.Height;
            b.Font = new Font("Microsoft Sans Serif", 10);
            b.Top = index.Top;
            b.Left = yVal.Left + yVal.Width + 5;
            b.Text = "Save";
            b.Name = index.ToString();
            b.Click += new EventHandler(buttonPosClick);
            groupBox1.Controls.Add(b);
            if (drawing)
                buttonPosClick(b, e);
            groupBox1.Refresh(); 
        }

        private void buttonPosClick(object sender, EventArgs e)
        {
            Button b = (Button)sender;
            int index = groupBox1.Controls.IndexOf(b);
            NumericUpDown x = (NumericUpDown)groupBox1.Controls[index - 3];
            NumericUpDown y = (NumericUpDown)groupBox1.Controls[index - 1];
            int ind = index / 6;
            Point cng = new Point(mmToPixels((int)x.Value), mmToPixels((int)y.Value));
            Point newPos = new Point(mmToPixels((int)x.Value) + Pos[ind - 1].X, mmToPixels((int)y.Value) + Pos[ind - 1].Y);

            if (ind == Pos.Count)
            {
                setLastPos = true;
                Pos.Add(newPos);
                Change.Add(cng);
            }

            else
            {
                Pos[ind] = newPos;
                Change[ind - 1] = cng;
                while (++ind < Pos.Count)
                    Pos[ind] = new Point(Pos[ind - 1].X + Change[ind - 1].X, Pos[ind - 1].Y + Change[ind - 1].Y);
            }
            refreshGraphics(); 
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (Pos.Count > 0)
            {
                try
                {
                    SaveFileDialog file = new SaveFileDialog();
                    file.InitialDirectory = Directory.GetCurrentDirectory();
                    file.Filter = "txt files (*.txt)|*.txt";
                    file.RestoreDirectory = true;

                    if (file.ShowDialog() == DialogResult.OK)
                    {
                        Stream stream = file.OpenFile();
                        if (stream != null)
                        {
                            StreamWriter fout = new StreamWriter(stream);
                            generatedCode = null;
                            fout.Write(robotWidth.ToString() + ' ' + robotHeight.ToString() + "\r\n");
                            foreach (Point a in Pos)
                            {
                                string s = pixelsToMm(a.X).ToString() + ' ' + pixelsToMm(a.Y).ToString() + "\r\n";
                                fout.Write(s);
                                if (a != Pos.First())
                                    generatedCode += linieCodGenerat(a);
                            }

                            fout.Close();
                            pathLoaded = true;
                        }
                        stream.Close();
                    }
                }

                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }

            }

            else
                MessageBox.Show("Input data!");
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (pathLoaded)
            {
                generatedCode = null;
                for (int i = 1; i < Pos.Count(); ++i)
                {
                    Point p = new Point(Pos[i].X, Pos[i].Y);
                    if (codeCoordinatesReversed)
                    {
                        int aux = p.X;
                        p.X = p.Y;
                        p.Y = aux;
                    }

                    generatedCode += linieCodGenerat(p);
                    richTextBox1.Text = generatedCode;
                }
            }
            else
                MessageBox.Show("Unsaved path!");
        }

        private void button7_Click(object sender, EventArgs e)
        {
            string msg = "This will erase any unsaved data, proceed?", title = "Warning";
            DialogResult choice = MessageBox.Show(msg, title, MessageBoxButtons.YesNo);
            if (choice == DialogResult.Yes)
            {
                try
                {
                    OpenFileDialog file = new OpenFileDialog();
                    file.InitialDirectory = Directory.GetCurrentDirectory();
                    file.Filter = "txt files (*.txt)|*.txt";

                    if (file.ShowDialog() == DialogResult.OK)
                    {
                        pictureBox1.Refresh();
                        groupBox1.Controls.Clear();
                        groupBox1.Controls.Add(vScrollBar1);
                        generatedCode = null;
                        richTextBox1.Text = null;
                        setStartPos = setLastPos = pathLoaded = true;
                        Pos = new List<Point>();
                        Change = new List<Point>();

                        StreamReader fin = new StreamReader(file.FileName);

                        string[] S = fin.ReadLine().Split(' ');
                        robotWidth = int.Parse(S[0]);
                        robotHeight = int.Parse(S[1]);
                        numericUpDown1.Value = robotWidth;
                        numericUpDown2.Value = robotHeight;

                        S = fin.ReadLine().Split();
                        Point start = new Point(int.Parse(S[0]), int.Parse(S[1]));
                        Pos.Add(new Point(mmToPixels(start.X), mmToPixels(start.Y)));
                        numericUpDown3.Value = start.X;
                        numericUpDown4.Value = start.Y;

                        while (!fin.EndOfStream)
                        {
                            S = fin.ReadLine().Split();
                            Point p = new Point(mmToPixels(int.Parse(S[0])), mmToPixels(int.Parse(S[1])));
                            Point cng = new Point(p.X - Pos.Last().X, p.Y - Pos.Last().Y);
                            addCoordinates(sender, e, true, cng);
                            generatedCode += linieCodGenerat(p);
                        }
                        fin.Close();
                    }
                }

                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            codeCoordinatesReversed = !codeCoordinatesReversed;
            button6_Click(sender, e);

        }

        private string linieCodGenerat(Point p)
        {
            if (codeCoordinatesReversed)
            {
                p.X = -1 * (pixelsToMm(p.X) - pixelsToMm(Pos.First().Y));
                p.Y = (pixelsToMm(p.Y) - pixelsToMm(Pos.First().X));
            }

            else
            {
                p.X = pixelsToMm(p.X) - pixelsToMm(Pos.First().X);
                p.Y = -1 * (pixelsToMm(p.Y) - pixelsToMm(Pos.First().Y));
            }
            return ".splineTo(new Pose2d(" + p.X.ToString() + ", " + p.Y.ToString() + ", 0), new ConstantInterpolator(0))\r\n";
        }

        private void vScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            int d = e.NewValue - e.OldValue;
            foreach (Control a in groupBox1.Controls)
                if (a != vScrollBar1)
                    a.Top -= d;

            groupBox1.Refresh(); 
        }

        private int mmToPixels(int mm)
        {
            double rez = ((double)mm * ((double)pictureBox1.Width / mmFieldWidth));
            return (rez - (int)rez >= 0.5 ? (int)rez + Math.Sign(rez) : (int)rez);
        }

        private int pixelsToMm(int pixels)
        {
            double rez = (int)((double)pixels * (mmFieldWidth / (double)pictureBox1.Width));
            return (rez - (int)rez >= 0.5 ? (int)rez + Math.Sign(rez) : (int)rez);
        }

        private void refreshGraphics()
        {
            pictureBox1.Refresh();
            for (int i = 0; i < Pos.Count - 1; ++i)
            {
                drawPoint(Pos[i]);
                drawLine(Pos[i], Pos[i + 1]);
            }

            if (Pos.Count > 0)
                drawRobot(Pos.Last()); 
        }

        private void drawPoint(Point a)
        {
            SolidBrush p = new SolidBrush(Color.Green);
            int d = 10, r = d / 2;
            Rectangle rct = new Rectangle(a.X - r, a.Y - r, d, d);
            gf.FillEllipse(p, rct);
        }

        private void drawLine(Point a, Point b)
        {
            Pen p = new Pen(Color.Green, 2);
            gf.DrawLine(p, a, b);
        }

        private void drawRobot(Point a)
        {
            int rW = mmToPixels(robotWidth), rH = mmToPixels(robotHeight);
            Pen p = new Pen(Color.Green, 2);
            Rectangle r = new Rectangle(a.X - rW / 2, a.Y - rH / 2, rW, rH);
            gf.DrawRectangle(p, r);
        }
    }
}
