using GeekShopping.MessageBus;

namespace GeekShopping.PaymentAPI.RabbitMQSender
{
    public interface IRabbitMQMessageSender
    {
        // quando não se usa exchange no RabbitMQ, tem que mandar o nome da fila (queueName)
        //void SendMessage(BaseMessage baseMessage, string queueName);

        // envio utilizando exchange
        void SendMessage(BaseMessage baseMessage);
    }
}
