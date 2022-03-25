using GeekShopping.PaymentAPI.Messages;
using GeekShopping.PaymentAPI.RabbitMQSender;
using GeekShopping.PaymentProcessor;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace GeekShopping.PaymentAPI.MessageConsumer
{
    public class RabbitMQPaymentConsumer : BackgroundService
    {
        private IConnection _connection;
        private IModel _channel; // responsavel por consumir a fila
        private IRabbitMQMessageSender _rabbitMQMessageSender;
        private readonly IProcessPayment _processPayment;

        public RabbitMQPaymentConsumer(IProcessPayment processPayment, IRabbitMQMessageSender rabbitMQMessageSender)
        {
            _processPayment = processPayment;
            _rabbitMQMessageSender = rabbitMQMessageSender;

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
            _channel.QueueDeclare(queue: "orderpaymentprocessqueue", false, false, false, arguments: null);

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
                  PaymentMessage vo = JsonSerializer.Deserialize<PaymentMessage>(content);

                  // 
                  ProcessPayment(vo).GetAwaiter().GetResult();

                  // removendo a mensagem da lista
                  _channel.BasicAck(evt.DeliveryTag, false);
              };

            _channel.BasicConsume("orderpaymentprocessqueue", false, consumer);
            return Task.CompletedTask;

        }

        private async Task ProcessPayment(PaymentMessage vo)
        {
            var result = _processPayment.PaymentProcessor();    // processa pagto e retorna true ou false
            UpdatePaymentResultMessage paymentResultMessage = new UpdatePaymentResultMessage
            {
                Status = result,
                OrderId = vo.OrderId,
                Email = vo.Email
            };

            // publicando a mensagem de payment no RabbitMQ - fila 'orderpaymentresultqueue'
            try
            {
                _rabbitMQMessageSender.SendMessage(paymentResultMessage, "orderpaymentresultqueue");
            }
            catch (Exception)
            {
                // log exception
                throw;
            }

        }
    }
}
