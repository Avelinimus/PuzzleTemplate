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
        public int Row;
        public int Column;
    }

    public class MagneticPiece : MonoBehaviour, IDisposable
    {
        [SerializeField] private BoxCollider2D _boxCollider2D;
        [SerializeField] private bool _isDebug;

        [HideInInspector] public MagneticStruct MagneticStruct;

        [HideInInspector] public MagneticPiece ConnectedPieceMagneticPiece;
        [HideInInspector] public BoxCollider2D BoxCollider2D => _boxCollider2D;

        [HideInInspector] public PuzzlePiece PuzzlePiece;

        public bool IsMagnetic => ConnectedPieceMagneticPiece != null;

        public void Dispose()
        {
        }

        private void OnDrawGizmos()
        {
            if(!_isDebug)
                return;

            if (!IsMagnetic)
            {
                Gizmos.color = Color.red;
            }
            else
            {
                Gizmos.color = Color.green;
            }

            Gizmos.DrawWireCube(transform.position, _boxCollider2D.size/2);
        }

        public void SetSizeCollider(float size)
        {
            _boxCollider2D.size *= size;
        }
    }
}