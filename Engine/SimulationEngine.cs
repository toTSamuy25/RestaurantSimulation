using RestaurantSimulation.Models.Entities;
using RestaurantSimulation.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Threading;

namespace RestaurantSimulation.Engine
{
    public class SimulationEngine
    {
        public SimulationConfig Config { get; }
        private readonly Canvas _canvas;
        private readonly DispatcherTimer _timer;

        // Fast Food Simulator зоны
        public (double X, double Y) EntryPoint = (50, 400);
        public (double X, double Y) ExitPoint = (600, 400);
        public (double X, double Y) OrderAreaPosition = (125, 200);  // Центр зоны заказов
        public (double X, double Y) KitchenPosition = (350, 200);    // Центр кухни
        public (double X, double Y) ServingPosition = (575, 200);    // Центр зоны выдачи

        // Позиции для очередей (чтобы не накладывались)
        private int _orderQueueIndex = 0;
        private int _servingQueueIndex = 0;

        // Актеры Fast Food Simulator
        private readonly List<Customer> _customers = new();
        private readonly List<OrderTaker> _orderTakers = new();
        private readonly List<Chef> _chefs = new();
        private readonly List<Server> _servers = new();
        private readonly Queue<Order> _kitchenOrderQueue = new();
        private readonly Queue<Order> _servingOrderQueue = new();
        private readonly List<Order> _activeOrders = new();

        private double _simulationTime;
        private double _customerSpawnAccumulator;
        private int _nextOrderNumber = 1;

        // События Fast Food Simulator
        public event EventHandler<SimObject>? CustomerSpawned;
        public event EventHandler<SimObject>? OrderTakerSpawned;
        public event EventHandler<SimObject>? ChefSpawned;
        public event EventHandler<SimObject>? ServerSpawned;
        public event EventHandler<CookStateEventArgs>? ChefStatusChanged;
        public event EventHandler<SimObject>? NewOrderTokenCreated;
        public event EventHandler<SimObject>? ReadyDishTokenCreated;
        public event EventHandler<SimObject>? EntityMoved;
        public event EventHandler<SimObject>? EntityRemoved;
        public event EventHandler<SimulationStatistics>? StatisticsUpdated;
        
        public event EventHandler<CustomerOrderEventArgs>? CustomerOrderAssigned;
        public event EventHandler<CustomerOrderEventArgs>? CustomerOrderCompleted;

        public SimulationEngine(Canvas canvas, SimulationConfig config)
        {
            Config = config;
            _canvas = canvas;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            _timer.Tick += OnTick;
        }

        private bool _isRunning = false;

        public void Start()
        {
            if (!_isRunning)
            {
                ResetActors();

                // Спавним принимающих заказы
                for (int i = 0; i < Config.OrderTakers; i++)
                {
                    var orderTaker = new OrderTaker(Guid.NewGuid(), OrderAreaPosition.X - 20 + i * 40, OrderAreaPosition.Y);
                    _orderTakers.Add(orderTaker);
                    OrderTakerSpawned?.Invoke(this, new SimObject(orderTaker.Id, orderTaker.X, orderTaker.Y));
                }

                // Спавним поваров
                for (int i = 0; i < Config.Chefs; i++)
                {
                    var chef = new Chef();
                    var col = i % 2;
                    var row = i / 2;
                    chef.X = KitchenPosition.X - 30 + col * 60;
                    chef.Y = KitchenPosition.Y - 20 + row * 40;
                    _chefs.Add(chef);
                    ChefSpawned?.Invoke(this, new SimObject(chef.Id, chef.X, chef.Y));
                }

                // Спавним серверов
                for (int i = 0; i < Config.Servers; i++)
                {
                    var server = new Server(Guid.NewGuid(), ServingPosition.X - 20 + i * 40, ServingPosition.Y);
                    _servers.Add(server);
                    ServerSpawned?.Invoke(this, new SimObject(server.Id, server.X, server.Y));
                }

                _simulationTime = 0; _customerSpawnAccumulator = 0;
                _isRunning = true;
            }
            _timer.Start();
        }

