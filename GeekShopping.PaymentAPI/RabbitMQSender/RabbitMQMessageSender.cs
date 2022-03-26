using GeekShopping.PaymentAPI.Messages;
using GeekShopping.MessageBus;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace GeekShopping.PaymentAPI.RabbitMQSender
{
    public class RabbitMQMessageSender : IRabbitMQMessageSender
    {
        // TODO colocar depois no appsettings
        private readonly string _hostName;
        private readonly string _password;
        private readonly string _userName;
        private IConnection _connection;

        //private const string ExchangeName = "FanoutPaymentUpdateExchange";

        private const string ExchangeName = "DirectPaymentUpdateExchange";
        private const string PaymentEmailUpdateQueueName = "PaymentEmailUpdateQueueName";
        private const string PaymentOrderUpdateQueueName = "PaymentOrderUpdateQueueName";

        public RabbitMQMessageSender()
        {
            _hostName = "localhost";
            _password = "guest";
            _userName = "guest";
        }

        // envio ao RabbitMQ sem usar exchange
        //public void SendMessage(BaseMessage message, string queueName)
        //{
        //    // criando a conexão com o RabbitMQ
        //    if (ConnectionExists())
        //    {
        //        //definindo o channel que iremos usar
        //        using var channel = _connection.CreateModel();
        //        channel.QueueDeclare(queue: queueName, false, false, false, arguments: null);

        //        // pegando a message que recebemos como parâmetro e convertendo em um array de bytes
        //        byte[] body = GetMessageAsByteArray(message);

        //        // publicando a mensagem
        //        channel.BasicPublish(
        //            exchange: "", // <---------------------
        //            routingKey: queueName,
        //            basicProperties: null,
        //            body: body);
        //    }
        //}

        // com exchange fanout
        //public void SendMessage(BaseMessage message)
        //{
        //    // criando a conexão com o RabbitMQ
        //    if (ConnectionExists())
        //    {
        //        //definindo o channel que iremos usar
        //        using var channel = _connection.CreateModel();
        //        channel.ExchangeDeclare(ExchangeName, ExchangeType.Fanout, durable: false);

        //        // pegando a message que recebemos como parâmetro e convertendo em um array de bytes
        //        byte[] body = GetMessageAsByteArray(message);

        //        // publicando a mensagem
        //        channel.BasicPublish(
        //            exchange: ExchangeName,
        //            routingKey: "",
        //            basicProperties: null,
        //            body: body);
        //    }
        //}

        // com exchange direct
        public void SendMessage(BaseMessage message)
        {
            // criando a conexão com o RabbitMQ
            if (ConnectionExists())
            {
                //definindo o channel que iremos usar
                using var channel = _connection.CreateModel();
                channel.ExchangeDeclare(ExchangeName, ExchangeType.Direct, durable: false);

                channel.QueueDeclare(PaymentEmailUpdateQueueName, false, false, false, null);
                channel.QueueDeclare(PaymentOrderUpdateQueueName, false, false, false, null);

                channel.QueueBind(PaymentEmailUpdateQueueName, ExchangeName, "PaymentEmail");
                channel.QueueBind(PaymentOrderUpdateQueueName, ExchangeName, "PaymentOrder");

                // pegando a message que recebemos como parâmetro e convertendo em um array de bytes
                byte[] body = GetMessageAsByteArray(message);

                // publicando a mensagem
                channel.BasicPublish(
                    exchange: ExchangeName,
                    routingKey: "PaymentEmail",
                    basicProperties: null,
                    body: body);
                channel.BasicPublish(
                    exchange: ExchangeName,
                    routingKey: "PaymentOrder",
                    basicProperties: null,
                    body: body);
            }
        }

        private byte[] GetMessageAsByteArray(BaseMessage message)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true   // para serializar as classes filhas
            };

            // serializando o UpdatePaymentResultMessage
            var json = JsonSerializer.Serialize<UpdatePaymentResultMessage>((UpdatePaymentResultMessage)message, options);

            // retornando o array de bytes para publicação da mensagem
            return Encoding.UTF8.GetBytes(json);
        }

        private bool ConnectionExists()
        {
            if (_connection != null) return true;

            CreateConnection();

            return _connection != null;
        }

        private void CreateConnection()
        {
            try
            {
                // criando a ConnectionFactory
                var factory = new ConnectionFactory
                {
                    HostName = _hostName,
                    UserName = _userName,
                    Password = _password
                };

                // criando a conexão de fato, com a factory definida acima
                _connection = factory.CreateConnection();
            }
            catch (Exception)
            {
                // Log exception
                throw;
            }
        }
    }
}
