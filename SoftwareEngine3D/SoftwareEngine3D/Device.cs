﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using SharpDX;
using System.Threading;

namespace SoftwareEngine3D
{
    public class Device
    {
        private byte[] backBuffer;
        private object[] lockBuffer;
        private readonly float[] depthBuffer;
        private WriteableBitmap bmp;
        private readonly int renderWidth;
        private readonly int renderHeight;
        private readonly SynchronizationContext uiContext;

        public Device(WriteableBitmap bmp, SynchronizationContext uiContext)
        {
            this.bmp = bmp;
            this.uiContext = uiContext;
            renderWidth = bmp.PixelWidth;
            renderHeight = bmp.PixelHeight;

            // The back buffer size is equal to the number of pixels to draw
            // on screen (width * height) * 4 (R, G, B & Alpha values).
            backBuffer = new byte[renderWidth * renderHeight * 4];
            depthBuffer = new float[renderWidth * renderHeight];
            lockBuffer = new object[renderWidth * renderHeight];
            for (var i = 0; i < lockBuffer.Length; i++)
            {
                lockBuffer[i] = new object();
            }
        }

        /// <summary>
        /// Clear the back buffer with a specific color.
        /// </summary>
        /// <param name="r">Red value</param>
        /// <param name="g">Green value</param>
        /// <param name="b">Blue value</param>
        /// <param name="a">Alpha value</param>
        public void Clear(byte r, byte g, byte b, byte a)
        {
            for (var index = 0; index < backBuffer.Length; index += 4)
            {
                // BGRA is used by Windows
                backBuffer[index] = b;
                backBuffer[index + 1] = g;
                backBuffer[index + 2] = r;
                backBuffer[index + 3] = a;
            }

            // Clearing Depth Buffer
            for (var index = 0; index < depthBuffer.Length; index++)
            {
                depthBuffer[index] = float.MaxValue;
            }
        }

        /// <summary>
        /// Flush the back buffer into the front buffer.
        /// </summary>
        public void Present()
        {
            bmp.Lock();

            unsafe
            {
                byte* pDst = (byte*)bmp.BackBuffer;
                int len = backBuffer.Length;
                fixed (byte* pSrc = backBuffer)
                {
                    byte* ps = pSrc;
                    byte* pd = pDst;

                    for (int i = 0; i < len / 4; i++)
                    {
                        *((int*)pd) = *((int*)ps);
                        pd += 4;
                        ps += 4;
                    }

                    for (int i = 0; i < len % 4; i++)
                    {
                        *pd = *ps;
                        pd++;
                        ps++;
                    }
                }
            }

            bmp.AddDirtyRect(new Int32Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight));
            bmp.Unlock();
        }

        /// <summary>
        /// Put a pixel on screen at a speccific X,Y coordinates.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color"></param>
        public void PutPixel(int x, int y, Color4 color)
        {
            var index = (x + y * bmp.PixelWidth) * 4;

            lock (lockBuffer[index])
            {
                backBuffer[index] = (byte)(color.Blue * 255);
                backBuffer[index + 1] = (byte)(color.Green * 255);
                backBuffer[index + 2] = (byte)(color.Red * 255);
                backBuffer[index + 3] = (byte)(color.Alpha * 255);
            }
        }

        public void PutPixel(int x, int y, float z, Color4 color)
        {
            // As we have a 1-D Array for our back buffer
            // we need to know the equivalent cell in 1-D based
            // on the 2D coordinates on screen
            var index = (x + y * bmp.PixelWidth);
            var index4 = index * 4;

            lock (lockBuffer[index])
            {
                if (depthBuffer[index] < z)
                {
                    return; // Discard
                }

                depthBuffer[index] = z;

                backBuffer[index4] = (byte)(color.Blue * 255);
                backBuffer[index4 + 1] = (byte)(color.Green * 255);
                backBuffer[index4 + 2] = (byte)(color.Red * 255);
                backBuffer[index4 + 3] = (byte)(color.Alpha * 255);
            }
        }

        public void DrawPoint(Vector2 point)
        {
            DrawPoint(point, new Color4(1.0f, 1.0f, 1.0f, 1.0f));
        }

