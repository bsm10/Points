using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;

namespace Points
{
    public partial class Game
    {
        private string msg = string.Empty;
        public string StatusMsg
        {
          get
            {
                return msg;
            }
          set
            {
                msg = value;
            }
        }

        #region RENDER

        private void DrawStatusMsg(string msg, CanvasDrawingSession drSession)
        {
            CanvasTextFormat format = new CanvasTextFormat()
            {
                FontFamily = "Arial",
                FontSize = 0.3f
            };
            if (canvasCtrl != null )
            {
                drSession.DrawText(msg, 0, iBoardHeight + startY, Colors.DarkGreen, format);
                canvasCtrl.Invalidate();
            }
        }
        

        public void DrawGame(CanvasControl canvasCtrl, CanvasDrawingSession drawingSession)//отрисовка хода игры
        {
            drawingSession.Antialiasing = CanvasAntialiasing.Antialiased;
            //Устанавливаем масштаб
            SetScale(drawingSession, (int)canvasCtrl.ActualWidth, (int)canvasCtrl.ActualHeight,
                startX, startX + iBoardWidth, startY, iBoardHeight + startY +1 );
            //Рисуем доску
            DrawBoard(drawingSession);
            //Рисуем точки
            DrawPoints(drawingSession);
            //Отрисовка замкнутого региона игрока1
            DrawLinks(drawingSession);
            //drawingSession.DrawLine(0, 100, 100, 0, colorBoard, 5.0f);
            //drawingSession.DrawText(StatusMsg, 0, iBoardHeight + startY , Colors.DarkGreen, format);
            DrawStatusMsg(StatusMsg, drawingSession);
        }
        public void DrawBoard(CanvasDrawingSession drawingSession)//рисуем доску из клеток
        {
            for (float i = 0; i < iBoardWidth; i++)
            {
                Color drB = i == 0 ? Colors.MediumSeaGreen : colorDrawBrush;
                drawingSession.DrawLine(i + startX + 0.5f, startY + 0.5f, i + startX + 0.5f, iBoardHeight + startY - 0.5f, colorBoard, 0.08f);
            }
            for (float i = 0; i < iBoardHeight; i++)
            {
                Color drB = i == 0 ? Colors.MediumSeaGreen : colorDrawBrush;
                drawingSession.DrawLine(startX + 0.5f, i + startY + 0.5f, iBoardWidth + startX - 0.5f, i + startY + 0.5f, colorBoard, 0.08f);
            }
            //drawingSession.DrawText("Points!", 1, iBoardHeight-2, Colors.DarkGreen); 
        }
        public void DrawLinks(CanvasDrawingSession drawingSession)//отрисовка связей
        {
            if (lnks != null)
            {
                Color colorGamer;
                for (int i = 0; i < lnks.Count; i++)
                {
                    if (lnks[i].Dot1.Blocked)//0.1f
                    {
                        colorGamer = lnks[i].Dot1.Own == 1 ? Color.FromArgb(130, colorGamer1.R, colorGamer1.G, colorGamer1.B) :
                                                             Color.FromArgb(130, colorGamer2.R, colorGamer2.G, colorGamer2.B);
                        drawingSession.DrawLine(lnks[i].Dot1.x, lnks[i].Dot1.y, lnks[i].Dot2.x, lnks[i].Dot2.y, colorGamer, 0.1f);
                    }
                    else
                    {
                        colorGamer = lnks[i].Dot1.Own == 1 ? colorGamer1 : colorGamer2;
                        drawingSession.DrawLine(lnks[i].Dot1.x, lnks[i].Dot1.y, lnks[i].Dot2.x, lnks[i].Dot2.y, colorGamer, 0.1f);
                    }
                }
            }
        }
        public void DrawPoints(CanvasDrawingSession drawingSession)//рисуем поставленные точки
        {
            //отрисовываем поставленные точки
            if (aDots.Count > 0)
            {
                foreach (Dot p in aDots)
                {
                    switch (p.Own)
                    {
                        case 1:
                            SetColorAndDrawDots(drawingSession, colorGamer1, p);
                            break;
                        case 2:
                            SetColorAndDrawDots(drawingSession, colorGamer2, p);
                            break;
                        case 0:
                            if (p.PatternsEmptyDot) SetColorAndDrawDots(drawingSession, Colors.Bisque, p);
                            if (p.PatternsAnyDot) SetColorAndDrawDots(drawingSession, Colors.DarkOrange, p);
                            break;
                    }
                }
            }
        }
        private void SetColorAndDrawDots(CanvasDrawingSession drawingSession, Color colorGamer, Dot p) //Вспомогательная функция для DrawPoints. Выбор цвета точки в зависимости от ее состояния и рисование элипса
        {

            Color c;
            if (p.Blocked)
            {
                drawingSession.FillEllipse(p.x, p.y, PointWidth, PointWidth, Color.FromArgb(130, colorGamer.R, colorGamer.G, colorGamer.B));
            }
            else if (last_move != null && p.x == last_move.x & p.y == last_move.y)//точка последнего хода должна для удоиства выделяться
            {
                drawingSession.FillEllipse(p.x, p.y, PointWidth, PointWidth, Color.FromArgb(140, colorGamer.R, colorGamer.G, colorGamer.B));
                drawingSession.DrawEllipse(p.x, p.y, PointWidth / 2, PointWidth / 2, Colors.WhiteSmoke, 0.05f);
                drawingSession.DrawEllipse(p.x, p.y, PointWidth, PointWidth, colorGamer, 0.08f);
            }
            else
            {
                int G = colorGamer.G > 50 ? colorGamer.G - 50 : 120;
                c = p.BlokingDots.Count > 0 ? Color.FromArgb(255, colorGamer.R, colorGamer.G, colorGamer.B) : colorGamer;
                drawingSession.FillEllipse(p.x, p.y, PointWidth, PointWidth, colorGamer);
                drawingSession.DrawEllipse(p.x, p.y, PointWidth, PointWidth, c, 0.08f);
            }
        }
        Matrix3x2 _transform;
        /// <summary>
        /// функция масштабирования, устанавливает массштаб
        /// </summary>
        /// <param name="gr - CanvasDrawingSession"></param>
        /// <param name="gr_width - ширина клиентской области"></param>
        /// <param name="gr_height - длина клиентской области"></param>
        /// <param name="left_x"></param>
        /// <param name="right_x"></param>
        /// <param name="top_y"></param>
        /// <param name="bottom_y"></param>
        private void SetScale(CanvasDrawingSession gr, int gr_width, int gr_height, float left_x, float right_x, float top_y, float bottom_y)
        {
            Matrix3x2 matrixTemp = gr.Transform;
            matrixTemp = Matrix3x2.CreateScale(new Vector2(gr_width / (right_x - left_x), gr_height / (bottom_y - top_y)),
                                               new Vector2(left_x, top_y));
            gr.Transform = matrixTemp;
            _transform = matrixTemp;
        }
        public Point TranslateCoordinates(Point MousePos)
        {
            Matrix3x2 transform;
            Matrix3x2.Invert(_transform, out transform);
            Vector2 v = Vector2.Transform(new Vector2((float)MousePos.X, (float)MousePos.Y), transform);
            Point result = new Point((int)Math.Round(v.X), (int)Math.Round(v.Y));
            return result;
        }

        #endregion



    }
}
