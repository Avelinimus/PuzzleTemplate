using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PuzzleCreator : MonoBehaviour, IDisposable
{
    private const int kDefaultOrder = -1;

    [SerializeField] private Texture2D _originalImage;
    [SerializeField] private GameObject _puzzlePiecePrefab;
    [SerializeField] private int _rows = 3;
    [SerializeField] private int _columns = 3;
    [SerializeField] private float _darkenAmount = 0.5f;
    [SerializeField] private int _circleRadius = 50;

    private PuzzlePiece _backgroundPiece;
    private readonly List<PuzzlePiece> _pieceList;

    public PuzzleCreator()
    {
        _pieceList = new List<PuzzlePiece>();
    }

    public void Start()
    {
        CreatePuzzlePieces();
    }

    public void OnDestroy()
    {
        Dispose();
    }

    public void Dispose()
    {
        Destroy(_backgroundPiece);

        foreach (var piece in _pieceList)
        {
            Destroy(piece);
        }

        _pieceList.Clear();
    }

    private void CreatePuzzlePieces()
    {
        CreateBackgroundSprite();
        var pieceWidth = (float)_originalImage.width / _columns;
        var pieceHeight = (float)_originalImage.height / _rows;

        for (var i = 0; i < _rows; i++)
        {
            for (var j = 0; j < _columns; j++)
            {
                var isRight = j != _columns - 1;
                var isUp = i != _rows - 1;
                var isLeft = j != 0;
                var isDown = i != 0;

                var x = j * pieceWidth - (isLeft ? _circleRadius : 0);
                var y = i * pieceHeight - (isDown ? _circleRadius : 0);
                var width = pieceWidth + (isLeft ? _circleRadius : 0) + (isRight ? _circleRadius : 0);
                var height = pieceHeight + (isUp ? _circleRadius : 0) + (isDown ? _circleRadius : 0);

                var rect = new Rect(x, y, width, height);

                var puzzleTexture = new Texture2D((int)width, (int)height);

                puzzleTexture.SetPixels(_originalImage.GetPixels((int)rect.x, (int)rect.y, (int)width, (int)height));

                if (isRight)
                {
                    DrawCircle(puzzleTexture, new Vector2(width - _circleRadius, height / 2), _circleRadius, Color.white);
                }

                if (isUp)
                {
                    DrawCircle(puzzleTexture, new Vector2(width / 2, height - _circleRadius), _circleRadius, Color.white);
                }

                if (isDown)
                {
                    DrawCircle(puzzleTexture, new Vector2(width / 2, _circleRadius), _circleRadius, Color.white);
                }

                if (isLeft)
                {
                    DrawCircle(puzzleTexture, new Vector2(_circleRadius, height / 2), _circleRadius, Color.white);
                }

                puzzleTexture.Apply();

                CreatePiece(i, j, puzzleTexture);
            }
        }
    }

    private void CreatePiece(int i, int j, Texture2D puzzleTexture)
    {
        var puzzlePiece = Instantiate(_puzzlePiecePrefab).GetComponent<PuzzlePiece>();
        puzzlePiece.name = "PuzzlePiece_" + i + "_" + j;

        var puzzlePieceSprite = Sprite.Create(puzzleTexture,
            new Rect(0, 0, puzzleTexture.width, puzzleTexture.height), new Vector2(0.5f, 0.5f), 100, 0,
            SpriteMeshType.Tight);

        puzzlePiece.Sprite = puzzlePieceSprite;
        puzzlePiece.transform.parent = transform;
        //puzzlePiece.MaskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        puzzlePiece.transform.position = GetRandomPosition(_backgroundPiece.SpriteRenderer);
        _pieceList.Add(puzzlePiece);
    }

    private void CreateBackgroundSprite()
    {
        _backgroundPiece = Instantiate(_puzzlePiecePrefab).GetComponent<PuzzlePiece>();
        _backgroundPiece.name = "PuzzlePieceExample";

        Sprite backgroundSpriteSprite = Sprite.Create(_originalImage,
            new Rect(0, 0, _originalImage.width, _originalImage.height), new Vector2(0.5f, 0.5f));

        _backgroundPiece.Sprite = backgroundSpriteSprite;

        _backgroundPiece.SpriteRendererColor = new Color(_backgroundPiece.SpriteRendererColor.r * _darkenAmount,
            _backgroundPiece.SpriteRendererColor.g * _darkenAmount,
            _backgroundPiece.SpriteRendererColor.b * _darkenAmount,
            _backgroundPiece.SpriteRendererColor.a);

        _backgroundPiece.Order = kDefaultOrder;
    }

    private Vector3 GetRandomPosition(SpriteRenderer spriteRenderer)
    {
        float textureWidth = _originalImage.width;
        float textureHeight = _originalImage.height;

        float spriteWidth = spriteRenderer.sprite.bounds.size.x;
        float spriteHeight = spriteRenderer.sprite.bounds.size.y;

        float randomXInTexture = Random.Range(0f, textureWidth);
        float randomYInTexture = Random.Range(0f, textureHeight);

        Vector3 randomPositionInTexture = new Vector3(
            (randomXInTexture / textureWidth - 0.5f) * spriteWidth,
            (randomYInTexture / textureHeight - 0.5f) * spriteHeight,
            0f
        );

        Vector3 randomPositionInWorld = transform.TransformPoint(randomPositionInTexture);

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
}