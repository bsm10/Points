using Microsoft.Graphics.Canvas.UI.Xaml;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// Документацию по шаблону элемента "Пустая страница" см. по адресу http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Points
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Game game;

        public MainPage()
        {
            InitializeComponent();
            game = new Game(canvas, textstatus);
            game.NewGame(4, 4);
            game.SetStatusMsg("Game started!");
        }

        private void Canvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            args.DrawingSession.Clear(Colors.White);
            game.DrawGame(sender, args.DrawingSession);
        }

        private async void Canvas_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await game.MoveGamerHuman(e);
        }

        private void Canvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            //UIElement q = sender as CanvasControl;
            //PointerPoint ptrPt = e.GetCurrentPoint(q);
        }

        private void NewGame_Tapped(object sender, TappedRoutedEventArgs e)
        {
            //game = new Game(canvas, boardWidth, boardHeight);
            game.NewGame(game.iBoardWidth, game.iBoardHeight);
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
    }
}