        public void Stop() => _timer.Stop();

        public void Reset()
        {
            Stop();
            _isRunning = false;
            ResetActors();
            _kitchenOrderQueue.Clear();
            _servingOrderQueue.Clear();
            _activeOrders.Clear();
            _nextOrderNumber = 1;
            PublishStats();
        }

        private void ResetActors()
        {
            foreach (var c in _customers) EntityRemoved?.Invoke(this, new SimObject(c.Id, c.X, c.Y));
            foreach (var ot in _orderTakers) EntityRemoved?.Invoke(this, new SimObject(ot.Id, ot.X, ot.Y));
            foreach (var c in _chefs) EntityRemoved?.Invoke(this, new SimObject(c.Id, c.X, c.Y));
            foreach (var s in _servers) EntityRemoved?.Invoke(this, new SimObject(s.Id, s.X, s.Y));
            _customers.Clear(); _orderTakers.Clear(); _chefs.Clear(); _servers.Clear();
        }

        private void OnTick(object? sender, EventArgs e)
        {
            var dt = 1.0 / 30.0; // 30 FPS
            _simulationTime += dt;

            TrySpawnCustomer(dt);
            StepCustomers(dt);
            StepOrderTakers(dt);
            StepChefs(dt);
            StepServers(dt);
            PublishStats();
        }

        private void TrySpawnCustomer(double dt)
        {
            _customerSpawnAccumulator += dt;
            var spawnInterval = 60.0 / Config.CustomersPerMinute;
            if (_customerSpawnAccumulator >= spawnInterval)
            {
                _customerSpawnAccumulator = 0;
                var queuePosition = GetOrderQueuePosition();
                var customer = new Customer(Guid.NewGuid(), EntryPoint.X, EntryPoint.Y)
                {
                    CurrentStatus = CustomerState.InOrderQueue,
                    MovementRoute = PathFromTo((EntryPoint.X, EntryPoint.Y), queuePosition)
                };
                _customers.Add(customer);
                CustomerSpawned?.Invoke(this, new SimObject(customer.Id, customer.X, customer.Y));
            }
        }

        private void StepCustomers(double dt)
        {
            foreach (var customer in _customers.ToList())
            {
                FollowPath(customer, customer.MovementRoute, Config.CustomersSpeed, dt);

                switch (customer.CurrentStatus)
                {
                    case CustomerState.InOrderQueue:
                        if (customer.MovementRoute.IsDone)
                        {
                            customer.CurrentStatus = CustomerState.Ordering;
                        }
                        break;

                    case CustomerState.Ordering:
                        // Клиент ждет, пока принимающий заказы не обработает его
                        break;

                    case CustomerState.WaitingForOrder:
                        // Клиент ждет готовый заказ - логика перенесена в сервер
                        break;

                    case CustomerState.PickingUpOrder:
                        if (customer.MovementRoute.IsDone)
                        {
                            // Клиент получил заказ и уходит
                            customer.CurrentStatus = CustomerState.Leaving;
                            customer.MovementRoute = PathFromTo((customer.X, customer.Y), ExitPoint);
                        }
                        break;

                    case CustomerState.Leaving:
                        if (customer.MovementRoute.IsDone)
                        {
                            EntityRemoved?.Invoke(this, new SimObject(customer.Id, customer.X, customer.Y));
                            _customers.Remove(customer);
                        }
                        break;
                }
            }
        }

