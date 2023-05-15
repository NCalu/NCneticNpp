using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using System.Drawing;

namespace NCneticCore.View
{
    public class ncView
    {
        #region fields

        public int SelId
        {
            get { return _selId; }
        }

        public string SelGuid
        {
            get { return _selGuid; }
        }

        public double SelPos
        {
            get { return _selPos; }
        }

        private string _selGuid = string.Empty;
        private double _selPos = 0.0;
        private int _selId = -1;

        private ncViewOptions Options = new ncViewOptions();

        private Camera Cam = new Camera();

        private ObjectsCollection ObjCollection;

        private Timer RefreshTimer;

        private bool MoveSelect = false;
        private bool HighLigthCheck = false;
        private double getHighLightedTime = 0.0;

        private List<CAO.Animation> Anims = new List<CAO.Animation>();
        private double ViewReCenterAnimTime = 400.0;

        private Shader ActiveShader;

        private Matrix4 GlobalViewMatrix = Matrix4.Identity;
        private Matrix4 GlobalModelMatrix = Matrix4.Identity;
        private Matrix4 GlobalProjectionMatrix = Matrix4.Identity;

        #endregion

        #region constructors
        public ncView(ncViewOptions options)
        {
            Options = options;
            ObjCollection = new ObjectsCollection(Options);

            RefreshTimer = new Timer(Options.RefreshTime);
            RefreshTimer.Elapsed += RefreshTimerElapsed;

            ObjCollection.Reload();
        }
        #endregion

        #region view

        public void IniGraphicContext(IntPtr handle)
        {
            IWindowInfo windowInfo = Utilities.CreateWindowsWindowInfo(handle);
            GraphicsContext context = new GraphicsContext(GraphicsMode.Default, windowInfo);
            context.MakeCurrent(windowInfo);
            context.LoadAll();
        }

        public void ViewPortLoad(int sizeX, int sizeY)
        {
            Options.ViewSizeX = sizeX;
            Options.ViewSizeY = sizeY;

            int ibo_elements;

            GL.ClearColor(0.7f, 0.7f, 0.7f, 0f);

            GL.GenBuffers(1, out ibo_elements);
            ActiveShader = new Shader();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo_elements);

            GL.DepthMask(true);
            GL.LineWidth(1.25f);

            GL.PointSize(7f);

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.PolygonOffsetLine);

            GL.Viewport(0, 0, Options.ViewSizeX, Options.ViewSizeY);

            GL.Enable(EnableCap.PolygonOffsetFill);
            GL.PolygonOffset(1f, 1f);

            ObjCollection.Reload();
            ObjCollection.SetMoveSelection(null);

            ViewPortUpdate();
            Bind();

            SelectMove(new ncMove());

