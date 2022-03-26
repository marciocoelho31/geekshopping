using GeekShopping.Email.Messages;
using GeekShopping.Email.Repository;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace GeekShopping.Email.MessageConsumer
{
    public class RabbitMQPaymentConsumer : BackgroundService
    {
        private readonly EmailRepository _repository;
        private IConnection _connection;
        private IModel _channel; // responsavel por consumir a fila

        //private const string ExchangeName = "FanoutPaymentUpdateExchange";
        private const string ExchangeName = "DirectPaymentUpdateExchange";
        private const string PaymentEmailUpdateQueueName = "PaymentEmailUpdateQueueName";

        public RabbitMQPaymentConsumer(EmailRepository repository)
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

            // sem usar exchange
            //_channel.QueueDeclare(queue: "orderpaymentresultqueue", false, false, false, arguments: null);

            // utilizando exchange - fanout
            //_channel.ExchangeDeclare(ExchangeName, ExchangeType.Fanout);
            //queueName = _channel.QueueDeclare().QueueName;  // cria dinamicamente uma fila no RabbitMQ e seta a variavel
            //// binding para o exchange
            //_channel.QueueBind(queueName, ExchangeName, "");

            // utilizando exchange - direct
            _channel.ExchangeDeclare(ExchangeName, ExchangeType.Direct);
            // declarando uma fila
            _channel.QueueDeclare(PaymentEmailUpdateQueueName, false, false, false, null);
            // binding para o exchange
            _channel.QueueBind(PaymentEmailUpdateQueueName, ExchangeName, "PaymentEmail");

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
                  UpdatePaymentResultMessage vo = JsonSerializer.Deserialize<UpdatePaymentResultMessage>(content);

                  // 
                  ProcessLogs(vo).GetAwaiter().GetResult();

                  // removendo a mensagem da lista
                  _channel.BasicAck(evt.DeliveryTag, false);
              };

            // sem usar exchange
            //_channel.BasicConsume("orderpaymentresultqueue", false, consumer);

            // usando exchange - fanout
            //_channel.BasicConsume(queueName, false, consumer);

            // usando exchange - direct
            _channel.BasicConsume(PaymentEmailUpdateQueueName, false, consumer);

            return Task.CompletedTask;

        }

        private async Task ProcessLogs(UpdatePaymentResultMessage message)
        {
            try
            {
                await _repository.LogEmail(message);
            }
            catch (Exception)
            {
                // log exception
                throw;
            }

        }
    }
}
