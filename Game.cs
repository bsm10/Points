using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using System.Numerics;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Graphics.Canvas.Brushes;
using Windows.UI.Xaml.Media;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml.Input;

namespace Points
{
    public partial class Game
    {
        public enum Player
        {
        NONE = 0,
        HUMAN = 1, 
        COMPUTER = 2
        }
        public int SkillLevel = 5;
        public int SkillDepth = 5;
        public int SkillNumSq = 3;

        //-------------------------------------------------
        public int iScaleCoef = 1;//- коэффициент масштаба
        public int iBoardWidth ;//- количество клеток квадрата в длинну
        public int iBoardHeight ;//- количество клеток квадрата в длинну

        public float startX = -0.5f, startY = -0.5f;
        public ArrayDots aDots;//Основной массив, где хранятся все поставленные точки. С єтого массива рисуются все точки
        private List<Links> lnks;
        private Dot best_move; //ход который должен сделать комп
        private Dot last_move; //последний ход
        private int win_player;//переменная получает номер игрока, котрый окружил точки

        private string status = string.Empty;
        public List<Dot> ListMoves { get; private set; }
        public Dot LastMove
        {
            get
            {
                if (last_move == null)//когда выбирается первая точка для хода
                {
                    var random = new Random(DateTime.Now.Millisecond);
                    var q = from Dot d in aDots
                            where d.x <= iBoardWidth / 2 & d.x > iBoardWidth / 3
                                & d.y <= iBoardHeight / 2 & d.y > iBoardHeight / 3
                            orderby (random.Next())
                            select d;

                    last_move = q.First();//это для того чтобы поставить первую точку                
                }
                return last_move;
            }
        }

        public List<Dot> dots_in_region;//записывает сюда точки, которые окружают точку противника
        //=========== цвета, шрифты ===================================================
        public Color colorGamer1 = Colors.Tomato; //Properties.Settings.Default.Color_Gamer1,
        public Color colorGamer2 = Colors.MediumSlateBlue;//Properties.Settings.Default.Color_Gamer2,
        public Color colorCursor = Color.FromArgb(50, 50, 200, 50);// Properties.Settings.Default.Color_Cursor;
        private float PointWidth = 0.20f;
        public Color colorBoard = Color.FromArgb(255, 150, 200, 200);//(Color.DarkSlateBlue, 0.08f);
        public Color colorDrawBrush = Colors.MediumPurple;
        public bool Redraw { get; set; }

        //===============================================================================

        public Point MousePos;

        //statistic
        public float square1;//площадь занятая игроком1
        public float square2;
        public int count_blocked;//счетчик количества окруженных точек
        public int count_blocked1, count_blocked2;
        public int count_dot1, count_dot2;//количество поставленных точек

        private CanvasControl canvas;
        private TextBlock textstatus;


        DispatcherTimer timer = new DispatcherTimer();
        private DateTimeOffset startTime;
        private DateTimeOffset lastTime;

        public int CurrentPlayerMove=2;

#if DEBUG
        //public Form f = new Form2();
#endif

#if DEBUG
        Stopwatch stopWatch = new Stopwatch();//для диагностики времени выполнения
        Stopwatch sW_BM = new Stopwatch();
        Stopwatch sW2 = new Stopwatch();
#endif

        public Game(CanvasControl CanvasCtrl, TextBlock TextBlockCtrl)
        {
            canvas = CanvasCtrl;
            textstatus = TextBlockCtrl;
        }

