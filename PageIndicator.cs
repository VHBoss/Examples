using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class PageIndicator : MaskableGraphic {

    public Texture texture;
    public override Texture mainTexture {
        get { return texture; }
    }

    [Range(0, 10)]
    public int m_Value;
    public int Value {
        get { return m_Value; }
        set {
            m_Value = Mathf.Min(count, value);
            SetVerticesDirty();
        }
    }

    public int count;
    public int Count {
        get { return count; }
        set {
            count = value;
        }
    }

    public float offset = 10;    

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (count == 1) return;

        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;
        float minX = (0f - rectTransform.pivot.x) * width;
        float minY = (0f - rectTransform.pivot.y) * height;
        Color col = color;

        float cellSize = (width - offset * (count - 1))/count;
        int k = 0;
        for (int i = 0; i < count; i++)
        {
            float shift = i * (cellSize + offset);
            col.a = i == m_Value ? 1 : 0.3f;
            vh.AddVert(new Vector3(minX + shift, minY), col, new Vector2(0, 0));
            vh.AddVert(new Vector3(minX + shift, minY + height), col, new Vector2(0, 1));
            vh.AddVert(new Vector3(minX + shift + cellSize, minY), col, new Vector2(1, 0));
            vh.AddVert(new Vector3(minX + shift + cellSize, minY + height), col, new Vector2(1, 1));

            vh.AddTriangle(k * 4, k * 4 + 1, k * 4 + 2);
            vh.AddTriangle(k * 4 + 2, k * 4 + 1, k * 4 + 3);

            k++;
        }
    }
}