        private void StepOrderTakers(double dt)
        {
            foreach (var orderTaker in _orderTakers)
            {
                FollowPath(orderTaker, orderTaker.MovementRoute, 200, dt);

                if (orderTaker.CurrentStatus == OrderTakerState.Idle)
                {
                    // Ищем клиента в очереди заказов, который еще не назначен официанту
                    var waitingCustomer = _customers.FirstOrDefault(c => c.CurrentStatus == CustomerState.Ordering &&
                        !_orderTakers.Any(ot => ot.AssignedCustomerId == c.Id));
                    if (waitingCustomer != null)
                    {
                        orderTaker.CurrentStatus = OrderTakerState.TakingOrder;
                        orderTaker.AssignedCustomerId = waitingCustomer.Id;
                        orderTaker.AssignedOrderNumber = _nextOrderNumber++;
                        orderTaker.WillBeFreeAtTime = _simulationTime + Config.OrderTakingTime;

                        // Официант идет к позиции перед клиентом
                        var positionInFrontOfCustomer = (waitingCustomer.X - 30, waitingCustomer.Y);
                        orderTaker.MovementRoute = PathFromTo((orderTaker.X, orderTaker.Y), positionInFrontOfCustomer);

                        // НАЗНАЧАЕМ НОМЕР ЗАКАЗА КЛИЕНТУ
                        waitingCustomer.OrderNumber = orderTaker.AssignedOrderNumber;

                        // ВЫЗЫВАЕМ СОБЫТИЕ ДЛЯ ОБНОВЛЕНИЯ ОТОБРАЖЕНИЯ
                        CustomerOrderAssigned?.Invoke(this, new CustomerOrderEventArgs(waitingCustomer.Id, orderTaker.AssignedOrderNumber.Value));
                    }
                }
                else if (orderTaker.CurrentStatus == OrderTakerState.TakingOrder && orderTaker.MovementRoute.IsDone)
                {
                    // Официант дошел до клиента, начинаем отсчет времени приема заказа
                    // Время уже установлено ранее, просто ждем его истечения
                    if (_simulationTime >= orderTaker.WillBeFreeAtTime)
                    {
                        // Создаем заказ после завершения времени приема
                        var order = new Order
                        {
                            OrderNumber = orderTaker.AssignedOrderNumber.Value,
                            CustomerId = orderTaker.AssignedCustomerId.Value,
                            OrderTakerId = orderTaker.Id,
                            CurrentStatus = OrderState.New
                        };
                        _activeOrders.Add(order);

                        // Переходим к обработке заказа (переносу на кухню)
                        orderTaker.CurrentStatus = OrderTakerState.ProcessingOrder;
                        orderTaker.MovementRoute = PathFromTo((orderTaker.X, orderTaker.Y), KitchenPosition);
                    }
                }
                else if (orderTaker.CurrentStatus == OrderTakerState.ProcessingOrder && orderTaker.MovementRoute.IsDone)
                {
                    // Официант доставил заказ к повару - добавляем в очередь кухни
                    var order = _activeOrders.FirstOrDefault(o => o.OrderTakerId == orderTaker.Id && o.CurrentStatus == OrderState.New);
                    if (order != null)
                    {
                        order.CurrentStatus = OrderState.InKitchenQueue;
                        _kitchenOrderQueue.Enqueue(order);
                        NewOrderTokenCreated?.Invoke(this, new SimObject(order.OrderTokenId, KitchenPosition.X, KitchenPosition.Y));
                    }

                    // Клиент переходит в очередь выдачи
                    var customer = _customers.FirstOrDefault(c => c.Id == orderTaker.AssignedCustomerId);
                    if (customer != null)
                    {
                        customer.CurrentStatus = CustomerState.WaitingForOrder;
                        customer.OrderNumber = orderTaker.AssignedOrderNumber;
                        var servingQueuePosition = GetServingQueuePosition();
                        customer.MovementRoute = PathFromTo((customer.X, customer.Y), servingQueuePosition);
                    }

                    // Официант возвращается в зону заказов
                    orderTaker.CurrentStatus = OrderTakerState.ReturningToOrderArea;
                    orderTaker.MovementRoute = PathFromTo((orderTaker.X, orderTaker.Y), OrderAreaPosition);
                    orderTaker.AssignedCustomerId = null;
                    orderTaker.AssignedOrderNumber = null;
                    orderTaker.WillBeFreeAtTime = 0;
                }
                else if (orderTaker.CurrentStatus == OrderTakerState.ReturningToOrderArea && orderTaker.MovementRoute.IsDone)
                {
                    // Официант вернулся в зону заказов и готов к работе
                    orderTaker.CurrentStatus = OrderTakerState.Idle;
                }
            }
        }

