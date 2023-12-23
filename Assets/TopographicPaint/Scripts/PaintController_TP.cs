using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PaintController_TP : MonoBehaviour
{
    [SerializeField] private Canvas_TP canvasTP;
    
    [SerializeField] Camera camera2dWindow;
    Texture2D drawTexture;
    Color32[] buffer;

    private bool drawingFlag;

    private Vector2Int previousPointerPos;
    private Vector2Int currentPointerPos;

    private Color32 brushColor;
    
    //マウスカーソル用画像
    [SerializeField] private Texture2D cursor_pencil;
    [SerializeField] private Texture2D cursor_eraser;


    [Serializable] private class DrawCompleteEvent : UnityEvent<Texture2D>{}
    [SerializeField] private DrawCompleteEvent drawCompleteEvent = null;
    
    void Start ()
    {
        Texture2D mainTexture = (Texture2D) canvasTP.canvasMat.mainTexture;
        Color32[] pixels = mainTexture.GetPixels32();

        buffer = new Color32[pixels.Length];
        pixels.CopyTo (buffer, 0);

        drawTexture = new Texture2D (mainTexture.width, mainTexture.height, TextureFormat.RGBA32, false);
        drawTexture.filterMode = FilterMode.Point;

        previousPointerPos = new Vector2Int(0, 0);
        currentPointerPos = new Vector2Int(0, 0);
        drawTexture.SetPixels32 (buffer);
        drawTexture.Apply ();
        canvasTP.canvasMat.mainTexture = drawTexture;

        drawingFlag = false;
        
        UseBrush();
    }
    
    void Update () 
    {
        //線を引く処理
        if (Input.GetMouseButton (0)) 
        {
            Ray ray = camera2dWindow.ScreenPointToRay (Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast (ray, out hit, 100.0f))
            {
                if (Input.GetMouseButtonDown(0))
                {
                    drawingFlag = true;
                    previousPointerPos = new Vector2Int((int)(hit.textureCoord.x * drawTexture.width), 
                        (int)(hit.textureCoord.y * drawTexture.width));
                } 
                currentPointerPos = new Vector2Int((int)(hit.textureCoord.x * drawTexture.width), 
                    (int)(hit.textureCoord.y * drawTexture.width));
                LineDraw(previousPointerPos, currentPointerPos, brushColor);
                previousPointerPos = new Vector2Int((int)(hit.textureCoord.x * drawTexture.width), 
                    (int)(hit.textureCoord.y * drawTexture.width));
                
                
                drawTexture.SetPixels32 (buffer);
                drawTexture.Apply ();
                canvasTP.canvasMat.mainTexture = drawTexture;
            }
        
        }

        //線を引き終わったらハイトマップを生成する処理
        if (Input.GetMouseButtonUp(0) && drawingFlag == true)
        {
            drawCompleteEvent.Invoke(drawTexture);
            drawingFlag = false;
        }
    }
    
    void LineDraw(Vector2Int p1, Vector2Int p2, Color32 col)
    {
        int	dx, dy;
        int	x, y;
        int signX, signY;
        int	e;
        int	dx2, dy2;
        int	dd2;			// 誤差更新 2回分の合計

        dx = p2.x - p1.x;
        dy = p2.y - p1.y;

        signX = 1;
        signY = 1;
        dx2 = dx*2;
        dy2 = dy*2;
        
        if (dx >= 0 && dy >= 0)
        {
            signX = 1;
            signY = 1;
            dx2 = dx*2;
            dy2 = dy*2;
        }
        else if (dx < 0 && dy >= 0)
        {
            signX = -1;
            signY = 1;
            dx *= -1;
            dx2 = dx*2;
            dy2 = dy*2;
        }
        else if (dx < 0 && dy < 0)
        {
            signX = -1;
            signY = -1;
            dx *= -1;
            dy *= -1;
            dx2 = dx*2;
            dy2 = dy*2;
        }
        else
        {
            signX = 1;
            signY = -1;
            dy *= -1;
            dx2 = dx*2;
            dy2 = dy*2;
        }
        
        if (dx >= dy)
        {
            dd2 = dy2 - dx2;
            
            e = dy2;			// e += dy2; ループの事前に更新
            y = 0;
            for (x = 0; x <= dx; x++) {
                //mat brush[][]
                
                buffer.SetValue(col, (p1.x + x * signX) + (p1.y + y * signY) * drawTexture.width);
                buffer.SetValue(col, (p1.x + x * signX + 1) + (p1.y + y * signY) * drawTexture.width);
                buffer.SetValue(col, (p1.x + x * signX) + (p1.y + y * signY + 1) * drawTexture.width);
                buffer.SetValue(col, (p1.x + x * signX - 1) + (p1.y + y * signY) * drawTexture.width);
                buffer.SetValue(col, (p1.x + x * signX) + (p1.y + y * signY -1) * drawTexture.width);
            
                if (e >= dx) {
                    y++;
                    e += dd2;	// 今回分 -dx2 と次回分 +dy2 を一括更新
                } else {
                    e += dy2;	// 次回分の事前更新
                }
            }
        }
        else
        {
            dd2 = dx2 - dy2;
            
            e = dx2;			// e += dy2; ループの事前に更新
            x = 0;
            for (y = 0; y <= dy; y++) {
                buffer.SetValue(col, (p1.x + x * signX) + (p1.y + y * signY) * drawTexture.width);
                buffer.SetValue(col, (p1.x + x * signX + 1) + (p1.y + y * signY) * drawTexture.width);
                buffer.SetValue(col, (p1.x + x * signX) + (p1.y + y * signY + 1) * drawTexture.width);
                buffer.SetValue(col, (p1.x + x * signX - 1) + (p1.y + y * signY) * drawTexture.width);
                buffer.SetValue(col, (p1.x + x * signX) + (p1.y + y * signY -1) * drawTexture.width);
        
                if (e >= dy) {
                    x++;
                    e += dd2;	// 今回分 -dx2 と次回分 +dy2 を一括更新
                } else {
                    e += dx2;	// 次回分の事前更新
                }
            }
        }
    }

    public void ClearCanvas()
    {
        for (int i = 0; i < buffer.GetLength(0); i++)
        {
            buffer.SetValue(new Color32(255,255,255,255), i);
        }
        drawTexture.SetPixels32 (buffer);
        drawTexture.Apply ();

        canvasTP.canvasMat.mainTexture = drawTexture;
        
        drawCompleteEvent.Invoke(drawTexture);
    }

    public void UseBrush()
    {
        brushColor = new Color32(0, 0, 0, 255);
        // Cursor.SetCursor(cursor_pencil, new Vector2(-1, 0), CursorMode.Auto);
    }

    public void UseEraser()
    {
        brushColor = new Color32(255, 255, 255, 255);
        // Cursor.SetCursor(cursor_eraser, new Vector2(0, 1), CursorMode.Auto);
    }
}