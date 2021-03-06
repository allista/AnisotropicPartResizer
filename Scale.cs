﻿//   Scale.cs
//
//  Author:
//       Allis Tauri <allista@gmail.com>
//
//  Copyright (c) 2016 Allis Tauri

using System;
using UnityEngine;

namespace AT_Utils
{
    public class Scale
    {
        public class SimpleScale
        {
            public readonly float scale;
            public readonly float aspect;
            public readonly float sqrt;
            public readonly float quad;
            public readonly float cube;
            public readonly float volume;

            public SimpleScale(float scale, float aspect)
            { 
                this.scale = scale; 
                this.aspect = aspect;
                sqrt = (float)Math.Sqrt(scale);
                quad = scale*scale;
                cube = quad*scale;
                volume = cube*aspect;
            }

            public static implicit operator float(SimpleScale s) { return s.scale; }
        }

        public readonly SimpleScale absolute;
        public readonly SimpleScale relative;
        public readonly bool FirstTime;

        public readonly float size;
        public readonly float orig_size;
        public float aspect => absolute.aspect;

        public Scale(
            float size,
            float old_size,
            float orig_size,
            float aspect,
            float old_aspect,
            float orig_aspect,
            bool first_time
        )
        {
            this.size = size;
            this.orig_size = orig_size;
            absolute = new SimpleScale(size / orig_size, aspect / orig_aspect);
            relative = new SimpleScale(size / old_size, aspect / old_aspect);
            FirstTime = first_time;
        }

        public static Vector3 ScaleVector(Vector3 v, float scale, float aspect)
        { return Vector3.Scale(v, new Vector3(scale, scale*aspect, scale)); }

        public Vector3 ScaleVector(Vector3 v)
        { return ScaleVector(v, absolute.scale, absolute.aspect); }

        public Vector3 ScaleVectorRelative(Vector3 v)
        { return ScaleVector(v, relative.scale, relative.aspect); }

        public static implicit operator float(Scale s) { return s.absolute; }
    }
}