        private void StepChefs(double dt)
        {
            foreach (var chef in _chefs)
            {
                if (chef.CurrentStatus == CookState.Idle && _kitchenOrderQueue.Count > 0)
                {
                    var order = _kitchenOrderQueue.Dequeue();
                    order.CurrentStatus = OrderState.Cooking;
                    order.AssignedChefId = chef.Id; // Добавим это свойство в Order
                    chef.CurrentStatus = CookState.Cooking;
                    chef.WillBeFreeAtTime = _simulationTime + Config.CookingTime;
                    order.WillBeCompletedAtTime = chef.WillBeFreeAtTime;
                    ChefStatusChanged?.Invoke(this, new CookStateEventArgs(chef.Id, chef.CurrentStatus));
                }

                if (chef.CurrentStatus == CookState.Cooking && _simulationTime >= chef.WillBeFreeAtTime)
                {
                    chef.CurrentStatus = CookState.Idle;
                    ChefStatusChanged?.Invoke(this, new CookStateEventArgs(chef.Id, chef.CurrentStatus));

                    // Находим все заказы, которые готовил этот повар и которые готовы
                    var readyOrders = _activeOrders
                        .Where(o => o.AssignedChefId == chef.Id &&
                                   o.WillBeCompletedAtTime <= _simulationTime &&
                                   o.CurrentStatus == OrderState.Cooking)
                        .ToList();

                    foreach (var readyOrder in readyOrders)
                    {
                        readyOrder.CurrentStatus = OrderState.Ready;
                        _servingOrderQueue.Enqueue(readyOrder);
                    }
                }
            }
        }

        private void StepServers(double dt)
        {
            foreach (var server in _servers)
            {
                // Серверы не перемещаются, убираем FollowPath

                if (server.CurrentStatus == ServerState.Idle && _servingOrderQueue.Count > 0)
                {
                    // Сервер забирает заказ из очереди выдачи
                    var order = _servingOrderQueue.Dequeue();
                    server.AssignedOrderId = order.Id;
                    server.AnnouncedOrderNumber = order.OrderNumber;
                    server.CurrentStatus = ServerState.AnnouncingOrder;

                    // Сразу создаем токен готового блюда (сервер на месте)
                    var orderObj = _activeOrders.FirstOrDefault(o => o.Id == server.AssignedOrderId);
                    if (orderObj != null)
                    {
                        orderObj.CurrentStatus = OrderState.Announced;
                        orderObj.ServerId = server.Id;
                        ReadyDishTokenCreated?.Invoke(this, new SimObject(orderObj.DishTokenId, ServingPosition.X, ServingPosition.Y));

                        // Клиент сразу переходит забирать заказ
                        var customer = _customers.FirstOrDefault(c => c.OrderNumber == order.OrderNumber);
                        if (customer != null)
                        {
                            customer.CurrentStatus = CustomerState.PickingUpOrder;
                            // Клиент идет к стойке выдачи
                            var pickupPosition = (ServingPosition.X - 30, ServingPosition.Y);
                            customer.MovementRoute = PathFromTo((customer.X, customer.Y), pickupPosition);
                        }
                    }
                }
                else if (server.CurrentStatus == ServerState.AnnouncingOrder)
                {
                    // Клиент забирает заказ
                    var order = _activeOrders.FirstOrDefault(o => o.Id == server.AssignedOrderId);
                    if (order != null)
                    {
                        var customer = _customers.FirstOrDefault(c => c.OrderNumber == order.OrderNumber &&
                            c.CurrentStatus == CustomerState.PickingUpOrder);
                        if (customer != null && Near((customer.X, customer.Y), (ServingPosition.X - 30, ServingPosition.Y)))
                        {
                            order.CurrentStatus = OrderState.PickedUp;
                            server.CurrentStatus = ServerState.HandingOverOrder;
                        }
                    }
                }
                else if (server.CurrentStatus == ServerState.HandingOverOrder)
                {
                    // Заказ передан клиенту - завершаем процесс
                    var order = _activeOrders.FirstOrDefault(o => o.Id == server.AssignedOrderId);
                    if (order != null)
                    {
                        order.CurrentStatus = OrderState.Completed;
                        _activeOrders.Remove(order);

                        // Удаляем токены
                        EntityRemoved?.Invoke(this, new SimObject(order.OrderTokenId, 0, 0));
                        EntityRemoved?.Invoke(this, new SimObject(order.DishTokenId, 0, 0));
                    }

                    server.CurrentStatus = ServerState.Idle;
                    server.AssignedOrderId = null;
                    server.AnnouncedOrderNumber = null;
                }
            }
        }