        public void SetLevel(int iLevel = 1)
        {
            switch (iLevel)
            {
                case 0://easy
                    SkillLevel = 10;
                    SkillDepth = 5;
                    SkillNumSq = 3;
                    break;
                case 1://mid
                    SkillLevel = 30;
                    SkillDepth = 10;//20;
                    SkillNumSq = 4;
                    break;
                case 2://hard
                    SkillLevel = 50;
                    SkillDepth = 50;//50;
                    SkillNumSq = 2;//5;
                    break;
            }
        }
        //  ************************************************
        public Dot PickComputerMove(Dot enemy_move)
        {
            #region если первый ход выбираем произвольную соседнюю точку
            if (ListMoves.Count < 2)
            {
                var random = new Random(DateTime.Now.Millisecond);
                var fm = from Dot d in aDots
                         where d.Own == 0 & Math.Sqrt(Math.Pow(Math.Abs(d.x - enemy_move.x), 2) + Math.Pow(Math.Abs(d.y - enemy_move.y), 2)) < 2
                         orderby random.Next()
                         select d;
                return new Dot(fm.First().x, fm.First().y); //так надо чтобы best_move не ссылался на точку в aDots;
            }
            #endregion
            #region  Если ситуация проигрышная - сдаемся          
            var q1 = from Dot d in aDots
                         //where d.Own == (int)Player.COMPUTER && (d.Blocked == false)
                     where d.Own ==(int) Player.COMPUTER && (d.Blocked == false)
                     select d;
            var q2 = from Dot d in aDots
                     where d.Own ==(int) Player.HUMAN && (d.Blocked == false)
                     select d;
            float res1 = q2.Count();
            float res2 = q1.Count();
            if (res1 / res2 > 2.0)
            {
                return null;
            }

            #endregion


            float s1 = square1; float s2 = square2;
            best_move = null;
            int depth = 0;
            var t1 = DateTime.Now.Millisecond;
#if DEBUG
            stopWatch.Start();
#endif
            Dot lm = new Dot(last_move.x, last_move.y);//точка последнего хода
            //проверяем ход который ведет сразу к окружению и паттерны
            //BestMove(pl1, pl2);
            int c1 = 0, c_root = 1000;// , dpth=0;
            lst_best_move.Clear();

#if DEBUG
                //f.lstDbg2.Items.Clear();
                //f.lstDbg1.Items.Clear();
#endif
            Dot dot1 = null, dot2 = null;
            //(int)Player.HUMAN - ставим в параметр - первым ходит игрок1(человек)
            Play(ref best_move, dot1, dot2, (int)Player.HUMAN, (int)Player.COMPUTER, ref depth, ref c1, lm, ref c_root);
            if (best_move == null)
            {
                //MessageBox.Show("best_move == null");
                var random = new Random(DateTime.Now.Millisecond);
                var q = from Dot d in aDots//любая точка
                        where d.Blocked == false & d.Own == (int)Player.NONE
                        orderby random.Next()
                        select d;

                if (q.Count() > 0) best_move = q.First();
                else return null;
            }

#if DEBUG
            stopWatch.Stop();

            //f.txtDebug.Text = "Skilllevel: " + SkillLevel + "\r\n Общее число ходов: " + depth.ToString() +
            //"\r\n Глубина просчета: " + c_root.ToString() +
            //"\r\n Ход на " + best_move.x + ":" + best_move.y +
            //"\r\n время просчета " + stopWatch.ElapsedMilliseconds.ToString() + " мс";
            stopWatch.Reset();
#endif

            square1 = s1; square2 = s2;

            return new Dot(best_move.x, best_move.y); //так надо чтобы best_move не ссылался на точку в aDots
        }
        //===============================================================================================
        //-----------------------------------Поиск лучшего хода------------------------------------------
        //===============================================================================================
        private Dot BestMove(int pl1, int pl2)
        {
           
            Dot bm;
            SetStatusMsg("CheckMove(pl2,pl1)...");

#if DEBUG
        String strDebug = String.Empty;
        sW2.Start();
        /f.lblBestMove.Text="CheckMove(pl2,pl1)...";
        
#endif

            #region CheckMove - проверяет ход в результате которого окружение.Возвращает ход который завершает окружение
            bm = CheckMove(pl2);
            if (bm != null)
            {
                #region DEBUG
                
#if DEBUG
                {
                    //f.lstDbg2.Items.Add(bm.x +":"+ bm.y  + " player" + pl2 + " - CheckMove!");
                }
#endif
                #endregion
                return bm;
            }
            bm = CheckMove(pl1);
            if (bm != null)
            {
                #region DEBUG
#if DEBUG
                {
                    //f.lstDbg2.Items.Add(bm.x + ":" + bm.y + " player" + pl1 + " - CheckMove!");
                }
#endif
                #endregion
                return bm;
            }

#if DEBUG
            sW2.Stop();
            strDebug = "CheckMove pl1,pl2 - " + sW2.Elapsed.Milliseconds.ToString();
            //f.txtBestMove.Text = strDebug;
            sW2.Reset();
            //проверяем паттерны
            sW2.Start();
            //f.lblBestMove.Text = "CheckPattern2Move проверяем ходы на два вперед...";
            
#endif
            #endregion
            SetStatusMsg("CheckPattern2Move проверяем ходы на два вперед...");
            #region CheckPattern2Move проверяем ходы на два вперед
            List<Dot> empty_dots = aDots.EmptyNeibourDots(pl2);
            List<Dot> lst_dots2;

            foreach (Dot dot in empty_dots)
            {
                if (CheckDot(dot, pl2) == false)
                {
                    if (MakeMove(dot, pl2) != 0)
                    {
                        UndoMove(dot);
                        #endregion
                        return dot;
                    }


                    
                }

                lst_dots2 = CheckPattern2Move(pl2);
                foreach (Dot nd in lst_dots2)
                {
                    if (MakeMove(nd, pl2) != 0)
                    {
                        UndoMove(nd);
                        UndoMove(dot);
                        #region DEBUG
#if DEBUG
                        {
                            //f.lstDbg2.Items.Add(dot.x + ":" + dot.y + " player" + pl2 + " - CheckPattern2Move!");
                        }
#endif
                        #endregion
                        return dot;
                    }
                    UndoMove(nd);
                }
                UndoMove(dot);
            }
        
            SetStatusMsg("CheckPattern_vilochka...");
            #region CheckPattern_vilochka
            bm = CheckPattern_vilochka(pl2);
            if (bm != null & aDots.Contains(bm))
            {
                #region DEBUG
#if DEBUG
                {
                    //f.lstDbg2.Items.Add(bm.x + ":" + bm.y + " player" + pl2 + " - CheckPattern_vilochka " + iNumberPattern);
                }
#endif
                #endregion
                return bm;
            }
            bm = CheckPattern_vilochka(pl1);
            if (bm != null & aDots.Contains(bm))
            {
                #region DEBUG
#if DEBUG

                {
                    //f.lstDbg2.Items.Add(bm.x + ":" + bm.y + " player" + pl1 + " - CheckPattern_vilochka " + iNumberPattern);
                }
#endif
                #endregion
                return bm;
            }

#if DEBUG
            sW2.Stop();
            strDebug = strDebug + "\r\nCheckPattern_vilochka - " + sW2.Elapsed.Milliseconds.ToString();
            //f.txtBestMove.Text = strDebug;
            sW2.Reset();
            sW2.Start();
            //f.lblBestMove.Text = "CheckPatternVilkaNextMove...";
            
#endif
            #endregion
            SetStatusMsg("CheckPattern...");
            #region CheckPattern
            bm = CheckPattern(pl2);
            if (bm != null & aDots.Contains(bm))
            {
                #region DEBUG
#if DEBUG
                {
                    //f.lstDbg2.Items.Add(bm.x + ":" + bm.y + " player" + pl2 + " - CheckPattern " + iNumberPattern);
                }
#endif
                #endregion
                if (CheckDot(bm, pl2) == false) return bm;
            }
#if DEBUG
            sW2.Stop();
            strDebug = strDebug + "\r\nCheckPattern(pl2) - " + sW2.Elapsed.Milliseconds.ToString();
            //f.txtBestMove.Text = strDebug;
            sW2.Reset();
            sW2.Start();
            //f.lblBestMove.Text = "CheckPattern(pl1)...";
            
#endif
            #region CheckPatternMove
            bm = CheckPatternMove(pl2);
            if (bm != null & aDots.Contains(bm))
            {
                #region DEBUG
#if DEBUG
                {
                    //f.lstDbg2.Items.Add(bm.x + ":" + bm.y + " player" + pl2 + " - CheckPatternMove " + iNumberPattern);
                }
#endif
                #endregion
                if (CheckDot(bm, pl2) == false) return bm;
            }
            bm = CheckPatternMove(pl1);
            if (bm != null & aDots.Contains(bm))
            {
                #region DEBUG
#if DEBUG
                {
                    //f.lstDbg2.Items.Add(bm.x + ":" + bm.y + " player" + pl2 + " - CheckPatternMove " + iNumberPattern);
                }
#endif
                #endregion
                if (CheckDot(bm, pl1) == false) return bm;
            }
#if DEBUG
            sW2.Stop();
            strDebug = strDebug + "\r\nCheckPatternMove(pl2) - " + sW2.Elapsed.Milliseconds.ToString();
            //f.txtBestMove.Text = strDebug;
            
            sW2.Reset();
#endif

            #endregion

            bm = CheckPattern(pl1);
            if (bm != null & aDots.Contains(bm))
            {
                #region DEBUG
#if DEBUG
                {
                    //f.lstDbg2.Items.Add(bm.x + ":" + bm.y + " player" + pl1 + " - CheckPattern " + iNumberPattern);
                }
#endif
                #endregion
                if (CheckDot(bm, pl2) == false) return bm;
            }

#if DEBUG
            sW2.Stop();
            strDebug = strDebug + "\r\nCheckPattern(pl1) - " + sW2.Elapsed.Milliseconds.ToString();
            //f.txtBestMove.Text = strDebug;
            sW2.Reset();
            sW2.Start();
            //f.lblBestMove.Text = "CheckPatternMove...";
            
#endif
            #endregion
            return null;
        }

