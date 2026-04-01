using OGLonOTK.Core;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OGLonOTK
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            TB.Text = "Управление следующее:\n\n   WASD - движение камеры/дрона по осям X/Z\n   Q/E - поворот дрона\n   Shift/Space - опустить/поднять камеру/дрона\n   F - захват груза\n   B - в(ы)ключить блюр\n   F1/2/3 - Переключение режима камеры: Следование/Орбита/Свободная камера\n   ESC - выход из игрового окна";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var nativeWindowSettings = new NativeWindowSettings()
            {
                ClientSize = new Vector2i(1366, 720),
                Title = "LearnOpenTK - Creating a Window",
                Flags = ContextFlags.ForwardCompatible,
                Vsync = VSyncMode.On
            };
            using (var game = new Game(GameWindowSettings.Default, nativeWindowSettings))
            {
                game.Run();
            }
        }
    }
}