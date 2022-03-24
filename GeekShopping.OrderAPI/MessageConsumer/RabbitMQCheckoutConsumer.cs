using GeekShopping.OrderAPI.Messages;
using GeekShopping.OrderAPI.Model;
using GeekShopping.OrderAPI.Repository;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace GeekShopping.OrderAPI.MessageConsumer
{
    public class RabbitMQCheckoutConsumer : BackgroundService
    {
        private readonly OrderRepository _repository;
        private IConnection _connection;
        private IModel _channel; // responsavel por consumir a fila

        public RabbitMQCheckoutConsumer(OrderRepository repository)
        {
            _repository = repository;

            // criando a ConnectionFactory
            var factory = new ConnectionFactory
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };

            // criando a conexão de fato, com a factory definida acima
            _connection = factory.CreateConnection();

            //definindo o channel que iremos usar
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(queue: "checkoutqueue", false, false, false, arguments: null);

        }

        // responsável por consumir a mensagem na fila
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new EventingBasicConsumer(_channel);

            // efetivamente consumindo a fila
            consumer.Received += (channel, evt) =>
              {
                  // transformando o json em objeto
                  var content = Encoding.UTF8.GetString(evt.Body.ToArray());

                  // deserializando
                  CheckoutHeaderVO vo = JsonSerializer.Deserialize<CheckoutHeaderVO>(content);

                  // 
                  ProcessOrder(vo).GetAwaiter().GetResult();

                  // removendo a mensagem da lista
                  _channel.BasicAck(evt.DeliveryTag, false);
              };

            _channel.BasicConsume("checkoutqueue", false, consumer);
            return Task.CompletedTask;

        }

        private async Task ProcessOrder(CheckoutHeaderVO vo)
        {
            // convertendo de CheckoutHeaderVO para OrderHeader
            // (neste caso não é uma boa ideia usar o AutoMapper)
            OrderHeader order = new OrderHeader()
            {
                UserId = vo.UserId,
                FirstName = vo.FirstName,
                LastName = vo.LastName,
                OrderDetails = new List<OrderDetail>(),
                CardNumber = vo.CardNumber,
                CouponCode = vo.CouponCode,
                CVV = vo.CVV,
                DiscountAmount = vo.DiscountAmount,
                //CartTotalItens = vo.CartTotalItens,
                Email = vo.Email,
                ExpiryMonthYear = vo.ExpiryMonthYear,
                OrderTime = DateTime.Now,
                PurchaseAmount = vo.PurchaseAmount,
                PaymentStatus = false,
                Phone = vo.Phone,
                DateTime = vo.DateTime
            };

            foreach (var details in vo.CartDetails)
            {
                OrderDetail detail = new OrderDetail()
                {
                    ProductId = details.ProductId,
                    ProductName = details.Product.Name,
                    Price = details.Product.Price,
                    Count = details.Count
                };

                order.CartTotalItens += details.Count;
                order.OrderDetails.Add(detail);
            }

            await _repository.AddOrder(order);
        }
    }
}
