using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NCneticCore
{
    public class ncMachineDef
    {
        #region fields
        public bool XYZRelative;

        public ncMove.ArcType IJKType;
        public bool IJKModeAutoDetect;
        public bool CircularMoveCorrection;

        public ncCoord HomePosXYZ;

        public ncMove.WorkingPlane DefaultWorkPlane;
        public double DefaultRapidSpeed;

        public double ConversionFactorX;
        public double ConversionFactorY;
        public double ConversionFactorZ;

        public double ConversionFactorI;
        public double ConversionFactorJ;
        public double ConversionFactorK;

        public double ConversionFactorR;
        public double ConversionFactorF;
        public double ConversionFactorS;

        public double ConversionFactorAngle;
        #endregion

        #region Constructors
        internal ncMachineDef()
        {
            DefaultWorkPlane = ncMove.WorkingPlane.XY;
            DefaultRapidSpeed = 10000;

            ConversionFactorX = 1.0;
            ConversionFactorY = 1.0;
            ConversionFactorZ = 1.0;

            ConversionFactorI = 1.0;
            ConversionFactorJ = 1.0;
            ConversionFactorK = 1.0;

            ConversionFactorR = 1.0;
            ConversionFactorF = 1.0;
            ConversionFactorS = 1.0;

            ConversionFactorAngle = 1.0;

            CircularMoveCorrection = true;
            XYZRelative = false;
            IJKModeAutoDetect = true;
            IJKType = ncMove.ArcType.Absolute;

            HomePosXYZ = new ncCoord(0, 0, 0);
        }
        #endregion

        #region method
        public ncMachineDef Clone()
        {
            ncMachineDef NewMachineParams = (ncMachineDef)MemberwiseClone();
            NewMachineParams.HomePosXYZ = HomePosXYZ.Clone();
            return NewMachineParams;
        }
        #endregion
    }

    public class ncMachine
    {
        #region public fields
        public string Name;
        public ncMachineDef Definition;
        public ncLexer Lexer;
        #endregion

        #region constructors
        public ncMachine()
        {
            Name = "NEW_MACHINE";
            Definition = new ncMachineDef();
            Lexer = new ncLexer();

            ncLexerEntry entry;

            string ModifiersRegEx = "(?i)(?x)\t# MODIFIERS";
            string AllowWhiteSpacesRegex = "\\s*\t# ALLOW SPACES";

            string DoubleRegex = "\\s*[+-]?[0-9.\\s]+";
            //string PositiveDoubleRegex = "[0-9.\\s]+";
            string PositiveIntRegex = "[0-9\\s]+";
            string IDRegex = "[1-9]([0-9]+)?";

            // *******************************************************************************************************
            // Coords ************************************************************************************************
            // *******************************************************************************************************

            string GetCoordsKeys(string s)
            {
                return
                ModifiersRegEx + "\r\n" +
                s + "\r\n" +
                AllowWhiteSpacesRegex + "\r\n" +
                DoubleRegex;
            }

            // *******************************************************************************************************

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(255, 0, 0),
                CmdName = "PX",
                Pattern = GetCoordsKeys("X"),
                KeepIfFound = true,
                Links = new List<ncLexerLink> { new ncLexerLink(DoubleRegex, ncLexerLink.CmdType.AxisX) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(0, 128, 0),
                CmdName = "PY",
                Pattern = GetCoordsKeys("Y"),
                KeepIfFound = true,
                Links = new List<ncLexerLink> { new ncLexerLink(DoubleRegex, ncLexerLink.CmdType.AxisY) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(0, 0, 255),
                CmdName = "PZ",
                Pattern = GetCoordsKeys("Z"),
                KeepIfFound = true,
                Links = new List<ncLexerLink> { new ncLexerLink(DoubleRegex, ncLexerLink.CmdType.AxisZ) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            // *******************************************************************************************************

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(255, 0, 0),
                CmdName = "CX",
                Pattern = GetCoordsKeys("I"),
                KeepIfFound = true,
                Links = new List<ncLexerLink> { new ncLexerLink(DoubleRegex, ncLexerLink.CmdType.ArcCenterX) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(0, 128, 0),
                CmdName = "CY",
                Pattern = GetCoordsKeys("J"),
                KeepIfFound = true,
                Links = new List<ncLexerLink> { new ncLexerLink(DoubleRegex, ncLexerLink.CmdType.ArcCenterY) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(0, 0, 255),
                CmdName = "CZ",
                Pattern = GetCoordsKeys("K"),
                KeepIfFound = true,
                Links = new List<ncLexerLink> { new ncLexerLink(DoubleRegex, ncLexerLink.CmdType.ArcCenterZ) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(255, 0, 255),
                CmdName = "R",
                Pattern = GetCoordsKeys("R"),
                Links = new List<ncLexerLink> { new ncLexerLink(DoubleRegex, ncLexerLink.CmdType.ArcRadius) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            // *******************************************************************************************************

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(255, 0, 255),
                CmdName = "L",
                Pattern = GetCoordsKeys("L"),
                Links = new List<ncLexerLink> { new ncLexerLink(DoubleRegex, ncLexerLink.CmdType.SubRepetitions) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            // *******************************************************************************************************

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(255, 0, 255),
                CmdName = "P",
                Pattern = GetCoordsKeys("P"),
                Links = new List<ncLexerLink> { new ncLexerLink(IDRegex, ncLexerLink.CmdType.SubName) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            // *******************************************************************************************************

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(128, 128, 0),
                CmdName = "F",
                Pattern = GetCoordsKeys("F"),
                Links = new List<ncLexerLink> { new ncLexerLink(DoubleRegex, ncLexerLink.CmdType.SetFeedRate) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(128, 128, 0),
                CmdName = "S",
                Pattern = GetCoordsKeys("S"),
                Links = new List<ncLexerLink> { new ncLexerLink(DoubleRegex, ncLexerLink.CmdType.SetSpindleSpeed) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            // *******************************************************************************************************
            // Lines/Block/Comments **********************************************************************************
            // *******************************************************************************************************

            string pattern;

            pattern = ModifiersRegEx + "\r\n" + "N\t# LINE NUMBER CHAR" + "\r\n" + AllowWhiteSpacesRegex + "\r\n" + "[0-9]+\t# LINE NUMBER";

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(140, 140, 140),
                CmdName = "Line number",
                Pattern = ModifiersRegEx + "\r\n" + "N\t# LINE NUMBER CHAR" + "\r\n" + AllowWhiteSpacesRegex + "\r\n" + "[0-9]+\t# LINE NUMBER",
                Links = new List<ncLexerLink> { new ncLexerLink(ModifiersRegEx + "\r\n" + "[1-9]([0-9]+)?\t# LINE NUMBER", ncLexerLink.CmdType.BlockNumber) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(140, 140, 140),
                CmdName = "Brackets Comment",
                Pattern = ModifiersRegEx + "\r\n" + "[(<]\t# OPEN BRACKETS" + "\r\n" + "[^()]+\t# MATCH ANYTHING EXCEPT BRACKETS" + "\r\n" + "[>)]\t# CLOSE BRACKETS",
                Links = new List<ncLexerLink> { new ncLexerLink("", ncLexerLink.CmdType.None) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(140, 140, 140),
                CmdName = "In-Line Comment",
                Pattern = ModifiersRegEx + "\r\n" + "[;']\t# START COMMENT CHARS" + "\r\n" + ".+\t# MATCH ANYTHING",
                Links = new List<ncLexerLink> { new ncLexerLink("", ncLexerLink.CmdType.None) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            // *******************************************************************************************************

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(236, 155, 172),
                CmdName = "Tool Call",
                Pattern = ModifiersRegEx + "\r\n" + 
                "(M\\s*0?6)?" + "\r\n" + 
                AllowWhiteSpacesRegex + "\r\n" + 
                "T" + "\r\n" +
                AllowWhiteSpacesRegex + "\r\n" +
                "[0-9]+" + "\r\n" +
                AllowWhiteSpacesRegex + "\r\n" +
                "(M\\s*0?6)?" +
                AllowWhiteSpacesRegex + "\r\n" +
                "(H\\s*[0-9]+)?",
                Links = new List<ncLexerLink> { new ncLexerLink("(?<=[Tt])" + PositiveIntRegex, ncLexerLink.CmdType.ToolCallByNumber) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            // *******************************************************************************************************

            string GetGeometricRegex(string s0, string s1)
            {
                return
                ModifiersRegEx + "\r\n" +
                s0 + "\t# GEOMETRIC FUNCTIONS CHAR" + "\r\n" +
                AllowWhiteSpacesRegex + "\r\n" +
                s1;
            }

            // *******************************************************************************************************

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(255, 105, 0),
                CmdName = "Linear Move",
                Pattern = GetGeometricRegex("G", "0?1\t# LINEAR (WITH OR WITHOUT LEADING 0)"),
                Links = new List<ncLexerLink> { new ncLexerLink("", ncLexerLink.CmdType.Linear) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(255, 105, 0),
                CmdName = "Circular CW Move",
                Pattern = GetGeometricRegex("G", "0?2\t# CIRCULAR CW (WITH OR WITHOUT LEADING 0)"),
                Links = new List<ncLexerLink> { new ncLexerLink("", ncLexerLink.CmdType.CircularCW) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(255, 105, 0),
                CmdName = "Circular CCW Move",
                Pattern = GetGeometricRegex("G", "0?3\t# CIRCULAR CW (WITH OR WITHOUT LEADING 0)"),
                Links = new List<ncLexerLink> { new ncLexerLink("", ncLexerLink.CmdType.CircularCCW) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(255, 105, 0),
                CmdName = "Rapid Move",
                Pattern = GetGeometricRegex("G", "0?0\t# CIRCULAR CW (WITH OR WITHOUT LEADING 0)"),
                Links = new List<ncLexerLink> { new ncLexerLink("", ncLexerLink.CmdType.Rapid) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(255, 105, 0),
                CmdName = "Dwell",
                Pattern = GetGeometricRegex("G", "0?4\t# CIRCULAR CW (WITH OR WITHOUT LEADING 0)"),
                Links = new List<ncLexerLink> { new ncLexerLink("", ncLexerLink.CmdType.Dwell) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            // *******************************************************************************************************

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(255, 105, 0),
                CmdName = "Absolute coordinates",
                Pattern = GetGeometricRegex("G", "90\t# ABSOLUTE COORDINATES"),
                Links = new List<ncLexerLink> { new ncLexerLink("", ncLexerLink.CmdType.SetXYZAbsolute) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(255, 105, 0),
                CmdName = "Absolute coordinates",
                Pattern = GetGeometricRegex("G", "91\t# ABSOLUTE COORDINATES"),
                Links = new List<ncLexerLink> { new ncLexerLink("", ncLexerLink.CmdType.SetXYZRelative) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(255, 105, 0),
                CmdName = "Absolute coordinates",
                Pattern = GetGeometricRegex("G", "90\\s*\\.\\s*1\t# ABSOLUTE COORDINATES"),
                Links = new List<ncLexerLink> { new ncLexerLink("", ncLexerLink.CmdType.SetIJKAbsolute) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(255, 105, 0),
                CmdName = "Absolute coordinates",
                Pattern = GetGeometricRegex("G", "91\\s*\\.\\s*1\t# ABSOLUTE COORDINATES"),
                Links = new List<ncLexerLink> { new ncLexerLink("", ncLexerLink.CmdType.SetIJKRelative) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            // *******************************************************************************************************

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(255, 105, 0),
                CmdName = "Select Plane XY",
                Pattern = GetGeometricRegex("G", "17\t# XY PLANE"),
                Links = new List<ncLexerLink> { new ncLexerLink("", ncLexerLink.CmdType.SetWorkingPlaneXY) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(255, 105, 0),
                CmdName = "Select Plane XZ",
                Pattern = GetGeometricRegex("G", "18\t# XZ PLANE"),
                Links = new List<ncLexerLink> { new ncLexerLink("", ncLexerLink.CmdType.SetWorkingPlaneXZ) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(255, 105, 0),
                CmdName = "Select Plane YZ",
                Pattern = GetGeometricRegex("G", "19\t# YZ PLANE"),
                Links = new List<ncLexerLink> { new ncLexerLink("", ncLexerLink.CmdType.SetWorkingPlaneYZ) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(255, 105, 0),
                CmdName = "Set Reference",
                Pattern = GetGeometricRegex("G", "92\t# SET COORDINATES REFERENCE"),
                Links = new List<ncLexerLink> { new ncLexerLink("", ncLexerLink.CmdType.SetReference) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(255, 105, 0),
                CmdName = "Reset Reference",
                Pattern = GetGeometricRegex("G", "92\\s*\\.\\s*1\t# RESSET COORDINATES REFERENCE"),
                Links = new List<ncLexerLink> { new ncLexerLink("", ncLexerLink.CmdType.ResetReference) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(255, 105, 0),
                CmdName = "Set Offset",
                Pattern = GetGeometricRegex("G", "52\t# SET OFFSET"),
                Links = new List<ncLexerLink> { new ncLexerLink("", ncLexerLink.CmdType.WorkOffset) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            // *******************************************************************************************************

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(255, 105, 0),
                CmdName = "Scale",
                Pattern = GetGeometricRegex("G", "51\t# SCALE"),
                Links = new List<ncLexerLink> { new ncLexerLink("", ncLexerLink.CmdType.Scale) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(255, 105, 0),
                CmdName = "Reset Scale",
                Pattern = GetGeometricRegex("G", "50\t# RESET SCALE"),
                Links = new List<ncLexerLink> { new ncLexerLink("", ncLexerLink.CmdType.ResetScale) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(255, 105, 0),
                CmdName = "Rotate",
                Pattern = GetGeometricRegex("G", "68\t# ROTATE"),
                Links = new List<ncLexerLink> { new ncLexerLink("", ncLexerLink.CmdType.Rotate) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(255, 105, 0),
                CmdName = "Reset Rotate",
                Pattern = GetGeometricRegex("G", "69\t# RESET ROTATE"),
                Links = new List<ncLexerLink> { new ncLexerLink("", ncLexerLink.CmdType.ResetRotate) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            // *******************************************************************************************************

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(100, 50, 100),
                CmdName = "Sub Call",
                Pattern = ModifiersRegEx + "\r\n" + "M\t# MISC FUNCTIONS CHAR" + "\r\n" + AllowWhiteSpacesRegex + "\r\n" + "98",
                Links = new List<ncLexerLink> { new ncLexerLink("", ncLexerLink.CmdType.SubCall) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(100, 50, 100),
                CmdName = "Sub End",
                Pattern = ModifiersRegEx + "\r\n" + "M\t# MISC FUNCTIONS CHAR" + "\r\n" + AllowWhiteSpacesRegex + "\r\n" + "((30)|(99)|(0?2))",
                Links = new List<ncLexerLink> { new ncLexerLink("", ncLexerLink.CmdType.SubEnd) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(88, 155, 172),
                CmdName = "Sub Start",
                Pattern = ModifiersRegEx + "\r\n" + "O" + "\r\n" + AllowWhiteSpacesRegex + "\r\n" + "[0-9]+",
                Links = new List<ncLexerLink> { new ncLexerLink("", ncLexerLink.CmdType.SubStart), new ncLexerLink(IDRegex, ncLexerLink.CmdType.SubName) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            // *******************************************************************************************************

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(100, 50, 100),
                CmdName = "Other Misc functions",
                Pattern = GetGeometricRegex("M", "[0-9]+"),
                Links = new List<ncLexerLink> { new ncLexerLink("", ncLexerLink.CmdType.None) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            entry = new ncLexerEntry
            {
                Color = Color.FromArgb(255, 105, 0),
                CmdName = "Other Geometric functions",
                Pattern = GetGeometricRegex("G", "[0-9]+"),
                Links = new List<ncLexerLink> { new ncLexerLink("", ncLexerLink.CmdType.None) }
            };
            Lexer.LexerEntries.Add(entry.Clone());

            return;
        }
        #endregion
    }
}
