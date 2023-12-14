using System;
using UnityEngine;
using Random = System.Random;

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
}

public class PuzzleCreator : MonoBehaviour, IDisposable
{
    private const int kDefaultOrder = -1;

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

    public Random Random;
    private PuzzlePiece _backgroundPiece;
    private PuzzlePiece[][] _pieceArray;

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
                Destroy(piece);
                row[j] = null;
            }
        }
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
                    !_pieceArray[i][j + 1].PuzzlePieceSides.IsLeft;

                var isLeftColor = (j - 1 < 0) ? NextBoolean(Random) :
                    (_pieceArray[i][j - 1] == null) ? NextBoolean(Random) :
                    !_pieceArray[i][j - 1].PuzzlePieceSides.IsRight;

                var isUpColor = (i + 1 > Rows - 1) ? NextBoolean(Random) :
                    (_pieceArray[i + 1][j] == null) ? NextBoolean(Random) :
                    !_pieceArray[i + 1][j].PuzzlePieceSides.IsDown;

                var isDownColor = (i - 1 < 0) ? NextBoolean(Random) :
                    (_pieceArray[i - 1][j] == null) ? NextBoolean(Random) :
                    !_pieceArray[i - 1][j].PuzzlePieceSides.IsUp;


                var x = (Joint == JointType.None) ? j * pieceWidth : j * pieceWidth - (isLeftSide ? SizeJoint : 0);
                var y = (Joint == JointType.None) ? i * pieceHeight : i * pieceHeight - (isDownSide ? SizeJoint : 0);

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
                    Texture = puzzleTexture
                };

                SwitchType(drawStruct);

                RepaintMask(puzzleTexture, originalTexture);
                puzzleTexture.Apply();

                CreatePiece(i, j, puzzleTexture, drawStruct);
            }
        }
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

            DrawShape(drawStruct.Texture, shapeType, new Vector2(drawStruct.Width / 2, drawStruct.Height - SizeJoint),
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

            DrawShape(drawStruct.Texture, shapeType, new Vector2(drawStruct.Width - SizeJoint, drawStruct.Height / 2),
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

    private void CreatePiece(int i, int j, Texture2D puzzleTexture, DrawStruct drawStruct)
    {
        var puzzlePiece = Instantiate(_puzzlePiecePrefab).GetComponent<PuzzlePiece>();
        puzzlePiece.name = "PuzzlePiece_" + i + "_" + j;

        var puzzlePieceSprite = Sprite.Create(puzzleTexture,
            new Rect(0, 0, puzzleTexture.width, puzzleTexture.height), new Vector2(0.5f, 0.5f), 100, 0,
            SpriteMeshType.Tight);

        puzzlePiece.Sprite = puzzlePieceSprite;
        puzzlePiece.transform.parent = transform;
        puzzlePiece.transform.position = GetRandomPosition(_backgroundPiece.SpriteRenderer);

        puzzlePiece.PuzzlePieceSides = new PuzzlePieceSides
        {
            IsDown = drawStruct.IsDown,
            IsUp = drawStruct.IsUp,
            IsLeft = drawStruct.IsLeft,
            IsRight = drawStruct.IsRight,
        };
        puzzlePiece.Initialize();
        _pieceArray[i][j] = puzzlePiece;
    }

    private void CreateBackgroundSprite()
    {
        _backgroundPiece = Instantiate(_puzzlePiecePrefab).GetComponent<PuzzlePiece>();
        _backgroundPiece.name = "PuzzlePieceExample";

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