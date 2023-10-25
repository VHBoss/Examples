using UnityEngine;
using UnityEngine.UI;

public class Bar : Graphic {

    public Color Bulls;
    public Color Bears;
    public Color Stroke;

    public double open = 2f;
    public double high = 6f;
    public double low = 1f;
    public double close = 4f;

    private float rectWidth;
    private float rectHeight;
    private float percentOpen;
    private float percentClose;
    private Color barColor;

    public void Init(BarInfo bar) {
        low = bar.low;
        high = bar.high;
        open = bar.open;
        close = bar.close;

        if (open < close) {
            barColor = Bulls;            
        } else {
            open = bar.close;
            close = bar.open;
            barColor = Bears;
        }

        double scale = high - low;
        percentOpen = (float)((open - low) / scale);
        percentClose = (float)((close - low) / scale);
    }

    protected override void OnPopulateMesh(VertexHelper vh) {
        rectWidth = rectTransform.rect.width;
        rectHeight = rectTransform.rect.height;
        float minX = (0f - rectTransform.pivot.x) * rectWidth;
        float minY = (0f - rectTransform.pivot.y) * rectHeight;
        vh.Clear();

        //barColor = open < close ? Bulls : Bears;

        int k = 0;
        //Min-Max
        vh.AddVert(new Vector3(minX + rectWidth* 0.5f, minY), Stroke, new Vector2(0, 0));
        vh.AddVert(new Vector3(minX + rectWidth* 0.5f + 1, minY), Stroke, new Vector2(1, 0));
        vh.AddVert(new Vector3(minX + rectWidth* 0.5f, rectHeight), Stroke, new Vector2(0, 1));
        vh.AddVert(new Vector3(minX + rectWidth* 0.5f + 1, rectHeight), Stroke, new Vector2(1, 1));

        vh.AddTriangle(k * 4, k * 4 + 1, k * 4 + 2);
        vh.AddTriangle(k * 4 + 2, k * 4 + 1, k * 4 + 3);
        k++;

        //Outline
        vh.AddVert(new Vector3(minX, percentOpen * rectHeight), Stroke, new Vector2(0, 0));
        vh.AddVert(new Vector3(minX + rectWidth, percentOpen * rectHeight), Stroke, new Vector2(1, 0));
        vh.AddVert(new Vector3(minX, percentClose * rectHeight), Stroke, new Vector2(0, 1));
        vh.AddVert(new Vector3(minX + rectWidth, percentClose * rectHeight), Stroke, new Vector2(1, 1));

        vh.AddTriangle(k * 4, k * 4 + 1, k * 4 + 2);
        vh.AddTriangle(k * 4 + 2, k * 4 + 1, k * 4 + 3);
        k++;

        //Open-Close
        vh.AddVert(new Vector3(minX + 1, percentOpen * rectHeight + 1), barColor, new Vector2(0, 0));
        vh.AddVert(new Vector3(minX + rectWidth - 1, percentOpen * rectHeight + 1), barColor, new Vector2(1, 0));
        vh.AddVert(new Vector3(minX + 1, percentClose * rectHeight - 1), barColor, new Vector2(0, 1));
        vh.AddVert(new Vector3(minX + rectWidth - 1, percentClose * rectHeight - 1), barColor, new Vector2(1, 1));

        vh.AddTriangle(k * 4, k * 4 + 1, k * 4 + 2);
        vh.AddTriangle(k * 4 + 2, k * 4 + 1, k * 4 + 3);
        k++;
    }

#if UNITY_EDITOR
    protected override void OnValidate() {
        double scale = high - low;
        percentOpen = (float)((open - low)/ scale);
        percentClose = (float)((close - low)/ scale);
        base.OnValidate();
    }
#endif
}
