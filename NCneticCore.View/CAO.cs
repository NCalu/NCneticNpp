using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using NCneticCore;

namespace NCneticCore.View
{
    public class CAO
    {
        internal class MoveObject
        {
            #region fields
            internal int IndiceCountLn = 0;
            internal int VertexCountLn = 0;
            internal int IndiceAtLn = 0;

            internal Vector3 Color;

            internal List<int> IndicesLn = new List<int>();
            internal List<Vector3> VertexLn = new List<Vector3>();

            internal string MoveGuid;

            #endregion

            #region constructors
            internal MoveObject(string guid)
            {
                MoveGuid = guid;
            }

            internal MoveObject(List<ncMove> movelist, Color col)
            {
                if (movelist.Any())
                {
                    Color = new Vector3(col.R / 255f, col.G / 255f, col.B / 255f);
                    MoveGuid = movelist.First().MoveGuid;
                    AddLn(movelist);
                }
            }
            #endregion

            #region methods
            private void AddLn(List<ncMove> movelist)
            {
                VertexLn.Add(new Vector3(
                    (float)(movelist.First().P0.X),
                    (float)(movelist.First().P0.Y),
                    (float)movelist.First().P0.Z));

                VertexCountLn++;

                for (int i = 0; i < movelist.Count(); i++)
                {
                    VertexLn.Add(new Vector3(
                        (float)(movelist[i].P.X),
                        (float)(movelist[i].P.Y),
                        (float)(movelist[i].P.Z)));

                    VertexCountLn++;

                    IndicesLn.Add(i);
                    IndicesLn.Add(i + 1);
                    IndiceCountLn += 2;
                }
            }

            internal Vector3 GetMin()
            {
                Vector3 min = new Vector3(1E9f, 1E9f, 1E9f);
                Vector3[] vArray;
                int[] ids;

                vArray = VertexLn.ToArray();
                ids = GetIndicesLn().ToArray();
                for (int i = 0; i < IndiceCountLn; i++)
                {
                    Vector4 vpos = new Vector4(vArray[ids[i]], 1f);
                    min.X = Math.Min(vpos.X, min.X);
                    min.Y = Math.Min(vpos.Y, min.Y);
                    min.Z = Math.Min(vpos.Z, min.Z);
                }

                return min;
            }

            internal Vector3 GetMax()
            {
                Vector3 max = new Vector3(-1E9f, -1E9f, -1E9f);
                Vector3[] vArray;
                int[] ids;

                vArray = VertexLn.ToArray();
                ids = GetIndicesLn().ToArray();
                for (int i = 0; i < IndiceCountLn; i++)
                {
                    Vector4 vpos = new Vector4(vArray[ids[i]], 1f);
                    max.X = Math.Max(vpos.X, max.X);
                    max.Y = Math.Max(vpos.Y, max.Y);
                    max.Z = Math.Max(vpos.Z, max.Z);
                }

                return max;
            }

            internal int[] GetIndicesLn(int offset = 0)
            {
                if (IndiceCountLn < 1)
                {
                    return new int[0];
                }
                else
                {
                    int[] ids = new int[IndiceCountLn];
                    for (int i = 0; i < IndiceCountLn; i++)
                    {
                        ids[i] = IndicesLn[i] + offset;
                    }
                    return ids;
                }
            }
            #endregion
        }

        internal class Animation
        {
            #region properties
            internal int CurStep { get; set; }
            internal int TotStep { get; set; }
            internal Matrix4 StartMatrix { get; set; }
            internal Matrix4 EndMatrix { get; set; }
            #endregion

            #region constructor
            internal Animation(int nt, Matrix4 m0, Matrix4 m1)
            {
                this.CurStep = 0;
                this.TotStep = nt;
                this.StartMatrix = m0;
                this.EndMatrix = m1;
            }
            #endregion
        }
    }
}
