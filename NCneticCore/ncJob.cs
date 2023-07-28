using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace NCneticCore
{
    public class ncJob
    {
        public string FileName = String.Empty;
        public string Text = String.Empty;
        public List<ncMove> MoveList = new List<ncMove>();

        public event EventHandler EndProcessing;

        public void Process(ncMachine machine)
        {
            BackgroundWorker worker = new BackgroundWorker();

            worker.DoWork += new DoWorkEventHandler((sw, eaw) =>
            {
                MoveList = new List<ncMove>();
                ncParser parser = new ncParser();
                parser.ComputeRawJob(Text, machine.Lexer);
                MoveList = FAO.GetMoveList(parser.Rawoperation, machine.Definition);
            });

            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler((s, ea) =>
            {
                EndProcessing?.Invoke(this, new EventArgs());
            });

            worker.RunWorkerAsync();
        }
    }
}