        private (double X, double Y) GetOrderQueuePosition()
        {
            // Позиции в очереди заказов (слева от зоны заказов)
            var positions = new[]
            {
                (50, 150),   // Первая позиция
                (50, 180),   // Вторая позиция
                (50, 210),   // Третья позиция
                (50, 240),   // Четвертая позиция
                (50, 270)    // Пятая позиция
            };

            var index = _orderQueueIndex % positions.Length;
            _orderQueueIndex++;
            return positions[index];
        }

        private (double X, double Y) GetServingQueuePosition()
        {
            // Позиции в очереди выдачи (справа от зоны выдачи)
            var positions = new[]
            {
                (650, 150),  // Первая позиция
                (650, 180),  // Вторая позиция
                (650, 210),  // Третья позиция
                (650, 240),  // Четвертая позиция
                (650, 270)   // Пятая позиция
            };

            var index = _servingQueueIndex % positions.Length;
            _servingQueueIndex++;
            return positions[index];
        }

        private void FollowPath(SimObject obj, WaypointPath path, double speed, double dt)
        {
            if (path.IsDone) return;
            var target = path.Current!.Value;
            var dx = target.X - obj.X;
            var dy = target.Y - obj.Y;
            var dist = Math.Sqrt(dx * dx + dy * dy);
            var step = speed * dt;
            if (step >= dist || dist < 0.1)
            {
                SetPos(obj, target.X, target.Y);
                path.Advance();
            }
            else
            {
                var k = step / dist;
                SetPos(obj, obj.X + dx * k, obj.Y + dy * k);
            }
        }

        private void SetPos(SimObject obj, double x, double y)
        {
            obj.X = x; obj.Y = y;
            EntityMoved?.Invoke(this, new SimObject(obj.Id, x, y));
        }

        private WaypointPath PathFromTo((double X, double Y) from, (double X, double Y) to)
        {
            var path = new WaypointPath();
            path.Points.Add(from);
            path.Points.Add(to);
            return path;
        }

        private bool Near((double X, double Y) pos1, (double X, double Y) pos2, double threshold = 20)
        {
            var dx = pos1.X - pos2.X;
            var dy = pos1.Y - pos2.Y;
            return Math.Sqrt(dx * dx + dy * dy) <= threshold;
        }

        private void PublishStats()
        {
            var stats = new SimulationStatistics
            {
                OrderQueueCount = _customers.Count(c => c.CurrentStatus == CustomerState.InOrderQueue || c.CurrentStatus == CustomerState.Ordering),
                CurrentCookingOrders = _activeOrders
                    .Where(o => o.CurrentStatus == OrderState.Cooking)
                    .Select(o => o.OrderNumber)
                    .ToList(),
                WaitingOrdersCount = _kitchenOrderQueue.Count,
                ReadyOrderNumbers = _activeOrders
                    .Where(o => o.CurrentStatus == OrderState.Ready || o.CurrentStatus == OrderState.Announced)
                    .Select(o => o.OrderNumber)
                    .ToList(),
                PickupQueueCount = _customers.Count(c => c.CurrentStatus == CustomerState.WaitingForOrder || c.CurrentStatus == CustomerState.PickingUpOrder)
            };
            StatisticsUpdated?.Invoke(this, stats);
        }
        public Customer? GetCustomerById(Guid customerId)
        {
            return _customers.FirstOrDefault(c => c.Id == customerId);
        }
    }
}