        /// <summary>
        /// функция проверяет не делается ли ход в точку, которая на следующем ходу будет окружена 
        /// </summary>
        /// <param name="dot">проверяемая точка</param>
        /// <param name="Plyr">игрок, чья точка проверяется</param>
        /// <returns></returns>
        // 
        //
        private bool CheckDot(Dot dot, int Plyr)
        {
            int res = MakeMove(dot, Plyr);
            int pl = Plyr == (int)Player.COMPUTER ? 1 : 2;
            if (win_player==pl || CheckMove(pl) != null) // первое условие - ход в уже оеруженный регион, второе окружен на следующем ходу
            //if (win_player == pl)
            {
                UndoMove(dot);
                return true; // да будет окружена
            }
            Dot dotEnemy = CheckMove(pl);
            if (dotEnemy != null)
            {
                res = MakeMove(dotEnemy, pl);
                bool flag = dot.Blocked;
                UndoMove(dotEnemy);
                return flag; // да будет окружена
            }
            //нет не будет
            UndoMove(dot);
            return false;
        }
        //==================================================================================================================
        List<Dot> lst_best_move = new List<Dot>();//сюда заносим лучшие ходы
        int res_last_move; //хранит результат хода
                           //===================================================================================================================
        private int Play(ref Dot best_move, Dot move1, Dot move2, int player1, int player2, ref int count_moves,
                               ref int recursion_depth, Dot lastmove, ref int counter_root)//возвращает Owner кто побеждает в результате хода
        {
            #region Debug Skill
#if DEBUG
            //SkillDepth=(int)f.numericUpDown2.Value;
            //SkillNumSq = (int)f.numericUpDown4.Value;
            //SkillLevel = (int)f.numericUpDown3.Value;
#endif
            #endregion
            recursion_depth++;
            if (recursion_depth >= 8)//SkillDepth)
            {
                return 0;
            }
            Dot enemy_move = null;
#if DEBUG
                sW_BM.Start();
#endif
            //проверяем ход который ведет сразу к окружению и паттерны
            best_move = BestMove(player1, player2);
#if DEBUG
                sW_BM.Stop();
                //f.lblBestMove.Text = "BestMove - " + sW_BM.Elapsed.Milliseconds.ToString();
                //
                sW_BM.Reset();
#endif

            if (CheckDot(best_move, player2)) best_move = null;
            if (best_move != null) return (int)Player.COMPUTER;
            var qry = from Dot d in aDots
                      where d.Own == (int)Player.NONE & d.Blocked == false & Math.Abs(d.x - lastmove.x) < SkillNumSq
                                                                    & Math.Abs(d.y - lastmove.y) < SkillNumSq
                      orderby Math.Sqrt(Math.Pow(Math.Abs(d.x - lastmove.x), 2) + Math.Pow(Math.Abs(d.y - lastmove.y), 2))
                      select d;
            Dot[] ad = qry.ToArray();
            int i = ad.Length;
            if (i != 0)
            {
                string sfoo = "";
                #region Cycle
                foreach (Dot d in ad)
                {

                    //player2=1;
                    player2 = player1 == (int)Player.HUMAN ? (int)Player.COMPUTER : (int)Player.HUMAN;
                    //if (count_moves>SkillLevel) break;
                    //**************делаем ход***********************************
                    res_last_move = MakeMove(d, player2);
                    count_moves++;
                    #region проверка на окружение

                    if (win_player == (int)Player.COMPUTER)
                    {
                        best_move = d;
                        UndoMove(d);
                        return (int)Player.COMPUTER;
                    }

                    //если ход в заведомо окруженный регион - пропускаем такой ход
                    if (win_player == (int)Player.HUMAN)
                    {
                        UndoMove(d);
                        continue;
                    }

                    #endregion
                    #region проверяем ход чтобы точку не окружили на следующем ходу
                    sfoo = "CheckMove player" + player1;
                    best_move = CheckMove(player1, false);
                    if (best_move == null)
                    {
                        sfoo = "next move win player" + player2;
                        best_move = CheckMove(player2, false);
                        if (best_move != null)
                        {
                            best_move = d;
                            UndoMove(d);
                            return player2;
                        }
                    }
                    else
                    {
                        UndoMove(d);
                        continue;
                    }
                    #endregion
                    #region Debug statistic
#if DEBUG
                    //if (f.chkMove.Checked) Pause(); //делает паузу если значение поля pause>0
                    //f.lstDbg1.Items.Add(d.Own + " - " + d.x + ":" + d.y);
                    //f.txtDebug.Text = "Общее число ходов: " + count_moves.ToString() +
                    //                   "\r\n Глубина просчета: " + recursion_depth.ToString() +
                    //                   "\r\n проверка вокруг точки " + lastmove +
                    //                   "\r\n move1 " + move1 +
                    //                   "\r\n move2 " + move2 +
                    //                   "\r\n время поиска " + stopWatch.ElapsedMilliseconds;
#endif
                    #endregion
                    //теперь ходит другой игрок ===========================================================================
                    int result = Play(ref enemy_move, move1, move2, player2, player1, ref count_moves, ref recursion_depth, lastmove, ref counter_root);
                    //отменить ход
                    UndoMove(d);
                    recursion_depth--;
#if DEBUG
                    //if (f.lstDbg1.Items.Count > 0) f.lstDbg1.Items.RemoveAt(f.lstDbg1.Items.Count - 1);
#endif
                    if (count_moves > 8)//SkillLevel)
                        return (int)Player.NONE;
                    if (result != 0)
                    {
                        //best_move = enemy_move;
                        best_move = d;
                        return result;
                    }
                    //это конец тела цикла
                }
                #endregion
            }
            return (int)Player.NONE;
        }

        private int FindMove(ref Dot move, Dot last_mv)//возвращает Owner кто побеждает в результате хода
        {
            int depth = 0, counter = 0, counter_root = 1000, own;
            own = (int)Player.HUMAN;//последним ходил игрок
            List<Dot> mvs = new List<Dot>();
            Dot[] ad = null;
            int minX = aDots.MinX();
            int minY = aDots.MinY();
            int maxX = aDots.MaxX();
            int maxY = aDots.MaxY();

            int i = 0;
            do
            {
                if (i == 0)
                {
                    var qry = from Dot d in aDots
                              where d.Own == (int)Player.NONE & d.Blocked == false
                                                        & d.x <= maxX + 1 & d.x >= minX - 1
                                                        & d.y <= maxY + 1 & d.y >= minY - 1
                              orderby d.x
                              select d;
                    ad = qry.ToArray();
                    if (qry.Count() == 0)
                    {
                        foreach (Dot d in mvs)
                        {
                            UndoMove(d);
                        }
                        mvs.Clear();
                        qry = null;
                        i++;
                    }
                }
                else if (i == 1)
                {
                    var qry1 = from Dot d in aDots
                               where d.Own == (int)Player.NONE & d.Blocked == false
                                                         & d.x <= maxX + 1 & d.x >= minX - 1
                                                         & d.y <= maxY + 1 & d.y >= minY - 1
                               orderby d.y descending
                               select d;
                    ad = qry1.ToArray();
                    if (qry1.Count() == 0)
                    {
                        foreach (Dot d in mvs)
                        {
                            UndoMove(d);
                        }
                        mvs.Clear();
                        return 0;
                    }

                }
                depth++;

                if (ad.Length != 0)
                {
                    foreach (Dot d in ad)
                    {
                        counter++;
                        switch (own)
                        {
                            case (int)Player.HUMAN:
                                own = (int)Player.COMPUTER;
                                break;
                            case (int)Player.COMPUTER:
                                own = (int)Player.HUMAN;
                                break;
                        }
                        //ход делает комп, если последним ходил игрок
                        int res_last_move = MakeMove(d, own);
                        mvs.Add(d);
                        //-----показывает проверяемые ходы-----------------------------------------------
#if DEBUG
                        //if (f.chkMove.Checked) Pause();

                        //f.lstDbg1.Items.Add(d.Own + " - " + d.x + ":" + d.y);
                        //f.txtDebug.Text = "Общее число ходов: " + depth.ToString() +
                        //        "\r\n Глубина просчета: " + counter.ToString() +
                        //        "\r\n проверка вокруг точки " + last_move;
#endif
                        //------------------------------------------------------------------------------
                        if (res_last_move != 0 & aDots[d.x, d.y].Blocked)//если ход в окруженный регион
                        {
                            move = null;
                            //UndoMove(d);
                            //return d.Own == (int)Player.HUMAN ? (int)Player.COMPUTER : (int)Player.HUMAN;
                            break;
                        }
                        if (d.Own == 1 & res_last_move != 0)
                        {
                            if (counter < counter_root)
                            {
                                counter_root = counter;
                                move = new Dot(d.x, d.y);
#if DEBUG
                                //f.lstDbg2.Items.Add("Ход на " + move.x + ":" + move.y + "; ход " + counter);
#endif
                            }
                            //UndoMove(d);
                            break;//return (int)Player.HUMAN;//побеждает игрок
                        }
                        else if (d.Own == 2 & res_last_move != 0 | d.Own == 1 & aDots[d.x, d.y].Blocked)
                        {
                            if (counter < counter_root)
                            {
                                counter_root = counter;
                                move = new Dot(d.x, d.y);
#if DEBUG
                                //f.lstDbg2.Items.Add("Ход на " + move.x + ":" + move.y + "; ход " + counter);
#endif
                            }
                            //UndoMove(d);
                            //return (int)Player.COMPUTER;//побеждает компьютер
                            break;
                        }
                        if (depth > SkillLevel * 100)//количество просчитываемых комбинаций
                        {
                            //return (int)Player.NONE;
                            break;
                        }

                    }
                }
            } while (true);

            //return (int)Player.NONE;
        }
        //===============================================================================================================
        private List<Dot> CheckRelation(int index)
        {
            List<Dot> lstDots = new List<Dot>();
            Dot d1, d2;
            var q = from Dot dot in aDots
                    where dot.IndexDot == index & dot.NeiborDots.Count == 1
                    select dot;

            if (q.Count() == 2)
            {
                d1 = q.First();
                d2 = q.Last();
                var qry = from Dot dot in aDots
                          where dot.Own == 0 & aDots.Distance(dot, d1) < 2 & aDots.Distance(dot, d2) < 2
                          select dot;
                return qry.ToList();
            }
            return null;
            //return lstDots;
        }
        public class DotEq : EqualityComparer<Dot>
        {
            public override int GetHashCode(Dot dot)
            {
                int hCode = dot.x ^ dot.y;
                return hCode.GetHashCode();
            }