        /// <summary>
        /// Transform 3D coordinates in 2D coordinates using the transformation matrix.
        /// It also transform the same coordinates and the normal to the vertex
        /// in the 3D world.
        /// </summary>
        /// <param name="coord"></param>
        /// <param name="transMat"></param>
        /// <returns></returns>
        public Vertex Project(Vertex vertex, Matrix transMat, Matrix world)
        {
            // Transforming the coordinates into 2D space
            var point2d = Vector3.TransformCoordinate(vertex.Coordinates, transMat);
            // Transforming the coordinates & the normal to the vertex in the 3D world
            var point3dWorld = Vector3.TransformCoordinate(vertex.Coordinates, world);
            var normal3dWorld = Vector3.TransformCoordinate(vertex.Normal, world);
            
            // The transformed coordinates will be based on coordinate system
            // starting on the center of the screen. But drawing on screen normally starts
            // from top left. We then need to transform them again to have x:0, y:0 on top left.
            var x = point2d.X * renderWidth + renderWidth / 2.0f;
            var y = -point2d.Y * renderHeight + renderHeight / 2.0f;

            return new Vertex
            {
                Coordinates = new Vector3(x, y, point2d.Z),
                Normal = normal3dWorld,
                WorldCoordinates = point3dWorld
            };
        }

        /// <summary>
        /// Invoke PutPixel method but does the clipping operation before.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="color"></param>
        public void DrawPoint(Vector2 point, Color4 color)
        {
            // Clipping what's visible on screen
            if (point.X >= 0 && point.Y >= 0 && point.X < bmp.PixelWidth && point.Y < bmp.PixelHeight)
            {
                // Drawing a yellow point
                PutPixel((int)point.X, (int)point.Y, color);
            }
        }

        public void DrawPoint(Vector3 point, Color4 color)
        {
            // Clipping what's visible on screen
            if (point.X >= 0 && point.Y >= 0 && point.X < bmp.PixelWidth && point.Y < bmp.PixelHeight)
            {
                // Drawing a yellow point
                PutPixel((int)point.X, (int)point.Y, point.Z, color);
            }
        }

        /// <summary>
        ///  Clamp values to keep them between 0 and 1.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        float Clamp(float value, float min = 0, float max = 1)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        /// <summary>
        /// Interpolate the value between 2 vertices.
        /// </summary>
        /// <param name="min">The starting point</param>
        /// <param name="max">The ending point</param>
        /// <param name="gradient">The percentage between the 2 points</param>
        /// <returns></returns>
        float Interpolate(float min, float max, float gradient)
        {
            return min + (max - min) * Clamp(gradient);
        }

        /// <summary>
        /// Draw line between 2 points from left to right.
        /// <remarks>pa, pb, pc and pd must then be sorted before.</remarks>
        /// </summary>
        /// <param name="y"></param>
        /// <param name="pa"></param>
        /// <param name="pb"></param>
        /// <param name="pc"></param>
        /// <param name="pd"></param>
        /// <param name="color"></param>
        void ProcessScanLine(ScanLineData data, Vertex va, Vertex vb, Vertex vc, Vertex vd, Color4 color)
        {
            Vector3 pa = va.Coordinates;
            Vector3 pb = vb.Coordinates;
            Vector3 pc = vc.Coordinates;
            Vector3 pd = vd.Coordinates;

            // Thanks to current Y, we can compute the gradient to compute others values like
            // the starting X (sx) and ending X (ex) to draw between
            // if pa.Y == pb.Y or pc.Y == pd.Y, gradient is forced to 1.
            var gradient1 = pa.Y != pb.Y ? (data.currentY - pa.Y) / (pb.Y - pa.Y) : 1;
            var gradient2 = pc.Y != pd.Y ? (data.currentY - pc.Y) / (pd.Y - pc.Y) : 1;

            int sx = (int)Interpolate(pa.X, pb.X, gradient1);
            int ex = (int)Interpolate(pc.X, pd.X, gradient2);

            // Stgarting Z & ending Z
            float z1 = Interpolate(pa.Z, pb.Z, gradient1);
            float z2 = Interpolate(pc.Z, pd.Z, gradient2);

            // Drawing a line from left (sx) to right (ex)
            for (var x = sx; x < ex; x++)
            {
                float gradient = (x - sx) / (float)(ex - sx);

                var z = Interpolate(z1, z2, gradient);
                var ndotl = data.ndotla;
                DrawPoint(new Vector3(x, data.currentY, z), color * ndotl);
            }
        }