            RefreshTimer.Start();
        }

        public void ViewPortPaint()
        {
            if (ActiveShader == null)
            {
                return;
            }

            GL.Viewport(0, 0, Options.ViewSizeX, Options.ViewSizeY);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            ActiveShader.EnableVertexAttribArrays();
            ObjCollection.Draw(GlobalProjectionMatrix, GlobalModelMatrix, GlobalViewMatrix, ActiveShader);
            ActiveShader.DisableVertexAttribArrays();

            GL.Flush();
            ViewPortUpdate();
        }

        public void ViewChangeSize(int sizeX, int sizeY)
        {
            Options.ViewSizeX = sizeX;
            Options.ViewSizeY = sizeY;

            ViewPortUpdate();
            Bind();
        }

        public void UpdateViewOpts(ncViewOptions opts)
        {
            RefreshTimer.Stop();

            opts.ViewSizeX = Options.ViewSizeX;
            opts.ViewSizeY = Options.ViewSizeY;

            Options = opts.Clone();
            RefreshTimer.Interval = opts.RefreshTime;
            ObjCollection.UpdateViewOpts(Options);

            ObjCollection.Reload();
            ObjCollection.SetMoveSelection(null);
            ViewPortUpdate();
            Bind();

            RefreshTimer.Start();
        }

        private void Bind()
        {
            if (ActiveShader == null)
            {
                return;
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, ActiveShader.GetBuffer("vPosition"));
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(ObjCollection.vertdata.Length * Vector3.SizeInBytes), ObjCollection.vertdata, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(ActiveShader.GetAttribute("vPosition"), 3, VertexAttribPointerType.Float, false, 0, 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, ActiveShader.GetBuffer("vNormal"));
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(ObjCollection.normdata.Length * Vector3.SizeInBytes), ObjCollection.normdata, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(ActiveShader.GetAttribute("vNormal"), 3, VertexAttribPointerType.Float, false, 0, 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, ActiveShader.GetBuffer("vColor"));
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(ObjCollection.coldata.Length * Vector3.SizeInBytes), ObjCollection.coldata, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(ActiveShader.GetAttribute("vColor"), 3, VertexAttribPointerType.Float, true, 0, 0);

            GL.UseProgram(ActiveShader.ProgramID);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(ObjCollection.indicedata.Length * sizeof(int)), ObjCollection.indicedata, BufferUsageHint.StaticDraw);

            GL.Uniform1(ActiveShader.GetUniform("sWidth"), Options.ViewSizeX);
            GL.Uniform1(ActiveShader.GetUniform("sHeight"), Options.ViewSizeY);
        }

        private void ViewPortUpdate()
        {
            GlobalViewMatrix = Cam.GetViewMatrix();
            GlobalProjectionMatrix = Matrix4.CreateOrthographic(2.0f * Convert.ToSingle(Options.ViewSizeX) / Convert.ToSingle(Options.ViewSizeY), 2f, 100f, -100f);
        }

        private void RefreshTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                Refresh?.Invoke(this, new EventArgs());
                List<CAO.Animation> latmp = new List<CAO.Animation>();

                foreach (CAO.Animation a in Anims)
                {
                    a.CurStep++;

                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            Vector3 NewR = new Vector3(a.StartMatrix.Column0 + ((float)a.CurStep / (float)a.TotStep) * (a.EndMatrix.Column0 - a.StartMatrix.Column0));
                            NewR.NormalizeFast();
                            Vector3 NewU = new Vector3(a.StartMatrix.Column1 + ((float)a.CurStep / (float)a.TotStep) * (a.EndMatrix.Column1 - a.StartMatrix.Column1));
                            NewU.NormalizeFast();
                            Vector3 NewV = Vector3.Cross(NewU, NewR);
                            NewV.NormalizeFast();
                            NewU = Vector3.Cross(NewR, NewV);
                            NewU.NormalizeFast();

                            Cam.M.Column0 = new Vector4(NewR, 0f);
                            Cam.M.Column1 = new Vector4(NewU, 0f);
                            Cam.M.Column2 = new Vector4(NewV, 0f);
                        }
                    }

                    Cam.M[3, 3] = a.StartMatrix[3, 3] + ((float)a.CurStep / (float)a.TotStep) * (a.EndMatrix[3, 3] - a.StartMatrix[3, 3]);

                    for (int j = 0; j < 3; j++)
                    {
                        GlobalModelMatrix[3, j] = a.StartMatrix[3, j] + ((float)a.CurStep / (float)a.TotStep) * (a.EndMatrix[3, j] - a.StartMatrix[3, j]);
                    }

                    if (a.CurStep < a.TotStep)
                    {
                        latmp.Add(a);
                    }
                }
                Anims = latmp;
            }
            catch { }
        }
        #endregion

        #region mouse

        public void WheelZoom(int X, int Y, float delta)
        {
            Vector4 V;
            V.X = ((float)X - Options.ViewSizeX / 2) / Options.ViewSizeY / 2f * Cam.M[3, 3] * Math.Sign(delta);
            V.Y = -((float)Y - Options.ViewSizeY / 2) / Options.ViewSizeY / 2f * Cam.M[3, 3] * Math.Sign(delta);
            V.Z = 0f;
            V.W = 1f;

            Matrix4 viewInvCam = Matrix4.Invert(Cam.M);
            Matrix4 projInvCam = Matrix4.Identity;

            Vector4.Transform(ref V, ref viewInvCam, out V);
            Vector4.Transform(ref V, ref projInvCam, out V);

            GlobalModelMatrix[3, 0] += V.X;
            GlobalModelMatrix[3, 1] += V.Y;
            GlobalModelMatrix[3, 2] += V.Z;

            Cam.Zoom_Wheel(delta);

            ViewPortUpdate();
        }

        public void MouseZoom(int dY)
        {
            float delta = Cam.M[3, 3] + dY;

            Cam.Zoom_Mouse(delta);
        }

        public void MousePan(ref int X, ref int Y, int dX, int dY)
        {
            Matrix4 viewInvCam = Matrix4.Invert(Cam.M);
            Matrix4 projInvCam = Matrix4.Identity;

            Vector4 V;
            V.X = ((float)dX) / (float)Options.ViewSizeY * 2 * Cam.M[3, 3];
            V.Y = -((float)dY) / (float)Options.ViewSizeY * 2 * Cam.M[3, 3];
            V.Z = 0f;
            V.W = 1f;

            Vector4.Transform(ref V, ref viewInvCam, out V);
            Vector4.Transform(ref V, ref projInvCam, out V);

            GlobalModelMatrix[3, 0] += V.X;
            GlobalModelMatrix[3, 1] += V.Y;
            GlobalModelMatrix[3, 2] += V.Z;

            X += dX;
            Y += dY;
        }

        public void MouseRotate(ref int X, ref int Y, int dX, int dY)
        {
            Cam.Rot((float)dX, (float)dY);

            X += dX;
            Y += dY;
        }

        public void MouseHighlight(int X, int Y)
        {
            if (!HighLigthCheck && getHighLightedTime < 80)
            {
                HighLigthCheck = true;
                DateTime now = DateTime.Now;
                ObjCollection.SetMoveHighLighted(GetMoveByMousePos(X, Y).move);
                getHighLightedTime = (DateTime.Now - now).TotalMilliseconds;
                HighLigthCheck = false;
            }
        }

        public void MouseMoveSelect(int X, int Y)
        {
            MoveSelection ms = GetMoveByMousePos(X, Y);
            if (ms.move == null)
            {
                MoveSelected?.Invoke(this, new MoveSelectedEventArgs(String.Empty, -1));
            }
            else
            {
                MoveSelected?.Invoke(this, new MoveSelectedEventArgs(ms.moveguid, ms.moveid));
            }
        }
        #endregion

        #region loading

        public void LoadJob(ncJob job)
        {
            LoadWork(job);
            LoadComplete(job);
        }

        public void LoadJobAsync(ncJob job)
        {
            BackgroundWorker worker = new BackgroundWorker();

            worker.DoWork += new DoWorkEventHandler((sw, eaw) =>
            {
                LoadWork(job);
            });

            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler((s, ea) =>
            {
                LoadComplete(job);
            });

            worker.RunWorkerAsync();
        }

        private void LoadWork(ncJob job)
        {
            StartLoading?.Invoke(this, new EventArgs());

            getHighLightedTime = 0.0;
            List<CAO.MoveObject> moveList = new List<CAO.MoveObject>();
            List<ncMove> segList;

            Dictionary<int, Color> colorTable = new Dictionary<int, Color>();

            Color[] colors = new Color[10]
            {
                Options.FeedColor0,
                Options.FeedColor1,
                Options.FeedColor2,
                Options.FeedColor3,
                Options.FeedColor4,
                Options.FeedColor5,
                Options.FeedColor6,
                Options.FeedColor7,
                Options.FeedColor8,
                Options.FeedColor9,
            };

            int curColorId = 0;
            int curNb;
            if (job.MoveList.Any())
            {
                curNb = job.MoveList.First().ToolNb;
                colorTable.Add(curNb, colors[curColorId]);
                curColorId++;
                foreach (ncMove m in job.MoveList)
                {
                    if (m.ToolNb != curNb)
                    {
                        curNb = m.ToolNb;
                        if (!colorTable.ContainsKey(curNb))
                        {
                            colorTable.Add(curNb, colors[curColorId]);
                            curColorId++;
                            if (curColorId >= 9) { curColorId = 0; }
                        }
                    }
                }
            }

            int p = 0;
            for (int j = 0; j < job.MoveList.Count; j++)
            {
                ncMove m = job.MoveList[j].Clone();

                if (p != j * 100 / job.MoveList.Count())
                {
                    p = j * 100 / job.MoveList.Count();
                    ReportProgressLoading?.Invoke(this, new ReportProgressLoadingEventArgs(p));
                }

                switch (m.Type)
                {
                    case ncMove.MoveType.Rapid:
                        if (!Options.HideRapids)
                        {
                            segList = new List<ncMove> { m };
                            moveList.Add(new CAO.MoveObject(segList, Options.RapidColor));
                        }
                        else
                        {
                            moveList.Add(new CAO.MoveObject(m.MoveGuid));
                        }
                        break;

                    case ncMove.MoveType.Linear:
                        segList = new List<ncMove> { m };
                        if (m.Color.A > 0)
                        {
                            moveList.Add(new CAO.MoveObject(segList, m.Color));
                        }
                        else
                        {
                            moveList.Add(new CAO.MoveObject(segList, colorTable[m.ToolNb]));
                        }

                        break;

                    case ncMove.MoveType.CircularCW:
                    case ncMove.MoveType.CircularCCW:
                        segList = ncFcts.PolygonizeArc(m, Options.ArcSectors);
                        if (m.Color.A > 0)
                        {
                            moveList.Add(new CAO.MoveObject(segList, m.Color));
                        }
                        else
                        {
                            moveList.Add(new CAO.MoveObject(segList, colorTable[m.ToolNb]));
                        }
                        break;
                }
            }

            ObjCollection.MoveObjList = moveList;
        }

        private void LoadComplete(ncJob job)
        {
            RefreshTimer.Stop();

            ObjCollection.Reload();
            ObjCollection.SetMoveSelection(null);
            ViewPortUpdate();
            Bind();

            RefreshTimer.Start();

            EndLoading?.Invoke(this, new EventArgs());
        }
        #endregion

        #region methods

        public void SelectMove(ncMove move)
        {
            if (MoveSelect)
            {
                return;
            }
            MoveSelect = true;

            CAO.MoveObject cm;

            cm = ObjCollection.MoveObjList.Find(x => x.MoveGuid == move.MoveGuid);
            ObjCollection.SetMoveSelection(cm);

            if (cm == null)
            {
                _selGuid = string.Empty;
                _selId = -1;
            }
            else
            {
                _selGuid = move.MoveGuid;
                _selId = ObjCollection.MoveObjList.FindIndex(x => x.MoveGuid == move.MoveGuid);
            }

            MoveSelect = false;
        }

        public void Recenter()
        {
            bool isdef = false;
            Vector3 min = new Vector3();
            Vector3 max = new Vector3();
            Vector3 minObj = new Vector3();
            Vector3 maxObj = new Vector3();

            foreach (CAO.MoveObject obj in ObjCollection.MoveObjList)
            {
                if (obj.IndiceCountLn > 0)
                {
                    minObj = obj.GetMin();
                    maxObj = obj.GetMax();

                    if (!isdef)
                    {
                        min = minObj;
                        max = maxObj;
                        isdef = true;
                    }

                    min.X = Math.Min(minObj.X, min.X);
                    min.Y = Math.Min(minObj.Y, min.Y);
                    min.Z = Math.Min(minObj.Z, min.Z);

                    max.X = Math.Max(maxObj.X, max.X);
                    max.Y = Math.Max(maxObj.Y, max.Y);
                    max.Z = Math.Max(maxObj.Z, max.Z);
                }
            }

            Matrix4 startMatrix = Cam.M;
            startMatrix[3, 0] = GlobalModelMatrix[3, 0];
            startMatrix[3, 1] = GlobalModelMatrix[3, 1];
            startMatrix[3, 2] = GlobalModelMatrix[3, 2];

            Matrix4 endMatrix = Cam.SetCam(min, max, Options);

            Anims.Add(new CAO.Animation((int)(ViewReCenterAnimTime / RefreshTimer.Interval), startMatrix, endMatrix));
        }

        private MoveSelection GetMoveByMousePos(int X, int Y)
        {
            try
            {
                Vector4 vec = UnProjectMouseClick(X, Y);
                Vector3 RayOrigin = new Vector3(
                    vec.X - GlobalModelMatrix[3, 0] - ObjCollection.MovesModelMatrix[3, 0],
                    vec.Y - GlobalModelMatrix[3, 1] - ObjCollection.MovesModelMatrix[3, 1],
                    vec.Z - GlobalModelMatrix[3, 2] - ObjCollection.MovesModelMatrix[3, 2]);
                Vector3 RayDir = new Vector3(GlobalViewMatrix[0, 2], GlobalViewMatrix[1, 2], GlobalViewMatrix[2, 2]);

                bool hit = false;
                int id = 0;
                float t = 0;
                float r = 0;
                float mint = 0;

                Vector3 pt = new Vector3();

                if (!hit)
                {
                    for (int i = 0; i < ObjCollection.MoveObjList.Count(); i++)
                    {
                        for (int j = 0; j < ObjCollection.MoveObjList[i].IndiceCountLn; j += 2)
                        {
                            Vector3 vec0 = Vector3.TransformVector(ObjCollection.MoveObjList[i].VertexLn[ObjCollection.MoveObjList[i].IndicesLn[j]],
                                ObjCollection.MovesModelMatrix); ;
                            Vector3 vec1 = Vector3.TransformVector(ObjCollection.MoveObjList[i].VertexLn[ObjCollection.MoveObjList[i].IndicesLn[j + 1]],
                                ObjCollection.MovesModelMatrix); ;

                            if (RayLnIntersection(RayOrigin, RayDir, vec0, vec1, ref t, ref r))
                            {
                                if (!hit || t < mint)
                                {
                                    hit = true;
                                    id = i;
                                    mint = t;

                                    pt.X = vec0.X + r * (vec1.X - vec0.X);
                                    pt.Y = vec0.Y + r * (vec1.Y - vec0.Y);
                                    pt.Z = vec0.Z + r * (vec1.Z - vec0.Z);
                                }
                            }
                        }
                    }
                }

                if (hit)
                {
                    return new MoveSelection(ObjCollection.MoveObjList[id], id, pt, r);
                }
                else
                {
                    return new MoveSelection(null, -1, new Vector3(), -1);
                }
            }
            catch
            {
                return new MoveSelection(null, -1, new Vector3(), -1);
            }
        }

        private Vector4 UnProjectMouseClick(int X, int Y)
        {
            Vector4 vec;

            vec.X = 2.0f * (float)X / (float)Options.ViewSizeX - 1f;
            vec.Y = -(2.0f * (float)Y / (float)Options.ViewSizeY - 1f);
            vec.Z = 0.0f;
            vec.W = 1.0f;

            Matrix4 viewInv = Matrix4.Invert(GlobalViewMatrix);
            Matrix4 projInv = Matrix4.Invert(GlobalProjectionMatrix);

            Vector4.Transform(ref vec, ref projInv, out vec);
            Vector4.Transform(ref vec, ref viewInv, out vec);

            if (vec.W > 0.000001f || vec.W < -0.000001f)
            {
                vec.X /= vec.W;
                vec.Y /= vec.W;
                vec.Z /= vec.W;
            }

            return vec;
        }

        private bool RayLnIntersection(Vector3 RayOrigin, Vector3 RayDir, Vector3 vec0, Vector3 vec1, ref float t, ref float r, float eps = 1E-2f)
        {
            Vector3 vec10, vecro0, vecro1, cross, dp, movedir;
            float d, d0, d1, uu, uv, vv, uw, vw, L, sc, tc;
            uu = Vector3.Dot(RayDir, RayDir);

            vec10 = new Vector3(vec1.X - vec0.X, vec1.Y - vec0.Y, vec1.Z - vec0.Z);
            L = vec10.Length;
            movedir = new Vector3(vec10.X / L, vec10.Y / L, vec10.Z / L);

            d = 1E9f;

            vecro0 = RayOrigin - vec0;
            vecro1 = vec1 - RayOrigin;

            cross = Vector3.Cross(vecro0, RayDir);
            d0 = cross.Length;

            cross = Vector3.Cross(vecro1, RayDir);
            d1 = cross.Length;

            uv = Vector3.Dot(RayDir, movedir);
            vv = Vector3.Dot(movedir, movedir);
            uw = Vector3.Dot(RayDir, vecro0);
            vw = Vector3.Dot(movedir, vecro0);

            if (Math.Abs(uu * vv - uv * uv) < eps)
            {
                d = d0;
            }
            else
            {
                sc = (uv * vw - vv * uw) / (uu * vv - uv * uv);
                tc = (uu * vw - uv * uw) / (uu * vv - uv * uv);

                if (tc >= 0 && tc <= L)
                {
                    dp = RayOrigin - vec0 + sc * RayDir - tc * movedir;
                    d = dp.Length;
                    t = sc;
                    r = tc / L;
                }
                else
                {
                    t = Vector3.Dot(vecro1, RayDir) / uu;
                    if (d0 < d1)
                    {
                        d = d0;
                        r = 0f;
                    }
                    else
                    {
                        d = d1;
                        r = 1f;
                    }
                }
            }

            if (d / Cam.M[3, 3] < eps)
            {
                return true;
            }

            return false;
        }

        private bool RayTriIntersection(Vector3 RayOrigin, Vector3 RayDir, Vector3 edge0, Vector3 edge1, Vector3 vec, ref float t, float eps = 1E-2f)
        {
            Vector3 h, s, q;
            float a, f, u, v;

            h = Vector3.Cross(RayDir, edge1);
            a = Vector3.Dot(edge0, h);

            if (Math.Abs(a) < eps)
            {
                return false;
            }

            f = 1f / a;
            s = RayOrigin - vec;
            u = f * Vector3.Dot(s, h);

            if (u < 0f || u > 1f)
            {
                return false;
            }

            q = Vector3.Cross(s, edge0);
            v = f * Vector3.Dot(RayDir, q);

            if (v < 0f || u + v > 1f)
            {
                return false;
            }

            t = f * Vector3.Dot(edge1, q);
            return true;
        }
        #endregion

        #region events
        public event EventHandler Refresh;

        public event MoveSelectedkEventHandler MoveSelected;
        public delegate void MoveSelectedkEventHandler(object source, MoveSelectedEventArgs e);
        public class MoveSelectedEventArgs : EventArgs
        {
            public int id;
            public string guid;

            public MoveSelectedEventArgs(string moveguid, int moveid)
            {
                guid = moveguid;
                id = moveid;
            }
        }

        public event EventHandler StartLoading;
        public event EventHandler EndLoading;

        public event ReportProgressLoadingEventHandler ReportProgressLoading;
        public delegate void ReportProgressLoadingEventHandler(object source, ReportProgressLoadingEventArgs e);
        public class ReportProgressLoadingEventArgs : EventArgs
        {
            public int progress;

            internal ReportProgressLoadingEventArgs(int p)
            {
                progress = p;
            }
        }
        #endregion

        internal class ObjectsCollection
        {
            #region enums
            private enum vRenderMode { COLOR, LIGHT, ARROW, INVISIBLE, SEL_OVER, SEL_PTS };
            private enum sRenderMode { NONE, TRANSPARENT, AFTERSEL };
            #endregion

            #region fields
            internal List<CAO.MoveObject> MoveObjList;

            internal Vector3 BackGroundColorTop;
            internal Vector3 BackGroundColorBot;
            internal Vector3 XAxisColor;
            internal Vector3 YAxisColor;
            internal Vector3 ZAxisColor;
            internal bool HideAxis;

            internal Matrix4 MovesModelMatrix = Matrix4.Identity;

            internal int[] indicedata;
            internal Vector3[] vertdata;
            internal Vector3[] normdata;
            internal Vector3[] coldata;

            private int IndiceAtSel = -1;
            private int LastIndiceAtSel = -1;
            private int IndiceAtOver = -1;

            private int SelIdCntToTri = 0;
            private int SelIdCntToLn = 0;
            private int SelTotIdsTri = 0;
            private int SelTotIdsLn = 0;

            private int OverIdCntToLn = 0;
            private int OverTotIdsLn = 0;

            private int SumAllIdsTri = 0;
            private int SumAllIdsLn = 0;

            #endregion

            #region constructors
            internal ObjectsCollection(ncViewOptions options)
            {
                MoveObjList = new List<CAO.MoveObject>();
                UpdateViewOpts(options);
            }
            #endregion

            #region methods

            internal void Reload()
            {
                IndiceAtSel = -1;
                LastIndiceAtSel = -1;
                IndiceAtOver = -1;

                SelIdCntToTri = 0;
                SelIdCntToLn = 0;
                SelTotIdsTri = 0;
                SelTotIdsLn = 0;

                OverIdCntToLn = 0;
                OverTotIdsLn = 0;

                SumAllIdsTri = 0;
                SumAllIdsLn = 0;

                int vertcount = 0;

                List<int> inds = new List<int>();
                List<Vector3> verts = new List<Vector3>();
                List<Vector3> normals = new List<Vector3>();
                List<Vector3> colors = new List<Vector3>();

                // Background
                verts.Add(new Vector3(-1.05f, -1.05f, -1f));
                verts.Add(new Vector3(1.05f, -1.05f, -1f));
                verts.Add(new Vector3(1.05f, 1.05f, -1f));
                verts.Add(new Vector3(-1.05f, 1.05f, -1f));
                inds.AddRange(new List<int> { 0, 1, 2, 0, 2, 3 });
                colors.AddRange(Enumerable.Repeat(BackGroundColorBot, 2));
                colors.AddRange(Enumerable.Repeat(BackGroundColorTop, 2));
                normals.AddRange(Enumerable.Repeat(new Vector3(1f, 1f, 1f), 4));
                vertcount += 4;

                // Axis
                float L = 1E6f;
                float l = 1f;

                verts.Add(new Vector3(-L, 0f, 0f));
                verts.Add(new Vector3(-l, 0f, 0f));
                verts.Add(new Vector3(l, 0f, 0f));
                verts.Add(new Vector3(L, 0f, 0f));
                inds.AddRange(new List<int> { 4, 5, 5, 6, 6, 7 });
                colors.AddRange(Enumerable.Repeat(XAxisColor, 4));
                normals.AddRange(Enumerable.Repeat(new Vector3(1f, 1f, 1f), 4));
                vertcount += 4;

                verts.Add(new Vector3(0f, -L, 0f));
                verts.Add(new Vector3(0f, -l, 0f));
                verts.Add(new Vector3(0f, l, 0f));
                verts.Add(new Vector3(0f, L, 0f));
                inds.AddRange(new List<int> { 8, 9, 9, 10, 10, 11 });
                colors.AddRange(Enumerable.Repeat(YAxisColor, 4));
                normals.AddRange(Enumerable.Repeat(new Vector3(1f, 1f, 1f), 4));
                vertcount += 4;

                verts.Add(new Vector3(0f, 0f, -L));
                verts.Add(new Vector3(0f, 0f, -l));
                verts.Add(new Vector3(0f, 0f, l));
                verts.Add(new Vector3(0f, 0f, L));
                inds.AddRange(new List<int> { 12, 13, 13, 14, 14, 15 });
                colors.AddRange(Enumerable.Repeat(ZAxisColor, 4));
                normals.AddRange(Enumerable.Repeat(new Vector3(1f, 1f, 1f), 4));
                vertcount += 4;

                //
                LoadMoves(MoveObjList);

                verts.Add(new Vector3());
                verts.Add(new Vector3());
                inds.Add(vertcount);
                inds.Add(vertcount + 1);
                colors.AddRange(Enumerable.Repeat(new Vector3(), 2));
                normals.AddRange(Enumerable.Repeat(new Vector3(1f, 1f, 1f), 2));
                vertcount += 2;
                //

                vertdata = verts.ToArray();
                indicedata = inds.ToArray();
                normdata = normals.ToArray();
                coldata = colors.ToArray();

                GC.Collect();

                void LoadMoves(List<CAO.MoveObject> moves)
                {
                    SumAllIdsLn = 0;
                    SumAllIdsTri = 0;

                    foreach (CAO.MoveObject obj in moves)
                    {
                        obj.IndiceAtLn = vertcount;

                        verts.AddRange(obj.VertexLn);
                        inds.AddRange(obj.GetIndicesLn(vertcount));
                        normals.AddRange(Enumerable.Repeat(new Vector3(1f, 1f, 1f), obj.VertexCountLn));
                        colors.AddRange(Enumerable.Repeat(obj.Color, obj.VertexCountLn));
                        vertcount += obj.VertexCountLn;

                        SumAllIdsLn += obj.IndiceCountLn;
                    }
                }
            }

            internal void UpdateViewOpts(ncViewOptions options)
            {
                BackGroundColorBot = GetVector3Color(options.BackGroundColorBot);
                BackGroundColorTop = GetVector3Color(options.BackGroundColorTop);
                XAxisColor = GetVector3Color(options.XAxisColor);
                YAxisColor = GetVector3Color(options.YAxisColor);
                ZAxisColor = GetVector3Color(options.ZAxisColor);
                HideAxis = options.HideAxis;
            }

            #endregion

            #region drawing
            internal void SetMoveHighLighted(CAO.MoveObject move)
            {
                if (move != null)
                {
                    IndiceAtOver = move.IndiceAtLn;
                }
                else
                {
                    IndiceAtOver = -1;
                }

                ComputeOverIds();
            }

            internal void Draw(Matrix4 globalprojectionmatrix, Matrix4 globalmodelmatrix, Matrix4 globalviewmatrix, Shader shader)
            {
                int indiceat = 0;

                DrawScene(ref indiceat, globalprojectionmatrix, MovesModelMatrix * globalmodelmatrix, globalviewmatrix, shader);

                GL.Clear(ClearBufferMask.DepthBufferBit);

                DrawMoves(ref indiceat, globalprojectionmatrix, MovesModelMatrix * globalmodelmatrix, globalviewmatrix, shader);
                DrawSelPoints(ref indiceat, globalprojectionmatrix, MovesModelMatrix * globalmodelmatrix, globalviewmatrix, shader);

                GL.Clear(ClearBufferMask.DepthBufferBit);
            }

            private void DrawScene(ref int indiceat, Matrix4 projectionmatrix, Matrix4 modelmatrix, Matrix4 viewmatrix, Shader shader)
            {
                Matrix4 Model = Matrix4.Identity;
                Matrix4 View = Matrix4.Identity;
                Matrix4 Projection = Matrix4.Identity;

                GL.UniformMatrix4(shader.GetUniform("vModel"), false, ref Model);
                GL.UniformMatrix4(shader.GetUniform("vView"), false, ref View);
                GL.UniformMatrix4(shader.GetUniform("vProjection"), false, ref Projection);

                GL.Uniform1(shader.GetUniform("vRenderMode"), (int)vRenderMode.COLOR);
                GL.Uniform1(shader.GetUniform("sRenderMode"), (int)sRenderMode.NONE);

                GL.DrawElements(BeginMode.Triangles, 6, DrawElementsType.UnsignedInt, indiceat * sizeof(uint));
                indiceat += 6;

                GL.Clear(ClearBufferMask.DepthBufferBit);

                if (HideAxis)
                {
                    GL.Uniform1(shader.GetUniform("vRenderMode"), (int)vRenderMode.INVISIBLE);
                }

                GL.UniformMatrix4(shader.GetUniform("vModel"), false, ref modelmatrix);
                GL.UniformMatrix4(shader.GetUniform("vView"), false, ref viewmatrix);
                GL.UniformMatrix4(shader.GetUniform("vProjection"), false, ref projectionmatrix);

                GL.LineWidth(1.25f);
                GL.DrawElements(BeginMode.Lines, 18, DrawElementsType.UnsignedInt, indiceat * sizeof(uint));
                indiceat += 18;

            }

            private void DrawMoves(ref int indiceat, Matrix4 projectionmatrix, Matrix4 modelmatrix, Matrix4 viewmatrix, Shader shader)
            {
                GL.UniformMatrix4(shader.GetUniform("vModel"), false, ref modelmatrix);
                GL.UniformMatrix4(shader.GetUniform("vView"), false, ref viewmatrix);
                GL.UniformMatrix4(shader.GetUniform("vProjection"), false, ref projectionmatrix);

                GL.Enable(EnableCap.DepthTest);

                // LN

                int local_indiceat = 0;

                if (OverIdCntToLn < SelIdCntToLn)
                {
                    GL.LineWidth(1.25f);
                    GL.Uniform1(shader.GetUniform("sRenderMode"), (int)sRenderMode.NONE);

                    GL.Uniform1(shader.GetUniform("vRenderMode"), (int)vRenderMode.COLOR);
                    GL.DrawElements(BeginMode.Lines, OverIdCntToLn, DrawElementsType.UnsignedInt, indiceat * sizeof(uint));
                    indiceat += OverIdCntToLn;
                    local_indiceat += OverIdCntToLn;

                    GL.LineWidth(3.25f);
                    GL.Uniform1(shader.GetUniform("vRenderMode"), (int)vRenderMode.SEL_OVER);
                    GL.DrawElements(BeginMode.Lines, OverTotIdsLn, DrawElementsType.UnsignedInt, indiceat * sizeof(uint));
                    indiceat += OverTotIdsLn;
                    local_indiceat += OverTotIdsLn;

                    GL.LineWidth(1.25f);
                    GL.Uniform1(shader.GetUniform("vRenderMode"), (int)vRenderMode.COLOR);
                    GL.DrawElements(BeginMode.Lines, SelIdCntToLn - local_indiceat, DrawElementsType.UnsignedInt, indiceat * sizeof(uint));
                    indiceat += SelIdCntToLn - local_indiceat;
                    local_indiceat = SelIdCntToLn;

                    GL.LineWidth(3.25f);
                    GL.Uniform1(shader.GetUniform("vRenderMode"), (int)vRenderMode.SEL_OVER);
                    GL.DrawElements(BeginMode.Lines, SelTotIdsLn, DrawElementsType.UnsignedInt, indiceat * sizeof(uint));
                    indiceat += SelTotIdsLn;
                    local_indiceat += SelTotIdsLn;

                    GL.LineWidth(0.5f);
                    GL.Uniform1(shader.GetUniform("vRenderMode"), (int)vRenderMode.COLOR);
                    GL.Uniform1(shader.GetUniform("sRenderMode"), (int)sRenderMode.AFTERSEL);
                    GL.DrawElements(BeginMode.Lines, SumAllIdsLn - local_indiceat, DrawElementsType.UnsignedInt, indiceat * sizeof(uint));
                    indiceat += SumAllIdsLn - local_indiceat;

                    GL.LineWidth(1.25f);
                }
                else
                {
                    GL.LineWidth(1.25f);
                    GL.Uniform1(shader.GetUniform("sRenderMode"), (int)sRenderMode.NONE);

                    GL.Uniform1(shader.GetUniform("vRenderMode"), (int)vRenderMode.COLOR);
                    GL.DrawElements(BeginMode.Lines, SelIdCntToLn, DrawElementsType.UnsignedInt, indiceat * sizeof(uint));
                    indiceat += SelIdCntToLn;
                    local_indiceat += SelIdCntToLn;

                    GL.LineWidth(3.25f);
                    GL.Uniform1(shader.GetUniform("vRenderMode"), (int)vRenderMode.SEL_OVER);
                    GL.DrawElements(BeginMode.Lines, SelTotIdsLn, DrawElementsType.UnsignedInt, indiceat * sizeof(uint));
                    indiceat += SelTotIdsLn;
                    local_indiceat += SelTotIdsLn;

                    GL.LineWidth(0.5f);
                    GL.Uniform1(shader.GetUniform("vRenderMode"), (int)vRenderMode.COLOR);
                    GL.Uniform1(shader.GetUniform("sRenderMode"), (int)sRenderMode.AFTERSEL);
                    GL.DrawElements(BeginMode.Lines, OverIdCntToLn - local_indiceat, DrawElementsType.UnsignedInt, indiceat * sizeof(uint));
                    indiceat += OverIdCntToLn - local_indiceat;
                    local_indiceat = OverIdCntToLn;

                    GL.LineWidth(3.25f);
                    GL.Uniform1(shader.GetUniform("vRenderMode"), (int)vRenderMode.SEL_OVER);
                    GL.Uniform1(shader.GetUniform("sRenderMode"), (int)sRenderMode.AFTERSEL);
                    GL.DrawElements(BeginMode.Lines, OverTotIdsLn, DrawElementsType.UnsignedInt, indiceat * sizeof(uint));
                    indiceat += OverTotIdsLn;
                    local_indiceat += OverTotIdsLn;

                    GL.LineWidth(0.5f);
                    GL.Uniform1(shader.GetUniform("vRenderMode"), (int)vRenderMode.COLOR);
                    GL.Uniform1(shader.GetUniform("sRenderMode"), (int)sRenderMode.AFTERSEL);
                    GL.DrawElements(BeginMode.Lines, SumAllIdsLn - local_indiceat, DrawElementsType.UnsignedInt, indiceat * sizeof(uint));
                    indiceat += SumAllIdsLn - local_indiceat;

                    GL.LineWidth(1.25f);
                }

                //GL.Uniform1(shader.GetUniform("sRenderMode"), (int)sRenderMode.NONE);
                //GL.Uniform1(shader.GetUniform("vRenderMode"), (int)vRenderMode.LIGHT);
                //GL.DrawElements(BeginMode.Triangles, SumAllIdsTri, DrawElementsType.UnsignedInt, indiceat * sizeof(uint));
                //indiceat += SumAllIdsTri;

                // TRI
                GL.Uniform1(shader.GetUniform("sRenderMode"), (int)sRenderMode.NONE);

                GL.Uniform1(shader.GetUniform("vRenderMode"), (int)vRenderMode.LIGHT);
                GL.DrawElements(BeginMode.Triangles, SelIdCntToTri, DrawElementsType.UnsignedInt, indiceat * sizeof(uint));
                indiceat += SelIdCntToTri;

                GL.Uniform1(shader.GetUniform("vRenderMode"), (int)vRenderMode.LIGHT);
                GL.DrawElements(BeginMode.Triangles, SelTotIdsTri, DrawElementsType.UnsignedInt, indiceat * sizeof(uint));
                indiceat += SelTotIdsTri;

                GL.Uniform1(shader.GetUniform("vRenderMode"), (int)vRenderMode.INVISIBLE);
                GL.Uniform1(shader.GetUniform("sRenderMode"), (int)sRenderMode.NONE);
                GL.DrawElements(BeginMode.Triangles, SumAllIdsTri - SelIdCntToTri - SelTotIdsTri, DrawElementsType.UnsignedInt, indiceat * sizeof(uint));
                indiceat += SumAllIdsTri - SelIdCntToTri - SelTotIdsTri;
            }

            private void DrawSelPoints(ref int indiceat, Matrix4 projectionmatrix, Matrix4 modelmatrix, Matrix4 viewmatrix, Shader shader)
            {
                Vector3 pt0 = new Vector3(0, 0, 0);
                Vector3 pt1 = new Vector3(0, 0, 0);
                Vector3 col = new Vector3(0, 0, 0);

                if (IndiceAtSel >= 0)
                {
                    pt0 = new Vector3(
                        vertdata[IndiceAtSel].X,
                        vertdata[IndiceAtSel].Y,
                        vertdata[IndiceAtSel].Z);

                    pt1 = new Vector3(
                        vertdata[LastIndiceAtSel].X,
                        vertdata[LastIndiceAtSel].Y,
                        vertdata[LastIndiceAtSel].Z);

                    col = coldata[IndiceAtSel];
                }

                GL.UniformMatrix4(shader.GetUniform("vView"), false, ref viewmatrix);
                GL.UniformMatrix4(shader.GetUniform("vProjection"), false, ref projectionmatrix);

                GL.Enable(EnableCap.DepthTest);

                GL.Uniform1(shader.GetUniform("sRenderMode"), (int)sRenderMode.NONE);

                if (IndiceAtSel >= 0)
                {
                    GL.Uniform3(shader.GetUniform("vCustomColor"), col);
                    GL.Uniform1(shader.GetUniform("vRenderMode"), (int)vRenderMode.SEL_PTS);
                }
                else
                {
                    GL.Uniform1(shader.GetUniform("vRenderMode"), (int)vRenderMode.INVISIBLE);
                }

                Matrix4 Model = Matrix4.Identity;
                Model[3, 0] = pt0.X;
                Model[3, 1] = pt0.Y;
                Model[3, 2] = pt0.Z;
                Model *= modelmatrix;
                GL.UniformMatrix4(shader.GetUniform("vModel"), false, ref Model);

                GL.DrawElements(BeginMode.Points, 1, DrawElementsType.UnsignedInt, indiceat * sizeof(uint));
                indiceat++;

                Model = Matrix4.Identity;
                Model[3, 0] = pt1.X;
                Model[3, 1] = pt1.Y;
                Model[3, 2] = pt1.Z;
                Model *= modelmatrix;
                GL.UniformMatrix4(shader.GetUniform("vModel"), false, ref Model);

                GL.DrawElements(BeginMode.Points, 1, DrawElementsType.UnsignedInt, indiceat * sizeof(uint));
                indiceat++;
            }

            private Vector3 GetVector3Color(Color color)
            {
                return new Vector3(color.R / 255f, color.G / 255f, color.B / 255f);
            }

            private void ComputeSelIds()
            {
                if (IndiceAtSel == IndiceAtOver)
                {
                    IndiceAtOver = -1;

                    OverIdCntToLn = 0;
                    OverTotIdsLn = 0;
                }

                if (IndiceAtSel < 0)
                {
                    SelIdCntToTri = SumAllIdsTri;
                    SelIdCntToLn = SumAllIdsLn;
                    SelTotIdsTri = 0;
                    SelTotIdsLn = 0;

                    return;
                }

                int _selIdCntToTri = 0;
                int _selIdCntToLn = 0;
                int _selTotIdsTri = 0;
                int _selTotIdsLn = 0;

                foreach (CAO.MoveObject obj in MoveObjList)
                {
                    if (IndiceAtSel > obj.IndiceAtLn)
                    {
                        _selIdCntToLn += obj.IndiceCountLn;
                    }
                    else if (IndiceAtSel == obj.IndiceAtLn)
                    {
                        _selTotIdsLn = obj.IndiceCountLn;
                    }
                    else
                    {
                        SelIdCntToTri = _selIdCntToTri;
                        SelIdCntToLn = _selIdCntToLn;
                        SelTotIdsTri = _selTotIdsTri;
                        SelTotIdsLn = _selTotIdsLn;

                        return;
                    }
                }

                SelIdCntToTri = _selIdCntToTri;
                SelIdCntToLn = _selIdCntToLn;
                SelTotIdsTri = _selTotIdsTri;
                SelTotIdsLn = _selTotIdsLn;
            }

            private void ComputeOverIds()
            {
                int _overIdCntToLn = 0;
                int _overTotIdsLn = 0;

                if (IndiceAtOver > 0)
                {
                    foreach (CAO.MoveObject obj in MoveObjList)
                    {
                        if (IndiceAtOver > obj.IndiceAtLn)
                        {
                            _overIdCntToLn += obj.IndiceCountLn;
                        }
                        else if (IndiceAtOver == obj.IndiceAtLn)
                        {
                            _overTotIdsLn = obj.IndiceCountLn;
                        }
                        else
                        {
                            OverIdCntToLn = _overIdCntToLn;
                            OverTotIdsLn = _overTotIdsLn;

                            return;
                        }
                    }
                }

                OverIdCntToLn = _overIdCntToLn;
                OverTotIdsLn = _overTotIdsLn;
            }
            #endregion

            #region selection

            internal void SetMoveSelection(CAO.MoveObject move)
            {
                if (move != null)
                {
                    IndiceAtSel = move.IndiceAtLn;
                    LastIndiceAtSel = move.IndiceAtLn + move.VertexCountLn - 1;
                }
                else
                {
                    IndiceAtSel = -1;
                    LastIndiceAtSel = -1;
                }
                ComputeSelIds();
            }
            #endregion
        }

        internal class Camera
        {
            #region public fields
            internal Matrix4 M = new Matrix4(1f, 0f, 0f, 0,
                                            0f, 1f, 0, 0,
                                            0, 0, -1f, 0,
                                            0, 0, 0, 1.0f);

            internal float WheelSensitivity = 0.05f;
            internal float RotSensitivity = 0.05f;
            #endregion

            #region constructors
            internal Camera()
            {
                this.M = new Matrix4(1f, 0f, 0f, 0,
                                    0f, 1f, 0, 0,
                                    0, 0, -1f, 0,
                                    0, 0, 0, 1.0f);

                this.RotSensitivity = 0.05f;
                this.WheelSensitivity = 0.05f;
            }
            #endregion

            #region methods
            internal Matrix4 SetCam(Vector3 v0, Vector3 v1, ncViewOptions opts)
            {

                Matrix4 newM = Matrix4.Identity;

                switch (opts.DefaultView)
                {
                    case ncViewOptions.CameraView.XY:
                        newM = new Matrix4(1f, 0f, 0f, 0,
                                        0f, 1f, 0, 0,
                                        0, 0, -1f, 0,
                                        0, 0, 0, 1f);
                        break;

                    case ncViewOptions.CameraView.XZ:
                        newM = new Matrix4(1f, 0, 0, 0,
                                        0, 0, -1f, 0,
                                        0, 1f, 0, 0,
                                        0, 0, 0, 1f);
                        break;

                    case ncViewOptions.CameraView.YZ:
                        newM = new Matrix4(0, 0, -1f, 0,
                                        1f, 0, 0, 0,
                                        0, 1f, 0, 0,
                                        0, 0, 0, 1f);
                        break;

                    case ncViewOptions.CameraView.xy:
                        newM = new Matrix4(1f, 0f, 0f, 0,
                                        0f, -1f, 0, 0,
                                        0, 0, 1f, 0,
                                        0, 0, 0, 1f);
                        break;

                    case ncViewOptions.CameraView.xz:
                        newM = new Matrix4(-1f, 0, 0, 0,
                                        0, 0, 1f, 0,
                                        0, 1f, 0, 0,
                                        0, 0, 0, 1f);
                        break;

                    case ncViewOptions.CameraView.yz:
                        newM = new Matrix4(0, 0, 1f, 0,
                                        -1f, 0, 0, 0,
                                        0, 1f, 0, 0,
                                        0, 0, 0, 1f);
                        break;

                    case ncViewOptions.CameraView.XYZ:
                        newM = new Matrix4(-1f / (float)Math.Sqrt(2), -1f / (float)Math.Sqrt(6), -1f / (float)Math.Sqrt(3), 0,
                                           1f / (float)Math.Sqrt(2), -1f / (float)Math.Sqrt(6), -1f / (float)Math.Sqrt(3), 0,
                                           0, 2f / (float)Math.Sqrt(6), -1f / (float)Math.Sqrt(3), 0,
                                           0, 0, 0, 1f);
                        break;

                    case ncViewOptions.CameraView.xYZ:
                        newM = new Matrix4(-1f / (float)Math.Sqrt(2), -1f / (float)Math.Sqrt(6), -1f / (float)Math.Sqrt(3), 0,
                                           1f / (float)Math.Sqrt(2), -1f / (float)Math.Sqrt(6), -1f / (float)Math.Sqrt(3), 0,
                                           0, 2f / (float)Math.Sqrt(6), -1f / (float)Math.Sqrt(3), 0,
                                           0, 0, 0, 1f);

                        newM = Matrix4.CreateFromAxisAngle(new Vector3(0f, 0f, 1f), -(float)Math.PI / 2f) * newM;
                        break;

                    case ncViewOptions.CameraView.XyZ:
                        newM = new Matrix4(-1f / (float)Math.Sqrt(2), -1f / (float)Math.Sqrt(6), -1f / (float)Math.Sqrt(3), 0,
                                           1f / (float)Math.Sqrt(2), -1f / (float)Math.Sqrt(6), -1f / (float)Math.Sqrt(3), 0,
                                           0, 2f / (float)Math.Sqrt(6), -1f / (float)Math.Sqrt(3), 0,
                                           0, 0, 0, 1f);

                        newM = Matrix4.CreateFromAxisAngle(new Vector3(0f, 0f, 1f), (float)Math.PI / 2f) * newM;
                        break;

                    case ncViewOptions.CameraView.xyZ:
                        newM = new Matrix4(-1f / (float)Math.Sqrt(2), -1f / (float)Math.Sqrt(6), -1f / (float)Math.Sqrt(3), 0,
                                           1f / (float)Math.Sqrt(2), -1f / (float)Math.Sqrt(6), -1f / (float)Math.Sqrt(3), 0,
                                           0, 2f / (float)Math.Sqrt(6), -1f / (float)Math.Sqrt(3), 0,
                                           0, 0, 0, 1f);

                        newM = Matrix4.CreateFromAxisAngle(new Vector3(0f, 0f, 1f), (float)Math.PI) * newM;
                        break;

                    case ncViewOptions.CameraView.XYz:
                        newM = new Matrix4(-1f / (float)Math.Sqrt(2), 1f / (float)Math.Sqrt(6), -1f / (float)Math.Sqrt(3), 0,
                                           1f / (float)Math.Sqrt(2), 1f / (float)Math.Sqrt(6), -1f / (float)Math.Sqrt(3), 0,
                                           0, 2f / (float)Math.Sqrt(6), 1f / (float)Math.Sqrt(3), 0,
                                           0, 0, 0, 1f);
                        break;

                    case ncViewOptions.CameraView.xYz:
                        newM = new Matrix4(-1f / (float)Math.Sqrt(2), 1f / (float)Math.Sqrt(6), -1f / (float)Math.Sqrt(3), 0,
                                           1f / (float)Math.Sqrt(2), 1f / (float)Math.Sqrt(6), -1f / (float)Math.Sqrt(3), 0,
                                           0, 2f / (float)Math.Sqrt(6), 1f / (float)Math.Sqrt(3), 0,
                                           0, 0, 0, 1f);

                        newM = Matrix4.CreateFromAxisAngle(new Vector3(0f, 0f, 1f), -(float)Math.PI / 2f) * newM;
                        break;

                    case ncViewOptions.CameraView.Xyz:
                        newM = new Matrix4(-1f / (float)Math.Sqrt(2), 1f / (float)Math.Sqrt(6), -1f / (float)Math.Sqrt(3), 0,
                                           1f / (float)Math.Sqrt(2), 1f / (float)Math.Sqrt(6), -1f / (float)Math.Sqrt(3), 0,
                                           0, 2f / (float)Math.Sqrt(6), 1f / (float)Math.Sqrt(3), 0,
                                           0, 0, 0, 1f);

                        newM = Matrix4.CreateFromAxisAngle(new Vector3(0f, 0f, 1f), (float)Math.PI / 2f) * newM;
                        break;

                    case ncViewOptions.CameraView.xyz:
                        newM = new Matrix4(-1f / (float)Math.Sqrt(2), 1f / (float)Math.Sqrt(6), -1f / (float)Math.Sqrt(3), 0,
                                           1f / (float)Math.Sqrt(2), 1f / (float)Math.Sqrt(6), -1f / (float)Math.Sqrt(3), 0,
                                           0, 2f / (float)Math.Sqrt(6), 1f / (float)Math.Sqrt(3), 0,
                                           0, 0, 0, 1f);

                        newM = Matrix4.CreateFromAxisAngle(new Vector3(0f, 0f, 1f), (float)Math.PI) * newM;
                        break;
                }

                if (opts.DefaultRoll == ncViewOptions.CameraRoll.R90)
                {
                    newM = Matrix4.CreateFromAxisAngle(new Vector3(newM.Column2), -(float)Math.PI / 2f) * newM;
                }
                else if (opts.DefaultRoll == ncViewOptions.CameraRoll.R180)
                {
                    newM = Matrix4.CreateFromAxisAngle(new Vector3(newM.Column2), (float)Math.PI) * newM;
                }
                else if (opts.DefaultRoll == ncViewOptions.CameraRoll.R270)
                {
                    newM = Matrix4.CreateFromAxisAngle(new Vector3(newM.Column2), (float)Math.PI / 2f) * newM;
                }

                float d;
                Vector3 n = new Vector3(newM[0, 2], newM[1, 2], newM[2, 2]);
                Vector3 ex = new Vector3(newM[0, 0], newM[1, 0], newM[2, 0]);
                Vector3 ey = new Vector3(newM[0, 1], newM[1, 1], newM[2, 1]);

                Vector3[] v = new Vector3[8]
                {
                    new Vector3(v0.X, v0.Y, v0.Z),
                    new Vector3(v1.X, v0.Y, v0.Z),
                    new Vector3(v0.X, v1.Y, v0.Z),
                    new Vector3(v1.X, v1.Y, v0.Z),
                    new Vector3(v0.X, v0.Y, v1.Z),
                    new Vector3(v1.X, v0.Y, v1.Z),
                    new Vector3(v0.X, v1.Y, v1.Z),
                    new Vector3(v1.X, v1.Y, v1.Z),
                };

                Vector3 min = new Vector3();
                Vector3 max = new Vector3();

                for (int i = 0; i < 8; i++)
                {
                    d = Vector3.Dot(v[i], n);
                    v[i] = v[i] - d * n;
                    v[i] = new Vector3(
                        ex.X * v[i].X + ex.Y * v[i].Y + ex.Z * v[i].Z,
                        ey.X * v[i].X + ey.Y * v[i].Y + ey.Z * v[i].Z, 0);

                    if (i == 0)
                    {
                        min = v[i];
                        max = v[i];
                    }
                    else
                    {
                        min.X = Math.Min(min.X, v[i].X);
                        min.Y = Math.Min(min.Y, v[i].Y);

                        max.X = Math.Max(max.X, v[i].X);
                        max.Y = Math.Max(max.Y, v[i].Y);
                    }
                }

                float z = Math.Max(Math.Abs(max.X - min.X), Math.Abs(max.Y - min.Y));
                if (opts.ViewSizeY > opts.ViewSizeX)
                {
                    z = Math.Max(Math.Abs(max.X - min.X) * (float)opts.ViewSizeY / (float)opts.ViewSizeX, z);
                }

                if (double.IsNaN(z) || double.IsInfinity(z))
                {
                    z = 1f;
                }
                if (z < 0.01f)
                {
                    z = 1f;
                    min = new Vector3(0, 0, 0);
                    max = new Vector3(0, 0, 0);
                }

                newM[3, 3] = z / 2.0f * 1.1f;

                Vector4 min4 = new Vector4(min);
                Vector4 max4 = new Vector4(max);
                Matrix4 viewInv = Matrix4.Invert(newM);
                Vector4.Transform(ref min4, ref viewInv, out min4);
                Vector4.Transform(ref max4, ref viewInv, out max4);

                newM[3, 0] = -((min4.X + max4.X) / 2f);
                newM[3, 1] = -((min4.Y + max4.Y) / 2f);
                newM[3, 2] = -((min4.Z + max4.Z) / 2f);

                return newM;
            }

            internal void Rot(float dx, float dy)
            {
                Vector3 R = new Vector3(M[0, 0], M[1, 0], M[2, 0]);
                Vector3 U = new Vector3(M[0, 1], M[1, 1], M[2, 1]);
                Vector3 V = new Vector3(M[0, 2], M[1, 2], M[2, 2]);

                Vector3 newV = V + (-dx * R + dy * U);
                newV.NormalizeFast();
                Vector3 A = Vector3.Cross(V, newV);
                A.NormalizeFast();

                double alpha = Math.Acos(Vector3.Dot(V, newV)) * RotSensitivity;

                A.X = Convert.ToSingle(A.X * Math.Sin(0.5 * alpha));
                A.Y = Convert.ToSingle(A.Y * Math.Sin(0.5 * alpha));
                A.Z = Convert.ToSingle(A.Z * Math.Sin(0.5 * alpha));
                float w = Convert.ToSingle(Math.Cos(0.5 * alpha));

                Matrix4 T = new Matrix4(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

                T[0, 0] = w * w + A.X * A.X - A.Y * A.Y - A.Z * A.Z;
                T[1, 1] = w * w - A.X * A.X + A.Y * A.Y - A.Z * A.Z;
                T[2, 2] = w * w - A.X * A.X - A.Y * A.Y + A.Z * A.Z;
                T[3, 3] = w * w + A.X * A.X + A.Y * A.Y + A.Z * A.Z;

                T[0, 1] = 2 * A.X * A.Y + 2 * w * A.Z;
                T[0, 2] = 2 * A.X * A.Z - 2 * w * A.Y;

                T[1, 0] = 2 * A.X * A.Y - 2 * w * A.Z;
                T[1, 2] = 2 * A.Y * A.Z + 2 * w * A.X;

                T[2, 0] = 2 * A.X * A.Z + 2 * w * A.Y;
                T[2, 1] = 2 * A.Y * A.Z - 2 * w * A.X;

                M = Matrix4.Mult(T, M);
            }

            internal void Zoom_Wheel(float d)
            {
                M[3, 3] += d * WheelSensitivity;

                if (M[3, 3] < 1E-2f)
                {
                    M[3, 3] = 1E-2f;
                }
                if (M[3, 3] > 1E10f)
                {
                    M[3, 3] = 1E10f;
                }
            }

            internal void Zoom_Mouse(float d)
            {
                M[3, 3] = d;
                if (M[3, 3] < 1E-2f)
                {
                    M[3, 3] = 1E-2f;
                }
                if (M[3, 3] > 1E10f)
                {
                    M[3, 3] = 1E10f;
                }
            }

            internal Matrix4 GetViewMatrix()
            {
                WheelSensitivity = M[3, 3] / 500.0f;
                return M;
            }
            #endregion
        }

        internal class Shader
        {
            #region public fields
            internal int ProgramID = -1;
            internal int VShaderID = -1;
            internal int FShaderID = -1;
            internal int AttributeCount = 0;
            internal int UniformCount = 0;

            internal Dictionary<String, AttributeInfo> Attributes = new Dictionary<string, AttributeInfo>();
            internal Dictionary<String, UniformInfo> Uniforms = new Dictionary<string, UniformInfo>();
            internal Dictionary<String, uint> Buffers = new Dictionary<string, uint>();
            #endregion

            #region constructors
            internal Shader()
            {
                ProgramID = GL.CreateProgram();

                LoadShaderFromStr(ShaderType.VertexShader);
                LoadShaderFromStr(ShaderType.FragmentShader);

                Link();
                GenBuffers();
            }
            #endregion

            #region methods
            internal void loadShader(String code, ShaderType type, out int address)
            {
                address = GL.CreateShader(type);
                GL.ShaderSource(address, code);
                GL.CompileShader(address);
                GL.AttachShader(ProgramID, address);
            }

            internal void LoadShaderFromStr(ShaderType type)
            {
                if (type == ShaderType.VertexShader)
                {
                    loadShader(VSFS.vs, type, out VShaderID);
                }
                else if (type == ShaderType.FragmentShader)
                {
                    loadShader(VSFS.fs, type, out FShaderID);
                }
            }

            internal void Link()
            {
                GL.LinkProgram(ProgramID);

                GL.GetProgram(ProgramID, ProgramParameter.ActiveAttributes, out AttributeCount);
                GL.GetProgram(ProgramID, ProgramParameter.ActiveUniforms, out UniformCount);

                for (int i = 0; i < AttributeCount; i++)
                {
                    AttributeInfo info = new AttributeInfo();
                    int length = 0;

                    string name = string.Empty;

                    GL.GetActiveAttrib(ProgramID, i, 256, out length, out info.size, out info.type, out name);

                    info.name = name;
                    info.address = GL.GetAttribLocation(ProgramID, info.name);
                    Attributes.Add(name.ToString(), info);
                }

                for (int i = 0; i < UniformCount; i++)
                {
                    UniformInfo info = new UniformInfo();
                    int length = 0;

                    string name = string.Empty;

                    GL.GetActiveUniform(ProgramID, i, 256, out length, out info.size, out info.type, out name);

                    info.name = name;
                    Uniforms.Add(name.ToString(), info);
                    info.address = GL.GetUniformLocation(ProgramID, info.name);
                }
            }

            public void GenBuffers()
            {
                for (int i = 0; i < Attributes.Count; i++)
                {
                    uint buffer = 0;
                    GL.GenBuffers(1, out buffer);

                    Buffers.Add(Attributes.Values.ElementAt(i).name, buffer);
                }

                for (int i = 0; i < Uniforms.Count; i++)
                {
                    uint buffer = 0;
                    GL.GenBuffers(1, out buffer);

                    Buffers.Add(Uniforms.Values.ElementAt(i).name, buffer);
                }
            }

            internal void EnableVertexAttribArrays()
            {
                for (int i = 0; i < Attributes.Count; i++)
                {
                    GL.EnableVertexAttribArray(Attributes.Values.ElementAt(i).address);
                }
            }

            internal void DisableVertexAttribArrays()
            {
                for (int i = 0; i < Attributes.Count; i++)
                {
                    GL.DisableVertexAttribArray(Attributes.Values.ElementAt(i).address);
                }
            }

            internal int GetAttribute(string name)
            {
                if (Attributes.ContainsKey(name))
                {
                    return Attributes[name].address;
                }
                else
                {
                    return -1;
                }
            }

            internal int GetUniform(string name)
            {
                if (Uniforms.ContainsKey(name))
                {
                    return Uniforms[name].address;
                }
                else
                {
                    return -1;
                }
            }

            internal uint GetBuffer(string name)
            {
                if (Buffers.ContainsKey(name))
                {
                    return Buffers[name];
                }
                else
                {
                    return 0;
                }
            }
            #endregion

            internal class AttributeInfo
            {
                public String name = "";
                public int address = -1;
                public int size = 0;
                public ActiveAttribType type;
            }

            internal class UniformInfo
            {
                public String name = "";
                public int address = -1;
                public int size = 0;
                public ActiveUniformType type;
            }
        }

        private class MoveSelection
        {
            #region fields
            internal CAO.MoveObject move;
            internal string moveguid;
            internal int moveid;
            private Vector3 point;
            private float ratio;
            #endregion

            #region constructors
            private MoveSelection()
            {
                move = null;
                moveguid = string.Empty;
                moveid = -1;
                point = new Vector3();
                ratio = 0f;
            }
            internal MoveSelection(CAO.MoveObject m, int id, Vector3 pt, float r)
            {
                move = m;
                moveid = id;
                point = pt;
                ratio = r;

                if (m != null)
                {
                    moveguid = move.MoveGuid;
                }
                else
                {
                    moveguid = String.Empty;
                }
            }
            #endregion
        }
    }

    public class ncViewOptions
    {
        #region enum
        public enum CameraView
        {
            XY, YZ, XZ,
            xy, yz, xz,
            XYZ, xYZ, XyZ, XYz, xyZ, xYz, Xyz, xyz
        }
        public enum CameraRoll { R0, R90, R180, R270 }
        #endregion

        #region public Fields
        public int ViewSizeX = 0;
        public int ViewSizeY = 0;

        public CameraView DefaultView = CameraView.XY;
        public CameraRoll DefaultRoll = CameraRoll.R0;

        public double RefreshTime = 30.0;

        public int ArcSectors = 64;

        public bool HideRapids = false;
        public bool HideAxis = false;

        [XmlElement(Type = typeof(XmlColor))]
        public Color BackGroundColorBot = Color.FromArgb(20, 20, 20);
        [XmlElement(Type = typeof(XmlColor))]
        public Color BackGroundColorTop = Color.FromArgb(100, 100, 100);
        [XmlElement(Type = typeof(XmlColor))]
        public Color RapidColor = Color.FromArgb(255, 255, 255);

        [XmlElement(Type = typeof(XmlColor))]
        public Color XAxisColor = Color.FromArgb(255, 0, 0);
        [XmlElement(Type = typeof(XmlColor))]
        public Color YAxisColor = Color.FromArgb(0, 255, 0);
        [XmlElement(Type = typeof(XmlColor))]
        public Color ZAxisColor = Color.FromArgb(0, 0, 255);

        [XmlElement(Type = typeof(XmlColor))]
        public Color FeedColor0 = Color.FromArgb(0, 255, 255);
        [XmlElement(Type = typeof(XmlColor))]
        public Color FeedColor1 = Color.FromArgb(255, 255, 0);
        [XmlElement(Type = typeof(XmlColor))]
        public Color FeedColor2 = Color.FromArgb(255, 0, 255);
        [XmlElement(Type = typeof(XmlColor))]
        public Color FeedColor3 = Color.FromArgb(255, 128, 0);
        [XmlElement(Type = typeof(XmlColor))]
        public Color FeedColor4 = Color.FromArgb(128, 255, 0);
        [XmlElement(Type = typeof(XmlColor))]
        public Color FeedColor5 = Color.FromArgb(0, 128, 255);
        [XmlElement(Type = typeof(XmlColor))]
        public Color FeedColor6 = Color.FromArgb(0, 255, 128);
        [XmlElement(Type = typeof(XmlColor))]
        public Color FeedColor7 = Color.FromArgb(0, 255, 0);
        [XmlElement(Type = typeof(XmlColor))]
        public Color FeedColor8 = Color.FromArgb(255, 0, 0);
        [XmlElement(Type = typeof(XmlColor))]
        public Color FeedColor9 = Color.FromArgb(0, 0, 255);
        #endregion

        #region methods
        public ncViewOptions Clone()
        {
            ncViewOptions NewViewOptions = (ncViewOptions)this.MemberwiseClone();
            return NewViewOptions;
        }
        #endregion
    }
}
