﻿using RLTK.Consoles;
using RLTK.MonoBehaviours;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.Jobs;

namespace RLTK.Samples
{
    public struct TestMap : FOV.IVisibilityMap, IDisposable
    {
        int width;
        int height;
        NativeArray<bool> opaquePoints;

        public void SetVisibility(int2 p, bool v) => opaquePoints[p.y * width + p.x] = !v;

        public TestMap(int width, int height, Allocator allocator, params int2[] opaquePoints)
        {
            this.width = width;
            this.height = height;
            this.opaquePoints = new NativeArray<bool>(width * height, allocator);
            foreach (var p in opaquePoints)
                this.opaquePoints[p.y * width + p.x] = true;
        }

        public bool IsInBounds(int2 p) => p.x >= 0 && p.x < width && p.y >= 0 && p.y < height;

        public bool IsOpaque(int2 p) => opaquePoints[p.y * width + p.x];

        public void Dispose() => opaquePoints.Dispose();
    }

    public class FOVExample : MonoBehaviour
    {
        [SerializeField]
        int _range = 10;

        [SerializeField]
        Transform _walls = null;

        [SerializeField]
        SimpleConsoleProxy _console = null;

        List<int2> _wallPositions = new List<int2>();

        int2 WorldToConsolePos(Vector3 p) => new int2(math.floor(p).xy) + (_console.Size / 2);

        TestMap _testMap;


        private void Start()
        {
            _testMap = new TestMap(_console.Width, _console.Height, Allocator.Persistent);

            if( _walls.gameObject.activeInHierarchy )
            {
                var walls = _walls.GetComponentInChildren<Transform>();
                foreach (Transform t in walls)
                {
                    var p = WorldToConsolePos(t.position);
                    _testMap.SetVisibility(p, false);
                }

                Destroy(_walls.gameObject);
            }
        }

        private void OnDestroy()
        {
            _testMap.Dispose();
        }

        private void Update()
        {
            var fovPos = WorldToConsolePos(transform.position);

            var points = new NativeList<int2>((_range * 2) * (_range * 2), Allocator.TempJob);
            FOV.GetVisiblePointsJob(fovPos, _range, _testMap, points).Run();

            _console.ClearScreen();
            
            foreach ( var p in points )
            {
                char ch = _testMap.IsOpaque(p) ? '#' : '.';
                _console.Set(p.x, p.y, Color.white, Color.black, CodePage437.ToCP437(ch));
            }
            
            points.Dispose();
        }
    }
}