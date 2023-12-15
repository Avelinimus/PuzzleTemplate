using System;
using UnityEngine;
using Random = System.Random;

namespace Game.Puzzle
{
    public enum JointType
    {
        None,
        Circle,
        Rect,
    }

    public enum ShapeType
    {
        Circle,
        Rect
    }

    public struct DrawStruct
    {
        public bool IsRightSide;
        public bool IsLeftSide;
        public bool IsUpSide;
        public bool IsDownSide;
        public bool IsRight;
        public bool IsLeft;
        public bool IsUp;
        public bool IsDown;
        public float Width;
        public float Height;
        public Texture2D Texture;
        public float SizeJoint;
    }

    [RequireComponent(typeof(Texture2D))]
    public class PuzzleCreator : MonoBehaviour, IDisposable
    {
        private const int kDefaultOrder = -1;
        private const float kMousePositionZ = 10f;
        private const string kBackgroundPuzzleName = "PuzzleBackground";
        private const string kPiecePuzzleNamePattern = "PuzzlePiece_{0}_{1}";
        private const string kPuzzleLayerName = "Puzzle";


        public event Action COMPLETE_LEVEL;
        public event Action<PuzzlePiece> COMPLETE_PIECE;

        [HideInInspector] public Random Random;
        [SerializeField] public Texture2D OriginalImage;
        [SerializeField] public int Rows = 3;
        [SerializeField] public int Columns = 3;
        [SerializeField, Range(0, 1)] public float BackgroundDarkenAmount = 0.5f;
        [SerializeField] public int SizeJoint = 50;
        [SerializeField] public JointType Joint = JointType.Circle;
        [SerializeField] public int Seed = 0;
        [SerializeField] public Color ColorToRemove = Color.white;
        [SerializeField] public Color ColorToAdd = Color.black;
        [SerializeField] private GameObject _puzzlePiecePrefab;

        private PuzzlePiece _backgroundPiece;
        private PuzzlePiece[][] _pieceArray;
        private PuzzlePiece _currentPuzzlePiece;

        public PuzzlePiece[][] PieceArray => _pieceArray;
        public PuzzlePiece BackgroundPiece => _backgroundPiece;

        public void Start()
        {
            Random ??= new Random(Seed);

            CreatePuzzlePieces();
        }

        public void OnDestroy()
        {
            Dispose();
        }

