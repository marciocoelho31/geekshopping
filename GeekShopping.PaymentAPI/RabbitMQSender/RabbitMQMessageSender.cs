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

        public RabbitMQMessageSender()
        {
            _hostName = "localhost";
            _password = "guest";
            _userName = "guest";
        }

        public void SendMessage(BaseMessage message, string queueName)
        {
            // criando a conexão com o RabbitMQ
            if (ConnectionExists())
            {
                //definindo o channel que iremos usar
                using var channel = _connection.CreateModel();
                channel.QueueDeclare(queue: queueName, false, false, false, arguments: null);

                // pegando a message que recebemos como parâmetro e convertendo em um array de bytes
                byte[] body = GetMessageAsByteArray(message);

                // publicando a mensagem
                channel.BasicPublish(
                    exchange: "",
                    routingKey: queueName,
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
