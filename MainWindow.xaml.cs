using RestaurantSimulation.Engine;
using RestaurantSimulation.Models.Entities;
using RestaurantSimulation.Models.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;


// some changes

namespace RestaurantSimulation
{
    public partial class MainWindow : Window
    {
        private SimulationEngine engine = null!;
        private readonly Dictionary<Guid, UIElement> _customerShapes = new();
        private readonly Dictionary<Guid, UIElement> _waiterShapes = new();
        private readonly Dictionary<Guid, UIElement> _chefShapes = new();

        // два  токена
        private readonly Dictionary<Guid, UIElement> _newOrderTokenShapes = new(); // зелёный  у стола
        private readonly Dictionary<Guid, UIElement> _readyDishTokenShapes = new(); // красный заказ

        // Внутри класса MainWindow, рядом с другими private полями
        private double _animationSpeed = 1.0; // множитель скорости анимации
        private double _animationInterval = 1.0; // интервал в секундах, введённый пользователем







        public MainWindow()
        {
            InitializeComponent();
            InitWorld();
        }

        private int GetInt(TextBox tb, int min, int max, int fallback)
        {
            if (!int.TryParse(tb.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var v) &&
                !int.TryParse(tb.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
            {
                v = fallback;
            }

            // 🔸 Никаких ограничений
            tb.Text = v.ToString(CultureInfo.CurrentCulture);
            return v;
        }

        private double GetDouble(TextBox tb, double min, double max, double fallback)
        {
            if (!double.TryParse(tb.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var v) &&
                !double.TryParse(tb.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
            {
                v = fallback;
            }

            // 🔸 Убираем Math.Clamp — теперь можно вводить любое значение
            tb.Text = v.ToString(CultureInfo.CurrentCulture);
            return v;
        }



        private void InitWorld()
        {
            // Зоны Fast Food Simulator
            // Зона заказов (слева)
            // === Зона заказов ===
            var orderArea = new Rectangle
            {
                Width = 150,
                Height = 300,
                Fill = new SolidColorBrush(Color.FromRgb(255, 220, 150)), // светло-оранжевый
                Stroke = new SolidColorBrush(Color.FromRgb(255, 167, 38)),
                StrokeThickness = 3,
                Opacity = 0.6
            };
            Canvas.SetLeft(orderArea, 50);
            Canvas.SetTop(orderArea, 100);
            WorldCanvas.Children.Add(orderArea);

            // Текст для зоны заказов
            var orderLabel = new TextBlock
            {
                Text = "🟨 Зона заказов",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(80, 60, 0))
            };
            Canvas.SetLeft(orderLabel, 60);
            Canvas.SetTop(orderLabel, 80);
            WorldCanvas.Children.Add(orderLabel);


            // === Кухня ===
            var kitchen = new Rectangle
            {
                Width = 200,
                Height = 200,
                Fill = new SolidColorBrush(Color.FromRgb(180, 180, 255)), // светло-фиолетовый
                Stroke = new SolidColorBrush(Color.FromRgb(108, 92, 231)),
                StrokeThickness = 3,
                Opacity = 0.7
            };
            Canvas.SetLeft(kitchen, 250);
            Canvas.SetTop(kitchen, 100);
            WorldCanvas.Children.Add(kitchen);

            // Текст для кухни
            var kitchenLabel = new TextBlock
            {
                Text = "🟪 Кухня",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(40, 40, 120))
            };
            Canvas.SetLeft(kitchenLabel, 270);
            Canvas.SetTop(kitchenLabel, 80);
            WorldCanvas.Children.Add(kitchenLabel);


            // === Зона выдачи (сервер) ===
            var serving = new Rectangle
            {
                Width = 150,
                Height = 300,
                Fill = new SolidColorBrush(Color.FromRgb(255, 200, 200)), // светло-красный
                Stroke = new SolidColorBrush(Color.FromRgb(214, 48, 49)),
                StrokeThickness = 3,
                Opacity = 0.7
            };
            Canvas.SetLeft(serving, 500);
            Canvas.SetTop(serving, 100);
            WorldCanvas.Children.Add(serving);

            // Текст для зоны выдачи
            var servingLabel = new TextBlock
            {
                Text = "🟥 Зона выдачи",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(120, 0, 0))
            };
            Canvas.SetLeft(servingLabel, 510);
            Canvas.SetTop(servingLabel, 80);
            WorldCanvas.Children.Add(servingLabel);


            WorldCanvas.Children.Add(Marker(30, 400, Brushes.Lime, "ВХОД"));
            WorldCanvas.Children.Add(Marker(600, 400, Brushes.Tomato, "ВЫХОД"));

            engine = new SimulationEngine(WorldCanvas, new SimulationConfig
            {
                CustomersPerMinute = GetInt(CustomersRateInput, 0, 30, 10),
                OrderTakers = GetInt(OrderTakerCountInput, 1, 3, 1),
                Chefs = GetInt(ChefCountInput, 1, 6, 2),
                Servers = 1,
                CookingTime = GetInt(CookTimeInput, 1, 20, 5),
                OrderTakingTime = GetInt(OrderTakingTimeInput, 1, 10, 3), // Добавляем
                CustomersSpeed = GetDouble(CustomersSpeedInput, 0.1, 10, 1)
            });

            // вешаем обработчики
            engine.CustomerSpawned += OnCustomerSpawned;
            engine.OrderTakerSpawned += OnOrderTakerSpawned;
            engine.ChefSpawned += OnChefSpawned;
            engine.ServerSpawned += OnServerSpawned;
            engine.NewOrderTokenCreated += OnNewOrderTokenCreated;
            engine.ReadyDishTokenCreated += OnReadyDishTokenCreated;
            engine.EntityMoved += OnEntityMoved;
            engine.EntityRemoved += OnEntityRemoved;
            engine.StatisticsUpdated += OnStatisticsUpdated;
            engine.ChefStatusChanged += OnChefStatusChanged;

            engine.CustomerOrderAssigned += OnCustomerOrderAssigned;
            engine.CustomerOrderCompleted += OnCustomerOrderCompleted;
        }

        private void OnCustomerSpawned(object? _, SimObject e)
        {
            // Находим клиента в списке чтобы получить его данные
            var customer = engine.GetCustomerById(e.Id);
            if (customer == null) return;

            var textBlock = new TextBlock
            {
                Text = customer.DisplayText, // Используем DisplayText вместо "К"
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var border = new Border
            {
                Width = 20,
                Height = 20,
                Background = Brushes.DarkBlue,
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Child = textBlock
            };
            _customerShapes[e.Id] = border;
            WorldCanvas.Children.Add(border);
            Canvas.SetLeft(border, e.X);
            Canvas.SetTop(border, e.Y);
        }

        private void OnOrderTakerSpawned(object? _, SimObject e)
        {
            var textBlock = new TextBlock
            {
                Text = "О",
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var border = new Border
            {
                Width = 20,
                Height = 20,
                Background = Brushes.Green,
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Child = textBlock
            };
            _waiterShapes[e.Id] = border; WorldCanvas.Children.Add(border);
            Canvas.SetLeft(border, e.X); Canvas.SetTop(border, e.Y);
        }

        private void OnServerSpawned(object? _, SimObject e)
        {
            var textBlock = new TextBlock
            {
                Text = "С",
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var border = new Border
            {
                Width = 20,
                Height = 20,
                Background = Brushes.Orange,
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Child = textBlock
            };
            _waiterShapes[e.Id] = border; WorldCanvas.Children.Add(border);
            Canvas.SetLeft(border, e.X); Canvas.SetTop(border, e.Y);
        }

        private void OnChefSpawned(object? _, SimObject e)
        {
            var textBlock = new TextBlock
            {
                Text = "П",
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var border = new Border
            {
                Width = 20,
                Height = 20,
                Background = Brushes.Purple,
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Child = textBlock
            };
            _chefShapes[e.Id] = border; WorldCanvas.Children.Add(border);
            Canvas.SetLeft(border, e.X); Canvas.SetTop(border, e.Y);
        }

        private void OnChefStatusChanged(object? _, CookStateEventArgs e)
        {
            if (_chefShapes.TryGetValue(e.CookId, out var shape) && shape is Border border)
            {
                if (e.State == CookState.Cooking)
                {
                    border.Background = Brushes.DarkGoldenrod; // Золотистый когда готовит
                    }
                    else
                    {
                    border.Background = Brushes.Purple; // Фиолетовый когда свободен
                }
            }
        }

        // Красный квадрат = новый заказ (несделанный)
        private void OnNewOrderTokenCreated(object? _, SimObject e)
        {
            var shape = new Rectangle
            {
                Width = 14,
                Height = 14,
                Fill = Brushes.Crimson,
                Stroke = Brushes.DarkRed,
                StrokeThickness = 2
            };
            _newOrderTokenShapes[e.Id] = shape;
            WorldCanvas.Children.Add(shape);
            Canvas.SetLeft(shape, e.X); Canvas.SetTop(shape, e.Y);
        }

        // Зеленый квадрат = готовый заказ (сделанный)
        private void OnReadyDishTokenCreated(object? _, SimObject e)
        {
            var shape = new Rectangle
            {
                Width = 14,
                Height = 14,
                Fill = Brushes.LimeGreen,
                Stroke = Brushes.DarkGreen,
                StrokeThickness = 2
            };
            _readyDishTokenShapes[e.Id] = shape;
            WorldCanvas.Children.Add(shape);
            Canvas.SetLeft(shape, e.X); Canvas.SetTop(shape, e.Y);
        }


        private void OnEntityMoved(object? _, SimObject e)
        {
            if (_customerShapes.TryGetValue(e.Id, out var c))
            {
                Canvas.SetLeft(c, e.X);
                Canvas.SetTop(c, e.Y);

                // ОБНОВЛЯЕМ ТЕКСТ ПРИ ПЕРЕМЕЩЕНИИ (на случай если номер изменился)
                UpdateCustomerDisplay(e.Id);
            }
            if (_waiterShapes.TryGetValue(e.Id, out var w))
            {
                Canvas.SetLeft(w, e.X);
                Canvas.SetTop(w, e.Y);
            }

            if (_newOrderTokenShapes.TryGetValue(e.Id, out var green))
            {
                Canvas.SetLeft(green, e.X);
                Canvas.SetTop(green, e.Y);
            }
            if (_readyDishTokenShapes.TryGetValue(e.Id, out var red))
            {
                Canvas.SetLeft(red, e.X);
                Canvas.SetTop(red, e.Y);
            }
        }

        private void OnEntityRemoved(object? _, SimObject e)
        {
            if (_customerShapes.Remove(e.Id, out var c)) WorldCanvas.Children.Remove(c);
            if (_waiterShapes.Remove(e.Id, out var w)) WorldCanvas.Children.Remove(w);
            if (_newOrderTokenShapes.Remove(e.Id, out var go)) WorldCanvas.Children.Remove(go);
            if (_readyDishTokenShapes.Remove(e.Id, out var ro)) WorldCanvas.Children.Remove(ro);
        }


        private const int MaxOrderQueue = 4; // порог перегрузки
        private bool _queueOverloadNotified = false;

        private void OnStatisticsUpdated(object? _, SimulationStatistics s)
        {
            // Обновляем UI
            OrderQueueText.Text = s.OrderQueueCount.ToString();

            CookingOrderText.Text = s.CurrentCookingOrders.Count > 0
                ? string.Join(", ", s.CurrentCookingOrders)
                : "-";

            WaitingOrdersText.Text = s.WaitingOrdersCount.ToString();

            ReadyOrderText.Text = s.ReadyOrderNumbers.Count > 0
                ? string.Join(", ", s.ReadyOrderNumbers)
                : "-";

            PickupQueueText.Text = s.PickupQueueCount.ToString();

            // Проверка перегрузки очереди заказов
            if (!_queueOverloadNotified && s.OrderQueueCount > MaxOrderQueue)
            {
                _queueOverloadNotified = true;

                // Останавливаем симуляцию
                engine?.Stop();

                // Показываем предупреждение
                MessageBox.Show(this,
                    $"Очередь заказов ({s.OrderQueueCount}) превысила порог {MaxOrderQueue}. Симуляция остановлена. Иди нафиг",
                    "Перегрузка очереди",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }




        // кнопки 
        private void OnStartClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // Берём значения напрямую, без ограничений
                engine.Config.CustomersPerMinute = GetInt(CustomersRateInput, 0, 0, 0);
                engine.Config.OrderTakers = GetInt(OrderTakerCountInput, 0, 0, 0);
                engine.Config.Chefs = GetInt(ChefCountInput, 0, 0, 0);
                engine.Config.Servers = 1;
                engine.Config.CookingTime = GetInt(CookTimeInput, 0, 0, 0);
                engine.Config.OrderTakingTime = GetInt(OrderTakingTimeInput, 0, 0, 0);
                engine.Config.CustomersSpeed = GetDouble(CustomersSpeedInput, 0, 0, 0);

                engine.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запуске симуляции: {ex.Message}");
            }
        }



        private void OnStopClick(object sender, RoutedEventArgs e)
        {
            engine.Stop();
        }

        private void OnResetClick(object sender, RoutedEventArgs e)
        {
            engine.Reset();
            _queueOverloadNotified = false;
            WorldCanvas.Children.Clear();
            _customerShapes.Clear(); _waiterShapes.Clear();
            _newOrderTokenShapes.Clear(); _readyDishTokenShapes.Clear();
            _chefShapes.Clear();
            InitWorld();
        }
        private void OnCustomerOrderAssigned(object? sender, CustomerOrderEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateCustomerDisplay(e.CustomerId);
            });
        }

        private void OnCustomerOrderCompleted(object? sender, CustomerOrderEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateCustomerDisplay(e.CustomerId);
            });
        }


        private UIElement Marker(double x, double y, Brush brush, string text)
        {
            var s = new StackPanel { Orientation = Orientation.Vertical };
            s.Children.Add(new Ellipse { Width = 10, Height = 10, Fill = brush });
            s.Children.Add(new TextBlock { Text = text, FontSize = 10 });
            Canvas.SetLeft(s, x); Canvas.SetTop(s, y);
            return s;
        }

        private void UpdateCustomerDisplay(Guid customerId)
        {
            if (_customerShapes.TryGetValue(customerId, out var shape) && shape is Border border)
            {
                if (border.Child is TextBlock textBlock)
                {
                    var customer = engine.GetCustomerById(customerId);
                    if (customer != null)
                    {
                        // Обновляем текст в кружке клиента
                        textBlock.Text = customer.DisplayText;
                    }
                }
            }
        }


        private void UpdateAnimationSpeed()
        {
            // Пример: вводим "секунды на путь" -> чем меньше значение, тем быстрее движение
            double inputSeconds = GetDouble(CustomersSpeedInput, 0.1, 10, 1); // 0.1–10 секунд
                                                                              // Переводим в множитель скорости (чем меньше inputSeconds, тем больше _animationSpeed)
            _animationSpeed = 1.0 / inputSeconds;
        }
    }

}


