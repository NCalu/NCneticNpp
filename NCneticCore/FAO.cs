using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NCneticCore
{
    public class ncMove
    {
        #region enums
        public enum MoveType { Undefined, Rapid, Linear, CircularCW, CircularCCW };
        public enum WorkingPlane { XY, XZ, YZ }
        public enum ArcType { Absolute, Relative }
        #endregion

        #region public fields
        public int Line;
        public int Block;
        public string MoveGuid = string.Empty;
        public MoveType Type = MoveType.Rapid;

        // Target
        public ncCoord P = new ncCoord(0, 0, 0);
        public ncCoord C = new ncCoord(0, 0, 0);
        public double R = 0.0;

        // Origin
        public ncCoord P0 = new ncCoord(0, 0, 0);

        // Params
        public double Length = 0;

        // Properies
        public WorkingPlane WorkPlane = WorkingPlane.XY;
        public double F = 0.0;
        public bool InvertedF = false;
        public double S = 0.0;
        public int ToolNb = 0;

        // Color
        public Color Color;
        #endregion

        #region constructors
        public ncMove() { }

        #endregion

        #region methods
        public ncMove Clone()
        {
            ncMove NewMove = (ncMove)this.MemberwiseClone();

            NewMove.P = this.P.Clone();
            NewMove.C = this.C.Clone();
            NewMove.P0 = this.P0.Clone();

            return NewMove;
        }
        #endregion
    }

    public class ncCoord
    {
        #region fields
        public double X;
        public double Y;
        public double Z;
        #endregion

        #region constructors

        public ncCoord()
        {
            this.X = Math.Sqrt(-1);
            this.Y = Math.Sqrt(-1);
            this.Z = Math.Sqrt(-1);
        }

        public ncCoord(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }
        #endregion

        #region methods
        public ncCoord Clone()
        {
            return (ncCoord)this.MemberwiseClone();
        }
        #endregion
    }

    public class ncFcts
    {
        public static List<ncMove> PolygonizeArc(ncMove move, int arcsec, bool arcout = false)
        {
            List<ncMove> Polygonization = new List<ncMove>();

            FAO.Vec V0 = new FAO.Vec(move.C, move.P0);
            FAO.Vec V1 = new FAO.Vec(move.C, move.P);
            FAO.Vec V2 = FAO.Vec.VecCrossProduct(V0, V1);

            if (move.WorkPlane == ncMove.WorkingPlane.XY)
            {
                V0 = new FAO.Vec(new ncCoord(move.C.X, move.C.Y, move.P0.Z), move.P0);
                V1 = new FAO.Vec(new ncCoord(move.C.X, move.C.Y, move.P0.Z), new ncCoord(move.P.X, move.P.Y, move.P0.Z));
                V2 = FAO.Vec.VecCrossProduct(V0, V1);
            }
            else if (move.WorkPlane == ncMove.WorkingPlane.XZ)
            {
                V0 = new FAO.Vec(new ncCoord(move.C.X, move.P0.Y, move.C.Z), move.P0);
                V1 = new FAO.Vec(new ncCoord(move.C.X, move.P0.Y, move.C.Z), new ncCoord(move.P.X, move.P0.Y, move.P.Z));
                V2 = FAO.Vec.VecCrossProduct(V0, V1);
            }
            else if (move.WorkPlane == ncMove.WorkingPlane.YZ)
            {
                V0 = new FAO.Vec(new ncCoord(move.P0.X, move.C.Y, move.C.Z), move.P0);
                V1 = new FAO.Vec(new ncCoord(move.P0.X, move.C.Y, move.C.Z), new ncCoord(move.P0.X, move.P.Y, move.P.Z));
                V2 = FAO.Vec.VecCrossProduct(V0, V1);
            }

            V2 = FAO.Vec.Normalyze(V2);

            double[,] TransformMatrix = new double[,] { { 1.0, 0.0, 0.0 }, { 0.0, 1.0, 0.0 }, { 0.0, 0.0, 1.0 } };

            if (move.WorkPlane == ncMove.WorkingPlane.XY)
            {
                V2 = new FAO.Vec(new ncCoord(0, 0, 0), new ncCoord(0, 0, 1));
            }
            else if (move.WorkPlane == ncMove.WorkingPlane.XZ)
            {
                V2 = new FAO.Vec(new ncCoord(0, 0, 0), new ncCoord(0, 1, 0));
            }
            else if (move.WorkPlane == ncMove.WorkingPlane.YZ)
            {
                V2 = new FAO.Vec(new ncCoord(0, 0, 0), new ncCoord(1, 0, 0));
            }

            if (FAO.Vec.VecNorm(FAO.Vec.Normalyze(V2)) != 0)
            {
                FAO.Vec Xt = FAO.Vec.Normalyze(V0);
                FAO.Vec Zt = FAO.Vec.Normalyze(V2);
                double dir = FAO.Vec.VecDotProduct(new FAO.Vec(new ncCoord(1, 1, 1)), V2);
                FAO.Vec Yt = FAO.Vec.VecCrossProduct(Zt, Xt);

                TransformMatrix[0, 0] = Xt.X;
                TransformMatrix[0, 1] = Xt.Y;
                TransformMatrix[0, 2] = Xt.Z;

                TransformMatrix[1, 0] = Yt.X;
                TransformMatrix[1, 1] = Yt.Y;
                TransformMatrix[1, 2] = Yt.Z;

                TransformMatrix[2, 0] = Zt.X;
                TransformMatrix[2, 1] = Zt.Y;
                TransformMatrix[2, 2] = Zt.Z;
            }
            else
            {
                // Should never happen but ...
                Polygonization.Add(move.Clone());
                return Polygonization;
            }

            FAO.Vec V0t = FAO.Vec.MatVecProduct(TransformMatrix, V0);
            FAO.Vec V1t = FAO.Vec.MatVecProduct(TransformMatrix, V1);

            double radius = FAO.Vec.VecNorm(V0t);
            double a0 = Math.Atan2(V0t.Y, V0t.X);
            double a1 = Math.Atan2(V1t.Y, V1t.X);

            if (Math.Abs(a1) < 1E-6)
            {
                if (move.Type == ncMove.MoveType.CircularCW)
                {
                    a1 = -2 * Math.PI;
                }
                else
                {
                    a1 = 2 * Math.PI;
                }
            }
            else if (Math.Abs(a1 - Math.PI) < 1E-6)
            {
                if (move.Type == ncMove.MoveType.CircularCW)
                {
                    a1 = -Math.PI;
                }
                else
                {
                    a1 = Math.PI;
                }
            }
            else
            {
                if (move.Type == ncMove.MoveType.CircularCW)
                {
                    if (a1 > 0)
                    {
                        a1 = a1 - 2 * Math.PI;
                    }
                }
                else
                {
                    if (a1 < 0)
                    {
                        a1 = a1 + 2 * Math.PI;
                    }
                }
            }

            double da = 2 * Math.PI / (double)arcsec;

            int nseg = (int)Math.Ceiling(Math.Abs(a1 - a0) / da);

            ncCoord P0 = new ncCoord(move.P0.X, move.P0.Y, move.P0.Z);

            double[,] InvTransformMatrix = FAO.Mat.MatInv(TransformMatrix);
            for (int i = 1; i <= nseg; i++)
            {
                ncMove SegMove = move.Clone();

                if (!arcout)
                {
                    SegMove.Type = ncMove.MoveType.Linear;
                    SegMove.R = 0.0;
                }

                if (move.WorkPlane == ncMove.WorkingPlane.YZ)
                {
                    SegMove.P = new ncCoord(
                        move.P0.X + i * (move.P.X - move.P0.X) / nseg,
                        InvTransformMatrix[1, 0] * (radius * Math.Cos(a0 + i * (a1 - a0) / nseg)) + InvTransformMatrix[1, 1] * (radius * Math.Sin(a0 + i * (a1 - a0) / nseg)) + move.C.Y,
                        InvTransformMatrix[2, 0] * (radius * Math.Cos(a0 + i * (a1 - a0) / nseg)) + InvTransformMatrix[2, 1] * (radius * Math.Sin(a0 + i * (a1 - a0) / nseg)) + move.C.Z);
                }
                else if (move.WorkPlane == ncMove.WorkingPlane.XZ)
                {
                    SegMove.P = new ncCoord(
                        InvTransformMatrix[0, 0] * (radius * Math.Cos(a0 + i * (a1 - a0) / nseg)) + InvTransformMatrix[0, 1] * (radius * Math.Sin(a0 + i * (a1 - a0) / nseg)) + move.C.X,
                        move.P0.Y + i * (move.P.Y - move.P0.Y) / nseg,
                        InvTransformMatrix[2, 0] * (radius * Math.Cos(a0 + i * (a1 - a0) / nseg)) + InvTransformMatrix[2, 1] * (radius * Math.Sin(a0 + i * (a1 - a0) / nseg)) + move.C.Z);
                }
                else if (move.WorkPlane == ncMove.WorkingPlane.XY)
                {
                    SegMove.P = new ncCoord(
                        InvTransformMatrix[0, 0] * (radius * Math.Cos(a0 + i * (a1 - a0) / nseg)) + InvTransformMatrix[0, 1] * (radius * Math.Sin(a0 + i * (a1 - a0) / nseg)) + move.C.X,
                        InvTransformMatrix[1, 0] * (radius * Math.Cos(a0 + i * (a1 - a0) / nseg)) + InvTransformMatrix[1, 1] * (radius * Math.Sin(a0 + i * (a1 - a0) / nseg)) + move.C.Y,
                        move.P0.Z + i * (move.P.Z - move.P0.Z) / nseg);
                }
                SegMove.Length = Math.Abs(2.0 * radius * Math.Sin(((a1 - a0) / nseg) / 2.0));

                SegMove.P0 = P0.Clone();
                P0 = SegMove.P.Clone();
                SegMove.Length = move.Length / nseg;
                Polygonization.Add(SegMove.Clone());
            }
            return Polygonization;
        }
    }

    internal class FAO
    {
        #region fao

        internal class SubCollection
        {
            #region internal Fields
            internal List<ncMove> Moves;
            internal ncTree<Sub> Sub;

            internal bool Cancel = false;
            #endregion

            #region constructors

            internal SubCollection()
            {
                this.Moves = new List<ncMove>();
                this.Sub = new ncTree<Sub>(new Sub());
            }

            #endregion

            #region events

            internal event ReportProgressComputingEventHandler ReportProgressComputing;

            internal delegate void ReportProgressComputingEventHandler(object source, ReportProgressComputingEventArgs e);

            internal class ReportProgressComputingEventArgs : EventArgs
            {
                public bool Cancel = false;

                internal int progress;

                internal ReportProgressComputingEventArgs(int p)
                {
                    progress = p;
                }
            }
            #endregion

            #region methods

            internal SubCollection Reprocess(ncMachineDef def)
            {
                CommandMove MoveRef = new CommandMove(def);
                SubCollection ReprocessedSubCollection = this.Reprocess(def.Clone(), ref MoveRef);

                if (!ReprocessedSubCollection.Moves.Any())
                {
                    MoveRef.Line = -999;
                    MoveRef.Type = ncMove.MoveType.Rapid;
                    ReprocessedSubCollection.Moves.Add(MoveRef.ConvertMove());
                }
                else
                {
                    if (ReprocessedSubCollection.Moves.Find(x => x.ToolNb != 0) != null)
                    {
                        int tnb = ReprocessedSubCollection.Moves[ReprocessedSubCollection.Moves.FindIndex(x => x.ToolNb != 0)].ToolNb;
                        for (int i = 0; i < ReprocessedSubCollection.Moves.Count; i++)
                        {
                            if (ReprocessedSubCollection.Moves[i].ToolNb == 0)
                            {
                                ReprocessedSubCollection.Moves[i].ToolNb = tnb;
                            }
                            else
                            {
                                i = ReprocessedSubCollection.Moves.Count;
                            }
                        }
                    }
                }

                return ReprocessedSubCollection;
            }

            internal SubCollection Reprocess(ncMachineDef def, ref CommandMove MoveRef)
            {
                SubCollection ReprocessedSubCollection = new SubCollection();
                ReprocessedSubCollection.Sub.Data.Name = this.Sub.Data.Name;

                if (Cancel)
                {
                    return ReprocessedSubCollection;
                }

                Command cmd;
                int p = 0;
                for (int cnt = 0; cnt < this.Sub.Data.Commands.Count(); cnt++)
                {
                    if (p != cnt * 100 / this.Sub.Data.Commands.Count())
                    {
                        p = cnt * 100 / this.Sub.Data.Commands.Count();

                        ReportProgressComputingEventArgs eventArgs = new ReportProgressComputingEventArgs(p);
                        ReportProgressComputing?.Invoke(this, eventArgs);

                        if (eventArgs.Cancel)
                        {
                            Cancel = true;
                            return ReprocessedSubCollection;
                        }
                    }

                    cmd = this.Sub.Data.Commands[cnt];

                    switch (cmd.CmdLink)
                    {
                        case Command.CommandLink.Move:
                            var m = cmd as CommandMove;
                            if (m != null)
                            {
                                if (m.Type == ncMove.MoveType.Undefined)
                                {
                                    m.Type = MoveRef.Type;
                                }
                                else
                                {
                                    MoveRef.Type = m.Type;
                                }

                                switch (m.Type)
                                {
                                    case ncMove.MoveType.Rapid:
                                    case ncMove.MoveType.Linear:
                                        ReprocessedSubCollection.Moves.Add(m.Clone().ComputeMove(ref MoveRef, def).ConvertMove());
                                        break;

                                    case ncMove.MoveType.CircularCW:
                                    case ncMove.MoveType.CircularCCW:
                                        ReprocessedSubCollection.Moves.Add(m.Clone().ComputeMove(ref MoveRef, def).ReComputeCircularMove(def).ConvertMove());
                                        break;
                                }
                            }
                            break;

                        case Command.CommandLink.SetFS:
                            SetFS sf = cmd as SetFS;
                            if (sf != null)
                            {
                                if (!double.IsNaN(sf.FeedRate.Value))
                                {
                                    MoveRef.F.Value = sf.FeedRate.Value * def.ConversionFactorF;
                                }
                                if (!double.IsNaN(sf.SpindleSpeed.Value))
                                {
                                    MoveRef.S.Value = sf.SpindleSpeed.Value * def.ConversionFactorS;
                                }
                                if (sf.FMode == SetFS.SetFMode.Normal)
                                {
                                    MoveRef.InvertedF = false;
                                }
                                if (sf.FMode == SetFS.SetFMode.Inverted)
                                {
                                    MoveRef.InvertedF = true;
                                }
                            }
                            break;

                        case Command.CommandLink.ToolCall:
                            ToolCall tc = cmd as ToolCall;
                            if (tc != null)
                            {
                                MoveRef.ToolNb = tc.ToolNb;
                            }
                            break;

                        case Command.CommandLink.SetXYZAbsolute:
                            MoveRef.XYZRelative = false;
                            break;

                        case Command.CommandLink.SetIJKAbsolute:
                            MoveRef.IJKType = ncMove.ArcType.Absolute;
                            break;

                        case Command.CommandLink.SetXYZRelative:
                            MoveRef.XYZRelative = true;
                            break;

                        case Command.CommandLink.SetIJKRelative:
                            MoveRef.IJKType = ncMove.ArcType.Relative;
                            break;

                        case Command.CommandLink.SetWorkingPlaneXY:
                            if (MoveRef.WorkPlane != ncMove.WorkingPlane.XY)
                            {
                                MoveRef.RotateAngle = 0.0;
                                MoveRef.RotateCenter = new CommandCoord(0, 0, 0);
                                MoveRef.WorkPlane = ncMove.WorkingPlane.XY;
                            }
                            break;

                        case Command.CommandLink.SetWorkingPlaneXZ:
                            if (MoveRef.WorkPlane != ncMove.WorkingPlane.XZ)
                            {
                                MoveRef.RotateAngle = 0.0;
                                MoveRef.RotateCenter = new CommandCoord(0, 0, 0);
                                MoveRef.WorkPlane = ncMove.WorkingPlane.XZ;
                            }
                            break;

                        case Command.CommandLink.SetWorkingPlaneYZ:
                            if (MoveRef.WorkPlane != ncMove.WorkingPlane.YZ)
                            {
                                MoveRef.RotateAngle = 0.0;
                                MoveRef.RotateCenter = new CommandCoord(0, 0, 0);
                                MoveRef.WorkPlane = ncMove.WorkingPlane.YZ;
                            }
                            break;

                        case Command.CommandLink.SetReference:
                            var sr = cmd as SetReference;
                            if (sr != null)
                            {
                                if (sr.Reset)
                                {
                                    MoveRef.PRef.X = 0.0;
                                    MoveRef.PRef.Y = 0.0;
                                    MoveRef.PRef.Z = 0.0;
                                }
                                else
                                {
                                    if (!double.IsNaN(sr.P.X)) { MoveRef.PRef.X = -sr.P.X * def.ConversionFactorX + MoveRef.P.X; }
                                    if (!double.IsNaN(sr.P.Y)) { MoveRef.PRef.Y = -sr.P.Y * def.ConversionFactorY + MoveRef.P.Y; }
                                    if (!double.IsNaN(sr.P.Z)) { MoveRef.PRef.Z = -sr.P.Z * def.ConversionFactorZ + MoveRef.P.Z; }
                                }
                            }
                            break;

                        case Command.CommandLink.WorkOffset:
                            var wo = cmd as WorkOffSet;
                            if (wo != null)
                            {
                                if (!double.IsNaN(wo.XYZOffSet.X)) { MoveRef.POffset.X = wo.XYZOffSet.X * def.ConversionFactorX; }
                                if (!double.IsNaN(wo.XYZOffSet.Y)) { MoveRef.POffset.Y = wo.XYZOffSet.Y * def.ConversionFactorY; }
                                if (!double.IsNaN(wo.XYZOffSet.Z)) { MoveRef.POffset.Z = wo.XYZOffSet.Z * def.ConversionFactorZ; }
                            }
                            break;

                        case Command.CommandLink.Scale:
                            var sl = cmd as Scale;
                            if (sl != null)
                            {
                                if (MoveRef.ScaleFactor == null)
                                {
                                    MoveRef.ScaleFactor = new CommandCoord(1.0, 1.0, 1.0);
                                    MoveRef.ScaleCenter = new CommandCoord(0.0, 0.0, 0.0);
                                }

                                MoveRef.ScaleCenter.X = sl.ScaleCenter.X * def.ConversionFactorX + MoveRef.PRef.X + MoveRef.POffset.X;
                                MoveRef.ScaleCenter.Y = sl.ScaleCenter.Y * def.ConversionFactorY + MoveRef.PRef.Y + MoveRef.POffset.Y;
                                MoveRef.ScaleCenter.Z = sl.ScaleCenter.Z * def.ConversionFactorZ + MoveRef.PRef.Z + MoveRef.POffset.Z;
                                MoveRef.ScaleFactor = sl.ScaleFactor.Clone();
                            }
                            break;

                        case Command.CommandLink.Rotate:
                            var rt = cmd as Rotate;
                            if (rt != null)
                            {
                                MoveRef.RotateCenter.X = rt.RotateCenter.X * def.ConversionFactorX + MoveRef.PRef.X + MoveRef.POffset.X;
                                MoveRef.RotateCenter.Y = rt.RotateCenter.Y * def.ConversionFactorY + MoveRef.PRef.Y + MoveRef.POffset.Y;
                                MoveRef.RotateCenter.Z = rt.RotateCenter.Z * def.ConversionFactorZ + MoveRef.PRef.Z + MoveRef.POffset.Z;

                                MoveRef.RotateAngle = rt.RotateAngle.Value * def.ConversionFactorAngle * Math.PI / 180.0;
                            }
                            break;

                        case Command.CommandLink.SubCall:
                            var sc = cmd as SubCall;
                            if (sc != null)
                            {
                                foreach (ncTree<Sub> SubPgmNode in this.Sub.Children)
                                {
                                    if (SubPgmNode.Data.Name == sc.SubName)
                                    {
                                        for (int i = 0; i < sc.Repetition.Value; i++)
                                        {
                                            SubCollection ChildSubCollection = new SubCollection();
                                            ChildSubCollection.Sub.Children = this.Sub.Children;
                                            ChildSubCollection.Sub.Data = SubPgmNode.Data;

                                            SubCollection ReprocessedChildSubCollection = ChildSubCollection.Reprocess(def, ref MoveRef);
                                            ReprocessedSubCollection.Moves.AddRange(ReprocessedChildSubCollection.Moves);
                                        }
                                    }
                                }
                            }
                            break;
                    }
                }

                ReportProgressComputing?.Invoke(this, new ReportProgressComputingEventArgs(100));

                return ReprocessedSubCollection;
            }

            #endregion
        }

        internal class Sub
        {
            #region internal Fields
            internal string Name;
            internal List<Command> Commands;
            #endregion

            #region Constructors

            internal Sub()
            {
                this.Name = "MAIN";
                this.Commands = new List<Command>();
            }

            #endregion

        }

        internal class BevelParams
        {
            #region enum
            internal enum TangentMacro { Undefined, None, Tracking, ToPrevious, ToNext, ApplyToPrevious }
            #endregion

            #region fields
            internal double AngleA;
            internal double AngleB;
            internal TangentMacro Tangent;
            #endregion

            #region constructors
            internal BevelParams()
            {
                this.AngleA = Math.Sqrt(-1);
                this.AngleB = Math.Sqrt(-1);
                this.Tangent = TangentMacro.Undefined;
            }
            #endregion

            #region methods
            internal BevelParams Clone()
            {
                BevelParams NewBevelParams = (BevelParams)this.MemberwiseClone();
                return NewBevelParams;
            }
            #endregion
        }

        #endregion

        #region commands

        internal class Command
        {
            #region enum
            internal enum CommandLink
            {
                None,
                Move,
                SetFS,
                SetXYZAbsolute, 
                SetIJKAbsolute,
                SetXYZRelative, 
                SetIJKRelative,
                SetWorkingPlaneXY,
                SetWorkingPlaneXZ, 
                SetWorkingPlaneYZ,
                SetReference,
                WorkOffset,
                Scale, 
                Rotate,
                SubCall, 
                SubStart, 
                SubEnd,
                ToolCall,
            }

            #endregion

            #region internal fields
            internal CommandLink CmdLink;
            internal int Line;
            internal int Block;

            internal bool IsReference;
            internal bool IsUpdated;
            #endregion

            #region constructors
            internal Command()
            {
                this.Line = 0;
                this.Block = 0;
                this.CmdLink = CommandLink.None;

                this.IsReference = false;
                this.IsUpdated = false;
            }

            internal Command(int l, int b, CommandLink cl)
            {
                this.Line = l;
                this.Block = b;
                this.CmdLink = cl;

                this.IsReference = false;
                this.IsUpdated = false;
            }

            #endregion
        }

        internal class CommandMove : Command
        {
            #region fields
            internal ncMove.MoveType Type = ncMove.MoveType.Undefined;

            // Target
            internal CommandCoord P = new CommandCoord();
            internal CommandCoord C = new CommandCoord();
            internal Input R = new Input();

            // Origin
            internal CommandCoord P0 = new CommandCoord();

            // Ref
            internal CommandCoord PRef = new CommandCoord();

            // Work Offset
            internal CommandCoord POffset = new CommandCoord();

            // Rotate
            internal double RotateAngle = 0.0;
            internal CommandCoord RotateCenter = new CommandCoord(0, 0, 0);

            // Scaling
            internal CommandCoord ScaleFactor;
            internal CommandCoord ScaleCenter;

            // Params
            internal double Length = 0;

            // Properties
            internal ncMove.WorkingPlane WorkPlane = ncMove.WorkingPlane.XY;
            internal Input F = new Input();
            internal bool InvertedF = false;
            internal Input S = new Input();
            internal int ToolNb = 0;

            // Arc Def
            internal ncMove.ArcType IJKType = ncMove.ArcType.Absolute;
            internal bool XYZRelative = false;

            #endregion

            #region constructors
            internal CommandMove(int l, int b)
            {
                this.CmdLink = CommandLink.Move;
                this.IsReference = true;

                this.Line = l;
                this.Block = b;

                this.Type = ncMove.MoveType.Undefined;
            }

            internal CommandMove(int l, int b, ncMove.MoveType mt)
            {
                this.Line = l;
                this.Block = b;

                this.CmdLink = CommandLink.Move;
                this.IsReference = true;
                this.Type = mt;
            }

            internal CommandMove(ncMachineDef def)
            {
                this.Type = ncMove.MoveType.Rapid;
                this.IsReference = true;

                this.P.X = def.HomePosXYZ.X * def.ConversionFactorX;
                this.P.Y = def.HomePosXYZ.Y * def.ConversionFactorY;
                this.P.Z = def.HomePosXYZ.Z * def.ConversionFactorZ;

                this.S = new Input(0.0);
                this.F = new Input(0.0);

                this.PRef = new CommandCoord(0, 0, 0);

                this.POffset = new CommandCoord(0, 0, 0);

                this.P0 = this.P.Clone();

                this.RotateAngle = 0.0;
                this.RotateCenter = new CommandCoord(0, 0, 0);

                this.XYZRelative = def.XYZRelative;
                this.IJKType = def.IJKType;

                this.WorkPlane = def.DefaultWorkPlane;

                this.ToolNb = 0;
            }

            #endregion

            #region methods

            internal CommandMove Clone()
            {
                CommandMove NewMove = (CommandMove)this.MemberwiseClone();

                NewMove.P = this.P.Clone();
                NewMove.C = this.C.Clone();

                NewMove.R = new Input(this.R.Value);
                NewMove.F = new Input(this.F.Value);
                NewMove.S = new Input(this.S.Value);

                NewMove.PRef = this.PRef.Clone();

                NewMove.POffset = this.POffset.Clone();

                NewMove.P0 = this.P0.Clone();

                if (NewMove.ScaleFactor != null)
                {
                    NewMove.ScaleFactor = this.ScaleFactor.Clone();
                }

                if (NewMove.ScaleCenter != null)
                {
                    NewMove.ScaleCenter = this.ScaleCenter.Clone();
                }

                return NewMove;
            }

            internal ncMove ConvertMove()
            {
                ncMove m = new ncMove();

                m.Line = this.Line;
                m.Block = this.Block;

                m.Type = this.Type;

                m.P = new ncCoord(this.P.X, this.P.Y, this.P.Z);
                m.C = new ncCoord(this.C.X, this.C.Y, this.C.Z);

                m.R = this.R.Value;
                m.F = this.F.Value;
                m.InvertedF = this.InvertedF;
                m.S = this.S.Value;

                // Origin
                m.P0 = new ncCoord(this.P0.X, this.P0.Y, this.P0.Z);

                // Params
                m.Length = this.Length;

                // Move
                m.WorkPlane = this.WorkPlane;

                m.ToolNb = this.ToolNb;

                return m;
            }

            internal CommandMove ComputeMove(ref CommandMove mr, ncMachineDef def)
            {
                // Properties From Reference
                this.PropertiesFromRef(ref mr, def);

                // Coords
                this.DefP(mr, def);

                if (this.Type == ncMove.MoveType.CircularCCW || this.Type == ncMove.MoveType.CircularCW)
                {
                    if (def.IJKModeAutoDetect)
                    {
                        this.AutoDetectArc(ref mr, def);
                    }
                    this.DefC(mr, def);
                }

                // Properties
                this.UpDateProperties(ref mr);

                // Update MoveRef
                this.UpDateMoveRef(ref mr, def);

                return this.Clone();
            }

            internal CommandMove ReComputeCircularMove(ncMachineDef def)
            {
                bool IsValid = false;

                double d = 0.0;
                double R0 = 0.0;
                double R1 = 0.0;
                double Alpha = 0.0;

                double tol;

                if (this.Type == ncMove.MoveType.CircularCW)
                {
                    if (this.WorkPlane == ncMove.WorkingPlane.YZ)
                    {
                        d = Math.Sqrt(Math.Pow(this.P.Y - this.P0.Y, 2) + Math.Pow(this.P.Z - this.P0.Z, 2));
                        R0 = Math.Sqrt(Math.Pow(this.C.Y - this.P0.Y, 2) + Math.Pow(this.C.Z - this.P0.Z, 2));
                        R1 = Math.Sqrt(Math.Pow(this.P.Y - this.C.Y, 2) + Math.Pow(this.P.Z - this.C.Z, 2));
                        this.C.X = this.P.X;
                    }
                    else if (this.WorkPlane == ncMove.WorkingPlane.XZ)
                    {
                        d = Math.Sqrt(Math.Pow(this.P.X - this.P0.X, 2) + Math.Pow(this.P.Z - this.P0.Z, 2));
                        R0 = Math.Sqrt(Math.Pow(this.C.X - this.P0.X, 2) + Math.Pow(this.C.Z - this.P0.Z, 2));
                        R1 = Math.Sqrt(Math.Pow(this.P.X - this.C.X, 2) + Math.Pow(this.P.Z - this.C.Z, 2));
                        this.C.Y = this.P.Y;
                    }
                    else if (this.WorkPlane == ncMove.WorkingPlane.XY)
                    {
                        d = Math.Sqrt(Math.Pow(this.P.X - this.P0.X, 2) + Math.Pow(this.P.Y - this.P0.Y, 2));
                        R0 = Math.Sqrt(Math.Pow(this.C.X - this.P0.X, 2) + Math.Pow(this.C.Y - this.P0.Y, 2));
                        R1 = Math.Sqrt(Math.Pow(this.P.X - this.C.X, 2) + Math.Pow(this.P.Y - this.C.Y, 2));
                        this.C.Z = this.P.Z;
                    }

                    if (double.IsNaN(this.R.Value))
                    {
                        tol = Math.Max(R0, R1) / 100;

                        this.R = new Input(0.0); ;
                        if (Math.Abs(R0 - R1) < tol)
                        {
                            if (Math.Abs(R0 * 2 - d) < tol || R0 * 2 > d)
                            {
                                IsValid = true;
                                this.R.Value = R0;
                            }
                        }
                        else
                        {
                            if (def.CircularMoveCorrection)
                            {
                                if (this.WorkPlane == ncMove.WorkingPlane.YZ)
                                {
                                    IsValid = true;
                                    Alpha = Math.Atan2(this.P.Z - this.C.Z, this.P.Y - this.C.Y);
                                    this.P.Y = this.C.Y + R0 * Math.Cos(Alpha);
                                    this.P.Z = this.C.Z + R0 * Math.Sin(Alpha);
                                    this.R.Value = R0;
                                }
                                else if (this.WorkPlane == ncMove.WorkingPlane.XZ)
                                {
                                    IsValid = true;
                                    Alpha = Math.Atan2(this.P.Z - this.C.Z, this.P.X - this.C.X);
                                    this.P.X = this.C.X + R0 * Math.Cos(Alpha);
                                    this.P.Z = this.C.Z + R0 * Math.Sin(Alpha);
                                    this.R.Value = R0;
                                }
                                else if (this.WorkPlane == ncMove.WorkingPlane.XY)
                                {
                                    IsValid = true;
                                    Alpha = Math.Atan2(this.P.Y - this.C.Y, this.P.X - this.C.X);
                                    this.P.X = this.C.X + R0 * Math.Cos(Alpha);
                                    this.P.Y = this.C.Y + R0 * Math.Sin(Alpha);
                                    this.R.Value = R0;
                                }
                            }
                        }
                    }
                    else
                    {
                        this.R.Value = Math.Abs(this.R.Value);

                        if (this.R.Value * 2 < d)
                        {
                            if (def.CircularMoveCorrection)
                            {
                                this.R.Value = d / 2.0;
                            }
                        }

                        if (this.WorkPlane == ncMove.WorkingPlane.YZ)
                        {
                            double Y2 = (this.P0.Y + this.P.Y) / 2.0;
                            double Z2 = (this.P0.Z + this.P.Z) / 2.0;

                            IsValid = true;

                            this.C.X = this.P.X;
                            this.C.Y = Y2 + Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P.Z - this.P0.Z) / d);
                            this.C.Z = Z2 + Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P0.Y - this.P.Y) / d);

                            this.GetArcProperties(false);
                            double Larc = this.Length;

                            this.C.Y = Y2 - Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P.Z - this.P0.Z) / d);
                            this.C.Z = Z2 - Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P0.Y - this.P.Y) / d);

                            this.GetArcProperties(false);

                            if (Larc < this.Length)
                            {
                                this.C.Y = Y2 + Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P.Z - this.P0.Z) / d);
                                this.C.Z = Z2 + Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P0.Y - this.P.Y) / d);
                            }
                            this.GetArcProperties();
                        }
                        else if (this.WorkPlane == ncMove.WorkingPlane.XZ)
                        {
                            double X2 = (this.P0.X + this.P.X) / 2.0;
                            double Z2 = (this.P0.Z + this.P.Z) / 2.0;

                            IsValid = true;

                            this.C.X = X2 + Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P.Z - this.P0.Z) / d);
                            this.C.Y = this.P.Y;
                            this.C.Z = Z2 + Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P0.X - this.P.X) / d);

                            this.GetArcProperties(false);
                            double Larc = this.Length;

                            this.C.X = X2 - Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P.Z - this.P0.Z) / d);
                            this.C.Z = Z2 - Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P0.X - this.P.X) / d);

                            this.GetArcProperties(false);

                            if (Larc < this.Length)
                            {
                                this.C.X = X2 + Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P.Z - this.P0.Z) / d);
                                this.C.Z = Z2 + Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P0.X - this.P.X) / d);
                            }
                            this.GetArcProperties();

                        }
                        else if (this.WorkPlane == ncMove.WorkingPlane.XY)
                        {
                            double X2 = (this.P0.X + this.P.X) / 2.0;
                            double Y2 = (this.P0.Y + this.P.Y) / 2.0;

                            IsValid = true;

                            this.C.X = X2 + Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P.Y - this.P0.Y) / d);
                            this.C.Y = Y2 + Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P0.X - this.P.X) / d);
                            this.C.Z = this.P.Z;

                            this.GetArcProperties(false);
                            double Larc = this.Length;

                            this.C.X = X2 - Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P.Y - this.P0.Y) / d);
                            this.C.Y = Y2 - Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P0.X - this.P.X) / d);

                            this.GetArcProperties(false);

                            if (Larc < this.Length)
                            {
                                this.C.X = X2 + Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P.Y - this.P0.Y) / d);
                                this.C.Y = Y2 + Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P0.X - this.P.X) / d);
                            }
                            this.GetArcProperties();
                        }
                    }
                    if (IsValid)
                    {
                        this.GetArcProperties();
                    }
                    else
                    {
                        this.Type = ncMove.MoveType.Linear;
                        this.Length = Math.Sqrt(Math.Pow(this.P.X - this.P0.X, 2) + Math.Pow(this.P.Y - this.P0.Y, 2) + Math.Pow(this.P.Z - this.P0.Z, 2));
                    }
                }
                else if (this.Type == ncMove.MoveType.CircularCCW)
                {
                    if (this.WorkPlane == ncMove.WorkingPlane.YZ)
                    {
                        d = Math.Sqrt(Math.Pow(this.P.Y - this.P0.Y, 2) + Math.Pow(this.P.Z - this.P0.Z, 2));
                        R0 = Math.Sqrt(Math.Pow(this.C.Y - this.P0.Y, 2) + Math.Pow(this.C.Z - this.P0.Z, 2));
                        R1 = Math.Sqrt(Math.Pow(this.P.Y - this.C.Y, 2) + Math.Pow(this.P.Z - this.C.Z, 2));
                        this.C.X = this.P.X;
                    }
                    else if (this.WorkPlane == ncMove.WorkingPlane.XZ)
                    {
                        d = Math.Sqrt(Math.Pow(this.P.X - this.P0.X, 2) + Math.Pow(this.P.Z - this.P0.Z, 2));
                        R0 = Math.Sqrt(Math.Pow(this.C.X - this.P0.X, 2) + Math.Pow(this.C.Z - this.P0.Z, 2));
                        R1 = Math.Sqrt(Math.Pow(this.P.X - this.C.X, 2) + Math.Pow(this.P.Z - this.C.Z, 2));
                        this.C.Y = this.P.Y;
                    }
                    else if (this.WorkPlane == ncMove.WorkingPlane.XY)
                    {
                        d = Math.Sqrt(Math.Pow(this.P.X - this.P0.X, 2) + Math.Pow(this.P.Y - this.P0.Y, 2));
                        R0 = Math.Sqrt(Math.Pow(this.C.X - this.P0.X, 2) + Math.Pow(this.C.Y - this.P0.Y, 2));
                        R1 = Math.Sqrt(Math.Pow(this.P.X - this.C.X, 2) + Math.Pow(this.P.Y - this.C.Y, 2));
                        this.C.Z = this.P.Z;
                    }

                    if (double.IsNaN(this.R.Value))
                    {
                        tol = Math.Max(R0, R1) / 100;

                        this.R.Value = 0.0;
                        if (Math.Abs(R0 - R1) < tol)
                        {
                            if (Math.Abs(R0 * 2 - d) < tol || R0 * 2 > d)
                            {
                                IsValid = true;
                                this.R.Value = R0;
                            }
                        }
                        else
                        {
                            if (def.CircularMoveCorrection)
                            {
                                if (this.WorkPlane == ncMove.WorkingPlane.YZ)
                                {
                                    IsValid = true;
                                    Alpha = Math.Atan2(this.P.Z - this.C.Z, this.P.Y - this.C.Y);
                                    this.P.Y = this.C.Y + R0 * Math.Cos(Alpha);
                                    this.P.Z = this.C.Z + R0 * Math.Sin(Alpha);
                                    this.R.Value = R0;
                                }
                                else if (this.WorkPlane == ncMove.WorkingPlane.XZ)
                                {
                                    IsValid = true;
                                    Alpha = Math.Atan2(this.P.Z - this.C.Z, this.P.X - this.C.X);
                                    this.P.X = this.C.X + R0 * Math.Cos(Alpha);
                                    this.P.Z = this.C.Z + R0 * Math.Sin(Alpha);
                                    this.R.Value = R0;
                                }
                                else if (this.WorkPlane == ncMove.WorkingPlane.XY)
                                {
                                    IsValid = true;
                                    Alpha = Math.Atan2(this.P.Y - this.C.Y, this.P.X - this.C.X);
                                    this.P.X = this.C.X + R0 * Math.Cos(Alpha);
                                    this.P.Y = this.C.Y + R0 * Math.Sin(Alpha);
                                    this.R.Value = R0;
                                }
                            }
                        }
                    }
                    else
                    {
                        this.R.Value = Math.Abs(this.R.Value);

                        if (this.R.Value * 2 < d)
                        {
                            if (def.CircularMoveCorrection)
                            {
                                this.R.Value = d / 2.0;
                            }
                        }

                        if (this.WorkPlane == ncMove.WorkingPlane.YZ)
                        {
                            double Y2 = (this.P0.Y + this.P.Y) / 2.0;
                            double Z2 = (this.P0.Z + this.P.Z) / 2.0;

                            IsValid = true;

                            this.C.X = this.P.X;
                            this.C.Y = Y2 + Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P0.Z - this.P.Z) / d);
                            this.C.Z = Z2 + Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P.Y - this.P0.Y) / d);

                            this.GetArcProperties(false);
                            double Larc = this.Length;

                            this.C.Y = Y2 - Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P0.Z - this.P.Z) / d);
                            this.C.Z = Z2 - Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P.Y - this.P0.Y) / d);

                            this.GetArcProperties(false);

                            if (Larc < this.Length)
                            {
                                this.C.Y = Y2 + Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P0.Z - this.P.Z) / d);
                                this.C.Z = Z2 + Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P.Y - this.P0.Y) / d);
                            }

                            this.GetArcProperties();
                        }
                        else if (this.WorkPlane == ncMove.WorkingPlane.XZ)
                        {
                            double X2 = (this.P0.X + this.P.X) / 2.0;
                            double Z2 = (this.P0.Z + this.P.Z) / 2.0;

                            IsValid = true;

                            this.C.X = X2 + Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P0.Z - this.P.Z) / d);
                            this.C.Y = this.P.Y;
                            this.C.Z = Z2 + Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P.X - this.P0.X) / d);

                            this.GetArcProperties(false);
                            double Larc = this.Length;

                            this.C.X = X2 - Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P0.Z - this.P.Z) / d);
                            this.C.Z = Z2 - Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P.X - this.P0.X) / d);

                            this.GetArcProperties(false);

                            if (Larc < this.Length)
                            {
                                this.C.X = X2 + Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P0.Z - this.P.Z) / d);
                                this.C.Z = Z2 + Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P.X - this.P0.X) / d);
                            }

                            this.GetArcProperties();
                        }
                        else if (this.WorkPlane == ncMove.WorkingPlane.XY)
                        {
                            double X2 = (this.P0.X + this.P.X) / 2.0;
                            double Y2 = (this.P0.Y + this.P.Y) / 2.0;

                            IsValid = true;

                            this.C.X = X2 + Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P0.Y - this.P.Y) / d);
                            this.C.Y = Y2 + Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P.X - this.P0.X) / d);
                            this.C.Z = this.P.Z;

                            this.GetArcProperties(false);
                            double Larc = this.Length;

                            this.C.X = X2 - Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P0.Y - this.P.Y) / d);
                            this.C.Y = Y2 - Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P.X - this.P0.X) / d);

                            this.GetArcProperties(false);

                            if (Larc < this.Length)
                            {
                                this.C.X = X2 + Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P0.Y - this.P.Y) / d);
                                this.C.Y = Y2 + Math.Sqrt(Math.Pow(this.R.Value, 2) - Math.Pow(d / 2.0, 2)) * ((this.P.X - this.P0.X) / d);
                            }

                            this.GetArcProperties();
                        }
                    }
                    if (IsValid)
                    {
                        this.GetArcProperties();
                    }
                    else
                    {
                        this.Type = ncMove.MoveType.Linear;
                        this.Length = Math.Sqrt(Math.Pow(this.P.X - this.P0.X, 2) + Math.Pow(this.P.Y - this.P0.Y, 2) + Math.Pow(this.P.Z - this.P0.Z, 2));
                    }
                }
                else
                {
                    this.Type = ncMove.MoveType.Linear;
                    this.Length = Math.Sqrt(Math.Pow(this.P.X - this.P0.X, 2) + Math.Pow(this.P.Y - this.P0.Y, 2) + Math.Pow(this.P.Z - this.P0.Z, 2));
                }

                return this.Clone();
            }

            private void UpDateMoveRef(ref CommandMove mr, ncMachineDef def)
            {
                mr.P = this.P.Clone();
                mr.C = new CommandCoord();
                mr.R = new Input();
            }

            private void AutoDetectArc(ref CommandMove mr, ncMachineDef def)
            {
                CommandMove m = this.Clone();
                //m.DefP(mr, def);

                CommandMove mrTmp = mr.Clone();

                CommandMove mabs = m.Clone();
                mrTmp.IJKType = ncMove.ArcType.Absolute;
                mabs.DefC(mrTmp, def);
                CommandCoord PCAbs = mabs.C.Clone();

                CommandMove mrel = m.Clone();
                mrTmp.IJKType = ncMove.ArcType.Relative;
                mrel.DefC(mrTmp, def);
                CommandCoord PCrel = mrel.C.Clone();

                double R0abs = 0, R1abs = 0, R0rel = 0, R1rel = 0, d = 0;

                if (mr.WorkPlane == ncMove.WorkingPlane.YZ)
                {
                    d = Math.Sqrt(Math.Pow(m.P.Y - mr.P.Y, 2) + Math.Pow(m.P.Z - mr.P.Z, 2));
                }
                else if (mr.WorkPlane == ncMove.WorkingPlane.XZ)
                {
                    d = Math.Sqrt(Math.Pow(m.P.X - mr.P.X, 2) + Math.Pow(m.P.Z - mr.P.Z, 2));
                }
                else if (mr.WorkPlane == ncMove.WorkingPlane.XY)
                {
                    d = Math.Sqrt(Math.Pow(m.P.X - mr.P.X, 2) + Math.Pow(m.P.Y - mr.P.Y, 2));
                }

                if (d < 1E-9)
                {
                    mr.IJKType = ncMove.ArcType.Relative;
                    return;
                }

                if (mr.WorkPlane == ncMove.WorkingPlane.YZ)
                {
                    R0abs = Math.Sqrt(
                        Math.Pow(mr.P.Y - PCAbs.Y, 2) +
                        Math.Pow(mr.P.Z - PCAbs.Z, 2));

                    R1abs = Math.Sqrt(
                        Math.Pow(m.P.Y - PCAbs.Y, 2) +
                        Math.Pow(m.P.Z - PCAbs.Z, 2));

                    R0rel = Math.Sqrt(
                        Math.Pow(mr.P.Y - PCrel.Y, 2) +
                        Math.Pow(mr.P.Z - PCrel.Z, 2));

                    R1rel = Math.Sqrt(
                        Math.Pow(m.P.Y - PCrel.Y, 2) +
                        Math.Pow(m.P.Z - PCrel.Z, 2));
                }

                if (mr.WorkPlane == ncMove.WorkingPlane.XZ)
                {
                    R0abs = Math.Sqrt(
                        Math.Pow(mr.P.X - PCAbs.X, 2) +
                        Math.Pow(mr.P.Z - PCAbs.Z, 2));

                    R1abs = Math.Sqrt(
                        Math.Pow(m.P.X - PCAbs.X, 2) +
                        Math.Pow(m.P.Z - PCAbs.Z, 2));

                    R0rel = Math.Sqrt(
                        Math.Pow(mr.P.X - PCrel.X, 2) +
                        Math.Pow(mr.P.Z - PCrel.Z, 2));

                    R1rel = Math.Sqrt(
                        Math.Pow(m.P.X - PCrel.X, 2) +
                        Math.Pow(m.P.Z - PCrel.Z, 2));
                }

                if (mr.WorkPlane == ncMove.WorkingPlane.XY)
                {
                    R0abs = Math.Sqrt(
                        Math.Pow(mr.P.X - PCAbs.X, 2) +
                        Math.Pow(mr.P.Y - PCAbs.Y, 2));

                    R1abs = Math.Sqrt(
                        Math.Pow(m.P.X - PCAbs.X, 2) +
                        Math.Pow(m.P.Y - PCAbs.Y, 2));

                    R0rel = Math.Sqrt(
                        Math.Pow(mr.P.X - PCrel.X, 2) +
                        Math.Pow(mr.P.Y - PCrel.Y, 2));

                    R1rel = Math.Sqrt(
                        Math.Pow(m.P.X - PCrel.X, 2) +
                        Math.Pow(m.P.Y - PCrel.Y, 2));
                }

                double tol;

                tol = Math.Max(R0abs, R1abs) / 1000.0;
                if (Math.Abs(R0abs - R1abs) < Math.Abs(R0rel - R1rel) / 100.0 && Math.Abs(R0rel - R1rel) > tol)
                {
                    mr.IJKType = ncMove.ArcType.Absolute;
                    return;
                }

                tol = Math.Max(R0rel, R1rel) / 1000.0;
                if (Math.Abs(R0rel - R1rel) < Math.Abs(R0abs - R1abs) / 100.0 && Math.Abs(R0abs - R1abs) > tol)
                {
                    mr.IJKType = ncMove.ArcType.Relative;
                    return;
                }

                tol = Math.Max(R0abs, R1abs) / 100.0;
                if (Math.Abs(R0abs - R1abs) < tol)
                {
                    if (mr.IJKType == ncMove.ArcType.Absolute)
                    {
                        return;
                    }
                    else
                    {
                        tol = Math.Max(R0rel, R1rel) / 100.0;
                        if (Math.Abs(R0rel - R1rel) > tol)
                        {
                            if (d > Math.Min(R0abs, R1abs) / 100.0)
                            {
                                mr.IJKType = ncMove.ArcType.Absolute;
                            }
                            return;
                        }
                    }
                }

                tol = Math.Max(R0rel, R1rel) / 100.0;
                if (Math.Abs(R0rel - R1rel) < tol)
                {
                    if (mr.IJKType == ncMove.ArcType.Relative)
                    {
                        return;
                    }
                    else
                    {
                        tol = Math.Max(R0abs, R1abs) / 100.0;
                        if (Math.Abs(R0abs - R1abs) > tol)
                        {
                            if (d > Math.Min(R0rel, R1rel) / 100.0)
                            {
                                mr.IJKType = ncMove.ArcType.Relative;
                            }
                            return;
                        }
                    }
                }
            }

            private void PropertiesFromRef(ref CommandMove mr, ncMachineDef def)
            {
                this.ToolNb = mr.ToolNb;

                if (!double.IsNaN(this.F.Value))
                {
                    mr.F.Value = this.F.Value * def.ConversionFactorF;
                }
                if (!double.IsNaN(this.S.Value))
                {
                    mr.S.Value = this.S.Value * def.ConversionFactorS;
                }

                this.F.Value = mr.F.Value;
                if (Type == ncMove.MoveType.Rapid)
                {
                    this.F.Value = def.DefaultRapidSpeed;
                }
                if (this.F.Value >= def.DefaultRapidSpeed)
                {
                    this.Type = ncMove.MoveType.Rapid;
                }

                this.S.Value = mr.S.Value;

                this.WorkPlane = mr.WorkPlane;
            }

            private void UpDateProperties(ref CommandMove mr)
            {
                this.P0 = mr.P.Clone();
                this.PRef = mr.PRef.Clone();
                this.POffset = mr.POffset.Clone();
                this.Length = Math.Sqrt(Math.Pow(this.P.X - mr.P.X, 2) + Math.Pow(this.P.Y - mr.P.Y, 2) + Math.Pow(this.P.Z - mr.P.Z, 2));
            }

            private void DefP(CommandMove mr, ncMachineDef def)
            {
                bool DefPX = false;
                bool DefPY = false;
                bool DefPZ = false;

                if (double.IsNaN(this.P.X))
                {
                    this.P.X = mr.P.X;
                }
                else
                {
                    this.P.X = this.P.X * def.ConversionFactorX;

                    if (mr.XYZRelative)
                    {
                        if (mr.ScaleFactor != null)
                        {
                            this.P.X = mr.P.X + this.P.X * mr.ScaleFactor.X;
                        }
                        else
                        {
                            this.P.X += mr.P.X;
                        }
                    }
                    else
                    {
                        this.P.X += mr.PRef.X + mr.POffset.X;
                        if (mr.ScaleFactor != null)
                        {
                            this.P.X = mr.ScaleCenter.X + (this.P.X - mr.ScaleCenter.X) * mr.ScaleFactor.X;
                        }
                    }
                    DefPX = true;
                }

                if (double.IsNaN(this.P.Y))
                {
                    this.P.Y = mr.P.Y;
                }
                else
                {
                    this.P.Y = this.P.Y * def.ConversionFactorY;
                    if (mr.XYZRelative)
                    {
                        if (mr.ScaleFactor != null)
                        {
                            this.P.Y = mr.P.Y + this.P.Y * mr.ScaleFactor.Y;
                        }
                        else
                        {
                            this.P.Y += mr.P.Y;
                        }
                    }
                    else
                    {
                        this.P.Y += mr.PRef.Y + mr.POffset.Y;
                        if (mr.ScaleFactor != null)
                        {
                            this.P.Y = mr.ScaleCenter.Y + (this.P.Y - mr.ScaleCenter.Y) * mr.ScaleFactor.Y;
                        }
                    }
                    DefPY = true;
                }

                if (double.IsNaN(this.P.Z))
                {
                    this.P.Z = mr.P.Z;
                }
                else
                {
                    this.P.Z = this.P.Z * def.ConversionFactorZ;
                    if (mr.XYZRelative)
                    {
                        if (mr.ScaleFactor != null)
                        {
                            this.P.Z = mr.P.Z + this.P.Z * mr.ScaleFactor.Z;
                        }
                        else
                        {
                            this.P.Z += mr.P.Z;
                        }
                    }
                    else
                    {
                        this.P.Z += mr.PRef.Z + mr.POffset.Z;
                        if (mr.ScaleFactor != null)
                        {
                            this.P.Z = mr.ScaleCenter.Z + (this.P.Z - mr.ScaleCenter.Z) * mr.ScaleFactor.Z;
                        }
                    }
                    DefPZ = true;
                }

                if (mr.WorkPlane == ncMove.WorkingPlane.XY)
                {
                    double RX = this.P.X;
                    double RY = this.P.Y;

                    double DXrelative = this.P.X - mr.P.X;
                    double DYrelative = this.P.Y - mr.P.Y;

                    double R0Relative = Math.Sqrt(Math.Pow(mr.P.X - mr.RotateCenter.X, 2) + Math.Pow(mr.P.Y - mr.RotateCenter.Y, 2));
                    double A0Relative = Math.Atan2(mr.P.Y - mr.RotateCenter.Y, mr.P.X - mr.RotateCenter.X);

                    if (!DefPX)
                    {
                        RX = mr.RotateCenter.X + R0Relative * Math.Cos(A0Relative - mr.RotateAngle);
                    }
                    else
                    {
                        if (mr.XYZRelative)
                        {
                            RX = mr.RotateCenter.X + R0Relative * Math.Cos(A0Relative - mr.RotateAngle);
                            RX += DXrelative;
                        }
                    }
                    if (!DefPY)
                    {
                        RY = mr.RotateCenter.Y + R0Relative * Math.Sin(A0Relative - mr.RotateAngle);
                    }
                    else
                    {
                        if (mr.XYZRelative)
                        {
                            RY = mr.RotateCenter.Y + R0Relative * Math.Sin(A0Relative - mr.RotateAngle);
                            RY += DYrelative;
                        }
                    }

                    double R0 = Math.Sqrt(Math.Pow(RX - mr.RotateCenter.X, 2) + Math.Pow(RY - mr.RotateCenter.Y, 2));
                    double A0 = Math.Atan2(RY - mr.RotateCenter.Y, RX - mr.RotateCenter.X);

                    this.P.X = mr.RotateCenter.X + R0 * Math.Cos(A0 + mr.RotateAngle);
                    this.P.Y = mr.RotateCenter.Y + R0 * Math.Sin(A0 + mr.RotateAngle);
                }
                else if (mr.WorkPlane == ncMove.WorkingPlane.XZ)
                {
                    double RX = this.P.X;
                    double RZ = this.P.Z;

                    double DXrelative = this.P.X - mr.P.X;
                    double DZrelative = this.P.Z - mr.P.Z;

                    double R0Relative = Math.Sqrt(Math.Pow(mr.P.X - mr.RotateCenter.X, 2) + Math.Pow(mr.P.Z - mr.RotateCenter.Z, 2));
                    double A0Relative = Math.Atan2(mr.P.Z - mr.RotateCenter.Z, mr.P.X - mr.RotateCenter.X);

                    if (!DefPX)
                    {
                        RX = mr.RotateCenter.X + R0Relative * Math.Cos(A0Relative - mr.RotateAngle);
                    }
                    else
                    {
                        if (mr.XYZRelative)
                        {
                            RX = mr.RotateCenter.X + R0Relative * Math.Cos(A0Relative - mr.RotateAngle);
                            RX += DXrelative;
                        }
                    }
                    if (!DefPZ)
                    {
                        RZ = mr.RotateCenter.Z + R0Relative * Math.Sin(A0Relative - mr.RotateAngle);
                    }
                    else
                    {
                        if (mr.XYZRelative)
                        {
                            RZ = mr.RotateCenter.Z + R0Relative * Math.Sin(A0Relative - mr.RotateAngle);
                            RZ += DZrelative;
                        }
                    }

                    double R0 = Math.Sqrt(Math.Pow(RX - mr.RotateCenter.X, 2) + Math.Pow(RZ - mr.RotateCenter.Z, 2));
                    double A0 = Math.Atan2(RZ - mr.RotateCenter.Z, RX - mr.RotateCenter.X);

                    this.P.X = mr.RotateCenter.X + R0 * Math.Cos(A0 + mr.RotateAngle);
                    this.P.Z = mr.RotateCenter.Z + R0 * Math.Sin(A0 + mr.RotateAngle);
                }
                else if (mr.WorkPlane == ncMove.WorkingPlane.YZ)
                {
                    double RY = this.P.Y;
                    double RZ = this.P.Z;

                    double DYrelative = this.P.Y - mr.P.Y;
                    double DZrelative = this.P.Z - mr.P.Z;

                    double R0Relative = Math.Sqrt(Math.Pow(mr.P.Y - mr.RotateCenter.Y, 2) + Math.Pow(mr.P.Z - mr.RotateCenter.Z, 2));
                    double A0Relative = Math.Atan2(mr.P.Z - mr.RotateCenter.Z, mr.P.Y - mr.RotateCenter.Y);

                    if (!DefPY)
                    {
                        RY = mr.RotateCenter.Y + R0Relative * Math.Cos(A0Relative - mr.RotateAngle);
                    }
                    else
                    {
                        if (mr.XYZRelative)
                        {
                            RY = mr.RotateCenter.Y + R0Relative * Math.Cos(A0Relative - mr.RotateAngle);
                            RY += DYrelative;
                        }
                    }
                    if (!DefPZ)
                    {
                        RZ = mr.RotateCenter.Z + R0Relative * Math.Sin(A0Relative - mr.RotateAngle);
                    }
                    else
                    {
                        if (mr.XYZRelative)
                        {
                            RZ = mr.RotateCenter.Z + R0Relative * Math.Sin(A0Relative - mr.RotateAngle);
                            RZ += DZrelative;
                        }
                    }

                    double R0 = Math.Sqrt(Math.Pow(RY - mr.RotateCenter.Y, 2) + Math.Pow(RZ - mr.RotateCenter.Z, 2));
                    double A0 = Math.Atan2(RZ - mr.RotateCenter.Z, RY - mr.RotateCenter.Y);

                    this.P.Y = mr.RotateCenter.Y + R0 * Math.Cos(A0 + mr.RotateAngle);
                    this.P.Z = mr.RotateCenter.Z + R0 * Math.Sin(A0 + mr.RotateAngle);
                }
            }

            private void DefC(CommandMove mr, ncMachineDef def)
            {
                double MaxScaleFactor = 1.0;
                if (mr.ScaleFactor != null)
                {
                    MaxScaleFactor = Math.Max(Math.Abs(mr.ScaleFactor.X), Math.Abs(mr.ScaleFactor.Y));
                    MaxScaleFactor = Math.Max(MaxScaleFactor, Math.Abs(mr.ScaleFactor.Z));
                }

                bool DefCX = false;
                bool DefCY = false;
                bool DefCZ = false;

                if (double.IsNaN(this.C.X))
                {
                    this.C.X = mr.P.X;
                }
                else
                {
                    this.C.X = this.C.X * def.ConversionFactorI;
                    if (mr.IJKType == ncMove.ArcType.Relative)
                    {
                        if (mr.ScaleFactor != null)
                        {
                            this.C.X *= mr.ScaleFactor.X;
                        }
                        this.C.X += mr.P.X;
                    }
                    else
                    {
                        this.C.X += mr.PRef.X + mr.POffset.X;
                        if (mr.ScaleFactor != null)
                        {
                            this.C.X = mr.ScaleCenter.X + (this.C.X - mr.ScaleCenter.X) * mr.ScaleFactor.X;
                        }
                    }

                    DefCX = true;
                }

                if (double.IsNaN(this.C.Y))
                {
                    this.C.Y = mr.P.Y;
                }
                else
                {
                    this.C.Y = this.C.Y * def.ConversionFactorJ;
                    if (mr.IJKType == ncMove.ArcType.Relative)
                    {
                        if (mr.ScaleFactor != null)
                        {
                            this.C.Y *= mr.ScaleFactor.Y;
                        }
                        this.C.Y += mr.P.Y;
                    }
                    else
                    {
                        this.C.Y += mr.PRef.Y + mr.POffset.Y;
                        if (mr.ScaleFactor != null)
                        {
                            this.C.Y = mr.ScaleCenter.Y + (this.C.Y - mr.ScaleCenter.Y) * mr.ScaleFactor.Y;
                        }
                    }

                    DefCY = true;
                }

                if (double.IsNaN(this.C.Z))
                {
                    this.C.Z = mr.P.Z;
                }
                else
                {
                    this.C.Z = this.C.Z * def.ConversionFactorK;
                    if (mr.IJKType == ncMove.ArcType.Relative)
                    {
                        if (mr.ScaleFactor != null)
                        {
                            this.C.Z *= mr.ScaleFactor.Z;
                        }
                        this.C.Z += mr.P.Z;
                    }
                    else
                    {
                        this.C.Z += mr.PRef.Z + mr.POffset.Z;
                        if (mr.ScaleFactor != null)
                        {
                            this.C.Z = mr.ScaleCenter.Z + (this.C.Z - mr.ScaleCenter.Z) * mr.ScaleFactor.Z;
                        }
                    }

                    DefCZ = true;
                }

                if (!double.IsNaN(this.R.Value))
                {
                    this.R.Value = this.R.Value * def.ConversionFactorR;
                    this.R.Value = this.R.Value * MaxScaleFactor;
                }

                if (mr.WorkPlane == ncMove.WorkingPlane.XY)
                {
                    double RX = this.C.X;
                    double RY = this.C.Y;

                    double DXrelative = this.C.X - mr.P.X;
                    double DYrelative = this.C.Y - mr.P.Y;

                    double DXrelativetoend = this.C.X - this.P.X;
                    double DYrelativetoend = this.C.Y - this.P.Y;

                    double R0relative = Math.Sqrt(Math.Pow(mr.P.X - mr.RotateCenter.X, 2) + Math.Pow(mr.P.Y - mr.RotateCenter.Y, 2));
                    double A0relative = Math.Atan2(mr.P.Y - mr.RotateCenter.Y, mr.P.X - mr.RotateCenter.X);

                    double R0relativetoend = Math.Sqrt(Math.Pow(this.P.X - mr.RotateCenter.X, 2) + Math.Pow(this.P.Y - mr.RotateCenter.Y, 2));
                    double A0relativetoend = Math.Atan2(this.P.Y - mr.RotateCenter.Y, this.P.X - mr.RotateCenter.X);

                    if (!DefCX)
                    {
                        RX = mr.RotateCenter.X + R0relative * Math.Cos(A0relative - mr.RotateAngle);
                    }
                    else
                    {
                        if (mr.IJKType == ncMove.ArcType.Relative)
                        {
                            RX = mr.RotateCenter.X + R0relative * Math.Cos(A0relative - mr.RotateAngle);
                            RX += DXrelative;
                        }
                    }
                    if (!DefCY)
                    {
                        RY = mr.RotateCenter.Y + R0relative * Math.Sin(A0relative - mr.RotateAngle);
                    }
                    else
                    {
                        if (mr.IJKType == ncMove.ArcType.Relative)
                        {
                            RY = mr.RotateCenter.Y + R0relative * Math.Sin(A0relative - mr.RotateAngle);
                            RY += DYrelative;
                        }
                    }

                    double R0 = Math.Sqrt(Math.Pow(RX - mr.RotateCenter.X, 2) + Math.Pow(RY - mr.RotateCenter.Y, 2));
                    double A0 = Math.Atan2(RY - mr.RotateCenter.Y, RX - mr.RotateCenter.X);

                    this.C.X = mr.RotateCenter.X + R0 * Math.Cos(A0 + mr.RotateAngle);
                    this.C.Y = mr.RotateCenter.Y + R0 * Math.Sin(A0 + mr.RotateAngle);
                }
                else if (mr.WorkPlane == ncMove.WorkingPlane.XZ)
                {
                    double RX = this.C.X;
                    double RZ = this.C.Z;

                    double DXrelative = this.C.X - mr.P.X;
                    double DZrelative = this.C.Z - mr.P.Z;

                    double DXrelativetoend = this.C.X - this.P.X;
                    double DZrelativetoend = this.C.Z - this.P.Z;

                    double R0relative = Math.Sqrt(Math.Pow(mr.P.X - mr.RotateCenter.X, 2) + Math.Pow(mr.P.Z - mr.RotateCenter.Z, 2));
                    double A0relative = Math.Atan2(mr.P.Z - mr.RotateCenter.Z, mr.P.X - mr.RotateCenter.X);

                    double R0relativetoend = Math.Sqrt(Math.Pow(this.P.X - mr.RotateCenter.X, 2) + Math.Pow(this.P.Z - mr.RotateCenter.Z, 2));
                    double A0relativetoend = Math.Atan2(this.P.Z - mr.RotateCenter.Z, this.P.X - mr.RotateCenter.X);

                    if (!DefCX)
                    {
                        RX = mr.RotateCenter.X + R0relative * Math.Cos(A0relative - mr.RotateAngle * def.ConversionFactorAngle * Math.PI / 180.0);
                    }
                    else
                    {
                        if (mr.IJKType == ncMove.ArcType.Relative)
                        {
                            RX = mr.RotateCenter.X + R0relative * Math.Cos(A0relative - mr.RotateAngle * def.ConversionFactorAngle * Math.PI / 180.0);
                            RX += DXrelative;
                        }
                    }
                    if (!DefCZ)
                    {
                        RZ = mr.RotateCenter.Z + R0relative * Math.Sin(A0relative - mr.RotateAngle * def.ConversionFactorAngle * Math.PI / 180.0);
                    }
                    else
                    {
                        if (mr.IJKType == ncMove.ArcType.Relative)
                        {
                            RZ = mr.RotateCenter.Z + R0relative * Math.Sin(A0relative - mr.RotateAngle * def.ConversionFactorAngle * Math.PI / 180.0);
                            RZ += DZrelative;
                        }
                    }

                    double R0 = Math.Sqrt(Math.Pow(RX - mr.RotateCenter.X, 2) + Math.Pow(RZ - mr.RotateCenter.Z, 2));
                    double A0 = Math.Atan2(RZ - mr.RotateCenter.Z, RX - mr.RotateCenter.X);

                    this.C.X = mr.RotateCenter.X + R0 * Math.Cos(A0 + mr.RotateAngle * def.ConversionFactorAngle * Math.PI / 180.0);
                    this.C.Z = mr.RotateCenter.Z + R0 * Math.Sin(A0 + mr.RotateAngle * def.ConversionFactorAngle * Math.PI / 180.0);
                }
                else if (mr.WorkPlane == ncMove.WorkingPlane.YZ)
                {
                    double RY = this.C.Y;
                    double RZ = this.C.Z;

                    double DYrelative = this.C.Y - mr.P.Y;
                    double DZrelative = this.C.Z - mr.P.Z;

                    double DYrelativetoend = this.C.Y - this.P.Y;
                    double DZrelativetoend = this.C.Z - this.P.Z;

                    double R0relative = Math.Sqrt(Math.Pow(mr.P.Y - mr.RotateCenter.Y, 2) + Math.Pow(mr.P.Z - mr.RotateCenter.Z, 2));
                    double A0relative = Math.Atan2(mr.P.Z - mr.RotateCenter.Z, mr.P.Y - mr.RotateCenter.Y);

                    double R0relativetoend = Math.Sqrt(Math.Pow(this.P.Y - mr.RotateCenter.Y, 2) + Math.Pow(this.P.Z - mr.RotateCenter.Z, 2));
                    double A0relativetoend = Math.Atan2(this.P.Z - mr.RotateCenter.Z, this.P.Y - mr.RotateCenter.Y);

                    if (!DefCY)
                    {
                        RY = mr.RotateCenter.Y + R0relative * Math.Cos(A0relative - mr.RotateAngle * def.ConversionFactorAngle * Math.PI / 180.0);
                    }
                    else
                    {
                        if (mr.IJKType == ncMove.ArcType.Relative)
                        {
                            RY = mr.RotateCenter.Y + R0relative * Math.Cos(A0relative - mr.RotateAngle * def.ConversionFactorAngle * Math.PI / 180.0);
                            RY += DYrelative;
                        }
                    }
                    if (!DefCZ)
                    {
                        RZ = mr.RotateCenter.Z + R0relative * Math.Sin(A0relative - mr.RotateAngle * def.ConversionFactorAngle * Math.PI / 180.0);
                    }
                    else
                    {
                        if (mr.IJKType == ncMove.ArcType.Relative)
                        {
                            RZ = mr.RotateCenter.Z + R0relative * Math.Sin(A0relative - mr.RotateAngle * def.ConversionFactorAngle * Math.PI / 180.0);
                            RZ += DZrelative;
                        }
                    }

                    double R0 = Math.Sqrt(Math.Pow(RY - mr.RotateCenter.Y, 2) + Math.Pow(RZ - mr.RotateCenter.Z, 2));
                    double A0 = Math.Atan2(RZ - mr.RotateCenter.Z, RY - mr.RotateCenter.Y);

                    this.C.Y = mr.RotateCenter.Y + R0 * Math.Cos(A0 + mr.RotateAngle * def.ConversionFactorAngle * Math.PI / 180.0);
                    this.C.Z = mr.RotateCenter.Z + R0 * Math.Sin(A0 + mr.RotateAngle * def.ConversionFactorAngle * Math.PI / 180.0);
                }
            }

            private void GetArcProperties(bool updateminmax = true)
            {
                double h = 0;

                Vec V0 = new Vec(this.C, this.P0);
                Vec V1 = new Vec(this.C, this.P);
                Vec V2 = new Vec();

                if (this.WorkPlane == ncMove.WorkingPlane.XY)
                {
                    h = this.P0.Z - this.P.Z;
                    V0 = new Vec(new CommandCoord(this.C.X, this.C.Y, this.P0.Z), this.P0);
                    V1 = new Vec(new CommandCoord(this.C.X, this.C.Y, this.P0.Z), new CommandCoord(this.P.X, this.P.Y, this.P0.Z));
                }
                else if (this.WorkPlane == ncMove.WorkingPlane.XZ)
                {
                    h = this.P0.Y - this.P.Y;
                    V0 = new Vec(new CommandCoord(this.C.X, this.P0.Y, this.C.Z), this.P0);
                    V1 = new Vec(new CommandCoord(this.C.X, this.P0.Y, this.C.Z), new CommandCoord(this.P.X, this.P0.Y, this.P.Z));
                }
                else if (this.WorkPlane == ncMove.WorkingPlane.YZ)
                {
                    h = this.P0.X - this.P.X;
                    V0 = new Vec(new CommandCoord(this.P0.X, this.C.Y, this.C.Z), this.P0);
                    V1 = new Vec(new CommandCoord(this.P0.X, this.C.Y, this.C.Z), new CommandCoord(this.P0.X, this.P.Y, this.P.Z));
                }

                double[,] TransformMatrix = new double[,] { { 1.0, 0.0, 0.0 }, { 0.0, 1.0, 0.0 }, { 0.0, 0.0, 1.0 } };

                if (this.WorkPlane == ncMove.WorkingPlane.XY)
                {
                    V2 = new Vec(new CommandCoord(0, 0, 0), new CommandCoord(0, 0, 1));
                }
                else if (this.WorkPlane == ncMove.WorkingPlane.XZ)
                {
                    V2 = new Vec(new CommandCoord(0, 0, 0), new CommandCoord(0, 1, 0));
                }
                else if (this.WorkPlane == ncMove.WorkingPlane.YZ)
                {
                    V2 = new Vec(new CommandCoord(0, 0, 0), new CommandCoord(1, 0, 0));
                }

                if (Vec.VecNorm(Vec.Normalyze(V2)) != 0)
                {
                    Vec Xt = Vec.Normalyze(V0);
                    Vec Zt = Vec.Normalyze(V2);
                    double dir = Vec.VecDotProduct(new Vec(new CommandCoord(1, 1, 1)), V2);
                    if (dir < 0)
                    {
                        Zt = Vec.Scale(Zt, -1);
                    }
                    Vec Yt = Vec.VecCrossProduct(Zt, Xt);

                    TransformMatrix[0, 0] = Xt.X;
                    TransformMatrix[0, 1] = Xt.Y;
                    TransformMatrix[0, 2] = Xt.Z;

                    TransformMatrix[1, 0] = Yt.X;
                    TransformMatrix[1, 1] = Yt.Y;
                    TransformMatrix[1, 2] = Yt.Z;

                    TransformMatrix[2, 0] = Zt.X;
                    TransformMatrix[2, 1] = Zt.Y;
                    TransformMatrix[2, 2] = Zt.Z;
                }
                else
                {
                    // Should never happen but ...
                    this.Length = 0.0;
                }

                Vec V0t = Vec.MatVecProduct(TransformMatrix, V0);
                Vec V1t = Vec.MatVecProduct(TransformMatrix, V1);

                double radius = Vec.VecNorm(V0t);
                double a0 = Math.Atan2(V0t.Y, V0t.X);
                double a1 = Math.Atan2(V1t.Y, V1t.X);

                if (Math.Abs(a1) < 1E-6)
                {
                    if (this.Type == ncMove.MoveType.CircularCW)
                    {
                        a1 = -2 * Math.PI;
                    }
                    else
                    {
                        a1 = 2 * Math.PI;
                    }
                }
                else if (Math.Abs(a1 - Math.PI) < 1E-6)
                {
                    if (this.Type == ncMove.MoveType.CircularCW)
                    {
                        a1 = -Math.PI;
                    }
                    else
                    {
                        a1 = Math.PI;
                    }
                }
                else
                {
                    if (this.Type == ncMove.MoveType.CircularCW)
                    {
                        if (a1 > 0)
                        {
                            a1 = a1 - 2 * Math.PI;
                        }
                    }
                    else
                    {
                        if (a1 < 0)
                        {
                            a1 = a1 + 2 * Math.PI;
                        }
                    }
                }

                // helix length
                double n = (a1 - a0) / 2 / Math.PI;
                this.Length = Math.Abs(n * Math.Sqrt(Math.Pow(2 * Math.PI * radius, 2) + Math.Pow(h / n, 2)));
                if (double.IsNaN(this.Length))
                {
                    this.Length = 0.0;
                }
            }

            #endregion
        }

        internal class SetFS : Command
        {
            #region 
            internal enum SetFMode { None, Normal, Inverted }
            #endregion

            #region internal fields
            internal Input FeedRate;
            internal SetFMode FMode;
            internal Input SpindleSpeed;
            #endregion

            #region constructor
            internal SetFS(int l, int b, Input f, Input s, Input e, SetFMode mode = SetFMode.None)
            {
                this.CmdLink = CommandLink.SetFS;
                this.Line = l;
                this.Block = b;
                this.FeedRate = f;
                this.FMode = mode;
                this.SpindleSpeed = s;
            }
            #endregion
        }

        internal class SetReference : Command
        {
            #region fields
            internal bool Reset = false;
            internal CommandCoord P = new CommandCoord();
            internal CommandCoord A = new CommandCoord();
            #endregion

            #region constructor
            internal SetReference(int l, int b, bool reset = false)
            {
                this.CmdLink = CommandLink.SetReference;
                this.Line = l;
                this.Block = b;
                this.P = new CommandCoord();
                this.A = new CommandCoord();
                this.Reset = reset;
            }
            #endregion
        }

        internal class WorkOffSet : Command
        {
            #region fields
            internal CommandCoord XYZOffSet = new CommandCoord();
            internal CommandCoord RotOffSet = new CommandCoord();
            #endregion

            #region constructor
            internal WorkOffSet(int l, int b)
            {
                this.CmdLink = CommandLink.WorkOffset;
                this.Line = l;
                this.Block = b;

                this.XYZOffSet = new CommandCoord();
                this.RotOffSet = new CommandCoord();
            }
            #endregion
        }

        internal class Scale : Command
        {
            #region fields
            internal CommandCoord ScaleFactor;
            internal CommandCoord ScaleCenter;
            #endregion

            #region constructors
            internal Scale(int l, int b)
            {
                this.CmdLink = CommandLink.Scale;
                this.Line = l;
                this.Block = b;
                this.ScaleFactor = new CommandCoord(1, 1, 1);
                this.ScaleCenter = new CommandCoord(0, 0, 0);
            }
            #endregion
        }

        internal class Rotate : Command
        {
            #region fields
            internal Input RotateAngle;
            internal CommandCoord RotateCenter;
            #endregion

            #region constructors

            internal Rotate(int l, int b)
            {
                this.CmdLink = CommandLink.Rotate;
                this.Line = l;
                this.Block = b;
                this.RotateAngle = new Input(0.0);
                this.RotateCenter = new CommandCoord(0, 0, 0);
            }

            #endregion
        }

        internal class SubCall : Command
        {
            #region fields
            internal string SubName;
            internal Input Repetition;
            #endregion

            #region constructors
            internal SubCall(int l, int b)
            {
                this.CmdLink = CommandLink.SubCall;

                this.Line = l;
                this.Block = b;

                this.SubName = string.Empty;
                this.Repetition = new Input(1);
            }
            #endregion

        }

        internal class SubStart : Command
        {
            #region fields
            internal string SubName;
            #endregion

            #region constructors

            internal SubStart(int l, int b)
            {
                this.CmdLink = CommandLink.SubStart;

                this.Line = l;
                this.Block = b;

                this.SubName = "Sub";
            }
            #endregion
        }

        internal class ToolCall : Command
        {
            #region fields
            internal int ToolNb;
            #endregion

            #region constructors

            internal ToolCall(int l, int b, int nb)
            {
                this.CmdLink = CommandLink.ToolCall;

                this.Line = l;
                this.Block = b;

                this.ToolNb = nb;
            }
            #endregion
        }

        #endregion

        #region inputs
        internal class Input
        {
            #region internal field
            internal double Value;
            #endregion

            #region constructor
            internal Input()
            {
                this.Value = Math.Sqrt(-1);
            }
            internal Input(double d)
            {
                this.Value = d;
            }
            #endregion
        }

        internal class CommandCoord : ncCoord 
        {
            internal CommandCoord() : base() { }
            internal CommandCoord(double x, double y, double z) : base(x, y, z) { }

            internal new CommandCoord Clone()
            {
                return (CommandCoord)this.MemberwiseClone();
            }
        }
        #endregion

        #region math

        internal class Vec
        {
            #region internal fields
            internal double X;
            internal double Y;
            internal double Z;
            #endregion

            #region constructors
            internal Vec()
            {
                this.X = 0.0;
                this.Y = 0.0;
                this.Z = 0.0;
            }

            internal Vec(double d)
            {
                this.X = d;
                this.Y = d;
                this.Z = d;
            }

            internal Vec(double x, double y, double z)
            {
                this.X = x;
                this.Y = y;
                this.Z = z;
            }

            internal Vec(ncCoord x)
            {
                this.X = x.X;
                this.Y = x.Y;
                this.Z = x.Z;
            }
            internal Vec(ncCoord x0, ncCoord x1)
            {
                this.X = x1.X - x0.X;
                this.Y = x1.Y - x0.Y;
                this.Z = x1.Z - x0.Z;
            }
            internal Vec(CommandCoord x)
            {
                this.X = x.X;
                this.Y = x.Y;
                this.Z = x.Z;
            }
            internal Vec(CommandCoord x0, CommandCoord x1)
            {
                this.X = x1.X - x0.X;
                this.Y = x1.Y - x0.Y;
                this.Z = x1.Z - x0.Z;
            }
            #endregion

            #region methods

            internal static Vec Normalyze(Vec v)
            {
                Vec vn = new Vec();
                double norm = Vec.VecNorm(v);

                vn.X = v.X / norm;
                vn.Y = v.Y / norm;
                vn.Z = v.Z / norm;

                return vn.Clone();
            }

            internal static Vec Scale(Vec v, double s)
            {
                Vec vs = new Vec();

                vs.X = v.X * s;
                vs.Y = v.Y * s;
                vs.Z = v.Z * s;

                return vs.Clone();
            }

            internal static Vec VecSum(Vec v0, Vec v1)
            {
                Vec vs = new Vec();

                vs.X = v0.X + v1.X;
                vs.Y = v0.Y + v1.Y;
                vs.Z = v0.Z + v1.Z;

                return vs.Clone();
            }

            internal static double VecNorm(Vec v)
            {
                return Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
            }

            internal static double VecDotProduct(Vec v0, Vec v1)
            {
                return v0.X * v1.X + v0.Y * v1.Y + v0.Z * v1.Z;
            }

            internal static Vec VecCrossProduct(Vec v0, Vec v1)
            {
                Vec vcp = new Vec();

                vcp.X = v0.Y * v1.Z - v0.Z * v1.Y;
                vcp.Y = -v0.X * v1.Z + v0.Z * v1.X;
                vcp.Z = v0.X * v1.Y - v0.Y * v1.X;

                return vcp.Clone();
            }

            internal static Vec MatVecProduct(double[,] m, Vec v)
            {
                Vec mv = new Vec();

                mv.X = m[0, 0] * v.X + m[0, 1] * v.Y + m[0, 2] * v.Z;
                mv.Y = m[1, 0] * v.X + m[1, 1] * v.Y + m[1, 2] * v.Z;
                mv.Z = m[2, 0] * v.X + m[2, 1] * v.Y + m[2, 2] * v.Z;

                return mv.Clone();
            }

            internal Vec Clone()
            {
                Vec NewVec = (Vec)this.MemberwiseClone();
                return NewVec;
            }

            #endregion
        }

        internal class Mat
        {
            #region methods
            internal static double MatDet(double[,] m)
            {
                double det = m[0, 0] * m[1, 1] * m[2, 2] + m[0, 1] * m[1, 2] * m[2, 0] + m[0, 2] * m[1, 0] * m[2, 1] -
                         m[0, 2] * m[1, 1] * m[2, 0] - m[0, 1] * m[1, 0] * m[2, 2] - m[0, 0] * m[1, 2] * m[2, 1];

                return det;
            }

            internal static double[,] MatInv(double[,] m)
            {
                double[,] iv = new double[3, 3];

                double det = MatDet(m);

                iv[0, 0] = (m[1, 1] * m[2, 2] - m[1, 2] * m[2, 1]) / det;
                iv[0, 1] = -(m[0, 1] * m[2, 2] - m[0, 2] * m[2, 1]) / det;
                iv[0, 2] = (m[0, 1] * m[1, 2] - m[0, 2] * m[1, 1]) / det;

                iv[1, 0] = -(m[1, 0] * m[2, 2] - m[1, 2] * m[2, 0]) / det;
                iv[1, 1] = (m[0, 0] * m[2, 2] - m[0, 2] * m[2, 0]) / det;
                iv[1, 2] = -(m[0, 0] * m[1, 2] - m[0, 2] * m[1, 0]) / det;

                iv[2, 0] = (m[1, 0] * m[2, 1] - m[1, 1] * m[2, 0]) / det;
                iv[2, 1] = -(m[0, 0] * m[2, 1] - m[0, 1] * m[2, 0]) / det;
                iv[2, 2] = (m[0, 0] * m[1, 1] - m[0, 1] * m[1, 0]) / det;

                return iv;
            }
            #endregion
        }

        #endregion

        #region methods

        internal static List<ncMove> GetMoveList(SubCollection subs, ncMachineDef def)
        {
            SubCollection sc = subs.Reprocess(def);

            foreach (ncMove move in sc.Moves)
            {
                move.MoveGuid = Guid.NewGuid().ToString();
            }

            return sc.Moves;
        }

        #endregion
    }
}
