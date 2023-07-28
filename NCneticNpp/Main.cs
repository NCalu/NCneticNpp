using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Kbg.NppPluginNET.PluginInfrastructure;
using NCneticCore;
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace NCneticNpp
{
    class Main
    {
        internal const string PluginName = "NCnetic";

        static string iniFilePath = null;
        static ViewForm frmMyDlg = null;
        static Bitmap tbBmp = NCneticNpp.Properties.Resources.icon;
        static Bitmap tbBmp_tbTab = NCneticNpp.Properties.Resources.icon;
        static Icon tbIcon = null;

        static ncMachine mach = new ncMachine();
        static bool styling = true;
        static int cam = 0;

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

        public static string PopValue(string section, string key, string path)
        {
            StringBuilder sb = new StringBuilder(4096);
            int n = GetPrivateProfileString(section, key, "", sb, 4096, path);
            if (n < 1) return string.Empty;
            return sb.ToString();
        }

        public static void OnNotification(ScNotification notification)
        {
            if (notification.Header.Code == (uint)SciMsg.SCN_UPDATEUI)
            {
                if (frmMyDlg == null)
                {
                    return;
                }

                StringBuilder sbCurFile = new StringBuilder(Win32.MAX_PATH);
                Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_GETFULLCURRENTPATH, Win32.MAX_PATH, sbCurFile);

                if (sbCurFile.ToString() != frmMyDlg.currentFile)
                {
                    return;
                }

                int currentPos = (int)Win32.SendMessage(PluginBase.nppData._scintillaMainHandle, SciMsg.SCI_GETCURRENTPOS, 0, 0);
                int currentLine = (int)Win32.SendMessage(PluginBase.nppData._scintillaMainHandle, SciMsg.SCI_LINEFROMPOSITION, currentPos, 0);

                if (frmMyDlg.currentLine != currentLine)
                {
                    frmMyDlg.SetSelection(currentLine);
                }

                if (styling) { StyleVisible(); }
            }
        }

        internal static void CommandMenuInit()
        {
            StringBuilder sbFile = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, sbFile);
            iniFilePath = sbFile.ToString();
            if (!Directory.Exists(iniFilePath)) Directory.CreateDirectory(iniFilePath);
            iniFilePath = Path.Combine(iniFilePath, PluginName + ".ini");

            styling = (Win32.GetPrivateProfileInt("NCNETIC", "Styling", 1, iniFilePath) != 0);
            cam = Win32.GetPrivateProfileInt("NCNETIC", "Camera", 0, iniFilePath);

            PluginBase.SetCommand(0, "Plot/Refresh", PlotFunction);
            PluginBase.SetCommand(1, "---", null);
            PluginBase.SetCommand(2, "Styling", ChangeStyleState, styling);
            PluginBase.SetCommand(3, "---", null);

            bool viewchk;

            viewchk = false;
            if (cam == 0) { viewchk = true; }
            PluginBase.SetCommand(4, "ISO", ChangeViewState_iso, viewchk);

            viewchk = false;
            if (cam == 1) { viewchk = true; }
            PluginBase.SetCommand(5, "XY", ChangeViewState_xy, viewchk);

            viewchk = false;
            if (cam == 2) { viewchk = true; }
            PluginBase.SetCommand(6, "XZ", ChangeViewState_xz, viewchk);

            viewchk = false;
            if (cam == 3) { viewchk = true; }
            PluginBase.SetCommand(7, "YZ", ChangeViewState_yz, viewchk);

        }

        internal static void SetToolBarIcon()
        {
            toolbarIcons tbIcons = new toolbarIcons();
            tbIcons.hToolbarBmp = tbBmp.GetHbitmap();
            IntPtr pTbIcons = Marshal.AllocHGlobal(Marshal.SizeOf(tbIcons));
            Marshal.StructureToPtr(tbIcons, pTbIcons, false);
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_ADDTOOLBARICON, PluginBase._funcItems.Items[0]._cmdID, pTbIcons);
            Marshal.FreeHGlobal(pTbIcons);
        }

        internal static void PluginCleanUp()
        {
        }

        internal static void ChangeStyleState()
        {
            if (styling)
            {
                styling = false;
                Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_SETMENUITEMCHECK, PluginBase._funcItems.Items[2]._cmdID, 0x00000000); // MF_UNCHECKED
            }
            else
            {
                styling = true;
                Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_SETMENUITEMCHECK, PluginBase._funcItems.Items[2]._cmdID, 0x00000008); // MF_CHECKED
            }

            Win32.WritePrivateProfileString("NCNETIC", "Styling", styling ? "1" : "0", iniFilePath);
        }

        internal static void ChangeViewState_iso()
        {
            cam = 0;
            ChangeViewState();
        }

        internal static void ChangeViewState_xy()
        {
            cam = 1;
            ChangeViewState();
        }

        internal static void ChangeViewState_xz()
        {
            cam = 2;
            ChangeViewState();
        }

        internal static void ChangeViewState_yz()
        {
            cam = 3;
            ChangeViewState();
        }

        private static void ChangeViewState()
        {
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_SETMENUITEMCHECK, PluginBase._funcItems.Items[4]._cmdID, 0x00000000); // MF_UNCHECKED
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_SETMENUITEMCHECK, PluginBase._funcItems.Items[5]._cmdID, 0x00000000); // MF_UNCHECKED
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_SETMENUITEMCHECK, PluginBase._funcItems.Items[6]._cmdID, 0x00000000); // MF_UNCHECKED
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_SETMENUITEMCHECK, PluginBase._funcItems.Items[7]._cmdID, 0x00000000); // MF_UNCHECKED

            switch (cam)
            {
                case 1:
                    Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_SETMENUITEMCHECK, PluginBase._funcItems.Items[5]._cmdID, 0x00000008); // MF_CHECKED
                    break;

                case 2:
                    Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_SETMENUITEMCHECK, PluginBase._funcItems.Items[6]._cmdID, 0x00000008); // MF_CHECKED
                    break;

                case 3:
                    Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_SETMENUITEMCHECK, PluginBase._funcItems.Items[7]._cmdID, 0x00000008); // MF_CHECKED
                    break;

                case 0:
                default:
                    Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_SETMENUITEMCHECK, PluginBase._funcItems.Items[4]._cmdID, 0x00000008); // MF_CHECKED
                    break;
            }

            Win32.WritePrivateProfileString("NCNETIC", "Camera", cam.ToString(), iniFilePath);

            frmMyDlg.SetCam(cam);
        }

        internal static void PlotFunction()
        {
            int currentPos, currentLine;

            if (frmMyDlg == null)
            {
                frmMyDlg = new ViewForm();

                // **********************************************************************************************************************************
                frmMyDlg.SelChanged += new ViewForm.SelChangedEventHandler((s, ea) =>
                {
                    StringBuilder sbFile = new StringBuilder(Win32.MAX_PATH);
                    Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_GETFULLCURRENTPATH, Win32.MAX_PATH, sbFile);

                    if (sbFile.ToString() != frmMyDlg.currentFile)
                    {
                        if (ea.GetLine() == -1)
                        {
                            return;
                        }

                        IntPtr filePathPtr = Marshal.StringToHGlobalUni(frmMyDlg.currentFile);
                        Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_SWITCHTOFILE, IntPtr.Zero, filePathPtr);

                        sbFile = new StringBuilder(Win32.MAX_PATH);
                        Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_GETFULLCURRENTPATH, Win32.MAX_PATH, sbFile);
                        if (sbFile.ToString() != frmMyDlg.currentFile)
                        {
                            return;
                        }
                    }

                    if (ea.GetLine() == -1)
                    {
                        frmMyDlg.ResetSelection();
                    }
                    else
                    {
                        frmMyDlg.SetSelection(ea.GetLine());

                        currentPos = (int)Win32.SendMessage(PluginBase.nppData._scintillaMainHandle, SciMsg.SCI_GETCURRENTPOS, 0, 0);
                        currentLine = (int)Win32.SendMessage(PluginBase.nppData._scintillaMainHandle, SciMsg.SCI_LINEFROMPOSITION, currentPos, 0);
                        if (currentLine != ea.GetLine())
                        {
                            int targetPos = (int)Win32.SendMessage(PluginBase.nppData._scintillaMainHandle, SciMsg.SCI_POSITIONFROMLINE, ea.GetLine(), 0);
                            Win32.SendMessage(PluginBase.nppData._scintillaMainHandle, SciMsg.SCI_GOTOPOS, targetPos, 0);
                        }
                    }
                });

                frmMyDlg.CloseClick += new ViewForm.CloseClickEventHandler((s, ea) =>
                {
                    StringBuilder sbFile = new StringBuilder(Win32.MAX_PATH);
                    Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_GETFULLCURRENTPATH, Win32.MAX_PATH, sbFile);

                    if (sbFile.ToString() != frmMyDlg.currentFile)
                    {
                        return;
                    }

                    Win32.SendMessage(PluginBase.nppData._scintillaMainHandle, SciMsg.SCI_CLEARDOCUMENTSTYLE, 0, 0);
                });
                // **********************************************************************************************************************************

                using (Bitmap newBmp = new Bitmap(16, 16))
                {
                    Graphics g = Graphics.FromImage(newBmp);
                    ColorMap[] colorMap = new ColorMap[1];
                    colorMap[0] = new ColorMap();
                    colorMap[0].OldColor = Color.Fuchsia;
                    colorMap[0].NewColor = Color.FromKnownColor(KnownColor.ButtonFace);
                    ImageAttributes attr = new ImageAttributes();
                    attr.SetRemapTable(colorMap);
                    g.DrawImage(tbBmp_tbTab, new Rectangle(0, 0, 16, 16), 0, 0, 16, 16, GraphicsUnit.Pixel, attr);
                    tbIcon = Icon.FromHandle(newBmp.GetHicon());
                }

                NppTbData _nppTbData = new NppTbData();
                _nppTbData.hClient = frmMyDlg.Handle;
                _nppTbData.pszName = "NCnetic";
                _nppTbData.dlgID = 0;
                _nppTbData.uMask = NppTbMsg.DWS_DF_CONT_RIGHT | NppTbMsg.DWS_ICONTAB | NppTbMsg.DWS_ICONBAR;
                _nppTbData.hIconTab = (uint)tbIcon.Handle;
                _nppTbData.pszModuleName = PluginName;
                IntPtr _ptrNppTbData = Marshal.AllocHGlobal(Marshal.SizeOf(_nppTbData));
                Marshal.StructureToPtr(_nppTbData, _ptrNppTbData, false);

                Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_DMMREGASDCKDLG, 0, _ptrNppTbData);
            }
            else
            {
                Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_DMMSHOW, 0, frmMyDlg.Handle);
            }

            // **********************************************************************************************************************************

            int length = (int)Win32.SendMessage(PluginBase.nppData._scintillaMainHandle, SciMsg.SCI_GETLENGTH, 0, 0);
            IntPtr ptrToText = Marshal.AllocHGlobal(length + 1);
            Win32.SendMessage(PluginBase.nppData._scintillaMainHandle, SciMsg.SCI_GETTEXT, length, ptrToText);
            string textAnsi = Marshal.PtrToStringAnsi(ptrToText);
            Marshal.FreeHGlobal(ptrToText);

            StringBuilder sbCurFile = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_GETFULLCURRENTPATH, Win32.MAX_PATH, sbCurFile);

            frmMyDlg.LoadFile(sbCurFile.ToString(), textAnsi, mach, cam);
            currentPos = (int)Win32.SendMessage(PluginBase.nppData._scintillaMainHandle, SciMsg.SCI_GETCURRENTPOS, 0, 0);
            currentLine = (int)Win32.SendMessage(PluginBase.nppData._scintillaMainHandle, SciMsg.SCI_LINEFROMPOSITION, currentPos, 0);
            frmMyDlg.SetSelection(currentLine);
            frmMyDlg.ResetSelection();

            if (styling) { StyleVisible(); }
        }

        internal static void StyleVisible()
        {
            // **********************************************************************************************************************************

            int id = 50;
            int intColor;
            foreach (ncLexerEntry entry in mach.Lexer.LexerEntries)
            {
                intColor = (entry.Color.B << 16) | (entry.Color.G << 8) | (entry.Color.R);
                Win32.SendMessage(PluginBase.nppData._scintillaMainHandle, SciMsg.SCI_STYLESETFORE, (IntPtr)id, intColor);
                Win32.SendMessage(PluginBase.nppData._scintillaMainHandle, SciMsg.SCI_STYLESETBOLD, (IntPtr)id, 1);
                id++;
            }

            // **********************************************************************************************************************************

            int firstLine = (int)Win32.SendMessage(PluginBase.nppData._scintillaMainHandle, SciMsg.SCI_GETFIRSTVISIBLELINE, 0, 0);
            firstLine = (int)Win32.SendMessage(PluginBase.nppData._scintillaMainHandle, SciMsg.SCI_DOCLINEFROMVISIBLE, firstLine, 0);
            int totLines = (int)Win32.SendMessage(PluginBase.nppData._scintillaMainHandle, SciMsg.SCI_LINESONSCREEN, 0, 0);

            int pos = 0;
            string line = "";
            int[,] styleTable;
            for (int i = firstLine; i <= firstLine + totLines; i++)
            {
                pos = (int)Win32.SendMessage(PluginBase.nppData._scintillaMainHandle, SciMsg.SCI_POSITIONFROMLINE, i, 0);
                Win32.SendMessage(PluginBase.nppData._scintillaMainHandle, SciMsg.SCI_STARTSTYLING, pos, 0);

                line = GetLine(i);
                line.Replace("\r\n", "");

                styleTable = mach.Lexer.GetLineStyleTable(line);

                for (int j = 0; j < styleTable.GetLength(0); j++)
                {
                    if (styleTable[j, 2] >= 0)
                    {
                        Win32.SendMessage(PluginBase.nppData._scintillaMainHandle, SciMsg.SCI_SETSTYLING, styleTable[j, 1], 50 + styleTable[j, 2]);
                    }
                    else
                    {
                        Win32.SendMessage(PluginBase.nppData._scintillaMainHandle, SciMsg.SCI_SETSTYLING, styleTable[j, 1], 0);
                    }
                }
            }
        }

        internal static unsafe string GetLine(int line)
        {
            byte[] textBuffer = new byte[10000];
            fixed (byte* textPtr = textBuffer)
            {
                Win32.SendMessage(PluginBase.nppData._scintillaMainHandle, SciMsg.SCI_GETLINE, (IntPtr)line, (IntPtr)textPtr);
                return Encoding.UTF8.GetString(textBuffer).TrimEnd('\0');
            }
        }
    }
}