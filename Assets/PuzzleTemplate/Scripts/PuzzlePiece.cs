using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace Game.Puzzle
{
    public struct PuzzlePieceStruct
    {
        public bool IsRight;
        public bool IsLeft;
        public bool IsUp;
        public bool IsDown;
        public int Row;
        public int Col;
        public Bounds Bounds;
    }

    [RequireComponent(typeof(SpriteRenderer))]
    public class PuzzlePiece : MonoBehaviour, IDisposable
    {
        private const float kSizeMagnetics = .02f;
        private const string kMagneticNamePattern = "Magnetic_{0}";

        [HideInInspector] public bool IsBackground;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Material _defaultMaterial;
        [SerializeField] private Material _selectedMaterial;
        [SerializeField] private Material _wrongMaterial;
        [SerializeField] private float _speedDragging = 0;
        [SerializeField] private int _selectedOrder = 10;
        [SerializeField] private int _wrongOrder = 2;
        [SerializeField] private int _defaultOrder = 1;
        [SerializeField] private int _magneticOrder = 0;
        [SerializeField] private float _scaleDefault = .5f;
        [SerializeField] private float _scaleSelected = .55f;
        [SerializeField] private GameObject _magneticPrefab;
        [SerializeField] private bool _isDebug;

        public PuzzlePieceStruct PuzzlePieceStruct;
        private BoxCollider2D _boxCollider2D;
        private readonly Dictionary<MagneticPiece, MagneticPiece> _magneticColliderMap;
        private bool _isDragging;

        public Sprite Sprite
        {
            get => _spriteRenderer.sprite;
            set => _spriteRenderer.sprite = value;
        }

        public Color SpriteRendererColor
        {
            get => _spriteRenderer.color;
            set => _spriteRenderer.color = value;
        }

        public int Order
        {
            get => _spriteRenderer.sortingOrder;
            set => _spriteRenderer.sortingOrder = value;
        }

        public SpriteRenderer SpriteRenderer => _spriteRenderer;

        public bool IsDragging => _isDragging;

        public bool IsMagnetic => IsEachOneMagneticCheck();
        public bool IsAllMagnetic => IsAllMagneticCheck();

        public Dictionary<MagneticPiece, MagneticPiece> MagneticColliderMap => _magneticColliderMap;

        public PuzzlePiece()
        {
            _magneticColliderMap = new Dictionary<MagneticPiece, MagneticPiece>();
        }

        public void Initialize(DrawStruct drawStruct = default)
        {
            SetMaterial(_defaultMaterial);

            if (!IsBackground)
            {
                transform.localScale = Vector3.one * _scaleDefault;
                Order = _defaultOrder;
                CreateMagnetics(drawStruct);
                _boxCollider2D = gameObject.AddComponent<BoxCollider2D>();
            }
        }

        public void Dispose()
        {
            Destroy(_boxCollider2D);

            foreach (var magnetic in _magneticColliderMap)
            {
                magnetic.Key?.Dispose();
                magnetic.Value?.Dispose();
            }

            _magneticColliderMap.Clear();
        }

        public void BeginDragging()
        {
            _isDragging = true;
            SetMaterial(_selectedMaterial);
            Order = _selectedOrder;
            ScaleAnimation(_scaleSelected);
        }

        public void EndDragging(List<PuzzlePiece> endElements, Vector3 lastPosition, bool isGroup)
        {
            endElements.Add(this);

            if (isGroup)
            {
                for (int i = _magneticColliderMap.Count - 1; i >= 0; i--)
                {
                    var magnetic = _magneticColliderMap.ElementAt(i);

                    if (magnetic.Value == null)
                        continue;

                    if (endElements.Contains(magnetic.Value.PuzzlePiece))
                        continue;

                    if (this == magnetic.Value.PuzzlePiece)
                        continue;

                    magnetic.Value.PuzzlePiece.EndDragging(endElements, lastPosition, true);
                    magnetic.Value.PuzzlePiece.ClearAllMagnetic();
                }
            }

            SetMaterial(_defaultMaterial);
            ScaleAnimation(_scaleDefault);
            ClearAllMagnetic();
            CheckMagnetic(Vector3.zero);
            var offsetLastPosition = (isGroup) ? lastPosition : transform.position;
            DraggingGroup(new List<PuzzlePiece>(), offsetLastPosition, true);
            _isDragging = false;
        }

        public void DraggingGroup(List<PuzzlePiece> dragElements, Vector3 position,
            bool isRepaint = false)
        {
            dragElements.Add(this);

            for (int i = _magneticColliderMap.Count - 1; i >= 0; i--)
            {
                var magnetic = _magneticColliderMap.ElementAt(i);

                if (magnetic.Value == null)
                    continue;

                if (dragElements.Contains(magnetic.Value.PuzzlePiece))
                    continue;

                if (this == magnetic.Value.PuzzlePiece)
                    continue;

                if (!magnetic.Value.PuzzlePiece.IsDragging && !isRepaint)
                {
                    magnetic.Value.PuzzlePiece.BeginDragging();
                }
                else
                {
                    var offsetResult = ((magnetic.Key.transform.position - magnetic.Value.transform.position) -
                                        (magnetic.Key.PuzzlePiece.transform.position -
                                         magnetic.Value.PuzzlePiece.transform.position));

                    magnetic.Value.PuzzlePiece.DraggingGroup(dragElements, position + offsetResult,
                        isRepaint);
                }
            }

            Dragging(position, !isRepaint);
        }

        public void Dragging(Vector3 position, bool isLerp = true)
        {
            var newPosition = CheckBounds(position);

            transform.position = (isLerp)
                ? Vector3.Lerp(transform.position, newPosition, _speedDragging * Time.deltaTime)
                : newPosition;
        }

        public void ClearAllMagnetic()
        {
            for (int i = _magneticColliderMap.Count - 1; i >= 0; i--)
            {
                var magnetic = _magneticColliderMap.ElementAt(i);

                RemoveMagnetic(magnetic.Key);

                if (magnetic.Value == null)
                    continue;

                magnetic.Value.PuzzlePiece.RemoveMagnetic(magnetic.Value);
            }
        }

        public void RemoveMagnetic(MagneticPiece magnetic)
        {
            _magneticColliderMap[magnetic] = null;
            magnetic.ConnectedPieceMagneticPiece = null;
        }

        public void AddMagneticCollider(MagneticPiece magneticKey, MagneticPiece magneticValue)
        {
            _magneticColliderMap[magneticKey] = magneticValue;
            magneticKey.ConnectedPieceMagneticPiece = magneticValue;
            magneticValue.ConnectedPieceMagneticPiece = magneticKey;
        }

        public Vector3 CheckMagnetic(Vector3 offset)
        {
            var colliders = new List<Collider2D>();

            for (int i = _magneticColliderMap.Count - 1; i >= 0; i--)
            {
                var magnetic = _magneticColliderMap.ElementAt(i);

                Physics2D.OverlapCollider(magnetic.Key.BoxCollider2D, new ContactFilter2D(), colliders);

                foreach (var collider in colliders)
                {
                    var magneticCheck = collider.GetComponent<MagneticPiece>();

                    if (magneticCheck == null)
                        continue;

                    if (magnetic.Key.IsMagnetic)
                        continue;

                    if (magneticCheck.IsMagnetic)
                        continue;

                    if (magneticCheck.MagneticStruct.IsShape == magnetic.Key.MagneticStruct.IsShape)
                        continue;

                    if (Mathf.Abs((int) magneticCheck.MagneticStruct.MagneticSideType) !=
                        Mathf.Abs((int) magnetic.Key.MagneticStruct.MagneticSideType))
                        continue;

                    if (magneticCheck.MagneticStruct.MagneticSideType ==
                        magnetic.Key.MagneticStruct.MagneticSideType)
                        continue;

                    AddMagneticCollider(magnetic.Key, magneticCheck);
                    magneticCheck.PuzzlePiece.AddMagneticCollider(magneticCheck, magnetic.Key);

                    var position = magnetic.Key.transform.position - magneticCheck.transform.position;
                    offset += position;

                    var newOffset = magneticCheck.PuzzlePiece.CheckMagnetic(offset);
                    offset -= newOffset;
                    magneticCheck.PuzzlePiece.MagneticAnimation(position - newOffset);
                    magneticCheck.PuzzlePiece.MagnetMagneticConnected();
                }
            }

            return offset;
        }

        private Vector3 CheckBounds(Vector3 position)
        {
            var calculation = BoundaryСalculation();

            var x = Mathf.Clamp(position.x, calculation.Item1, calculation.Item2);
            var y = Mathf.Clamp(position.y, calculation.Item3, calculation.Item4);

            position = new Vector3(x, y, position.z);

            return position;
        }

        private void SetMaterial(Material material)
        {
            if (!IsBackground)
            {
                _spriteRenderer.material = material;
            }
        }

        public void SetSelectedMaterial()
        {
            SetMaterial(_selectedMaterial);
            Order = _selectedOrder;
        }

        public bool CheckIsCorrectBlocks()
        {
            for (int i = _magneticColliderMap.Count - 1; i >= 0; i--)
            {
                var magnetic = _magneticColliderMap.ElementAt(i);
                var isCorrect = IsCorrectBlock(magnetic.Key, magnetic.Value);
                SetMaterial(isCorrect ? _defaultMaterial : _wrongMaterial);
                var order = (IsMagnetic) ? _magneticOrder : _defaultOrder;
                Order = isCorrect ? order : _wrongOrder;
                if (!isCorrect)
                    return false;
            }

            return true;
        }

        private bool IsAllMagneticCheck()
        {
            for (int i = _magneticColliderMap.Count - 1; i >= 0; i--)
            {
                var magnetic = _magneticColliderMap.ElementAt(i);

                if (magnetic.Value == null)
                    return false;


                if (!magnetic.Key.IsMagnetic)
                    return false;
            }

            return true;
        }

        private bool IsEachOneMagneticCheck()
        {
            for (int i = _magneticColliderMap.Count - 1; i >= 0; i--)
            {
                var magnetic = _magneticColliderMap.ElementAt(i);

                if (magnetic.Value == null)
                    continue;

                if (magnetic.Key.IsMagnetic)
                    return true;
            }

            return false;
        }

        private bool IsCorrectBlock(MagneticPiece magneticKey, MagneticPiece magneticCheck)
        {
            if (magneticKey == null || magneticCheck == null)
                return true;

            var keyStruct = magneticKey.MagneticStruct;
            var checkStruct = magneticCheck.MagneticStruct;

            bool isMagneticSideCorrect = keyStruct.Column == checkStruct.Column && keyStruct.Row == checkStruct.Row;

            return isMagneticSideCorrect;
        }

        private void CreateMagnetics(DrawStruct drawStruct)
        {
            bool[] sides = {drawStruct.IsRightSide, drawStruct.IsLeftSide, drawStruct.IsUpSide, drawStruct.IsDownSide};

            MagneticSideType[] sidesType =
                {MagneticSideType.Right, MagneticSideType.Left, MagneticSideType.Up, MagneticSideType.Down};

            for (int i = 0; i < sides.Length; i++)
            {
                var side = sides[i];
                var sideType = sidesType[i];

                if (side)
                {
                    CreateMagnetic(sideType, drawStruct);
                }
            }
        }

        private void CreateMagnetic(MagneticSideType magneticSideType, DrawStruct drawStruct)
        {
            var magnetic = Instantiate(_magneticPrefab, transform).GetComponent<MagneticPiece>();
            magnetic.name = string.Format(kMagneticNamePattern, magneticSideType);
            var position = Vector3.zero;
            var size = drawStruct.SizeJoint * kSizeMagnetics;
            var isShape = false;
            int row = 0;
            int col = 0;

            switch (magneticSideType)
            {
                case MagneticSideType.Left:
                    position = new Vector3(-_spriteRenderer.size.x / 2 + size / 2, 0);
                    isShape = drawStruct.IsLeft;
                    row = drawStruct.Row;
                    col = drawStruct.Col;

                    break;
                case MagneticSideType.Right:
                    position = new Vector3(_spriteRenderer.size.x / 2 - size / 2, 0);
                    isShape = drawStruct.IsRight;
                    row = drawStruct.Row;
                    col = drawStruct.Col + 1;

                    break;
                case MagneticSideType.Down:
                    position = new Vector3(0, -_spriteRenderer.size.y / 2 + size / 2);
                    isShape = drawStruct.IsDown;
                    row = drawStruct.Row;
                    col = drawStruct.Col;

                    break;
                case MagneticSideType.Up:
                    position = new Vector3(0, _spriteRenderer.size.y / 2 - size / 2);
                    isShape = drawStruct.IsUp;
                    row = drawStruct.Row + 1;
                    col = drawStruct.Col;

                    break;
            }

            magnetic.transform.localPosition = position;
            magnetic.BoxCollider2D.size = Vector2.one * size;
            magnetic.PuzzlePiece = this;


            magnetic.MagneticStruct = new MagneticStruct
            {
                MagneticSideType = magneticSideType,
                IsShape = isShape,
                Row = row,
                Column = col
            };

            _magneticColliderMap.Add(magnetic, null);
        }

        private void ScaleAnimation(float targetScale)
        {
            transform.localScale = Vector3.one * targetScale;
        }

        private void MagneticAnimation(Vector3 position)
        {
            var resultPosition = transform.position + position;
            transform.position = resultPosition;
        }

        private void MagnetMagneticConnected()
        {
            for (int i = _magneticColliderMap.Count - 1; i >= 0; i--)
            {
                var magnetic = _magneticColliderMap.ElementAt(i);

                if (!_magneticColliderMap.ContainsKey(magnetic.Key) || magnetic.Value == null)
                    continue;

                Vector3 position = magnetic.Key.transform.position - magnetic.Value.transform.position;
                magnetic.Value.PuzzlePiece.MagneticAnimation(position);
            }
        }

        private (float, float, float, float) BoundaryСalculation()
        {
            float spriteWidth = _spriteRenderer.size.x;
            float spriteHeight = _spriteRenderer.size.y;

            float minX = PuzzlePieceStruct.Bounds.min.x + (spriteWidth / 2) * transform.lossyScale.x;
            float maxX = PuzzlePieceStruct.Bounds.max.x - (spriteWidth / 2) * transform.lossyScale.x;
            float minY = PuzzlePieceStruct.Bounds.min.y + (spriteHeight / 2) * transform.lossyScale.y;
            float maxY = PuzzlePieceStruct.Bounds.max.y - (spriteHeight / 2) * transform.lossyScale.y;

            return (minX, maxX, minY, maxY);
        }

        private void OnDrawGizmos()
        {
            if (!_isDebug)
                return;

            if (!_isDragging)
                return;

            var calculation = BoundaryСalculation();

            Vector3 center = new Vector3((calculation.Item1 + calculation.Item2) / 2,
                (calculation.Item3 + calculation.Item4) / 2, 0);

            Vector3 size = new Vector3(calculation.Item2 - calculation.Item1, calculation.Item4 - calculation.Item3, 0);

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(center, size);
        }
    }
}