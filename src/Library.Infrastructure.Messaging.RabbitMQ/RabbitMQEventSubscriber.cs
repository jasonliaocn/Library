using Library.Domain.Core;
using Library.Domain.Core.Messaging;
using Library.Infrastructure.InjectionFramework;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

namespace Library.Infrastructure.Messaging.RabbitMQ
{
	public class RabbitMQEventSubscriber : IEventSubscriber
	{
		private readonly IConnection connection;
		private readonly IModel channel;

		private readonly ICommandTracker tracker;

		public RabbitMQEventSubscriber(IRabbitMQUrlProvider provider, ICommandTracker tracker)
		{
			var factory = new ConnectionFactory() { Uri = new Uri(provider.Url), UserName = provider.UserName, Password = provider.Password };
			this.connection = factory.CreateConnection();
			this.channel = connection.CreateModel();
			this.tracker = tracker;
		}

		public void Dispose()
		{
			this.channel.Dispose();
			this.connection.Dispose();
		}

		public void Subscribe<T>(T domainEvent) where T : DomainEvent
		{
			this.channel.QueueDeclare(queue: domainEvent.EventKey,
					   durable: true,
					   exclusive: false,
					   autoDelete: false,
					   arguments: null);

			channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

			var consumer = new EventingBasicConsumer(channel);

			IEventHandler<T> instance = InjectContainer.GetInstance<IEventHandler<T>>();
			if (instance == null)
			{
				Console.WriteLine("no Mapping");
				return;
			}

			consumer.Received += (model, ea) =>
			{
				var body = ea.Body;
				var message = Encoding.UTF8.GetString(body);

				var cmd = JsonConvert.DeserializeObject<T>(message);
				Console.WriteLine("[x] Receive New Event: {0}", domainEvent.EventKey);
				Console.WriteLine("[x] Event Parameters: {0}", message);

				try
				{
					//执行命令操作
					instance.Handle(cmd);
					//tracker.Finish(cmd.CommandUniqueId, cmd.EventKey);
				}
				catch (Exception ex)
				{
					//tracker.Error(cmd.CommandUniqueId, cmd.EventKey, "100001", ex.Message);
				}

				Console.WriteLine("[x] Event Handler Completed");

				channel.BasicAck(ea.DeliveryTag, false);
			};

			channel.BasicConsume(queue: domainEvent.EventKey, autoAck: false, consumer: consumer);
		}
	}
}