            public override bool Equals(Dot d1, Dot d2)
            {
                return (d1.x == d2.x & d1.y == d2.y & d1.Rating == d2.Rating);
            }
        }
        private Dot ОбщаяТочкаSNWE(Dot d1, Dot d2)//*1d1* 
        {
            return NeiborDotsSNWE(d1).Intersect(NeiborDotsSNWE(d2), new DotEq()).FirstOrDefault();
        }
        private List<Dot> ОбщаяТочка(Dot d1, Dot d2)
        {
            return NeiborDots(d1).Intersect(NeiborDots(d2), new DotEq()).ToList();
        }
        private List<Dot> NeiborDotsSNWE(Dot dot)//SNWE -S -South, N -North, W -West, E -East
        {
            Dot[] dts = new Dot[4] {
                                    aDots[dot.x + 1, dot.y],
                                    aDots[dot.x - 1, dot.y],
                                    aDots[dot.x, dot.y + 1],
                                    aDots[dot.x, dot.y - 1]
                                    };
            return dts.ToList();
        }
        private List<Dot> NeiborDots(Dot dot)
        {
            Dot[] dts = new Dot[8] {
                                    aDots[dot.x + 1, dot.y],
                                    aDots[dot.x - 1, dot.y],
                                    aDots[dot.x, dot.y + 1],
                                    aDots[dot.x, dot.y - 1],
                                    aDots[dot.x + 1, dot.y + 1],
                                    aDots[dot.x - 1, dot.y - 1],
                                    aDots[dot.x - 1, dot.y + 1],
                                    aDots[dot.x + 1, dot.y - 1]
                                    };
            return dts.ToList();
        }
        //==============================================================================================
        //проверяет ход в результате которого окружение.Возвращает ход который завершает окружение
        private Dot CheckMove(int Owner, bool AllBoard = true)
        {
            List<Dot> happy_dots = new List<Dot>();
            var qry = from Dot d1 in aDots
                      where d1.Own == Owner
                      from Dot d2 in aDots
                      where
                            d2.IndexRelation == d1.IndexRelation
                            && aDots.Distance(d1, d2) > 2
                            && aDots.Distance(d1, d2) < 3
                            && ОбщаяТочка(d1, d2).Where(dt => dt.Own == Owner).Count() == 0
                            ||
                            d2.IndexRelation == d1.IndexRelation
                            && aDots.Distance(d1, d2) == 2
                      from Dot d in aDots
                      where d.ValidMove && aDots.Distance(d, d1) < 2 && aDots.Distance(d, d2) < 2
                                && NeiborDotsSNWE(d).Where(dt => dt.Own == Owner).Count() <= 2
                      select d;

            foreach (Dot d in qry.Distinct(new DotEq()).ToList())
            {
                //делаем ход
                int result_last_move = MakeMove(d, Owner);
#if DEBUG
                //if (f.chkMove.Checked) Pause();
#endif
                //-----------------------------------
                if (result_last_move != 0 & aDots[d.x, d.y].Blocked == false)
                {
                    UndoMove(d);
                    d.CountBlockedDots = result_last_move;
                    happy_dots.Add(d);
                    //break;
                }
                UndoMove(d);
            }

            //выбрать точку, которая максимально окружит
            var x = happy_dots.Where(dd =>
                    dd.CountBlockedDots == happy_dots.Max(dt => dt.CountBlockedDots));

            return x.Count() > 0 ? x.First() : null;
        }
        private Dot CheckMove_old(int Owner, bool AllBoard = true)
        {
            var qry = AllBoard ? from Dot d in aDots
                                 where d.Blocked == false && d.Own == 0 &
aDots[d.x + 1, d.y - 1].Blocked == false & aDots[d.x + 1, d.y + 1].Blocked == false & aDots[d.x + 1, d.y - 1].Own == Owner & aDots[d.x + 1, d.y + 1].Own == Owner
| d.Blocked == false & aDots[d.x, d.y + 1].Blocked == false & aDots[d.x, d.y - 1].Blocked == false & d.Own == 0 & aDots[d.x, d.y - 1].Own == Owner & aDots[d.x, d.y + 1].Own == Owner
| d.Blocked == false & d.Own == 0 & aDots[d.x - 1, d.y - 1].Blocked == false & aDots[d.x - 1, d.y + 1].Blocked == false & aDots[d.x - 1, d.y - 1].Own == Owner & aDots[d.x - 1, d.y + 1].Own == Owner
| d.Blocked == false & d.Own == 0 & aDots[d.x - 1, d.y - 1].Blocked == false & aDots[d.x + 1, d.y + 1].Blocked == false & aDots[d.x - 1, d.y - 1].Own == Owner & aDots[d.x + 1, d.y + 1].Own == Owner
| d.Blocked == false & d.Own == 0 & aDots[d.x - 1, d.y + 1].Blocked == false & aDots[d.x + 1, d.y - 1].Blocked == false & aDots[d.x - 1, d.y + 1].Own == Owner & aDots[d.x + 1, d.y - 1].Own == Owner
| d.Blocked == false & d.Own == 0 & aDots[d.x - 1, d.y].Blocked == false & aDots[d.x + 1, d.y].Blocked == false & aDots[d.x - 1, d.y].Own == Owner & aDots[d.x + 1, d.y].Own == Owner

| d.Blocked == false & aDots[d.x - 1, d.y].Blocked == false & aDots[d.x + 1, d.y - 1].Blocked == false & d.Own == 0 & aDots[d.x - 1, d.y].Own == Owner & aDots[d.x + 1, d.y - 1].Own == Owner
| d.Blocked == false & aDots[d.x - 1, d.y].Blocked == false & aDots[d.x + 1, d.y + 1].Blocked == false & d.Own == 0 & aDots[d.x - 1, d.y].Own == Owner & aDots[d.x + 1, d.y + 1].Own == Owner

| d.Blocked == false & aDots[d.x + 1, d.y].Blocked == false & aDots[d.x - 1, d.y - 1].Blocked == false & d.Own == 0 & aDots[d.x + 1, d.y].Own == Owner & aDots[d.x - 1, d.y - 1].Own == Owner
| d.Blocked == false & aDots[d.x + 1, d.y].Blocked == false & aDots[d.x - 1, d.y + 1].Blocked == false & d.Own == 0 & aDots[d.x + 1, d.y].Own == Owner & aDots[d.x - 1, d.y + 1].Own == Owner

| d.Blocked == false & aDots[d.x, d.y + 1].Blocked == false & aDots[d.x - 1, d.y - 1].Blocked == false & d.Own == 0 & aDots[d.x, d.y + 1].Own == Owner & aDots[d.x - 1, d.y - 1].Own == Owner
| d.Blocked == false & aDots[d.x, d.y - 1].Blocked == false & aDots[d.x - 1, d.y + 1].Blocked == false & d.Own == 0 & aDots[d.x, d.y - 1].Own == Owner & aDots[d.x - 1, d.y + 1].Own == Owner

| d.Blocked == false & aDots[d.x, d.y - 1].Blocked == false & aDots[d.x + 1, d.y + 1].Blocked == false & d.Own == 0 & aDots[d.x, d.y - 1].Own == Owner & aDots[d.x + 1, d.y + 1].Own == Owner
| d.Blocked == false & aDots[d.x, d.y + 1].Blocked == false & aDots[d.x + 1, d.y - 1].Blocked == false & d.Own == 0 & aDots[d.x, d.y + 1].Own == Owner & aDots[d.x + 1, d.y - 1].Own == Owner

| d.Blocked == false & aDots[d.x + 1, d.y + 1].Blocked == false & aDots[d.x - 1, d.y + 1].Blocked == false & d.Own == 0 & aDots[d.x + 1, d.y + 1].Own == Owner & aDots[d.x - 1, d.y + 1].Own == Owner
| d.Blocked == false & aDots[d.x - 1, d.y - 1].Blocked == false & aDots[d.x + 1, d.y - 1].Blocked == false & d.Own == 0 & aDots[d.x - 1, d.y - 1].Own == Owner & aDots[d.x + 1, d.y - 1].Own == Owner

| d.Blocked == false & aDots[d.x + 1, d.y + 1].Blocked == false & aDots[d.x + 1, d.y - 1].Blocked == false & d.Own == 0 & aDots[d.x + 1, d.y + 1].Own == Owner & aDots[d.x + 1, d.y - 1].Own == Owner
| d.Blocked == false & aDots[d.x - 1, d.y - 1].Blocked == false & aDots[d.x - 1, d.y + 1].Blocked == false & d.Own == 0 & aDots[d.x - 1, d.y - 1].Own == Owner & aDots[d.x - 1, d.y + 1].Own == Owner
                                 select d :
                    from Dot d in aDots
                    where d.Own == (int)Player.NONE & d.Blocked == false &
                                            Math.Abs(d.x - LastMove.x) < 2 & Math.Abs(d.y - LastMove.y) < 2
                    select d;

            Dot[] ad = qry.ToArray();
            if (ad.Length != 0)
            {
                foreach (Dot d in ad)
                {
                    //делаем ход
                    int result_last_move = (int)MakeMove(d, Owner);
#if DEBUG
                    //if (f.chkMove.Checked) Pause();
#endif
                    //-----------------------------------
                    if (result_last_move != 0 & aDots[d.x, d.y].Blocked == false)
                    {
                        UndoMove(d);
                        return d;
                    }
                    UndoMove(d);
                }
            }
            return null;
        }

