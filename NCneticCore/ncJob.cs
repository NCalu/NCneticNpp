using System;
using System.Collections;
using System.Collections.Generic;
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

        public void Process(ncMachine machine)
        {
            MoveList = new List<ncMove>();
            ncParser parser = new ncParser();
            parser.ComputeRawJob(Text, machine.Lexer);
            MoveList = FAO.GetMoveList(parser.Rawoperation, machine.Definition);
        }
    }
}
