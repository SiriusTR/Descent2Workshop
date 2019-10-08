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

namespace LibDescent.Data
{
    public class Reactor : HAMElement
    {
        public int model_id;
        public int n_guns;
        public FixVector[] gun_points = new FixVector[8];
        public FixVector[] gun_dirs = new FixVector[8];

        public Polymodel model;
        public int ModelID { get { if (model == null) return -1; return model.ID; } }

        public void InitReferences(IElementManager manager)
        {
            model = manager.GetModel(model_id);
        }

        public void AssignReferences(IElementManager manager)
        {
            if (model != null) model.AddReference(HAMType.Reactor, this, 0);
        }

        public void ClearReferences()
        {
            if (model != null) model.ClearReference(HAMType.Reactor, this, 0);
        }

        public static string GetTagName(int tag)
        {
            return "Model";
        }
    }
}