        void Swap<T>(ref T v1, ref T v2)
        {
            var temp = v2;
            v2 = v1;
            v1 = temp;
        }

        float ComputeNDotL(Vector3 vertex, Vector3 normal, Vector3 lightPosition)
        {
            var lightDirection = lightPosition - vertex;

            normal.Normalize();
            lightDirection.Normalize();

            return Math.Max(0, Vector3.Dot(normal, lightDirection));
        }

        public void DrawTriangle(Vertex v1, Vertex v2, Vertex v3, Color4 color) 
        {
            // Sorting the points in order to always have this order on screen p1, p2 & p3
            // with p1 always up (thus having the Y the lowest possible to be near the top screen)
            // then p2 between p1 & p3
            if (v1.Coordinates.Y > v2.Coordinates.Y)
            {
                var temp = v2;
                v2 = v1;
                v1 = temp;
            }

            if (v2.Coordinates.Y > v3.Coordinates.Y)
            {
                var temp = v2;
                v2 = v3;
                v3 = temp;
            }

            if (v1.Coordinates.Y > v2.Coordinates.Y)
            {
                var temp = v2;
                v2 = v1;
                v1 = temp;
            }

            Vector3 p1 = v1.Coordinates;
            Vector3 p2 = v2.Coordinates;
            Vector3 p3 = v3.Coordinates;
            
            // normal face's vector is the average normal between each vertex's normal
            // computing also the center point of the face
            Vector3 vnFace = (v1.Normal + v2.Normal + v3.Normal) / 3;
            Vector3 centerPoint = (v1.WorldCoordinates + v2.WorldCoordinates + v3.WorldCoordinates) / 3;
            // Light position
            Vector3 lightPos = new Vector3(0, 10, 10);
            // computing the cos of the angle between the light vector and the normal vector
            // it will return a value between 0 and 1 that will be used as the intensity of the color
            float ndotl = ComputeNDotL(centerPoint, vnFace, lightPos);

            var data = new ScanLineData { ndotla = ndotl };

            // inverse slopes
            float dP1P2, dP1P3;

            // Computing inverse slopes. See:
            // http://en.wikipedia.org/wiki/Slope
            if (p2.Y - p1.Y > 0)
                dP1P2 = (p2.X - p1.X) / (p2.Y - p1.Y);
            else
                dP1P2 = 0;

            if (p3.Y - p1.Y > 0)
                dP1P3 = (p3.X - p1.X) / (p3.Y - p1.Y);
            else
                dP1P3 = 0;

            // First case: P2 is in the right side of P1-P3
            if (dP1P2 > dP1P3)
            {
                for (var y = (int)p1.Y; y <= (int)p3.Y; y++)
                {
                    if (y < p2.Y)
                    {
                        ProcessScanLine(data, v1, v3, v1, v2, color);
                    }
                    else
                    {
                        ProcessScanLine(data, v1, v3, v2, v3, color);
                    }
                }
            }
            // First case: P2 is in the left side of P1-P3
            else
            {
                for (var y = (int)p1.Y; y <= (int)p3.Y; y++)
                {
                    if (y < p2.Y)
                    {
                        ProcessScanLine(data, v1, v2, v1, v3, color);
                    }
                    else
                    {
                        ProcessScanLine(data, v2, v3, v1, v3, color);
                    }
                }
            }
        }

