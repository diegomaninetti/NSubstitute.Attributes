using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace NSubstitute.Attributes;

public static class TestUtils {
	public static void InitializeServices(object test, Action<IServiceCollection>? action = null) {
		var serviceCollection = new ServiceCollection();

		action?.Invoke(serviceCollection);

		CreateMocked(test, serviceCollection);
		AddProvided(test, serviceCollection);
		AddMissing(test, serviceCollection);

		var serviceProvider = serviceCollection.BuildServiceProvider();
		SetProvided(test, serviceProvider);

	}

	private static void CreateMocked(object test, ServiceCollection serviceCollection) {
		MockedList(test).ForEach(p => {
			Type fieldType = p.FieldType;
			if (fieldType.IsInterface) {
				var instance = Substitute.For(new Type[] { fieldType }, null);
				serviceCollection.Add(new ServiceDescriptor(fieldType, instance));
				p.SetValue(test, instance);
			}
		});
	}

	private static void AddProvided(object test, ServiceCollection serviceCollection) {
		ProvidedList(test).ForEach(p => {
			serviceCollection.AddSingleton(p.FieldType);
		});
	}

	private static void AddMissing(object test, ServiceCollection serviceCollection) {
		ProvidedList(test).ForEach(p => {
			Type fieldType = p.FieldType;
			var constructors = fieldType.GetConstructors();

			// At least one cunstructor can be resolved with provided or mocked services.
			var dependenciesFound = constructors.Any(c => c.GetParameters().All(p => serviceCollection.Any(d => d.ServiceType == p.ParameterType)));
			if (dependenciesFound) {
				return;
			}
			// Use the first constructor
			var defaultConstructor = constructors.FirstOrDefault();
			// For each parameter not present in serviceCollection, add a Mock.
			defaultConstructor?.GetParameters()
				.Where(p => !serviceCollection.Any(d => d.ServiceType == p.ParameterType))
				.ToList()
				.ForEach(p => {
					var instance = Substitute.For(new Type[] { p.ParameterType }, null);
					serviceCollection.Add(new ServiceDescriptor(p.ParameterType, instance));
				});
		});

	}

	private static void SetProvided(object test, ServiceProvider serviceProvider) {
		ProvidedList(test).ForEach(p => {
			var instance = serviceProvider.GetRequiredService(p.FieldType);
			p.SetValue(test, instance);
		});
	}


	private static List<FieldInfo> MockedList(object test) {
		return test.GetType().GetFields(BindingFlags.Default | BindingFlags.NonPublic | BindingFlags.Instance)
			.Where(p => p.IsDefined(typeof(MockedAttribute), false))
			.ToList();
	}


	private static List<FieldInfo> ProvidedList(object test) {
		return test.GetType().GetFields(BindingFlags.Default | BindingFlags.NonPublic | BindingFlags.Instance)
			.Where(p => p.IsDefined(typeof(ProvidedAttribute), false))
			.ToList();
	}

}