        public void Dispose()
        {
            Destroy(_backgroundPiece);

            foreach (var row in _pieceArray)
            {
                for (var j = 0; j < row.Length; j++)
                {
                    var piece = row[j];
                    piece.COMPLETE_PIECE -= OnCompletePiece;
                    piece.Dispose();
                    Destroy(piece);
                    row[j] = null;
                }
            }
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (Input.GetMouseButton(0))
            {
                var mousePosition = Input.mousePosition;
                mousePosition.z = kMousePositionZ;
#else
            if (Input.touchCount > 0)
            {
            var touchPosition = Input.touches[0].position;
            var mousePosition = new Vector3(touchPosition.x, touchPosition.y, kMousePositionZ);
#endif

                TouchPuzzlePiece(mousePosition);
            }
            else
            {
                EndDragging();
            }
        }

        private void TouchPuzzlePiece(Vector3 position)
        {
            int layerMask = 1 << LayerMask.NameToLayer(kPuzzleLayerName);

            RaycastHit2D[] hits = Physics2D.RaycastAll(Camera.main.ScreenToWorldPoint(position), Vector2.zero,
                Mathf.Infinity, layerMask);

            if (hits.Length <= 0)
            {
                if (_currentPuzzlePiece != null)
                {
                    position.z = kMousePositionZ;
                    _currentPuzzlePiece.Dragging(Camera.main.ScreenToWorldPoint(position));
                }

                return;
            }

            if (_currentPuzzlePiece == null)
            {
                PuzzlePiece[] puzzlePiecesArray = new PuzzlePiece[hits.Length];

                for (var i = 0; i < puzzlePiecesArray.Length; i++)
                {
                    puzzlePiecesArray[i] = hits[i].transform.GetComponent<PuzzlePiece>();
                }

                Array.Sort(puzzlePiecesArray, (x, y) => Mathf.Max(x.Order, y.Order));
                _currentPuzzlePiece = hits[0].transform.GetComponent<PuzzlePiece>();
            }

            if (_currentPuzzlePiece == null)
                return;

            if (!_currentPuzzlePiece.IsDragging)
            {
                _currentPuzzlePiece.BeginDragging();
            }
            else
            {
                position.z = kMousePositionZ;
                _currentPuzzlePiece.Dragging(Camera.main.ScreenToWorldPoint(position));
            }
        }

        private void EndDragging()
        {
            if (_currentPuzzlePiece == null)
                return;

            _currentPuzzlePiece.EndDragging();
            _currentPuzzlePiece.CheckMagnetic();

            _currentPuzzlePiece = null;
        }

        private void CreatePuzzlePieces()
        {
            CreateBackgroundSprite();
            var pieceWidth = (float) OriginalImage.width / Columns;
            var pieceHeight = (float) OriginalImage.height / Rows;

            _pieceArray = new PuzzlePiece[Rows][];

            for (int i = 0; i < _pieceArray.Length; i++)
            {
                _pieceArray[i] = new PuzzlePiece[Columns];
            }

            for (var i = 0; i < Rows; i++)
            {
                for (var j = 0; j < Columns; j++)
                {
                    var isRightSide = j != Columns - 1;
                    var isLeftSide = j != 0;
                    var isUpSide = i != Rows - 1;
                    var isDownSide = i != 0;

                    var isRightColor = (j + 1 > Columns - 1) ? NextBoolean(Random) :
                        (_pieceArray[i][j + 1] == null) ? NextBoolean(Random) :
                        !_pieceArray[i][j + 1].PuzzlePieceStruct.IsLeft;

                    var isLeftColor = (j - 1 < 0) ? NextBoolean(Random) :
                        (_pieceArray[i][j - 1] == null) ? NextBoolean(Random) :
                        !_pieceArray[i][j - 1].PuzzlePieceStruct.IsRight;

                    var isUpColor = (i + 1 > Rows - 1) ? NextBoolean(Random) :
                        (_pieceArray[i + 1][j] == null) ? NextBoolean(Random) :
                        !_pieceArray[i + 1][j].PuzzlePieceStruct.IsDown;

                    var isDownColor = (i - 1 < 0) ? NextBoolean(Random) :
                        (_pieceArray[i - 1][j] == null) ? NextBoolean(Random) :
                        !_pieceArray[i - 1][j].PuzzlePieceStruct.IsUp;


                    var x = (Joint == JointType.None) ? j * pieceWidth : j * pieceWidth - (isLeftSide ? SizeJoint : 0);

                    var y = (Joint == JointType.None)
                        ? i * pieceHeight
                        : i * pieceHeight - (isDownSide ? SizeJoint : 0);

                    var width = (Joint == JointType.None)
                        ? pieceWidth
                        : pieceWidth + (isLeftSide ? SizeJoint : 0) + (isRightSide ? SizeJoint : 0);

                    var height = (Joint == JointType.None)
                        ? pieceHeight
                        : pieceHeight + (isUpSide ? SizeJoint : 0) + (isDownSide ? SizeJoint : 0);

                    var rect = new Rect(x, y, width, height);

                    var puzzleTexture = new Texture2D((int) width, (int) height);
                    var originalTexture = new Texture2D((int) width, (int) height);
                    var examplePixels = OriginalImage.GetPixels((int) rect.x, (int) rect.y, (int) width, (int) height);
                    puzzleTexture.SetPixels(examplePixels);
                    originalTexture.SetPixels(examplePixels);

                    var drawStruct = new DrawStruct
                    {
                        IsLeftSide = isLeftSide,
                        IsDownSide = isDownSide,
                        IsRightSide = isRightSide,
                        IsUpSide = isUpSide,
                        IsLeft = isLeftColor,
                        IsDown = isDownColor,
                        IsRight = isRightColor,
                        IsUp = isUpColor,
                        Width = width,
                        Height = height,
                        Texture = puzzleTexture,
                        SizeJoint = SizeJoint
                    };

                    SwitchType(drawStruct);

                    RepaintMask(puzzleTexture, originalTexture);
                    puzzleTexture.Apply();

                    CreatePiece(i, j, puzzleTexture, drawStruct);
                }
            }
        }

        private void CreateBackgroundSprite()
        {
            _backgroundPiece = Instantiate(_puzzlePiecePrefab, transform).GetComponent<PuzzlePiece>();
            _backgroundPiece.name = kBackgroundPuzzleName;

            var backgroundSpriteSprite = Sprite.Create(OriginalImage,
                new Rect(0, 0, OriginalImage.width, OriginalImage.height), new Vector2(0.5f, 0.5f));

            _backgroundPiece.Sprite = backgroundSpriteSprite;

            _backgroundPiece.SpriteRendererColor = new Color(
                _backgroundPiece.SpriteRendererColor.r * BackgroundDarkenAmount,
                _backgroundPiece.SpriteRendererColor.g * BackgroundDarkenAmount,
                _backgroundPiece.SpriteRendererColor.b * BackgroundDarkenAmount,
                _backgroundPiece.SpriteRendererColor.a);

            _backgroundPiece.IsBackground = this;
            _backgroundPiece.Order = kDefaultOrder;
            _backgroundPiece.Initialize();
        }

        private void CreatePiece(int i, int j, Texture2D puzzleTexture, DrawStruct drawStruct)
        {
            var puzzlePiece = Instantiate(_puzzlePiecePrefab).GetComponent<PuzzlePiece>();
            puzzlePiece.name = string.Format(kPiecePuzzleNamePattern, i, j);

            var puzzlePieceSprite = Sprite.Create(puzzleTexture,
                new Rect(0, 0, puzzleTexture.width, puzzleTexture.height), new Vector2(0.5f, 0.5f), 100, 0,
                SpriteMeshType.Tight);

            puzzlePiece.Sprite = puzzlePieceSprite;
            puzzlePiece.transform.parent = transform;
            puzzlePiece.transform.position = GetRandomPosition(_backgroundPiece.SpriteRenderer);
            puzzlePiece.COMPLETE_PIECE += OnCompletePiece;

            puzzlePiece.PuzzlePieceStruct = new PuzzlePieceStruct
            {
                IsDown = drawStruct.IsDown,
                IsUp = drawStruct.IsUp,
                IsLeft = drawStruct.IsLeft,
                IsRight = drawStruct.IsRight,
                IsMagnetic = false,
            };

            puzzlePiece.Initialize(drawStruct);
            _pieceArray[i][j] = puzzlePiece;
        }

        private void SwitchType(DrawStruct drawStruct)
        {
            if (Joint == JointType.None)
                return;

            switch (Joint)
            {
                case JointType.Circle:
                    DrawShapeMask(drawStruct, ShapeType.Circle);

                    break;
                case JointType.Rect:
                    DrawShapeMask(drawStruct, ShapeType.Rect);

                    break;
                // You can add new JointType and use it
            }
        }

        private void DrawShapeMask(DrawStruct drawStruct, ShapeType shapeType)
        {
            if (drawStruct.IsUpSide)
            {
                DrawRectangle(drawStruct.Texture,
                    new Rect(0, drawStruct.Height - SizeJoint, drawStruct.Width, SizeJoint * 2), ColorToRemove);

                DrawShape(drawStruct.Texture, shapeType,
                    new Vector2(drawStruct.Width / 2, drawStruct.Height - SizeJoint),
                    drawStruct.IsUp ? ColorToRemove : ColorToAdd);
            }

            if (drawStruct.IsDownSide)
            {
                DrawRectangle(drawStruct.Texture, new Rect(0, 0, drawStruct.Width, SizeJoint), ColorToRemove);

                DrawShape(drawStruct.Texture, shapeType, new Vector2(drawStruct.Width / 2, SizeJoint),
                    drawStruct.IsDown ? ColorToRemove : ColorToAdd);
            }

            if (drawStruct.IsRightSide)
            {
                DrawRectangle(drawStruct.Texture,
                    new Rect(drawStruct.Width - SizeJoint, 0, SizeJoint * 2, drawStruct.Height), ColorToRemove);

                DrawShape(drawStruct.Texture, shapeType,
                    new Vector2(drawStruct.Width - SizeJoint, drawStruct.Height / 2),
                    drawStruct.IsRight ? ColorToRemove : ColorToAdd);
            }

            if (drawStruct.IsLeftSide)
            {
                DrawRectangle(drawStruct.Texture, new Rect(0, 0, SizeJoint, drawStruct.Height), ColorToRemove);

                DrawShape(drawStruct.Texture, shapeType, new Vector2(SizeJoint, drawStruct.Height / 2),
                    drawStruct.IsLeft ? ColorToRemove : ColorToAdd);
            }
        }

        private void DrawShape(Texture2D texture, ShapeType shapeType, Vector2 position, Color color)
        {
            switch (shapeType)
            {
                case ShapeType.Circle:
                    DrawCircle(texture, position, SizeJoint, color);

                    break;
                case ShapeType.Rect:
                    DrawRectangle(texture,
                        new Rect(position.x - SizeJoint, position.y - SizeJoint, SizeJoint * 2, SizeJoint * 2), color);

                    break;
                // You can add new shapes and use it
            }
        }

        private Vector3 GetRandomPosition(SpriteRenderer spriteRenderer)
        {
            var textureWidth = OriginalImage.width;
            var textureHeight = OriginalImage.height;

            var spriteWidth = spriteRenderer.sprite.bounds.size.x;
            var spriteHeight = spriteRenderer.sprite.bounds.size.y;

            var randomXInTexture = UnityEngine.Random.Range(0f, textureWidth);
            var randomYInTexture = UnityEngine.Random.Range(0f, textureHeight);

            var randomPositionInTexture = new Vector3(
                (randomXInTexture / textureWidth - 0.5f) * spriteWidth,
                (randomYInTexture / textureHeight - 0.5f) * spriteHeight,
                0f
            );

            var randomPositionInWorld = transform.TransformPoint(randomPositionInTexture);

            return randomPositionInWorld;
        }

        private void OnCompletePiece(PuzzlePiece puzzlePiece)
        {
            COMPLETE_PIECE?.Invoke(puzzlePiece);
        }

        private void DrawCircle(Texture2D texture, Vector2 center, float radius, Color color)
        {
            int textureWidth = texture.width;
            int textureHeight = texture.height;

            for (int x = 0; x < textureWidth; x++)
            {
                for (int y = 0; y < textureHeight; y++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);

                    if (distance <= radius)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        private void DrawRectangle(Texture2D texture, Rect rect, Color color)
        {
            int textureWidth = texture.width;
            int textureHeight = texture.height;

            for (int x = (int) rect.xMin; x < rect.xMax; x++)
            {
                for (int y = (int) rect.yMin; y < rect.yMax; y++)
                {
                    if (x >= 0 && x < textureWidth && y >= 0 && y < textureHeight)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        private void RepaintMask(Texture2D texture, Texture2D originalTexture)
        {
            var textureWidth = texture.width;
            var textureHeight = texture.height;

            for (int x = 0; x < textureWidth; x++)
            {
                for (int y = 0; y < textureHeight; y++)
                {
                    if (texture.GetPixel(x, y).Equals(ColorToAdd))
                    {
                        texture.SetPixel(x, y, originalTexture.GetPixel(x, y));
                    }
                    else if (texture.GetPixel(x, y).Equals(ColorToRemove))
                    {
                        texture.SetPixel(x, y, new Color(0, 0, 0, 0));
                    }

                    if (x <= 0 || y <= 0 || x >= textureWidth || y >= textureHeight)
                    {
                        texture.SetPixel(x, y, new Color(0, 0, 0, 0));
                    }
                }
            }
        }

        private bool NextBoolean(Random random)
        {
            return random.Next() > (int.MaxValue / 2);
        }
    }
}