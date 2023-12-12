using UnityEngine;

public class PuzzlePiece : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;

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

    public SpriteMaskInteraction MaskInteraction
    {
        get => _spriteRenderer.maskInteraction;
        set => _spriteRenderer.maskInteraction = value;
    }

    public SpriteRenderer SpriteRenderer => _spriteRenderer;
}