        public async Task<Mesh[]> LoadJSONFileAsync(string fileName)
        {
            var meshes = new List<Mesh>();
            var data = await Util.ReadTextAsync(fileName, Encoding.UTF8);
            dynamic jsonObject = Newtonsoft.Json.JsonConvert.DeserializeObject(data);

            for (var meshIndex = 0; meshIndex < jsonObject.meshes.Count; meshIndex++)
            {
                var verticesArray = jsonObject.meshes[meshIndex].vertices;
                var indicesArray = jsonObject.meshes[meshIndex].indices;
                var uvCount = jsonObject.meshes[meshIndex].uvCount.Value;
                var verticesStep = 1;

                // Depending of the number of texture's coordinates per vertex
                // we're jumping in the vertices array by 6, 8 & 10 windows frame
                switch ((int)uvCount)
                {
                    case 0: verticesStep = 6; break;
                    case 1: verticesStep = 8; break;
                    case 2: verticesStep = 10; break;
                }

                // The number of interesting vertices information for us
                var verticesCount = verticesArray.Count / verticesStep;
                // Number of faces is logically the size of the array divided by 3 (A, B, C)
                var facesCount = indicesArray.Count / 3;
                var mesh = new Mesh(jsonObject.meshes[meshIndex].name.Value, verticesCount, facesCount);

                // Filling the Vertices array of our mesh first
                for (var index = 0; index < verticesCount; index++)
                {
                    var x = (float)verticesArray[index * verticesStep].Value;
                    var y = (float)verticesArray[index * verticesStep + 1].Value;
                    var z = (float)verticesArray[index * verticesStep + 2].Value;
                    // Loading the vertex normal exported by Blender
                    var nx = (float)verticesArray[index * verticesStep + 3].Value;
                    var ny = (float)verticesArray[index * verticesStep + 4].Value;
                    var nz = (float)verticesArray[index * verticesStep + 5].Value;
                    mesh.Vertices[index] = new Vertex
                    {
                        Coordinates = new Vector3(x, y, z),
                        Normal = new Vector3(nx, ny, nz)
                    };
                }

                // Then filling the Faces array
                for (var index = 0; index < facesCount; index++)
                {
                    var a = (int)indicesArray[index * 3].Value;
                    var b = (int)indicesArray[index * 3 + 1].Value;
                    var c = (int)indicesArray[index * 3 + 2].Value;
                    mesh.Faces[index] = new Face { A = a, B = b, C = c };
                }

                // Getting the position you've set in Blender
                var position = jsonObject.meshes[meshIndex].position;
                mesh.Position = new Vector3((float)position[0].Value, (float)position[1].Value, (float)position[2].Value);
                meshes.Add(mesh);
            }

            return meshes.ToArray();
        }

        public void Render(Camera camera, params Mesh[] meshes)
        {
            var viewMatrix = Matrix.LookAtLH(camera.Position, camera.Target, Vector3.UnitY);
            var projectionMatrix = Matrix.PerspectiveFovRH(0.78f, (float)bmp.PixelWidth / bmp.PixelHeight, 0.01f, 1.0f);

            foreach (var mesh in meshes)
            {
                // Beware to apply rotation before translation
                var worldMatrix =
                    Matrix.RotationYawPitchRoll(mesh.Rotation.Y, mesh.Rotation.X, mesh.Rotation.Z)
                    * Matrix.Translation(mesh.Position);
                var transformMatrix = worldMatrix * viewMatrix * projectionMatrix;

                //var faceIndex = 0;
                //foreach (var face in mesh.Faces)
                Parallel.For(0, mesh.Faces.Length, faceIndex =>
                {
                    uiContext.Post(_ =>
                    {
                        var face = mesh.Faces[faceIndex];
                        var vertexA = mesh.Vertices[face.A];
                        var vertexB = mesh.Vertices[face.B];
                        var vertexC = mesh.Vertices[face.C];

                        var pixelA = Project(vertexA, transformMatrix, worldMatrix);
                        var pixelB = Project(vertexB, transformMatrix, worldMatrix);
                        var pixelC = Project(vertexC, transformMatrix, worldMatrix);

                        var color = 1.0f;
                        DrawTriangle(pixelA, pixelB, pixelC, new Color4(color, color, color, 1));
                        faceIndex++;
                    }, null);
                });
            }
        }
    }
}
