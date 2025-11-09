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

        public MainWindow()
        {
            InitializeComponent();
            InitWorld();
        }

        private int GetInt(TextBox tb, int min, int max, int fallback)
        {
            if (!int.TryParse(tb.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var v))
                if (!int.TryParse(tb.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
                    v = fallback;
            v = Math.Clamp(v, min, max);
            tb.Text = v.ToString(CultureInfo.CurrentCulture);
            return v;
        }
       
        private double GetDouble(TextBox tb, double min, double max, double fallback)
        {
            if (!double.TryParse(tb.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var v))
                if (!double.TryParse(tb.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                    v = fallback;
            v = Math.Clamp(v, min, max);
            tb.Text = v.ToString(CultureInfo.CurrentCulture);
            return v;
        }

        private void InitWorld()-
        {
            // Зоны Fast Food Simulator
            // Зона заказов (слева)
            var orderArea = new Rectangle
            {
                Width = 150,
                Height = 300,
                Fill = new SolidColorBrush(Color.FromRgb(224, 247, 250)),   // #E0F7FA — светло-голубой
                Stroke = new SolidColorBrush(Color.FromRgb(77, 208, 225)),  // #4DD0E1 — голубая рамка
                StrokeThickness = 3,
                Opacity = 0.8
            };
            Canvas.SetLeft(orderArea, 50); Canvas.SetTop(orderArea, 50); 
            WorldCanvas.Children.Add(orderArea);

            // Кухня (центр)
            var kitchen = new Rectangle
            {
                Width = 200,
                Height = 200,
                Fill = new SolidColorBrush(Color.FromRgb(200, 230, 201)),   // #C8E6C9 — светло-зелёный
                Stroke = new SolidColorBrush(Color.FromRgb(129, 199, 132)), // #81C784 — зелёная рамка
                StrokeThickness = 3,
                Opacity = 0.8
            };
            Canvas.SetLeft(kitchen, 250); Canvas.SetTop(kitchen, 100); WorldCanvas.Children.Add(kitchen);

            // Зона выдачи (справа)
            var serving = new Rectangle
            {
                Width = 150,
                Height = 300,
                Fill = new SolidColorBrush(Color.FromRgb(255, 249, 196)),   // #FFF9C4 — пастельно-жёлтый
                Stroke = new SolidColorBrush(Color.FromRgb(255, 241, 118)), // #FFF176 — жёлтая рамка
                StrokeThickness = 3,
                Opacity = 0.8
            };

            Canvas.SetLeft(serving, 500); Canvas.SetTop(serving, 50); WorldCanvas.Children.Add(serving);

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
                CustomersSpeed = GetDouble(CustomersSpeedInput, 30, 200, 90)
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

        private void OnStatisticsUpdated(object? _, SimulationStatistics s)
        {
            OrderQueueText.Text = s.OrderQueueCount.ToString();

            // Отображаем список готовящихся заказов
            CookingOrderText.Text = s.CurrentCookingOrders.Count > 0
                ? string.Join(", ", s.CurrentCookingOrders)
                : "-";

            WaitingOrdersText.Text = s.WaitingOrdersCount.ToString();

            // Отображаем список готовых заказов
            ReadyOrderText.Text = s.ReadyOrderNumbers.Count > 0
                ? string.Join(", ", s.ReadyOrderNumbers)
                : "-";

            PickupQueueText.Text = s.PickupQueueCount.ToString();
        }

        // кнопки 
        private void OnStartClick(object sender, RoutedEventArgs e)
        {
            engine.Config.CustomersPerMinute = GetInt(CustomersRateInput, 0, 30, 10);
            engine.Config.OrderTakers = GetInt(OrderTakerCountInput, 1, 3, 1);
            engine.Config.Chefs = GetInt(ChefCountInput, 1, 6, 2);
            engine.Config.Servers = 1;
            engine.Config.CookingTime = GetInt(CookTimeInput, 1, 20, 5);
            engine.Config.OrderTakingTime = GetInt(OrderTakingTimeInput, 1, 10, 3); // Добавляем
            engine.Config.CustomersSpeed = GetDouble(CustomersSpeedInput, 30, 200, 90);

            engine.Start();
        }
        private void OnStopClick(object sender, RoutedEventArgs e)
        {
            engine.Stop();
        }

        private void OnResetClick(object sender, RoutedEventArgs e)
        {
            engine.Reset();
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
    }
}