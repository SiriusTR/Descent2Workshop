﻿/*
    Copyright (c) 2019 SaladBadger

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibDescent.Data;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Descent2Workshop
{
    public class ModelRenderer
    {
        private int frame = -1;
        private Polymodel model;
        private PIGFile piggyFile;
        private HAMFile dataFile;
        private List<int> textureList;
        private ModelTextureManager texMan = new ModelTextureManager();
        private bool wireframe = false;
        private bool showRadius = false;
        private bool showBBs = false;
        private bool showNormals = false;
        private bool emulateSoftware = false;
        private double pitch = 0;
        private double angle = 0;

        private FixVector lightVector = new FixVector(0.707f, 0.707f, 0.0f);

        private FixVector[] interpPoints = new FixVector[1000];
        private FixVector cameraPoint = new FixVector(0.0, 0.0, 0.0);
        private FixVector cameraFacing = new FixVector(0.0, 0.0, 1.0);

        private FixVector currentOffset;

        public bool Wireframe { get => wireframe; set => wireframe = value; }
        public bool ShowRadius { get => showRadius; set => showRadius = value; }
        public bool ShowBBs { get => showBBs; set => showBBs = value; }
        public bool ShowNormals { get => showNormals; set => showNormals = value; }
        public double Pitch { get => pitch; set => pitch = value; }
        public double Angle { get => angle; set => angle = value; }
        public int Frame { get => frame; set => frame = value; }
        public bool EmulateSoftware { get => emulateSoftware; set => emulateSoftware = value; }

        public ModelRenderer(PIGFile piggyFile)
        {
            this.piggyFile = piggyFile;
        }

        public ModelRenderer(HAMFile dataFile, PIGFile piggyFile)
        {
            this.piggyFile = piggyFile;
            this.dataFile = dataFile;
        }

        public void SetModel(Polymodel model)
        {
            if (textureList != null)
                texMan.FreeTextureList(textureList);
            this.model = model;
            frame = -1;
            if (dataFile == null)
                textureList = texMan.LoadPolymodelTextures(model, piggyFile);
            else
                textureList = texMan.LoadPolymodelTextures(model, piggyFile, dataFile);
        }

        public void Init()
        {
            GL.Enable(EnableCap.Texture2D);
            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }

        public void SetupViewport(int width, int height, double scale)
        {
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Viewport(0, 0, width, height);
            GL.Ortho(-scale, scale, -scale, scale, -32, 32);
        }

        public void Draw()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            if (model.data.InterpreterData.Length == 0) return;
            GL.CullFace(CullFaceMode.Front);
            if (wireframe)
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            else
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.Disable(EnableCap.Texture2D);
            if (emulateSoftware)
                GL.Disable(EnableCap.DepthTest);
            else
                GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);

            GL.Scale(-1.0, 1.0, 1.0);
            GL.Rotate(pitch, 1.0, 0.0, 0.0);
            GL.Rotate(angle, 0.0, 1.0, 0.0);

            double angler = angle * Math.PI / 180.0;
            double pitchr = pitch * Math.PI / 180.0;

            cameraPoint = new FixVector(-32.0 * Math.Sin(angler) * Math.Cos(pitchr), 32.0 * Math.Sin(pitchr) , 32.0 * Math.Cos(angler) * Math.Cos(pitchr));

            lightVector.x = -.707 * Math.Sin((Math.PI * 2)-angler + (Math.PI / 4));
            lightVector.y = .707;
            lightVector.z = .707 * Math.Cos((Math.PI * 2) - angler + (Math.PI / 4));

            //Console.WriteLine("x: {0} y: {1} z: {2} angle: {3} pitch: {4}", cameraPoint.x, cameraPoint.y, cameraPoint.z, angle, pitch);

            Execute(model.data.InterpreterData, 0, model, model.submodels[0]);
            //DrawSubobject(model, model.submodels[0]);

            if (ShowRadius)
            {
                GL.LoadIdentity();
                GL.Scale(-1.0, 1.0, 1.0);
                GL.Disable(EnableCap.DepthTest);
                GL.Disable(EnableCap.Texture2D);

                //Draw the radius
                double radius = (double)model.rad;
                double minx = (double)model.mins.x;
                double miny = (double)model.mins.y;
                double minz = (double)model.mins.z;
                double maxx = (double)model.maxs.x;
                double maxy = (double)model.maxs.y;
                double maxz = (double)model.maxs.z;
                GL.Begin(PrimitiveType.LineLoop);
                GL.Color3(0.0, 0.0, 1.0);
                double radx, rady, radang;
                for (int i = 0; i < 32; i++)
                {
                    radang = (i / 16.0) * (Math.PI);
                    radx = Math.Cos(radang);
                    rady = Math.Sin(radang);
                    GL.Vertex3(radx * radius, rady * radius, 0);
                }
                GL.End();

                //Draw the bounding box
                GL.Rotate(pitch, 1.0, 0.0, 0.0);
                GL.Rotate(angle, 0.0, 1.0, 0.0);
                GL.Begin(PrimitiveType.LineLoop);
                GL.Color3(1.0, 0.0, 0.0);

                GL.Vertex3(minx, miny, maxz);
                GL.Vertex3(minx, maxy, maxz);
                GL.Vertex3(maxx, maxy, maxz);
                GL.Vertex3(maxx, miny, maxz);

                GL.End();

                GL.Begin(PrimitiveType.LineLoop);
                GL.Color3(1.0, 0.0, 0.0);

                GL.Vertex3(minx, miny, minz);
                GL.Vertex3(minx, maxy, minz);
                GL.Vertex3(maxx, maxy, minz);
                GL.Vertex3(maxx, miny, minz);

                GL.End();

                GL.Begin(PrimitiveType.Lines);
                GL.Color3(1.0, 0.0, 0.0);

                GL.Vertex3(minx, miny, minz);
                GL.Vertex3(minx, miny, maxz);
                GL.Vertex3(minx, maxy, minz);
                GL.Vertex3(minx, maxy, maxz);
                GL.Vertex3(maxx, maxy, minz);
                GL.Vertex3(maxx, maxy, maxz);
                GL.Vertex3(maxx, miny, minz);
                GL.Vertex3(maxx, miny, maxz);

                GL.End();
            }
        }

        private bool CheckNormalFacing(FixVector p, FixVector norm)
        {
            FixVector t = cameraPoint - p;
            Fix dot = t.Dot(norm);
            return dot > 0;
        }

        private short GetShort(byte[] data, ref int offset)
        {
            short res = (short)(data[offset] + (data[offset + 1] << 8));
            offset += 2;
            return res;
        }

        private int GetInt(byte[] data, ref int offset)
        {
            int res = data[offset] + (data[offset + 1] << 8) + (data[offset + 2] << 16) + (data[offset + 3] << 24);
            offset += 4;
            return res;
        }

        private FixVector GetFixVector(byte[] data, ref int offset)
        {
            FixVector res;
            res.x = new Fix(GetInt(data, ref offset));
            res.y = new Fix(GetInt(data, ref offset));
            res.z = new Fix(GetInt(data, ref offset));
            return res;
        }

        private void Execute(byte[] data, int offset, Polymodel mainModel, Submodel model)
        {
            short instruction = GetShort(data, ref offset);
            while (true)
            {
                switch (instruction)
                {
                    case 0: //END
                        if (showBBs)
                        {
                            GL.Disable(EnableCap.Texture2D);
                            GL.Disable(EnableCap.DepthTest);

                            //Draw the radius
                            double radius = model.Radius;
                            double minx = model.Mins.x;
                            double miny = model.Mins.y;
                            double minz = model.Mins.z;
                            double maxx = model.Maxs.x;
                            double maxy = model.Maxs.y;
                            double maxz = model.Maxs.z;
                            //Follow the chain
                            GL.PushMatrix();
                            GL.Rotate(-angle, 0.0, 1.0, 0.0);
                            GL.Rotate(-pitch, 1.0, 0.0, 0.0);
                            GL.Begin(PrimitiveType.LineLoop);
                            GL.Color3(0.0, 0.0, 1.0);
                            double radx, rady, radang;
                            for (int i = 0; i < 32; i++)
                            {
                                radang = (i / 16.0) * (Math.PI);
                                radx = Math.Cos(radang);
                                rady = Math.Sin(radang);
                                GL.Vertex3(radx * radius, rady * radius, 0);
                            }
                            GL.End();
                            GL.PopMatrix();

                            //Draw the bounding box
                            GL.Begin(PrimitiveType.LineLoop);
                            GL.Color3(1.0, 0.0, 0.0);

                            GL.Vertex3(minx, miny, maxz);
                            GL.Vertex3(minx, maxy, maxz);
                            GL.Vertex3(maxx, maxy, maxz);
                            GL.Vertex3(maxx, miny, maxz);

                            GL.End();

                            GL.Begin(PrimitiveType.LineLoop);
                            GL.Color3(1.0, 0.0, 0.0);

                            GL.Vertex3(minx, miny, minz);
                            GL.Vertex3(minx, maxy, minz);
                            GL.Vertex3(maxx, maxy, minz);
                            GL.Vertex3(maxx, miny, minz);

                            GL.End();

                            GL.Begin(PrimitiveType.Lines);
                            GL.Color3(1.0, 0.0, 0.0);

                            GL.Vertex3(minx, miny, minz);
                            GL.Vertex3(minx, miny, maxz);
                            GL.Vertex3(minx, maxy, minz);
                            GL.Vertex3(minx, maxy, maxz);
                            GL.Vertex3(maxx, maxy, minz);
                            GL.Vertex3(maxx, maxy, maxz);
                            GL.Vertex3(maxx, miny, minz);
                            GL.Vertex3(maxx, miny, maxz);

                            GL.End();

                            if (!emulateSoftware)
                                GL.Enable(EnableCap.DepthTest);
                        }

                        return;
                    case 1: //POINTS
                        {
                            short pointc = GetShort(data, ref offset);
                            for (int i = 0; i < pointc; i++)
                            {
                                interpPoints[i] = GetFixVector(data, ref offset);
                            }
                        }
                        break;
                    case 2: //FLATPOLY
                        {
                            short pointc = GetShort(data, ref offset);
                            FixVector point = GetFixVector(data, ref offset);
                            FixVector normal = GetFixVector(data, ref offset);
                            short color = GetShort(data, ref offset);

                            short[] points = new short[pointc]; //TODO: seems wasteful to do all these allocations?
                            for (int i = 0; i < pointc; i++)
                            {
                                points[i] = GetShort(data, ref offset);
                            }
                            if (pointc % 2 == 0)
                                GetShort(data, ref offset);

                            //Draw
                            GL.Disable(EnableCap.Texture2D); //TODO: too many state changes
                            int cr = ((color >> 10) & 31) * 255 / 31;
                            int cg = ((color >> 5) & 31) * 255 / 31;
                            int cb = (color & 31) * 255 / 31;

                            GL.Begin(PrimitiveType.TriangleFan);
                            for (int i = 0; i < pointc; i++)
                            {
                                double vx = interpPoints[points[i]].x;
                                double vy = interpPoints[points[i]].y;
                                double vz = interpPoints[points[i]].z;

                                GL.Color3(cr / 255.0f, cg / 255.0f, cb / 255.0f);
                                GL.Vertex3(vx, vy, vz);
                            }
                            GL.End();

                            if (showNormals)
                            {
                                normal = normal.Scale(.25 * mainModel.rad);
                                GL.Color3(1.0f, 1.0f, 1.0f);
                                GL.Begin(PrimitiveType.Lines);
                                {
                                    double vx = point.x;
                                    double vy = point.y;
                                    double vz = point.z;

                                    double dx = point.x + normal.x;
                                    double dy = point.y + normal.y;
                                    double dz = point.z + normal.z;

                                    GL.Vertex3(vx, vy, vz);
                                    GL.Vertex3(dx, dy, dz);
                                }
                                GL.End();
                            }
                        }
                        break;
                    case 3: //TMAPPOLY
                        {
                            short pointc = GetShort(data, ref offset);
                            FixVector point = GetFixVector(data, ref offset);
                            FixVector normal = GetFixVector(data, ref offset);
                            short texture = GetShort(data, ref offset);
                            Fix shade = normal.Dot(lightVector) * .25 + .75;

                            short[] points = new short[pointc]; //TODO: seems wasteful to do all these allocations?
                            FixVector[] uvls = new FixVector[pointc];
                            for (int i = 0; i < pointc; i++)
                            {
                                points[i] = GetShort(data, ref offset);
                            }
                            if (pointc % 2 == 0)
                                GetShort(data, ref offset);

                            for (int i = 0; i < pointc; i++)
                            {
                                uvls[i] = GetFixVector(data, ref offset);
                            }

                            //Draw
                            GL.Enable(EnableCap.Texture2D); //TODO: too many state changes
                            GL.BindTexture(TextureTarget.Texture2D, textureList[texture]);
                            GL.Begin(PrimitiveType.TriangleFan);
                            for (int i = 0; i < pointc; i++)
                            {
                                double vx = interpPoints[points[i]].x;
                                double vy = interpPoints[points[i]].y;
                                double vz = interpPoints[points[i]].z;

                                double uvx = uvls[i].x;
                                double uvy = uvls[i].y;

                                GL.Color3(shade, shade, shade);
                                GL.TexCoord2(uvx, uvy);
                                GL.Vertex3(vx, vy, vz);
                            }
                            GL.End();

                            if (showNormals)
                            {
                                GL.Disable(EnableCap.Texture2D); //TODO: waaaay too many state changes
                                normal = normal.Scale(.25 * mainModel.rad);
                                GL.Color3(1.0f, 1.0f, 1.0f);
                                GL.Begin(PrimitiveType.Lines);
                                {
                                    double vx = point.x;
                                    double vy = point.y;
                                    double vz = point.z;

                                    double dx = point.x + normal.x;
                                    double dy = point.y + normal.y;
                                    double dz = point.z + normal.z;

                                    GL.Vertex3(vx, vy, vz);
                                    GL.Vertex3(dx, dy, dz);
                                }
                                GL.End();
                            }
                        }
                        break;
                    case 4: //SORTNORM
                        {
                            int baseOffset = offset - 2;
                            int n_points = GetShort(data, ref offset);
                            FixVector norm = GetFixVector(data, ref offset);
                            FixVector point = GetFixVector(data, ref offset);
                            short backOffset = GetShort(data, ref offset);
                            short frontOffset = GetShort(data, ref offset);

                            if (CheckNormalFacing(point, norm))
                            {
                                Execute(data, baseOffset + frontOffset, mainModel, model);
                                Execute(data, baseOffset + backOffset, mainModel, model);
                            }
                            else
                            {
                                Execute(data, baseOffset + backOffset, mainModel, model);
                                Execute(data, baseOffset + frontOffset, mainModel, model);
                            }
                        }
                        break;
                    case 5: //RODBM
                        {
                            offset += 34;
                        }
                        break;
                    case 6: //SUBCALL
                        {
                            int baseOffset = offset - 2;
                            short submodelNum = GetShort(data, ref offset);
                            FixVector submodelOffset = GetFixVector(data, ref offset);
                            short modelOffset = GetShort(data, ref offset);
                            offset += 2;

                            Submodel newModel = mainModel.submodels[submodelNum];
                            GL.PushMatrix();
                            GL.Translate(submodelOffset.x, submodelOffset.y, submodelOffset.z);
                            if (frame >= 0)
                            {
                                GL.Rotate((mainModel.animationMatrix[submodelNum, frame].b / 16384.0f) * 90.0, 0.0, 0.0, 1.0);
                                GL.Rotate((mainModel.animationMatrix[submodelNum, frame].h / 16384.0f) * 90.0, 0.0, 1.0, 0.0);
                                GL.Rotate((mainModel.animationMatrix[submodelNum, frame].p / 16384.0f) * 90.0, 1.0, 0.0, 0.0);
                            }
                            Execute(data, baseOffset + modelOffset, mainModel, newModel);
                            GL.PopMatrix();
                        }
                        break;
                    case 7: //DEFPSTART
                        {
                            short pointc = GetShort(data, ref offset);
                            short firstPoint = GetShort(data, ref offset);
                            offset += 2;
                            for (int i = 0; i < pointc; i++)
                            {
                                interpPoints[i + firstPoint] = GetFixVector(data, ref offset);
                            }
                        }
                        break;
                    case 8:
                        offset += 2;
                        break;
                    default:
                        throw new Exception(string.Format("Unknown interpreter instruction {0} at offset {1}\n", instruction, offset));
                }
                instruction = GetShort(data, ref offset);
            }
        }
    }
}
