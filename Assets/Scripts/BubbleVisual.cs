using UnityEngine;

public class BubbleVisual : MonoBehaviour
{
    // ���� ����� ������ ������
    [Header("Bubble Sprites")]
    public Sprite normalSprite;    // ���� ���� � ��� ������
    public Sprite cracked1Sprite;  // ���� ����� ���
    public Sprite cracked2Sprite;  // ���� ��� ������

    private SpriteRenderer sr;

    private void Awake()
    {
        // ������ �� �-SpriteRenderer ��� ���� prefab
        sr = GetComponent<SpriteRenderer>();

        // ������� ������� ����� ����� ������
        if (normalSprite != null)
            sr.sprite = normalSprite;
    }

    /// <summary>
    /// ����� ����� ����� ����� �������
    /// </summary>
    public void UpdateVisual(int hits)
    {
        if (hits == 1 && cracked1Sprite != null)
        {
            sr.sprite = cracked1Sprite;   // ���� ����� ���
        }
        else if (hits == 2 && cracked2Sprite != null)
        {
            sr.sprite = cracked2Sprite;   // ���� ��� ������
        }
        // �� hits >=3 � ���� �-Grid ��� ���� �� ����� �����
    }
}