        private Dot CheckPatternVilkaNextMove(int Owner)
        {
            var qry = from Dot d in aDots where d.Own == Owner & d.Blocked == false select d;
            Dot dot_ptn;
            Dot[] ad = qry.ToArray();
            if (ad.Length != 0)
            {
                foreach (Dot d in ad)
                {
                    Dot[] dots = new Dot[8] { aDots[d.x + 1, d.y], aDots[d.x - 1, d.y], aDots[d.x, d.y + 1], aDots[d.x, d.y - 1],
                                              aDots[d.x + 1, d.y+1], aDots[d.x - 1, d.y-1], aDots[d.x-1, d.y + 1], aDots[d.x+1, d.y - 1]};
                    foreach (Dot dot_move in dots)
                    {
                        if (dot_move.Blocked == false & dot_move.Own == 0)
                        {
                            //делаем ход
                            int result_last_move = MakeMove(dot_move, Owner);
                            int pl = Owner == (int)Player.COMPUTER ? (int)Player.HUMAN : (int)Player.COMPUTER;
                            Dot dt = CheckMove(pl, false); // проверка чтобы не попасть в капкан
                            if (dt != null)
                            {
                                UndoMove(dot_move);
                                continue;
                            }
                            dot_ptn = CheckPattern_vilochka(d.Own);
#if DEBUG
                                //if (f.chkMove.Checked) Pause();
#endif
                            //-----------------------------------
                            if (dot_ptn != null & result_last_move == 0)
                            {
                                UndoMove(dot_move);
                                return dot_move;
                                //return dot_ptn;
                            }
                            UndoMove(dot_move);
                        }
                    }
                }
            }
            return null;
        }

        private void CheckNextMoves(Dot dot)
        {

            //foreach (Dot d in qry)
            //    {
            //        //**************делаем ход***********************************
            //        d.Own = dot.Own;
            //        res_last_move = MakeMove(d);

            //    }

        }

        public string Statistic()
        {
            var q5 = from Dot d in aDots where d.Own == 1 select d;
            var q6 = from Dot d in aDots where d.Own == 2 select d;
            var q7 = from Dot d in aDots where d.Own == 1 & d.Blocked select d;
            var q8 = from Dot d in aDots where d.Own == 2 & d.Blocked select d;
            return "Игрок1 окружил точек: " + q8.Count() + "; \r\n" +
              "Игрок1 Захваченая площадь: " + square1 + "; \r\n" +
              "Игрок1 точек поставил: " + q5.Count() + "; \r\n" +
              "Игрок2 окружил точек: " + q7.Count() + "; \r\n" +
              "Игрок2 Захваченая площадь: " + square2 + "; \r\n" +
              "Игрок2 точек поставил: " + q6.Count() + "; \r\n";
        }
        public void Statistic(int x, int y)
        {
            if (aDots.Contains(x, y))
            {
#if DEBUG
                //f.txtDotStatus.Text = "Blocked: " + aDots[x, y].Blocked + "\r\n" +
                //              "BlokingDots.Count: " + aDots[x, y].BlokingDots.Count + "\r\n" +
                //              "NeiborDots.Count: " + aDots[x, y].NeiborDots.Count + "\r\n" +
                //              "Rating: " + aDots[x, y].Rating + "\r\n" +
                //              "IndexDot: " + aDots[x, y].IndexDot + "\r\n" +
                //              "IndexRelation: " + aDots[x, y].IndexRelation + "\r\n" +
                //              "Own: " + aDots[x, y].Own + "\r\n" +
                //              "X: " + aDots[x, y].x + "; Y: " + aDots[x, y].y;
#endif
            }
        }

        public async Task Pause(double sec)
        {
            canvas.Invalidate();
            await Task.Delay(TimeSpan.FromSeconds(sec));
        }
        public void DispatcherTimerSetup()
        {
            timer = new DispatcherTimer();
            timer.Tick += Timer_Tick;
            timer.Interval = new TimeSpan(0, 0, 1);
            startTime = DateTimeOffset.Now;
            lastTime = startTime;
            timer.Start();
        }

        private async void Timer_Tick(object sender, object e)
        {
            //============Ход компьютера=================
            try
            {
                if (CurrentPlayerMove == 2)
                {
                    if (await MoveGamer(2) == 1)
                    {
                        return;
                    }
                }
            }
            catch (InvalidCastException ex)
            {
                SetStatusMsg(ex.Message.ToString());
                return;
            }
        }

        public async Task MoveGamerHuman(TappedRoutedEventArgs e)
        {
            MousePos = TranslateCoordinates(e.GetPosition(canvas));
            Dot dot = new Dot((int)MousePos.X, (int)MousePos.Y);
            if (MousePos.X > startX - 0.5f & MousePos.Y > startY - 0.5f)
            {
                 #region Ходы игроков
                if (aDots[(int)MousePos.X, (int)MousePos.Y].Own > 0)
                {
                    return;//предовращение хода если клик был по занятой точке
                }

                if (CurrentPlayerMove == 1 | CurrentPlayerMove == 0)
                {
                    CurrentPlayerMove = 1;
                    if (await MoveGamer(1, new Dot((int)MousePos.X, (int)MousePos.Y, 1)) > 0)
                    {
                        return;
                    }
                }
                #endregion
            }

        }

