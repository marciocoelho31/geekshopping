using GeekShopping.OrderAPI.Messages;
using GeekShopping.OrderAPI.Model;
using GeekShopping.OrderAPI.RabbitMQSender;
using GeekShopping.OrderAPI.Repository;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace GeekShopping.OrderAPI.MessageConsumer
{
    public class RabbitMQPaymentConsumer : BackgroundService
    {
        private readonly OrderRepository _repository;
        private IConnection _connection;
        private IModel _channel; // responsavel por consumir a fila

        public RabbitMQPaymentConsumer(OrderRepository repository)
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
            _channel.QueueDeclare(queue: "orderpaymentresultqueue", false, false, false, arguments: null);

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
                  UpdatePaymentResultVO vo = JsonSerializer.Deserialize<UpdatePaymentResultVO>(content);

                  // 
                  UpdatePaymentStatus(vo).GetAwaiter().GetResult();

                  // removendo a mensagem da lista
                  _channel.BasicAck(evt.DeliveryTag, false);
              };

            _channel.BasicConsume("orderpaymentresultqueue", false, consumer);
            return Task.CompletedTask;

        }

        private async Task UpdatePaymentStatus(UpdatePaymentResultVO vo)
        {
            try
            {
                await _repository.UpdateOrderPaymentStatus(vo.OrderId, vo.Status);
            }
            catch (Exception)
            {
                // log exception
                throw;
            }

        }
    }
}
