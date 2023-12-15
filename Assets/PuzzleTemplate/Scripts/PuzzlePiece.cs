using System;
using System.Collections.Generic;
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
        public bool IsMagnetic;
        public int LeftIndex;
        public int RightIndex;
        public int UpIndex;
        public int DownIndex;
    }

    [RequireComponent(typeof(SpriteRenderer))]
    public class PuzzlePiece : MonoBehaviour, IDisposable
    {
        private const float kSizeMagnetics = .02f;
        private const string kMagneticNamePattern = "Magnetic_{0}";

        public event Action<PuzzlePiece> COMPLETE_PIECE;

        [HideInInspector] public bool IsBackground;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Material _defaultMaterial;
        [SerializeField] private Material _selectedMaterial;
        [SerializeField] private float _speedDragging = 0;
        [SerializeField] private int _selectedOrder = 10;
        [SerializeField] private int _defaultOrder = 1;
        [SerializeField] private int _magneticOrder = 0;
        [SerializeField] private float _scaleAnimation = 5;
        [SerializeField] private float _scaleDefault = .5f;
        [SerializeField] private float _scaleSelected = 1;
        [SerializeField] private GameObject _magneticPrefab;

        public PuzzlePieceStruct PuzzlePieceStruct;
        private BoxCollider2D _boxCollider2D;
        private readonly List<MagneticPiece> _magneticColliderList;
        private readonly List<MagneticPiece> _magneticColliderCheckList;
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

        public PuzzlePiece()
        {
            _magneticColliderList = new List<MagneticPiece>();
            _magneticColliderCheckList = new List<MagneticPiece>();
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
            foreach (var magnetic in _magneticColliderList)
            {
                magnetic.Dispose();
            }

            _magneticColliderCheckList.Clear();
            _magneticColliderList.Clear();
        }

        public void BeginDragging()
        {
            _isDragging = true;
            SetMaterial(_selectedMaterial);
            Order = _selectedOrder;
            ScaleAnimation(_scaleSelected);
            PuzzlePieceStruct.IsMagnetic = false;

            foreach (var magnetic in _magneticColliderList)
            {
                magnetic.MagneticStruct.IsMagnetic = false;

                foreach (var magneticCheck in _magneticColliderCheckList)
                {
                    magneticCheck.MagneticStruct.IsMagnetic = false;
                }
            }

            _magneticColliderCheckList.Clear();
        }

        public void EndDragging()
        {
            _isDragging = false;
            SetMaterial(_defaultMaterial);
            Order = (PuzzlePieceStruct.IsMagnetic) ? _magneticOrder : _defaultOrder;
            ScaleAnimation(_scaleDefault);
        }

        public void Dragging(Vector3 position)
        {
            transform.position = Vector3.Lerp(transform.position, position, _speedDragging * Time.deltaTime);
        }

        public void CheckMagnetic()
        {
            foreach (var magnetic in _magneticColliderList)
            {
                var colliders = new List<Collider2D>();
                Physics2D.OverlapCollider(magnetic.BoxCollider2D, new ContactFilter2D(), colliders);

                foreach (var collider in colliders)
                {
                    var magneticCheck = collider.GetComponent<MagneticPiece>();

                    if (magneticCheck == null)
                        continue;

                    if (magneticCheck.MagneticStruct.IsMagnetic)
                        continue;

                    if (magneticCheck.MagneticStruct.IsShape == magnetic.MagneticStruct.IsShape)
                        continue;

                    if (Mathf.Abs((int) magneticCheck.MagneticStruct.MagneticSideType) !=
                        Mathf.Abs((int) magnetic.MagneticStruct.MagneticSideType))
                        continue;

                    PuzzlePieceStruct.IsMagnetic = true;

                    Vector3 position = magneticCheck.transform.position - magnetic.transform.position;

                    magneticCheck.MagneticStruct.IsMagnetic = true;
                    magnetic.MagneticStruct.IsMagnetic = true;
                    MagneticAnimation(position);
                    COMPLETE_PIECE?.Invoke(this);
                    AddMagneticCollider(magneticCheck);
                    magneticCheck.PuzzlePiece.AddMagneticCollider(magnetic);
                }
            }
        }

        public void AddMagneticCollider(MagneticPiece magnetic)
        {
            _magneticColliderCheckList.Add(magnetic);
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

            switch (magneticSideType)
            {
                case MagneticSideType.Left:
                    position = new Vector3(-_spriteRenderer.size.x / 2 + size / 2, 0);
                    isShape = drawStruct.IsLeft;

                    break;
                case MagneticSideType.Right:
                    position = new Vector3(_spriteRenderer.size.x / 2 - size / 2, 0);
                    isShape = drawStruct.IsRight;

                    break;
                case MagneticSideType.Down:
                    position = new Vector3(0, -_spriteRenderer.size.y / 2 + size / 2);
                    isShape = drawStruct.IsDown;

                    break;
                case MagneticSideType.Up:
                    position = new Vector3(0, _spriteRenderer.size.y / 2 - size / 2);
                    isShape = drawStruct.IsUp;

                    break;
            }

            magnetic.transform.localPosition = position;
            magnetic.BoxCollider2D.size = Vector2.one * size;
            magnetic.PuzzlePiece = this;

            magnetic.MagneticStruct = new MagneticStruct
            {
                MagneticSideType = magneticSideType,
                IsShape = isShape,
                IsMagnetic = false
            };

            _magneticColliderList.Add(magnetic);
        }

        private void ScaleAnimation(float targetScale)
        {
            //Can animate scale
            transform.localScale = Vector3.one * targetScale;
        }

        private void MagneticAnimation(Vector3 position)
        {
            var resultPosition = transform.position + position;
            transform.position = resultPosition;
        }

        private void SetMaterial(Material material)
        {
            if (!IsBackground)
            {
                _spriteRenderer.material = material;
            }
        }
    }
}