        private async Task<int> MoveGamer(int Player, Dot pl_move = null)
        {
            if (gameover)
            {
                return 1;
            }
            if (pl_move == null)
            {
                pl_move = PickComputerMove(LastMove);
            }
            pl_move.Own = Player;

            MakeMove(pl_move, Player);
            ListMoves.Add(pl_move);

            canvas.Invalidate();
            CurrentPlayerMove = Player == 1 ? 2 : 1;

            if (gameover)
            {
                SetStatusMsg("Game over! \r\n" + Statistic());
                //timer.Stop();
                await Pause(5);
                NewGame(iBoardWidth, iBoardHeight);
                SetStatusMsg("New game started!");
                //await Pause(1);
                return 1;
            }

            SetStatusMsg("Move player" + CurrentPlayerMove + "...");

            return 0;
        }
        public void NewGame(int boardWidth, int boardHeight)
        {
            timer = null;//обнуляем таймер
            aDots = new ArrayDots(boardWidth, boardHeight);
            iBoardWidth = boardWidth;
            iBoardHeight = boardHeight;
            lnks = new List<Links>();
            dots_in_region = new List<Dot>();
            ListMoves = new List<Dot>();
            count_dot1 = 0; count_dot2 = 0;
            startX = -0.5f;
            startY = -0.5f;
            square1 = 0; square2 = 0;
            count_blocked1 = 0; count_blocked2 = 0;
            count_blocked = 0;
            SetLevel(3);
            Redraw = true;
            gameover = false;
            DispatcherTimerSetup();
#if DEBUG
        //f.Show();

#endif
            canvas.Invalidate();
        }
        private bool GameOver()
        {
            var qry = from Dot d in aDots
                      where d.Own == (int)Player.NONE & d.Blocked == false
                      select d;
            
            return (qry.Count() == 0);
        }
        private bool DotIsFree(Dot dot, int flg_own)//проверяет заблокирована ли точка. Перед использованием функции надо установить flg_own-владелец проверяемой точки
        {
            dot.Marked = true;

            //if (dot.x == 0 | dot.y == 0 | dot.x == iMapSize - 1 | dot.y == iMapSize - 1)
            if (dot.x == 0 | dot.y == 0 | dot.x == iBoardWidth - 1 | dot.y == iBoardHeight - 1)
            {
                return true;
            }
            Dot[] d = new Dot[4] { aDots[dot.x + 1, dot.y], aDots[dot.x - 1, dot.y], aDots[dot.x, dot.y + 1], aDots[dot.x, dot.y - 1] };
            //--------------------------------------------------------------------------------
            if (flg_own == 0)// если точка не принадлежит никому и рядом есть незаблокированные точки - эта точка считается свободной(незаблокированной)
            {
                var q = from Dot fd in d where fd.Blocked == false select fd;
                if (q.Count() > 0) return true;
            }
            //----------------------------------------------------------------------------------
            for (int i = 0; i < 4; i++)
            {
                if (d[i].Marked == false)
                {
                    if (d[i].Own == 0 | d[i].Own == flg_own | d[i].Own != flg_own & d[i].Blocked & d[i].BlokingDots.Contains(dot) == false)
                    {
                        if (DotIsFree(d[i], flg_own))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        //------------------------------------------------------------------------------------
        /// <summary>
        /// устанавливает связь между двумя точками и возвращает массив связей 
        /// </summary>
        private void LinkDots()
        {
            lnks.Clear();
            var qry = from Dot d1 in aDots
                      where d1.BlokingDots.Count > 0
                      from Dot d2 in aDots
                          //where d2.Own == d1.Own && d1.Blocked == d2.Blocked && d2.BlokingDots.Count > 0
                      where d2.Own == d1.Own && d1.Blocked == d2.Blocked
                      & aDots.Distance(d1, d2) < 2 & aDots.Distance(d1, d2) > 0 && d2.BlokingDots.Count > 0
                      || d2.Own == d1.Own && d1.Blocked == d2.Blocked && aDots.Distance(d1, d2) == 1
                      //|| !d1.Blocked & d2.Blocked & Distance(d1, d2) == 1

                      select new Links(d1, d2);

            var temp = qry.Distinct(new LinksComparer());
            lnks = temp.ToList(); //обновляем основной массив связей - lnks              

            qry = from Links l1 in lnks
                  from Links l2 in lnks
                  where l1.Dot1.Equals(l2.Dot1) && aDots.Distance(l1.Dot2, l2.Dot2) < 2
                  select new Links(l1.Dot2, l2.Dot2);
            lnks.AddRange(qry.ToList());


        }
        private float SquarePolygon(int nBlockedDots, int nRegionDots)
        {
            return nBlockedDots + nRegionDots / 2.0f - 1;//Формула Пика
        }
        private int count_in_region;
        private int count_blocked_dots;
        //=================================================================================================
        public int MakeMove(Dot dot, int Owner = 0)//Основная функция - ход игрока - возвращает количество окруженных точек
        {
            if (aDots.Contains(dot) == false) return 0;
            if (aDots[dot.x, dot.y].Own == 0)//если точка не занята
            {
                if (Owner == 0) aDots.Add(dot, dot.Own);
                else aDots.Add(dot, Owner);
            }
            //--------------------------------
            int res = CheckBlocked(dot.Own);
            //--------------------------------
            var q = from Dot d in aDots where d.Blocked select d;
            count_blocked_dots = q.Count();
            last_move = dot;//зафиксировать последний ход
            if (res != 0)
            {
                LinkDots();
            }
            gameover = GameOver();
            return res;
        }
        private int CheckBlocked(int last_moveOwner = 0)//проверяет блокировку точек, маркирует точки которые блокируют, возвращает количество окруженных точек
        {
            int counter = 0;
            var q = from Dot dots in aDots where dots.Own != 0 | dots.Own == 0 & dots.Blocked select dots;
            Dot[] arrDot = q.ToArray();
            switch (last_moveOwner)
            {
                case 1:
                    IEnumerable<Dot> query1 = arrDot.OrderBy(dot => dot.Own == 1);
                    arrDot = query1.ToArray();
                    break;
                case 2:
                    IEnumerable<Dot> query2 = arrDot.OrderBy(dot => dot.Own == 2);
                    arrDot = query2.ToArray();
                    break;
            }
            lst_blocked_dots.Clear(); lst_in_region_dots.Clear();
            foreach (Dot d in arrDot)
            {
                aDots.UnmarkAllDots();
                if (DotIsFree(d, d.Own) == false)
                {
                    //lst_blocked_dots.Clear(); lst_in_region_dots.Clear();
                    if (d.Own != 0) d.Blocked = true;
                    d.IndexRelation = 0;
                    var q1 = from Dot dots in aDots where dots.BlokingDots.Contains(d) select dots;
                    if (q1.Count() == 0)
                    {
                        aDots.UnmarkAllDots();
                        MarkDotsInRegion(d, d.Own);

                        foreach (Dot dr in lst_in_region_dots)
                        {
                            win_player = dr.Own;
                            count_in_region++;
                            foreach (Dot bd in lst_blocked_dots)
                            {
                                if (bd.Own != 0) counter += 1;
                                if (dr.BlokingDots.Contains(bd) == false & bd.Own != 0 & dr.Own != bd.Own)
                                {
                                    dr.BlokingDots.Add(bd);
                                }
                            }
                        }
                    }
                }
                else
                {
                    d.Blocked = false;
                }
            }
            RescanBlocked();

            if (lst_blocked_dots.Count == 0) win_player = 0;
            return lst_blocked_dots.Count;
        }
        private void RescanBlocked()//функция ресканирует списки блокированных точек и устанавливает статус Blocked у єтих точек
        {
            var q = from Dot d in aDots where d.BlokingDots.Count > 0 select d;
            foreach (Dot _d in q)
            {
                foreach (Dot bl_dot in _d.BlokingDots)
                {
                    bl_dot.Blocked = true;
                }
            }
            ScanBlockedFreeDots();
        }
        private List<Dot> lst_blocked_dots = new List<Dot>();//список блокированных точек
        private List<Dot> lst_in_region_dots = new List<Dot>();//список блокирующих точек
        public bool gameover;

        private void MarkDotsInRegion(Dot blocked_dot, int flg_own)//Ставит InRegion=true точкам которые блокируют заданную в параметре точку
        {
            blocked_dot.Marked = true;
            Dot[] dts = new Dot[4] {aDots[blocked_dot.x + 1, blocked_dot.y], aDots[blocked_dot.x - 1, blocked_dot.y],
                                  aDots[blocked_dot.x, blocked_dot.y + 1], aDots[blocked_dot.x, blocked_dot.y - 1]};
            //добавим точки которые попали в окружение
            if (lst_blocked_dots.Contains(blocked_dot) == false)
            {
                lst_blocked_dots.Add(blocked_dot);
            }
            foreach (Dot _d in dts)
            {
                if (_d.Own != 0 & _d.Blocked == false & _d.Own != flg_own)//_d-точка которая окружает
                {
                    //добавим в коллекцию точки которые окружают
                    if (lst_in_region_dots.Contains(_d) == false) lst_in_region_dots.Add(_d);
                }
                else
                {
                    if (_d.Marked == false & _d.Fixed == false)
                    {
                        _d.Blocked = true;
                        MarkDotsInRegion(_d, flg_own);
                    }
                }
            }
        }
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        private void MakeRating()//возвращает массив вражеских точек вокруг заданной точки
        {
            int res;
            var qd = from Dot dt in aDots where dt.Own != 0 & dt.Blocked == false select dt;
            foreach (Dot dot in qd)
            {
                //if (dot.x > 0 & dot.y > 0 & dot.x < iMapSize - 1 & dot.y < iMapSize - 1)
                if (dot.x > 0 & dot.y > 0 & dot.x < iBoardWidth - 1 & dot.y < iBoardHeight - 1)
                {
                    Dot[] dts = new Dot[4] { aDots[dot.x + 1, dot.y], aDots[dot.x - 1, dot.y], aDots[dot.x, dot.y + 1], aDots[dot.x, dot.y - 1] };
                    res = 0;
                    foreach (Dot item in dts)
                    {
                        if (item.Own != 0 & item.Own != dot.Own) res++;
                        else if (item.Own == dot.Own & item.Rating == 0)
                        {
                            res = -1;
                            break;
                        }
                    }
                    dot.Rating = res + 1;//точка без связей получает рейт 1
                }
            }
        }
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        public void ScanBlockedFreeDots()//сканирует не занятые узлы на предмет блокировки
        {
            var q = from Dot d in aDots where d.Own == (int)Player.NONE && d.Blocked == false select d;
            if (q.Count() == 0) return;
            foreach (Dot dot in q)
            {
                Dot[] dts = new Dot[4] {aDots[dot.x + 1, dot.y], aDots[dot.x - 1, dot.y],
                                        aDots[dot.x, dot.y + 1], aDots[dot.x, dot.y - 1]};
                foreach (Dot neibour_dot in dts)
                {
                    if (neibour_dot.Blocked)
                    {
                        dot.Blocked = true;
                        ScanBlockedFreeDots();
                        break;
                    }
                }
            }

        }
        public void ResizeBoard(int boardWidth, int boardHeight)//изменение размера доски
        {
            NewGame(boardWidth, boardHeight);
            canvas.Invalidate();
        }
        public void UndoMove(int x, int y)//поле отмена хода
        {
            Undo(x, y);
        }
        public void UndoMove(Dot dot)//поле отмена хода
        {
            if (dot != null) Undo(dot.x, dot.y);
        }
        private void Undo(int x, int y)//отмена хода
        {
            List<Dot> bl_dot = new List<Dot>();
            List<Links> ln = new List<Links>();
            if (aDots[x, y].Blocked)//если точка была блокирована, удалить ее из внутренних списков у блокирующих точек
            {
                lst_blocked_dots.Remove(aDots[x, y]);
                bl_dot.Add(aDots[x, y]);
                foreach (Dot d in lst_in_region_dots)
                {
                    d.BlokingDots.Remove(aDots[x, y]);
                }
                count_blocked_dots = CheckBlocked();
            }
            if (aDots[x, y].BlokingDots.Count > 0)
            {
                //снимаем блокировку с точки bd, которая была блокирована UndoMove(int x, int y)
                foreach (Dot d in aDots[x, y].BlokingDots)
                {
                    bl_dot.Add(d);
                }
            }

            foreach (Dot d in bl_dot)
            {
                foreach (Links l in lnks)//подготовка связей которые блокировали точку
                {
                    if (l.Dot1.BlokingDots.Contains(d) | l.Dot2.BlokingDots.Contains(d))
                    {
                        ln.Add(l);
                    }
                }
                //удаляем из списка блокированных точек
                foreach (Dot bd in aDots)
                {
                    if (bd.BlokingDots.Count > 0)
                    {
                        bd.BlokingDots.Remove(d);
                    }
                }
                //восстанавливаем связи у которых одна из точек стала свободной
                var q_lnks = from lnk in lnks
                             where lnk.Dot1.x == d.x & lnk.Dot1.y == d.y | lnk.Dot2.x == d.x & lnk.Dot2.y == d.y
                             select lnk;
                foreach (Links l in q_lnks)
                {
                    l.Dot1.Blocked = false;
                    l.Dot2.Blocked = false;
                }

            }
            //удаляем связи
            foreach (Links l in ln)
            {
                lnks.Remove(l);
            }
            ln = null;
            bl_dot = null;

            aDots.Remove(x, y);
            count_blocked_dots = CheckBlocked();
            ScanBlockedFreeDots();
            aDots.UnmarkAllDots();
            LinkDots();
            last_move = ListMoves.Count == 0 ? null : ListMoves.Last();
        }

        //=========================================================================
#if DEBUG
        //public void MoveDebugWindow(int top, int left, int width)
        //{
        //    f.Top = top;
        //    f.Left = left + width;
        //}
        #region Pattern Editor
        private List<Dot> lstPat;
        public List<Dot> ListPatterns
        {
            get { return lstPat; }
        }

        //public bool Autoplay
        //{
        
        //    //get { return f.rbtnHand.Checked; }
        //    //set { f.rbtnHand.Checked = value; }
        
        //}


        //public bool PE_FirstDot
        //{
        //    get { return f.tlsТочкаОтсчета.Checked; }
        //    set { f.tlsТочкаОтсчета.Checked = value; }
        //}
        //public bool PE_EmptyDot
        //{
        //    get { return f.tlsПустая.Checked; }
        //    set { f.tlsПустая.Checked = value; }

        //}

        //public bool PE_AnyDot
        //{
        //    get { return f.tlsКромеВражеской.Checked; }
        //    set { f.tlsКромеВражеской.Checked = value; }

        //}
        //public bool PE_MoveDot
        //{
        //    get { return f.tlsТочкаХода.Checked; }
        //    set { f.tlsТочкаХода.Checked = value; }

        //}
        //public bool PE_On
        //{
        //    get
        //    {
        //        if (f.tlsEditPattern.Checked & lstPat==null) lstPat = new List<Dot>();
        //        return f.tlsEditPattern.Checked;

        //    }
        //    set { f.tlsEditPattern.Checked = value; }
        //}
        //public void MakePattern()//сохраняет паттерн в текстовое поле
        //{
        //    string s, strdX, strdY, sWhere = "", sMove = "";
        //    int dx, dy, ind;
        //    ind = lstPat.FindIndex(
        //        delegate (Dot dt)
        //        {
        //            return dt.PatternsFirstDot == true;
        //        });
        //    var random = new Random(DateTime.Now.Millisecond);
        //    string n = random.Next(1, 1000).ToString();
        //    for (int i = 0; i < lstPat.Count; i++)
        //    {
        //        string own = "";
        //        if (lstPat[ind].Own == lstPat[i].Own) own = "== Owner";
        //        if (lstPat[ind].Own != lstPat[i].Own) own = "== enemy_own";
        //        if (lstPat[i].Own == 0 & lstPat[i].PatternsAnyDot==false) own = " == 0";
        //        if (lstPat[i].PatternsAnyDot) own = " != enemy_own";

        //        dx = lstPat[i].x - lstPat[ind].x;
        //        if (dx == 0) strdX = "";
        //        else if (dx > 0) strdX = "+" + dx.ToString();
        //        else strdX = dx.ToString();

        //        dy = lstPat[i].y - lstPat[ind].y;
        //        if (dy == 0) strdY = "";
        //        else if (dy > 0) strdY = "+" + dy.ToString();
        //        else strdY = dy.ToString();

        //        if ((dx == 0 & dy == 0) == false) sWhere += " && aDots[d.x" + strdX + ", d.y" + strdY + "].Own " + own + " && aDots[d.x" + strdX + ", d.y" + strdY + "].Blocked == false \r\n";

        //        if (lstPat[i].PatternsMoveDot)
        //        {
        //            sMove = " if (pat" + n + ".Count() > 0) return new Dot(pat" + n + ".First().x" + strdX + "," + "pat" + n + ".First().y" + strdY + ");";
        //        }
        //    }
        //    s = "iNumberPattern = " + n + "; \r\n";
        //    s += "var pat" + n + " = from Dot d in aDots where d.Own == Owner \r\n" + sWhere + "select d; \r\n" + sMove + "\r\n";
        //    n += "_2";
        //    sWhere = ""; sMove = "";
        //    for (int i = 0; i < lstPat.Count ; i++)
        //    {
        //        string own = "";
        //        if (lstPat[ind].Own == lstPat[i].Own) own = "== Owner";
        //        if (lstPat[ind].Own != lstPat[i].Own) own = "== enemy_own";
        //        if (lstPat[i].Own == 0 & lstPat[i].PatternsAnyDot == false) own = " == 0";
        //        if (lstPat[i].PatternsAnyDot) own = " != enemy_own";

        //        dx = lstPat[ind].x - lstPat[i].x;
        //        if (dx == 0) strdX = "";
        //        else if (dx > 0) strdX = "+" + dx.ToString();
        //        else strdX = dx.ToString();

        //        dy = lstPat[ind].y - lstPat[i].y;
        //        if (dy == 0) strdY = "";
        //        else if (dy > 0) strdY = "+" + dy.ToString();
        //        else strdY = dy.ToString();
        //        if ((dx == 0 & dy == 0) == false) sWhere += " && aDots[d.x" + strdX + ", d.y" + strdY + "].Own " + own + " && aDots[d.x" + strdX + ", d.y" + strdY + "].Blocked == false \r\n";
        //        if (lstPat[i].PatternsMoveDot)
        //        {
        //            sMove = " if (pat" + n + ".Count() > 0) return new Dot(pat" + n + ".First().x" + strdX + "," + "pat" + n + ".First().y" + strdY + ");";
        //        }

        //    }
        //    s += "//180 Rotate=========================================================================================================== \r\n";
        //    s += "var pat" + n + " = from Dot d in aDots where d.Own == Owner \r\n" + sWhere + "select d; \r\n" + sMove + "\r\n";
            
        //    n += "_3";
        //    sWhere = ""; sMove = "";
        //    List<Dot> l =RotateMatrix(90);
        //    for (int i = 0; i < l.Count ; i++)
        //    {
        //        string own = "";
        //        if (l[ind].Own == l[i].Own) own = "== Owner";
        //        if (l[ind].Own != l[i].Own) own = "== enemy_own";
        //        if (l[i].Own == 0 & l[i].PatternsAnyDot == false) own = " == 0";
        //        if (l[i].PatternsAnyDot) own = " != enemy_own";

        //        dx = l[ind].x - l[i].x;
        //        if (dx == 0) strdX = "";
        //        else if (dx > 0) strdX = "+" + dx.ToString();
        //        else strdX = dx.ToString();

        //        dy = l[ind].y - l[i].y;
        //        if (dy == 0) strdY = "";
        //        else if (dy > 0) strdY = "+" + dy.ToString();
        //        else strdY = dy.ToString();
        //        if ((dx == 0 & dy == 0) == false) sWhere += " && aDots[d.x" + strdX + ", d.y" + strdY + "].Own " + own + " && aDots[d.x" + strdX + ", d.y" + strdY + "].Blocked == false \r\n";
        //        if (l[i].PatternsMoveDot)
        //        {
        //            sMove = " if (pat" + n + ".Count() > 0) return new Dot(pat" + n + ".First().x" + strdX + "," + "pat" + n + ".First().y" + strdY + ");";
        //        }
        //    }
        //    s += "//--------------Rotate on 90----------------------------------- \r\n";
        //    s += "var pat" + n + " = from Dot d in aDots where d.Own == Owner \r\n" + sWhere + "select d; \r\n" + sMove + "\r\n";
        //    n += "_4";
        //    sWhere = ""; sMove = "";
        //    for (int i = 0; i < l.Count ; i++)
        //    {
        //        string own = "";
        //        if (l[ind].Own == l[i].Own) own = "== Owner";
        //        if (l[ind].Own != l[i].Own) own = "== enemy_own";
        //        if (l[i].Own == 0 & l[i].PatternsAnyDot == false) own = " == 0";
        //        if (l[i].PatternsAnyDot) own = " != enemy_own";

        //        dx = l[i].x - l[ind].x;
        //        if (dx == 0) strdX = "";
        //        else if (dx > 0) strdX = "+" + dx.ToString();
        //        else strdX = dx.ToString();

        //        dy = l[i].y - l[ind].y;
        //        if (dy == 0) strdY = "";
        //        else if (dy > 0) strdY = "+" + dy.ToString();
        //        else strdY = dy.ToString();
        //        if ((dx == 0 & dy == 0) == false) sWhere += " && aDots[d.x" + strdX + ", d.y" + strdY + "].Own " + own + " && aDots[d.x" + strdX + ", d.y" + strdY + "].Blocked == false \r\n";
        //        if (l[i].PatternsMoveDot)
        //        {
        //            sMove = " if (pat" + n + ".Count() > 0) return new Dot(pat" + n + ".First().x" + strdX + "," + "pat" + n + ".First().y" + strdY + ");";
        //        }
        //    }
        //    s += "//--------------Rotate on 90 - 2----------------------------------- \r\n";
        //    s += "var pat" + n + " = from Dot d in aDots where d.Own == Owner \r\n" + sWhere + "select d; \r\n" + sMove + "\r\n";
        //    s += "//============================================================================================================== \r\n";
        //    f.txtDebug.Text = s;
        //    MessageBox.Show("Into clipboard!");
        //    Clipboard.Clear();
        //    Clipboard.SetText(s);

        //    lstPat.Clear();
        //    f.tlsEditPattern.Checked=false;
        //    aDots.UnmarkAllDots();
        //}

        //private List<Dot> RotateMatrix(int ungle)
        //{
        //Array m = new Array[lstPat.Count];
        //List<Dot> l = new List<Dot>(lstPat.Count);
        //    if(ungle==90)
        //    {
        //        foreach(Dot d in lstPat)
        //        {
        //            int x=d.x; 
        //            int y = d.y;
        //            d.x = y; d.y = x;
        //            l.Add(d);
        //        }        
        //    }
        //    return l;
        //}

        #endregion
#endif
        //==========================================================================
        #region SAVE_LOAD Game
        //public string path_savegame = ApplicationData.Current.LocalFolder + @"\dots.dts";
        public async void SaveGame()
        {
            byte[] saveData = new byte[ListMoves.Count*3];
            int i = 0;
            foreach(Dot d in ListMoves)
            {
                saveData[i++] = (byte)d.x;
                saveData[i++] = (byte)d.y;
                saveData[i++] = (byte)d.Own;
                //i++;
            }
            try
            {
                var folder = ApplicationData.Current.LocalFolder;
                var file = await folder.CreateFileAsync(@"\dots.dts", CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteBytesAsync(file, saveData);
            }
            catch (Exception ex)
            {
                MessageDialog d = new MessageDialog(ex.Message + " saveStringToLocalFile");
                await d.ShowAsync();
            }
        }
        //private static async Task<string> readStringFromLocalFile(string filename)
        //{
        //    StorageFolder local = ApplicationData.Current.LocalFolder;
        //    Stream stream = await local.OpenStreamForReadAsync(filename);
        //    string text;
        //    using (StreamReader reader = new StreamReader(stream))
        //    {
        //        text = reader.ReadToEnd();
        //    }
        //    return text;
        //}

        public async void LoadGame()
        {
            aDots.Clear();
            lnks.Clear();
            ListMoves.Clear();
            Dot d = null;
            try
            {
                var folder = ApplicationData.Current.LocalFolder;
                var file = await folder.CreateFileAsync(@"\dots.dts", CreationCollisionOption.OpenIfExists);

                // создаем объект BinaryReader
                BinaryReader reader = new BinaryReader(File.Open(file.Path, FileMode.Open));
                // пока не достигнут конец файла считываем каждое значение из файла
                while (reader.PeekChar() > -1)
                {
                    d = new Dot((int)reader.ReadByte(), (int)reader.ReadByte(), (int)reader.ReadByte());
                    MakeMove(d, d.Own);
                    ListMoves.Add(aDots[d.x, d.y]);
                }
                last_move = d;
                //CheckBlocked();//проверяем блокировку
                LinkDots();//восстанавливаем связи между точками
                RescanBlocked();
                //ScanBlockedFreeDots();
                reader.Dispose();
            }
            catch (Exception ex)
            {
                MessageDialog dlg = new MessageDialog(ex.Message + " LoadGame");
                await dlg.ShowAsync();
            }

        }
        #endregion


        //struct Dots_sg//структура для сохранения игры в файл
        //{
        //    public byte x;
        //    public byte y;
        //    public byte Own;
        //    public Dots_sg(int X, int Y, int Owner)
        //    {
        //        x = (byte)X;
        //        y = (byte)Y;
        //        Own = (byte)Owner;
        //    }
        //}
    }

}
