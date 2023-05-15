using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NCneticCore
{
    internal class ncParser
    {
        #region fields

        internal FAO.SubCollection Rawoperation = new FAO.SubCollection();

        private ncTree<FAO.Sub> CurrentSub;
        private FAO.Command ReferenceCommand = new FAO.Command();
        private FAO.Command CurrentCommand = new FAO.Command();

        private static NumberStyles NbStyle = NumberStyles.Number;
        private static CultureInfo Culture = CultureInfo.InvariantCulture;

        #endregion

        #region events
        internal event ReportProgressParsingEventHandler ReportProgressParsing;
        internal delegate void ReportProgressParsingEventHandler(object source, ReportProgressParsingEventArgs e);
        internal class ReportProgressParsingEventArgs : EventArgs
        {
            internal int progress;

            internal ReportProgressParsingEventArgs(int p)
            {
                progress = p;
            }
        }
        #endregion

        #region methods

        internal void ComputeRawJob(string text, ncLexer lexer)
        {
            Reset();
            string[] Lines = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

            int p = 0;
            for (int l = 0; l < Lines.Count(); l++)
            {
                if (p != l * 100 / Lines.Count())
                {
                    p = l * 100 / Lines.Count();
                    ReportProgressParsing?.Invoke(this, new ReportProgressParsingEventArgs(p));
                }

                ComputeRawLine(Lines[l], l, lexer);
            }
            CheckMain();

            ReportProgressParsing?.Invoke(this, new ReportProgressParsingEventArgs(100));
        }

        internal void Reset()
        {
            Rawoperation = new FAO.SubCollection();
            CurrentSub = Rawoperation.Sub;

            ReferenceCommand = new FAO.Command();
            CurrentCommand = new FAO.Command();
        }

        internal void CheckMain()
        {
            if (Rawoperation.Sub.Data.Commands.Count(x => x.CmdLink == FAO.Command.CommandLink.Move) == 0 &&
                Rawoperation.Sub.Data.Commands.Count(x => x.CmdLink == FAO.Command.CommandLink.SubCall) == 0)
            {
                if (Rawoperation.Sub.Children.Any())
                {
                    Rawoperation.Sub.Data.Commands.Clear();
                    FAO.SubCall StartSubCall = new FAO.SubCall(0, 0);
                    StartSubCall.SubName = Rawoperation.Sub.Children.First().Data.Name;
                    Rawoperation.Sub.Data.Commands.Add(StartSubCall);
                }
            }
        }

        internal void ComputeRawLine(string line, int l, ncLexer lexer)
        {
            List<FAO.Command> RawCmds = new List<FAO.Command>();

            int CurrentBlock = 0;
            string CurrentLine = line + '\r' + '\n'; ;

            int StartPos = 0;
            int Lengtj = 0;

            CurrentCommand.Line = l;

            List<string> StringsBuilder = new List<string>();
            List<ncLexerLink.CmdType> Cmds = new List<ncLexerLink.CmdType>();

            for (int i = 0; i < line.Length; i++)
            {
                if (i < line.Length)
                {
                    Lengtj = 0;
                    StartPos = line.Length;
                    StringsBuilder = new List<string>();
                    Cmds = new List<ncLexerLink.CmdType>();

                    lexer.CheckKeysRawoperation(CurrentLine, i, ref StartPos, ref Lengtj, ref StringsBuilder, ref Cmds);

                    for (int j = 0; j < Cmds.Count(); j++)
                    {
                        if (j < StringsBuilder.Count)
                        {
                            ApplyLink(StringsBuilder[j], l, ref CurrentBlock, Cmds[j], ref CurrentCommand, ref ReferenceCommand, ref CurrentSub);
                        }
                    }

                    if (Lengtj > 0)
                    {
                        i += Lengtj + StartPos - 1;
                    }
                    else
                    {
                        break;
                    }

                    if (i >= line.Length)
                    {
                        l += CurrentLine.Take(i).Count(c => c == '\n');
                        break;
                    }
                }
            }

            AddCommand(ref CurrentCommand, ref ReferenceCommand, ref CurrentSub);

            CurrentCommand.Line = l;
        }

        private void ApplyLink(string s, int line, ref int block, ncLexerLink.CmdType link, ref FAO.Command cmd, ref FAO.Command refcmd, ref ncTree<FAO.Sub> sub)
        {
            bool IsParsed = false;
            double resdbl = 0;
            int resint = 0;

            switch (link)
            {
                case ncLexerLink.CmdType.BlockNumber:
                    IsParsed = Int32.TryParse(s.Replace(" ", ""), NbStyle, Culture, out resint);
                    if (IsParsed)
                    {
                        cmd.Block = resint;
                        block = resint;
                    }
                    break;

                case ncLexerLink.CmdType.Rapid:
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    cmd = new FAO.CommandMove(line, block, ncMove.MoveType.Rapid);
                    cmd.IsUpdated = false;
                    break;

                case ncLexerLink.CmdType.Linear:
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    cmd = new FAO.CommandMove(line, block, ncMove.MoveType.Linear);
                    cmd.IsUpdated = false;
                    break;

                case ncLexerLink.CmdType.CircularCW:
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    cmd = new FAO.CommandMove(line, block, ncMove.MoveType.CircularCW);
                    cmd.IsUpdated = false;
                    break;

                case ncLexerLink.CmdType.CircularCCW:
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    cmd = new FAO.CommandMove(line, block, ncMove.MoveType.CircularCCW);
                    cmd.IsUpdated = false;
                    break;

                case ncLexerLink.CmdType.Dwell:
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    cmd = new FAO.Command();
                    cmd.IsUpdated = false;
                    break;

                case ncLexerLink.CmdType.SetFeedRateModeInverted:
                    sub.Data.Commands.Add(new FAO.SetFS(line, block,
                        new FAO.Input(), new FAO.Input(), new FAO.Input(),
                        FAO.SetFS.SetFMode.Inverted));
                    break;

                case ncLexerLink.CmdType.SetFeedRateModeNormal:
                    sub.Data.Commands.Add(new FAO.SetFS(line, block,
                        new FAO.Input(), new FAO.Input(), new FAO.Input(),
                        FAO.SetFS.SetFMode.Normal));
                    break;

                case ncLexerLink.CmdType.SetFeedRate:
                    IsParsed = double.TryParse(s.Replace(" ", ""), NbStyle, Culture, out resdbl);
                    if (IsParsed)
                    {
                        sub.Data.Commands.Add(new FAO.SetFS(line, block, new FAO.Input(resdbl), new FAO.Input(), new FAO.Input()));
                    }
                    break;

                case ncLexerLink.CmdType.SetSpindleSpeed:
                    IsParsed = double.TryParse(s.Replace(" ", ""), NbStyle, Culture, out resdbl);
                    if (IsParsed)
                    {
                        sub.Data.Commands.Add(new FAO.SetFS(line, block, new FAO.Input(), new FAO.Input(resdbl), new FAO.Input()));
                    }
                    break;

                case ncLexerLink.CmdType.SetReference:
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    cmd = new FAO.SetReference(line, block);
                    cmd.IsUpdated = false;
                    break;

                case ncLexerLink.CmdType.ResetReference:
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    cmd = new FAO.SetReference(line, block, true);
                    cmd.IsUpdated = true;
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    break;

                case ncLexerLink.CmdType.WorkOffset:
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    cmd = new FAO.WorkOffSet(line, block);
                    cmd.IsUpdated = false;
                    break;

                case ncLexerLink.CmdType.Scale:
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    cmd = new FAO.Scale(line, block);
                    cmd.IsUpdated = false;
                    break;

                case ncLexerLink.CmdType.ResetScale:
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    cmd = new FAO.Scale(line, block);
                    cmd.IsUpdated = true;
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    break;

                case ncLexerLink.CmdType.Rotate:
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    cmd = new FAO.Rotate(line, block);
                    cmd.IsUpdated = false;
                    break;

                case ncLexerLink.CmdType.ResetRotate:
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    cmd = new FAO.Rotate(line, block);
                    cmd.IsUpdated = true;
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    break;

                case ncLexerLink.CmdType.SubCall:
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    cmd = new FAO.SubCall(line, block);
                    cmd.IsUpdated = false;
                    break;

                case ncLexerLink.CmdType.SubStart:
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    cmd = new FAO.SubStart(line, block);
                    cmd.IsUpdated = false;
                    break;

                case ncLexerLink.CmdType.SubEnd:
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    cmd = new FAO.Command(line, block, FAO.Command.CommandLink.SubEnd);
                    cmd.IsUpdated = true;
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    break;

                case ncLexerLink.CmdType.ToolCallByNumber:
                    IsParsed = Int32.TryParse(s.Replace(" ", ""), NbStyle, Culture, out resint);
                    if (IsParsed)
                    {
                        sub.Data.Commands.Add(new FAO.ToolCall(line, block, resint));
                    }
                    break;

                case ncLexerLink.CmdType.SetXYZAbsolute:
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    cmd = new FAO.Command(line, block, FAO.Command.CommandLink.SetXYZAbsolute);
                    cmd.IsUpdated = true;
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    break;

                case ncLexerLink.CmdType.SetIJKAbsolute:
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    cmd = new FAO.Command(line, block, FAO.Command.CommandLink.SetIJKAbsolute);
                    cmd.IsUpdated = true;
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    break;

                case ncLexerLink.CmdType.SetXYZRelative:
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    cmd = new FAO.Command(line, block, FAO.Command.CommandLink.SetXYZRelative);
                    cmd.IsUpdated = true;
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    break;

                case ncLexerLink.CmdType.SetIJKRelative:
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    cmd = new FAO.Command(line, block, FAO.Command.CommandLink.SetIJKRelative);
                    cmd.IsUpdated = true;
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    break;

                case ncLexerLink.CmdType.SetWorkingPlaneXY:
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    cmd = new FAO.Command(line, block, FAO.Command.CommandLink.SetWorkingPlaneXY);
                    cmd.IsUpdated = true;
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    break;

                case ncLexerLink.CmdType.SetWorkingPlaneXZ:
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    cmd = new FAO.Command(line, block, FAO.Command.CommandLink.SetWorkingPlaneXZ);
                    cmd.IsUpdated = true;
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    break;

                case ncLexerLink.CmdType.SetWorkingPlaneYZ:
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    cmd = new FAO.Command(line, block, FAO.Command.CommandLink.SetWorkingPlaneYZ);
                    cmd.IsUpdated = true;
                    AddCommand(ref cmd, ref refcmd, ref sub);
                    break;
            }

            switch (cmd.CmdLink)
            {
                case FAO.Command.CommandLink.Move:
                    FAO.CommandMove m = cmd as FAO.CommandMove;
                    if (m != null)
                    {
                        switch (link)
                        {
                            case ncLexerLink.CmdType.AxisX:
                                IsParsed = double.TryParse(s.Replace(" ", ""), NbStyle, Culture, out resdbl);
                                if (IsParsed)
                                {
                                    m.P.X = resdbl;
                                    cmd.IsUpdated = true;
                                }
                                break;

                            case ncLexerLink.CmdType.AxisY:
                                IsParsed = double.TryParse(s.Replace(" ", ""), NbStyle, Culture, out resdbl);
                                if (IsParsed)
                                {
                                    m.P.Y = resdbl;
                                    cmd.IsUpdated = true;
                                }
                                break;

                            case ncLexerLink.CmdType.AxisZ:
                                IsParsed = double.TryParse(s.Replace(" ", ""), NbStyle, Culture, out resdbl);
                                if (IsParsed)
                                {
                                    m.P.Z = resdbl;
                                    cmd.IsUpdated = true;
                                }
                                break;

                            case ncLexerLink.CmdType.ArcCenterX:
                                IsParsed = double.TryParse(s.Replace(" ", ""), NbStyle, Culture, out resdbl);
                                if (IsParsed)
                                {
                                    m.C.X = resdbl;
                                    cmd.IsUpdated = true;
                                }
                                break;

                            case ncLexerLink.CmdType.ArcCenterY:
                                IsParsed = double.TryParse(s.Replace(" ", ""), NbStyle, Culture, out resdbl);
                                if (IsParsed)
                                {
                                    m.C.Y = resdbl;
                                    cmd.IsUpdated = true;
                                }
                                break;

                            case ncLexerLink.CmdType.ArcCenterZ:
                                IsParsed = double.TryParse(s.Replace(" ", ""), NbStyle, Culture, out resdbl);
                                if (IsParsed)
                                {
                                    m.C.Z = resdbl;
                                    cmd.IsUpdated = true;
                                }
                                break;

                            case ncLexerLink.CmdType.ArcRadius:
                                IsParsed = double.TryParse(s.Replace(" ", ""), NbStyle, Culture, out resdbl);
                                if (IsParsed)
                                {
                                    m.R.Value = resdbl;
                                    cmd.IsUpdated = true;
                                }
                                break;
                        }
                    }
                    break;

                case FAO.Command.CommandLink.SetReference:
                    FAO.SetReference sr = cmd as FAO.SetReference;
                    if (sr != null)
                    {
                        switch (link)
                        {
                            case ncLexerLink.CmdType.AxisX:
                                IsParsed = double.TryParse(s.Replace(" ", ""), NbStyle, Culture, out resdbl);
                                if (IsParsed)
                                {
                                    sr.P.X = resdbl;
                                    cmd.IsUpdated = true;
                                }
                                break;

                            case ncLexerLink.CmdType.AxisY:
                                IsParsed = double.TryParse(s.Replace(" ", ""), NbStyle, Culture, out resdbl);
                                if (IsParsed)
                                {
                                    sr.P.Y = resdbl;
                                    cmd.IsUpdated = true;
                                }
                                break;

                            case ncLexerLink.CmdType.AxisZ:
                                IsParsed = double.TryParse(s.Replace(" ", ""), NbStyle, Culture, out resdbl);
                                if (IsParsed)
                                {
                                    sr.P.Z = resdbl;
                                    cmd.IsUpdated = true;
                                }
                                break;
                        }
                    }
                    break;

                case FAO.Command.CommandLink.WorkOffset:
                    FAO.WorkOffSet wo = cmd as FAO.WorkOffSet;
                    if (wo != null)
                    {
                        switch (link)
                        {
                            case ncLexerLink.CmdType.AxisX:
                                IsParsed = double.TryParse(s.Replace(" ", ""), NbStyle, Culture, out resdbl);
                                if (IsParsed)
                                {
                                    wo.XYZOffSet.X = resdbl;
                                    cmd.IsUpdated = true;
                                }
                                break;

                            case ncLexerLink.CmdType.AxisY:
                                IsParsed = double.TryParse(s.Replace(" ", ""), NbStyle, Culture, out resdbl);
                                if (IsParsed)
                                {
                                    wo.XYZOffSet.Y = resdbl;
                                    cmd.IsUpdated = true;
                                }
                                break;

                            case ncLexerLink.CmdType.AxisZ:
                                IsParsed = double.TryParse(s.Replace(" ", ""), NbStyle, Culture, out resdbl);
                                if (IsParsed)
                                {
                                    wo.XYZOffSet.Z = resdbl;
                                    cmd.IsUpdated = true;
                                }
                                break;
                        }
                    }
                    break;

                case FAO.Command.CommandLink.Scale:
                    FAO.Scale sl = cmd as FAO.Scale;
                    if (sl != null)
                    {
                        switch (link)
                        {
                            case ncLexerLink.CmdType.AxisX:
                                IsParsed = double.TryParse(s.Replace(" ", ""), NbStyle, Culture, out resdbl);
                                if (IsParsed)
                                {
                                    sl.ScaleCenter.X = resdbl;
                                    cmd.IsUpdated = true;
                                }
                                break;

                            case ncLexerLink.CmdType.AxisY:
                                IsParsed = double.TryParse(s.Replace(" ", ""), NbStyle, Culture, out resdbl);
                                if (IsParsed)
                                {
                                    sl.ScaleCenter.Y = resdbl;
                                    cmd.IsUpdated = true;
                                }
                                break;

                            case ncLexerLink.CmdType.AxisZ:
                                IsParsed = double.TryParse(s.Replace(" ", ""), NbStyle, Culture, out resdbl);
                                if (IsParsed)
                                {
                                    sl.ScaleCenter.Z = resdbl;
                                    cmd.IsUpdated = true;
                                }
                                break;

                            case ncLexerLink.CmdType.ScaleX:
                                IsParsed = double.TryParse(s.Replace(" ", ""), NbStyle, Culture, out resdbl);
                                if (IsParsed)
                                {
                                    sl.ScaleFactor.X = resdbl;
                                    cmd.IsUpdated = true;
                                }
                                break;

                            case ncLexerLink.CmdType.ScaleY:
                                IsParsed = double.TryParse(s.Replace(" ", ""), NbStyle, Culture, out resdbl);
                                if (IsParsed)
                                {
                                    sl.ScaleFactor.Y = resdbl;
                                    cmd.IsUpdated = true;
                                }
                                break;

                            case ncLexerLink.CmdType.ScaleZ:
                                IsParsed = double.TryParse(s.Replace(" ", ""), NbStyle, Culture, out resdbl);
                                if (IsParsed)
                                {
                                    sl.ScaleFactor.Z = resdbl;
                                    cmd.IsUpdated = true;
                                }
                                break;

                            case ncLexerLink.CmdType.ScaleUniform:
                                IsParsed = double.TryParse(s.Replace(" ", ""), NbStyle, Culture, out resdbl);
                                if (IsParsed)
                                {
                                    sl.ScaleFactor.X = resdbl;
                                    sl.ScaleFactor.Y = resdbl;
                                    sl.ScaleFactor.Z = resdbl;
                                    cmd.IsUpdated = true;
                                }
                                break;
                        }
                    }
                    break;

                case FAO.Command.CommandLink.Rotate:
                    FAO.Rotate rt = cmd as FAO.Rotate;
                    if (rt != null)
                    {
                        switch (link)
                        {
                            case ncLexerLink.CmdType.AxisX:
                                IsParsed = double.TryParse(s.Replace(" ", ""), NbStyle, Culture, out resdbl);
                                if (IsParsed)
                                {
                                    rt.RotateCenter.X = resdbl;
                                    cmd.IsUpdated = true;
                                }
                                break;

                            case ncLexerLink.CmdType.AxisY:
                                IsParsed = double.TryParse(s.Replace(" ", ""), NbStyle, Culture, out resdbl);
                                if (IsParsed)
                                {
                                    rt.RotateCenter.Y = resdbl;
                                    cmd.IsUpdated = true;
                                }
                                break;

                            case ncLexerLink.CmdType.AxisZ:
                                IsParsed = double.TryParse(s.Replace(" ", ""), NbStyle, Culture, out resdbl);
                                if (IsParsed)
                                {
                                    rt.RotateCenter.Z = resdbl;
                                    cmd.IsUpdated = true;
                                }
                                break;

                            case ncLexerLink.CmdType.RotateAngle:
                                IsParsed = double.TryParse(s.Replace(" ", ""), NbStyle, Culture, out resdbl);
                                if (IsParsed)
                                {
                                    rt.RotateAngle.Value = resdbl;
                                    cmd.IsUpdated = true;
                                }
                                break;
                        }
                    }
                    break;

                case FAO.Command.CommandLink.SubCall:
                    FAO.SubCall sc = cmd as FAO.SubCall;
                    if (sc != null)
                    {
                        switch (link)
                        {
                            case ncLexerLink.CmdType.SubName:
                                sc.SubName = s;
                                sc.IsUpdated = true;
                                break;

                            case ncLexerLink.CmdType.SubRepetitions:
                                IsParsed = Int32.TryParse(s.Replace(" ", ""), NbStyle, Culture, out resint);
                                if (IsParsed)
                                {
                                    sc.Repetition.Value = resint;
                                }
                                break;
                        }
                    }
                    break;

                case FAO.Command.CommandLink.SubStart:
                    FAO.SubStart ss = cmd as FAO.SubStart;
                    if (ss != null)
                    {
                        switch (link)
                        {
                            case ncLexerLink.CmdType.SubName:
                                ss.SubName = s;
                                ss.IsUpdated = true;
                                break;
                        }
                    }
                    break;
            }
        }

        private void AddCommand(ref FAO.Command acmd, ref FAO.Command arefcmd, ref ncTree<FAO.Sub> asub)
        {
            if (acmd.IsUpdated)
            {
                if (acmd.CmdLink == FAO.Command.CommandLink.SubStart)
                {
                    asub = asub.AddChild(new FAO.Sub());
                    asub.Data.Name = ((FAO.SubStart)acmd).SubName;
                    asub.Data.Commands.Add(acmd);
                    arefcmd = new FAO.CommandMove(acmd.Line, acmd.Block);
                }
                else if (acmd.CmdLink == FAO.Command.CommandLink.SubEnd)
                {
                    asub.Data.Commands.Add(acmd);
                    if (!asub.IsRoot)
                    {
                        asub = asub.Parent;
                    }
                    arefcmd = new FAO.CommandMove(acmd.Line, acmd.Block);
                }
                else
                {
                    if (acmd.Block > 0 || acmd.CmdLink != FAO.Command.CommandLink.None)
                    {
                        asub.Data.Commands.Add(acmd);
                    }
                }
            }
            else
            {
                if (acmd.Block > 0)
                {
                    asub.Data.Commands.Add(new FAO.Command(acmd.Line, acmd.Block, FAO.Command.CommandLink.None));
                }
            }

            if (acmd.IsReference)
            {
                arefcmd = new FAO.Command();
                if (acmd.CmdLink == FAO.Command.CommandLink.Move)
                {
                    FAO.CommandMove am = (FAO.CommandMove)acmd;
                    if (am != null)
                    {
                        arefcmd = new FAO.CommandMove(acmd.Line, acmd.Block, (am.Type));
                    }
                }
            }

            acmd = arefcmd;
        }

        #endregion
    }

    public class ncLexer
    {
        #region public fields
        public List<ncLexerEntry> LexerEntries;
        #endregion

        #region constructors
        public ncLexer()
        {
            LexerEntries = new List<ncLexerEntry>();
        }
        #endregion

        #region styling
        public int[,] GetLineStyleTable(string line)
        {
            List<int[]> styleTableList = new List<int[]>();

            int MatchPos = 0;
            int Length = 0;
            int StyleId = 0;

            int startpos = 0;
            int endpos = line.Count();

            while (startpos < endpos)
            {
                MatchPos = endpos - startpos;
                Length = 0;
                StyleId = 0;

                CheckPatternStyling(line, startpos, ref MatchPos, ref Length, ref StyleId);

                if (Length > 0)
                {
                    if (MatchPos > 0)
                    {
                        styleTableList.Add(new int[3] { startpos, MatchPos, -1 });
                    }
                    startpos += MatchPos;
                    styleTableList.Add(new int[3] { startpos, Length, StyleId });
                    startpos += Length;
                }
                else
                {
                    styleTableList.Add(new int[3] { startpos, MatchPos, -1 });
                    startpos += MatchPos;
                }
            }

            int[,] styleTable = new int[styleTableList.Count(), 3];

            for (int i = 0; i < styleTableList.Count(); i++)
            {
                styleTable[i, 0] = styleTableList[i][0];
                styleTable[i, 1] = styleTableList[i][1];
                styleTable[i, 2] = styleTableList[i][2];
            }

            return styleTable;
        }

        private void CheckPatternStyling(string line, int startpos, ref int matchpos, ref int length, ref int styleid)
        {
            for (int i = 0; i < LexerEntries.Count(); i++)
            {
                if (LexerEntries[i].RegexPattern != null)
                {
                    Match MatchKey = LexerEntries[i].RegexPattern.Match(line.Substring(startpos));
                    if (MatchKey.Success)
                    {
                        if (MatchKey.Index < matchpos)
                        {
                            matchpos = MatchKey.Index;
                            length = MatchKey.Length;
                            styleid = i;
                        }
                        else if (MatchKey.Index == matchpos)
                        {
                            if (MatchKey.Length > length)
                            {
                                matchpos = MatchKey.Index;
                                length = MatchKey.Length;
                                styleid = i;
                            }
                        }

                        if (matchpos == 0)
                        {
                            if (LexerEntries[i].KeepIfFound)
                            {
                                return;
                            }
                            if (length == line.Length)
                            {
                                return;
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region parsing
        internal void CheckKeysRawoperation(string line, int startpos, ref int matchpos, ref int length, ref List<string> stringbuilder, ref List<ncLexerLink.CmdType> cmds)
        {
            //for (int i = 0; i < LexerStyles.Count(); i++)
            foreach (ncLexerEntry ls in LexerEntries)
            {
                if (ls.RegexPattern != null)
                {
                    Match MatchKey = ls.RegexPattern.Match(line.Substring(startpos));

                    if (MatchKey.Success)
                    {
                        if (MatchKey.Index < matchpos)
                        {
                            matchpos = MatchKey.Index;
                            length = MatchKey.Length;

                            stringbuilder = new List<string>();
                            cmds = new List<ncLexerLink.CmdType>();

                            ls.AddLinks(MatchKey, ref stringbuilder, ref cmds);
                        }
                        else if (MatchKey.Index == matchpos)
                        {
                            if (MatchKey.Length > length)
                            {
                                matchpos = MatchKey.Index;
                                length = MatchKey.Length;

                                stringbuilder = new List<string>();
                                cmds = new List<ncLexerLink.CmdType>();

                                ls.AddLinks(MatchKey, ref stringbuilder, ref cmds);
                            }
                        }

                        if (matchpos == 0)
                        {
                            if (ls.KeepIfFound)
                            {
                                return;
                            }
                            if (length == line.Length)
                            {
                                return;
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region methods
        public ncLexer Clone()
        {
            ncLexer NewLexerDefinition = (ncLexer)this.MemberwiseClone();
            NewLexerDefinition.LexerEntries = new List<ncLexerEntry>();
            foreach (ncLexerEntry style in this.LexerEntries)
            {
                NewLexerDefinition.LexerEntries.Add(style.Clone());
            }

            return NewLexerDefinition;
        }
        #endregion
    }

    public class ncLexerEntry
    {
        #region public fields

        public string CmdName = "NEW_CMD";
        public bool KeepIfFound = false;
        public List<ncLexerLink> Links = new List<ncLexerLink>();

        private string _pattern;
        public string Pattern
        {
            get
            {
                return _pattern;
            }
            set
            {
                _pattern = value;
                RegexPattern = new Regex(_pattern, RegexOptions.Compiled);
            }
        }
        [XmlIgnore]
        internal Regex RegexPattern;

        [XmlElement(Type = typeof(XmlColor))]
        public Color Color = Color.Black;
        #endregion

        #region constructors
        public ncLexerEntry() { }
        #endregion

        #region methods

        internal void AddLinks(Match matchkey, ref List<string> stringbuilder, ref List<ncLexerLink.CmdType> cmds)
        {
            foreach (ncLexerLink mo in Links)
            {
                if (mo.Extract)
                {
                    MatchCollection MatchExtract = mo.RegexPattern.Matches(matchkey.Value);
                    if (MatchExtract.Count > 0)
                    {
                        if (mo.ExtractId < MatchExtract.Count)
                        {
                            stringbuilder.Add(MatchExtract[mo.ExtractId].Value);
                            cmds.Add(mo.Cmd);
                        }
                    }
                }
                else
                {
                    stringbuilder.Add(mo.Pattern);
                    cmds.Add(mo.Cmd);
                }
            }
        }

        #endregion

        #region clone

        public ncLexerEntry Clone()
        {
            ncLexerEntry NewLexerStyle = (ncLexerEntry)this.MemberwiseClone();
            NewLexerStyle.Links = new List<ncLexerLink>();
            foreach (ncLexerLink links in this.Links)
            {
                NewLexerStyle.Links.Add(links.Clone());
            }

            return NewLexerStyle;
        }

        #endregion
    }

    public class ncLexerLink
    {
        #region enum
        public enum CmdType
        {
            None,
            BlockNumber,
            Rapid, 
            Linear, 
            CircularCW, 
            CircularCCW,
            AxisX, 
            AxisY, 
            AxisZ,
            ArcCenterX, 
            ArcCenterY, 
            ArcCenterZ, 
            ArcRadius,
            SetFeedRate, 
            SetSpindleSpeed,
            SetFeedRateModeInverted, 
            SetFeedRateModeNormal,
            Dwell,
            ToolCallByNumber,
            SetWorkingPlaneXY, 
            SetWorkingPlaneXZ, 
            SetWorkingPlaneYZ,
            SetXYZAbsolute, 
            SetIJKAbsolute,
            SetXYZRelative, 
            SetIJKRelative,
            SetReference, 
            ResetReference,
            WorkOffset,
            Scale,
            ResetScale,
            ScaleX, 
            ScaleY, 
            ScaleZ, 
            ScaleUniform,
            Rotate,
            ResetRotate,
            RotateAngle,
            SubCall,
            SubStart,
            SubName,
            SubRepetitions,
            SubEnd,
        }
        #endregion

        #region public fields
        public int ExtractId;
        public bool Extract;
        public CmdType Cmd;

        private string _pattern;
        public string Pattern
        {
            get
            {
                return _pattern;
            }
            set
            {
                _pattern = value;
                RegexPattern = new Regex(_pattern, RegexOptions.Compiled);
            }
        }
        [XmlIgnore]
        internal Regex RegexPattern;
        #endregion

        #region constructors
        public ncLexerLink()
        {
            this.Cmd = CmdType.None;
            this.Extract = true;
            this.ExtractId = 0;
        }

        public ncLexerLink(string pattern, CmdType cmd, bool extract = true, int extractid = 0)
        {
            this.Pattern = pattern;
            this.Cmd = cmd;
            this.Extract = extract;
            this.ExtractId = extractid;
        }
        #endregion

        #region methods
        internal void CheckPattern(string s, ref int startpos, ref int length)
        {
            if (Extract)
            {
                MatchCollection MatchExtract = RegexPattern.Matches(s);
                if (MatchExtract.Count > 0)
                {
                    if (ExtractId < MatchExtract.Count)
                    {
                        startpos = MatchExtract[ExtractId].Index;
                        length = MatchExtract[ExtractId].Length;
                    }
                }
            }
        }

        public int[] GetMatch(string s)
        {
            int startpos = 0;
            int lenght = 0;

            CheckPattern(s, ref startpos, ref lenght);

            return new int[] { startpos, lenght };
        }
        #endregion

        #region clone
        public ncLexerLink Clone()
        {
            ncLexerLink NewMatchOption = (ncLexerLink)this.MemberwiseClone();

            return NewMatchOption;
        }
        #endregion
    }
}
