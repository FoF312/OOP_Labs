using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ConsoleApp1
{
    // Жизненные циклы зарегистрированных сервисов
    public enum LifeStyle
    {
        PerRequest,
        Scoped,
        Singleton
    }

    // Запись регистрации зависимости
    class Registration
    {
        public Type InterfaceType { get; set; }
        public Type ImplementationType { get; set; }
        public LifeStyle LifeStyle { get; set; }
        public object[] Parameters { get; set; }
        public Func<Injector, object> Factory { get; set; }
        public object SingletonInstance { get; set; }
    }

    // Объект Scope для хранения экземпляров с жизненным циклом Scoped и их освобождения
    public class Scope : IDisposable
    {
        private readonly Injector _injector;
        private readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();
        private bool _disposed;

        public Scope(Injector injector)
        {
            _injector = injector;
        }

        internal bool TryGet(Type t, out object inst) => _instances.TryGetValue(t, out inst);
        internal void Store(Type t, object inst) => _instances[t] = inst;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            // очистить ссылку на текущую область в инжекторе
            _injector.ClearScope(this);

            foreach (var v in _instances.Values)
            {
                if (v is IDisposable d) d.Dispose();
            }
        }
    }

    // Простой DI-инжектор
    public class Injector
    {
        private readonly Dictionary<Type, Registration> _registrations = new Dictionary<Type, Registration>();
        private Scope _currentScope;

        // зарегистрировать реализацию для интерфейса с опциональными параметрами конструктора
        public void Register<TInterface, TImplementation>(LifeStyle lifeStyle = LifeStyle.PerRequest, params object[] parameters)
            where TImplementation : class
        {
            Register(typeof(TInterface), typeof(TImplementation), lifeStyle, parameters);
        }

        public void Register(Type interfaceType, Type implementationType, LifeStyle lifeStyle = LifeStyle.PerRequest, params object[] parameters)
        {
            var reg = new Registration
            {
                InterfaceType = interfaceType,
                ImplementationType = implementationType,
                LifeStyle = lifeStyle,
                Parameters = parameters
            };
            _registrations[interfaceType] = reg;
        }

        // зарегистрировать фабричный метод для интерфейса
        public void RegisterFactory<TInterface>(Func<Injector, object> factory, LifeStyle lifeStyle = LifeStyle.PerRequest)
        {
            var reg = new Registration
            {
                InterfaceType = typeof(TInterface),
                Factory = factory,
                LifeStyle = lifeStyle
            };
            _registrations[typeof(TInterface)] = reg;
        }

        // начать область (Scope) для обслуживания Scoped жизненного цикла
        public Scope BeginScope()
        {
            var scope = new Scope(this);
            _currentScope = scope;
            return scope;
        }

        internal void ClearScope(Scope scope)
        {
            if (_currentScope == scope) _currentScope = null;
        }

        // получить экземпляр по типу интерфейса
        public object GetInstance(Type interfaceType)
        {
            if (!_registrations.TryGetValue(interfaceType, out var reg))
                throw new InvalidOperationException($"Type {interfaceType} is not registered");

            // для Singleton возвращаем ранее созданный экземпляр, если он есть
            if (reg.LifeStyle == LifeStyle.Singleton && reg.SingletonInstance != null)
                return reg.SingletonInstance;

            // для Scoped — проверить текущую область на наличие экземпляра
            if (reg.LifeStyle == LifeStyle.Scoped)
            {
                if (_currentScope == null)
                    throw new InvalidOperationException("Нет активной области. Вызовите BeginScope() перед получением Scoped-сервиса.");

                if (_currentScope.TryGet(interfaceType, out var inst))
                    return inst;
            }

            object instance;
            if (reg.Factory != null)
            {
                instance = reg.Factory(this);
            }
            else
            {
                instance = CreateByType(reg.ImplementationType, reg.Parameters);
            }

            if (reg.LifeStyle == LifeStyle.Singleton)
                reg.SingletonInstance = instance;

            if (reg.LifeStyle == LifeStyle.Scoped)
                _currentScope.Store(interfaceType, instance);

            return instance;
        }

        // вспомогательный generic-метод
        public T GetInstance<T>() => (T)GetInstance(typeof(T));

        // создать экземпляр через reflection, автоматически внедряя зарегистрированные интерфейсы
        private object CreateByType(Type implType, object[] extraParams)
        {
            var ctors = implType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            var ctor = ctors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
            if (ctor == null) throw new InvalidOperationException($"No public constructor for {implType}");

            var ctorParams = ctor.GetParameters();
            var args = new object[ctorParams.Length];

            var extras = extraParams != null ? new List<object>(extraParams) : new List<object>();

            for (int i = 0; i < ctorParams.Length; i++)
            {
                var p = ctorParams[i];

                // если тип параметра зарегистрирован — резолвить через инжектор
                if (_registrations.ContainsKey(p.ParameterType))
                {
                    args[i] = GetInstance(p.ParameterType);
                    continue;
                }

                // иначе попытаться сопоставить с предоставленными дополнительными параметрами
                var idx = extras.FindIndex(e => e != null && p.ParameterType.IsAssignableFrom(e.GetType()));
                if (idx >= 0)
                {
                    args[i] = extras[idx];
                    extras.RemoveAt(idx);
                    continue;
                }

                // если есть значение по умолчанию — использовать его
                if (p.HasDefaultValue)
                {
                    args[i] = p.DefaultValue;
                    continue;
                }

                throw new InvalidOperationException($"Cannot resolve parameter '{p.Name}' of type {p.ParameterType} when creating {implType}");
            }

            return ctor.Invoke(args);
        }
    }

    // ====== Интерфейсы и их реализации ======
    public interface IInterface1 { string WhoAmI(); }
    public interface IInterface2 { string WhoAmI(); }
    public interface IInterface3 { string WhoAmI(); }

    // Реализации IInterface1
    public class Interface1Debug : IInterface1
    {
        private readonly IInterface2 _dep2;
        private readonly string _config;
        public Interface1Debug(IInterface2 dep2, string config)
        {
            _dep2 = dep2;
            _config = config;
        }
        public string WhoAmI() => $"Interface1Debug(config={_config}) -> {_dep2.WhoAmI()}";
    }

    public class Interface1Release : IInterface1
    {
        private readonly IInterface3 _dep3;
        public Interface1Release(IInterface3 dep3) { _dep3 = dep3; }
        public string WhoAmI() => $"Interface1Release -> {_dep3.WhoAmI()}";
    }

    // Реализации IInterface2
    public class Interface2Debug : IInterface2
    {
        public Interface2Debug() { }
        public string WhoAmI() => "Interface2Debug";
    }

    public class Interface2Release : IInterface2
    {
        private readonly string _name;
        public Interface2Release(string name) { _name = name; }
        public string WhoAmI() => $"Interface2Release(name={_name})";
    }

    // Реализации IInterface3
    public class Interface3Debug : IInterface3
    {
        public Interface3Debug() { }
        public string WhoAmI() => "Interface3Debug";
    }

    public class Interface3Release : IInterface3
    {
        private readonly int _value;
        public Interface3Release(int value) { _value = value; }
        public string WhoAmI() => $"Interface3Release(value={_value})";
    }

    // ====== Демонстрация программы: две конфигурации ======
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("--- Лабораторная работа 7: Внедрение зависимостей (один файл) ---\n");

            var injectorA = new Injector();
            Console.WriteLine("=== Конфигурация A ===");
            ConfigureA(injectorA);
            Demo(injectorA);

            Console.WriteLine();
            var injectorB = new Injector();
            Console.WriteLine("=== Конфигурация B ===");
            ConfigureB(injectorB);
            Demo(injectorB);
        }

        static void ConfigureA(Injector inj)
        {
            // IInterface2: фабричный метод возвращает Interface2Release (PerRequest)
            inj.RegisterFactory<IInterface2>(ctx => new Interface2Release("factory-A"), LifeStyle.PerRequest);

            // IInterface3: Singleton, конструктор принимает параметр
            inj.Register<IInterface3, Interface3Release>(LifeStyle.Singleton, 100);

            // IInterface1: Scoped, зависит от IInterface2 и принимает дополнительную строку
            inj.Register<IInterface1, Interface1Debug>(LifeStyle.Scoped, "cfg-A");
        }

        static void ConfigureB(Injector inj)
        {
            // Другое сопоставление: IInterface2 как Singleton
            inj.Register<IInterface2, Interface2Release>(LifeStyle.Singleton, "svc-B");
            // IInterface3: PerRequest -> Interface3Debug
            inj.Register<IInterface3, Interface3Debug>(LifeStyle.PerRequest);
            // IInterface1: PerRequest -> Interface1Release (зависит от IInterface3)
            inj.Register<IInterface1, Interface1Release>(LifeStyle.PerRequest);
        }

        static void Demo(Injector inj)
        {
            Console.WriteLine("-- Демонстрация PerRequest (каждый запрос) для IInterface2 --");
            var p1 = inj.GetInstance<IInterface2>();
            var p2 = inj.GetInstance<IInterface2>();
            Console.WriteLine($"Первый:  {p1.WhoAmI()}");
            Console.WriteLine($"Второй: {p2.WhoAmI()}");
            Console.WriteLine($"Равны по ссылке: {ReferenceEquals(p1, p2)}\n");

            Console.WriteLine("-- Демонстрация Singleton для IInterface3 --");
            var s1 = inj.GetInstance<IInterface3>();
            var s2 = inj.GetInstance<IInterface3>();
            Console.WriteLine($"Первый:  {s1.WhoAmI()}");
            Console.WriteLine($"Второй: {s2.WhoAmI()}");
            Console.WriteLine($"Равны по ссылке: {ReferenceEquals(s1, s2)}\n");

            Console.WriteLine("-- Демонстрация Scoped для IInterface1 --");
            using (var scope = inj.BeginScope())
            {
                var sc1 = inj.GetInstance<IInterface1>();
                var sc2 = inj.GetInstance<IInterface1>();
                Console.WriteLine($"Внутри области 1: {sc1.WhoAmI()}");
                Console.WriteLine($"Внутри области 2: {sc2.WhoAmI()}");
                Console.WriteLine($"Равны по ссылке внутри области: {ReferenceEquals(sc1, sc2)}");
            }

            using (var scope2 = inj.BeginScope())
            {
                var sc3 = inj.GetInstance<IInterface1>();
                Console.WriteLine($"Экземпляр в новой области: {sc3.WhoAmI()}");
            }

            Console.WriteLine();
        }
    }
}
