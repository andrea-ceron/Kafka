
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Utility.Kafka.Abstraction.Clients;
using Utility.Kafka.Abstraction.Errors;
using Utility.Kafka.Abstraction.MessageHandlers;
using Utility.Kafka.Clients;
using Utility.Kafka.ExceptionManager;
using Utility.Kafka.ExceptionManager.CircuitBreaker;
using Utility.Kafka.Services;
using Timer = Utility.Kafka.ExceptionManager.CircuitBreaker.Timer;

namespace Utility.Kafka.DependencyInjection;

public static  class KafkaDI
{
	private static IServiceCollection AddAdministrator(this IServiceCollection service, IConfiguration configuration)
	{
		service.Configure<KafkaAdminClientOptions>(
			configuration.GetSection(KafkaAdminClientOptions.SectionName));
		service.AddSingleton<IAdministratorClient, AdministratorClient>();
		return service;
	}

	private static IServiceCollection AddConsumerService<TKafkaTopicsInput, TMessageHandlerFactory>(this IServiceCollection service, IConfiguration configuration)
		where TKafkaTopicsInput : class, IKafkaTopics
		where TMessageHandlerFactory : class, IMessageHandlerFactory<string, string>
	{
		service.AddSingleton<IHostedService, ConsumerService<TKafkaTopicsInput>>();
		service.AddSingleton<IMessageHandlerFactory<string, string>, TMessageHandlerFactory>();
		service.Configure<KafkaConsumerClientOptions>(
			configuration.GetSection(KafkaConsumerClientOptions.SectionName));
		service.AddSingleton<IConsumerClient<string, string>, Consumer>();
		service.Configure<TKafkaTopicsInput>(
		   configuration.GetSection(AbstractInputKafkaTopics.SectionName));
		return service;
	}

	private static IServiceCollection AddProducerServiceWithSubscription<TKafkaTopicsOutput, TProducerService>
		(this IServiceCollection service, IConfiguration configuration)
		where TKafkaTopicsOutput : class, IKafkaTopics
		where TProducerService :  ProducerServiceWithSubscription
	{
		service.AddSingleton<IHostedService, TProducerService>();
		service.Configure<KafkaProducerClientOptions>(
			configuration.GetSection(KafkaProducerClientOptions.SectionName));
		service.AddSingleton<IProducerClient<string, string>, Producer>();
		service.Configure<TKafkaTopicsOutput>(
		   configuration.GetSection(AbstractOutputKafkaTopics.SectionName));
		return service;
	}
	private static bool IsEnable(IConfiguration configuration)
	{

		KafkaOptions? options = configuration.GetSection(KafkaOptions.SectionName).Get<KafkaOptions>();
		if (options == null)
		{
			return false; 
		}
		return options.Enable;
	}
	private static IServiceCollection AddCircuitBreaker(this IServiceCollection service, IConfiguration configuration)
	{
		service.Configure<KafkaCircuitBreakerOptions>(
			configuration.GetSection(KafkaCircuitBreakerOptions.SectionName));
		service.Configure<KafkaTimerOptions>(
			configuration.GetSection(KafkaCircuitBreakerOptions.SectionName));

		service.AddSingleton<ICircuitBreakerTimer, Timer>(sp =>
		{
			var options = sp.GetRequiredService<IOptions<KafkaTimerOptions>>();
			var errorManagerMiddleware = sp.GetRequiredService<ErrorManagerMiddleware>();
			return new Timer(options, errorManagerMiddleware);
		});
		service.AddSingleton<ICircuitBreaker, CircuitBreaker>(sp =>
		{
			var options = sp.GetRequiredService<IOptions<KafkaCircuitBreakerOptions>>();
			var errorManagerMiddleware = sp.GetRequiredService<ErrorManagerMiddleware>();
			ICircuitBreakerTimer timer = sp.GetRequiredService<ICircuitBreakerTimer>();
			return new CircuitBreaker(options, errorManagerMiddleware, timer);
		});

		return service;
	}
	private static IServiceCollection AddErrorManager(this IServiceCollection service)
	{
		service.AddSingleton<ErrorManagerMiddleware>();
		return service;
	}
	public static IServiceCollection AddKafkaProducer<TKafkaTopicsOutput, TProducerService>(
		this IServiceCollection service, IConfiguration configuration)
		where TKafkaTopicsOutput : class, IKafkaTopics
		where TProducerService : ProducerServiceWithSubscription
	{
		if (!IsEnable(configuration))
		{
			return service;
		}
		service.AddErrorManager();
		service.AddAdministrator(configuration);
		service.AddProducerServiceWithSubscription<TKafkaTopicsOutput, TProducerService>(configuration);
		return service;
	}

	public static IServiceCollection AddKafkaConsumer<TKafkaTopicsInput, TMessageHandlerFactory>(
	this IServiceCollection service, IConfiguration configuration)
	where TKafkaTopicsInput : class, IKafkaTopics
	where TMessageHandlerFactory : class, IMessageHandlerFactory<string, string>
	{
		if (!IsEnable(configuration))
		{
			return service;
		}
		service.AddErrorManager();
		service.AddAdministrator(configuration);
		service.AddConsumerService<TKafkaTopicsInput, TMessageHandlerFactory>(configuration);
		return service;
	}

	public static IServiceCollection AddKafkaConsumerAndProducer<TKafkaTopicsInput, TKafkaTopicsOutput, TMessageHandlerFactory, TProducerService>(
		this IServiceCollection service, IConfiguration configuration)
		where TKafkaTopicsInput : class, IKafkaTopics
		where TKafkaTopicsOutput : class, IKafkaTopics
		where TProducerService : ProducerServiceWithSubscription
		where TMessageHandlerFactory : class, IMessageHandlerFactory<string, string>
	{
		if (!IsEnable(configuration))
		{
			return service;
		}
		service.AddErrorManager();
		service.AddAdministrator(configuration);
		service.AddConsumerService<TKafkaTopicsInput, TMessageHandlerFactory>(configuration);
		service.AddProducerServiceWithSubscription<TKafkaTopicsOutput, TProducerService>(configuration);

		return service;
	}

}
