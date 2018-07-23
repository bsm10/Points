using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Документацию по шаблону элемента "Пустая страница" см. по адресу http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Points
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Game game;
        int boardWidth = 4;
        int boardHeight = 4;
        private int player_move;//переменная хранит значение игрока который делает ход
        //int game_result;
        DispatcherTimer timer = new DispatcherTimer();

        private DateTimeOffset startTime;
        private DateTimeOffset lastTime;

        //private bool autoplay;

        public MainPage()
        {
            InitializeComponent();
            //double Xres = canvas.ActualWidth;
            //double Yres = canvas.ActualHeight;
            //double scl_coef = canvas.Dpi;//Xres / Yres;
            //pixels = dips * dpi / 96
            //Height = 4 * Yres / 5;
            //Width = Height - 50;

            game = new Game(canvas, boardWidth, boardHeight);
            game.StatusMsg = "Game started!";
            player_move = 2;
            DispatcherTimerSetup();
        }
        public void DispatcherTimerSetup()
        {
            timer = new DispatcherTimer();
            timer.Tick += Timer_Tick;
            timer.Interval = new TimeSpan(0, 0, 1);
            startTime = DateTimeOffset.Now;
            lastTime = startTime;
            timer.Start();
            ////IsEnabled should now be true after calling start
            //TimerLog.Text += "dispatcherTimer.IsEnabled = " + dispatcherTimer.IsEnabled + "\n";
        }

        private async void Timer_Tick(object sender, object e)
        {
            //============Ход компьютера=================
            try {
                if (player_move == 2)
                {
                    if (await MoveGamer(2) == 1)
                    {
                        return;
                    }
                }

            }
            catch (InvalidCastException ex)
            {
                game.StatusMsg = ex.Message.ToString();
                return;
            }
            

        }

        private void canvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            //var session = args.DrawingSession;
            args.DrawingSession.Clear(Colors.White);
            //session.Clear(Colors.White);
            game.DrawGame(sender, args.DrawingSession);
            //game.cnvs = sender;
            //game.drSession = args.DrawingSession;
        }

        private async void canvas_Tapped(object sender, TappedRoutedEventArgs e)
        {
            UIElement q = sender as CanvasControl;
            var mpos = e.GetPosition(q);
            game.MousePos = game.TranslateCoordinates(mpos);
            Dot dot = new Dot((int)game.MousePos.X, (int)game.MousePos.Y);
            if (game.MousePos.X > game.startX - 0.5f & game.MousePos.Y > game.startY - 0.5f)
            {
                switch (e.PointerDeviceType)
                {
                    case Windows.Devices.Input.PointerDeviceType.Mouse:
                        break;
                    case Windows.Devices.Input.PointerDeviceType.Pen:
                        break;
                    case Windows.Devices.Input.PointerDeviceType.Touch:
                        break;
                    default:
                        break;
                }

                #region Ходы игроков
                if (game.aDots[(int)game.MousePos.X, (int)game.MousePos.Y].Own > 0) return;//предовращение хода если клик был по занятой точке

                if (player_move == 1 | player_move == 0)
                {
                    player_move = 1;
                    if (await MoveGamer(1, new Dot((int)game.MousePos.X, (int)game.MousePos.Y, 1)) > 0)
                    {
                        return;
                    }
                }
                #endregion


            }
        }
        private async Task<int> MoveGamer(int Player, Dot pl_move = null)
        {
            if (game.gameover)
            {
                return 1;
            }
            if (pl_move == null)
            {
                pl_move = game.PickComputerMove(game.LastMove);
            }
            //if (pl_move == null)
            //{
            //    //MessageBox.Show("You win!!! \r\n" + game.Statistic());
            //    game.NewGame(boardWidth, boardHeight);
            //    return 1;
            //}
            pl_move.Own = Player;

            game.MakeMove(pl_move, Player);
            game.ListMoves.Add(pl_move);

            canvas.Invalidate();
            player_move = Player == 1 ? 2 : 1;


            if (game.gameover)
            {
                game.StatusMsg = "Game over! \r\n" + game.Statistic();
                await game.Pause(5);
                game = new Game(canvas, boardWidth, boardHeight);
                game.StatusMsg = "New game started!";
                await game.Pause(1);

                return 1;
            }

            game.StatusMsg = "Move player" + player_move + "...";

            return 0;
        }
        private void canvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            UIElement q = sender as CanvasControl;
            PointerPoint ptrPt = e.GetCurrentPoint(q);
            //txtCoordinate.Text = game.TranslateCoordinates(ptrPt.Position).ToString();
        }

        private void NewGame_Tapped(object sender, TappedRoutedEventArgs e)
        {
            game = new Game(canvas, boardWidth, boardHeight);
        }

        private void SaveGame_Tapped(object sender, TappedRoutedEventArgs e)
        {
            game.SaveGame();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            canvas.RemoveFromVisualTree();
            canvas = null;
        }

        private void LoadGame_Tapped(object sender, TappedRoutedEventArgs e)
        {
            game.LoadGame();
            canvas.Invalidate();
        }

        private void Settings_Tapped(object sender, TappedRoutedEventArgs e)
        {

        }
    }
}
