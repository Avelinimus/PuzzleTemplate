using System;
using UnityEngine;

namespace Game.Puzzle
{
    public enum MagneticSideType
    {
        Right = 1,
        Left = -1,
        Up = 2,
        Down = -2,
    }

    public struct MagneticStruct
    {
        public MagneticSideType MagneticSideType;
        public bool IsShape;
        public bool IsMagnetic;
    }

    public class MagneticPiece : MonoBehaviour , IDisposable
    {
        [SerializeField] private BoxCollider2D _boxCollider2D;

        public MagneticStruct MagneticStruct;

        public BoxCollider2D BoxCollider2D => _boxCollider2D;

        public PuzzlePiece PuzzlePiece;

        public void Dispose()
        {
        }
    }
}