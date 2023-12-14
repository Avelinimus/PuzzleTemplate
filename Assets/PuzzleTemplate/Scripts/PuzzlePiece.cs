using Unity.VisualScripting;
using UnityEngine;

public struct PuzzlePieceSides
{
    public bool IsRight;
    public bool IsLeft;
    public bool IsUp;
    public bool IsDown;
}

public class PuzzlePiece : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Material _defaultMaterial;
    [SerializeField] private Material _selectedMaterial;

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

    public PuzzlePieceSides PuzzlePieceSides;

    public bool IsBackground;

    public void Initialize()
    {
        SetMaterial(_defaultMaterial);
    }

    private void SetMaterial(Material material)
    {
        if (!IsBackground)
        {
            _spriteRenderer.material = material;
        }
    }
}