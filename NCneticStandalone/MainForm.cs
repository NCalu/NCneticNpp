using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NCneticCore;
using NCneticCore.View;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace NCneticStandalone
{
    public class MainForm : Form
    {
        private RichTextBox editor = new RichTextBox();
        private GLControl glControl = new GLControl();
        private ncView view;
        private ncJob job = new ncJob();
        private ncMachine machine = new ncMachine();
        private TrackBar trackBar = new TrackBar();
        private SplitContainer split = new SplitContainer();
        private MenuStrip menu = new MenuStrip();
        private ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");
        private ToolStripMenuItem openItem = new ToolStripMenuItem("Open");
        private ToolStripMenuItem reloadItem = new ToolStripMenuItem("Reload");
        private ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit");
        private string currentFile = string.Empty;

        public MainForm()
        {
            Text = "NCnetic Standalone";
            Width = 1000;
            Height = 700;

            split.Dock = DockStyle.Fill;
            split.SplitterDistance = 350;
            split.Panel1.Controls.Add(editor);
            split.Panel2.Controls.Add(glControl);
            editor.Dock = DockStyle.Fill;
            glControl.Dock = DockStyle.Fill;
            Controls.Add(split);
            Controls.Add(trackBar);
            Controls.Add(menu);

            menu.Items.Add(fileMenu);
            fileMenu.DropDownItems.Add(openItem);
            fileMenu.DropDownItems.Add(reloadItem);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(exitItem);

            trackBar.Dock = DockStyle.Bottom;
            trackBar.Minimum = 0;
            trackBar.ValueChanged += TrackBar_ValueChanged;

            openItem.Click += OpenItem_Click;
            reloadItem.Click += ReloadItem_Click;
            exitItem.Click += (s,e) => Close();

            view = new ncView(new ncViewOptions());
            view.IniGraphicContext(glControl.Handle);
            view.ViewPortLoad(glControl.Width, glControl.Height);
            HookViewEvents();
        }

        private void HookViewEvents()
        {
            glControl.Resize += (s, e) => view.ViewChangeSize(glControl.Width, glControl.Height);
            glControl.Paint += (s, e) =>
            {
                view.ViewPortPaint();
                glControl.SwapBuffers();
            };
            view.Refresh += (s, e) => glControl.Invalidate();
            view.MoveSelected += (s, e) =>
            {
                int selId = job.MoveList.FindIndex(m => m.MoveGuid == e.guid);
                if (selId >= 0)
                {
                    trackBar.Value = selId;
                    HighlightLine(job.MoveList[selId].Line);
                }
            };
        }

        private void TrackBar_ValueChanged(object sender, EventArgs e)
        {
            if (job.MoveList.Any() && trackBar.Value < job.MoveList.Count)
            {
                view.SelectMove(job.MoveList[trackBar.Value]);
                HighlightLine(job.MoveList[trackBar.Value].Line);
            }
        }

        private void HighlightLine(int line)
        {
            if (line >= 0 && line < editor.Lines.Length)
            {
                int idx = editor.GetFirstCharIndexFromLine(line);
                editor.SelectionStart = idx;
                editor.ScrollToCaret();
            }
        }

        private void OpenItem_Click(object sender, EventArgs e)
        {
            using OpenFileDialog dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                currentFile = dlg.FileName;
                LoadFile(currentFile);
            }
        }

        private void ReloadItem_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(currentFile) && File.Exists(currentFile))
            {
                LoadFile(currentFile);
            }
        }

        private void LoadFile(string file)
        {
            editor.Text = File.ReadAllText(file);
            job = new ncJob { FileName = file, Text = editor.Text };
            job.Process(machine);
            job.EndProcessing += (s, e) =>
            {
                view.LoadJob(job);
                view.Recenter();
                trackBar.Maximum = Math.Max(0, job.MoveList.Count - 1);
                trackBar.Value = 0;
            };
        }
    }
}
