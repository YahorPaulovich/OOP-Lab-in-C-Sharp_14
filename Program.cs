using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.Remoting;
using System.Text;
using System.Threading;

namespace WorkwithThreads//Вариант 14
{/*№ 15 Работа с потоками выполнения*/
    class Program
    {/*Задание
1. Определите и выведите на консоль/в файл все запущенные процессы:id, имя,
приоритет, время запуска, текущее состояние, сколько всего времени использовал
процессор и т.д.
2. Исследуйте текущий домен вашего приложения: имя, детали конфигурации, все
сборки, загруженные в домен. Создайте новый домен. Загрузите туда сборку.
Выгрузите домен.
3. Создайте в отдельном потоке следующую задачу расчета (можно сделать sleep для
задержки) и записи в файл и на консоль простых чисел от 1 до n (задает
пользователь). Вызовите методы управления потоком (запуск, приостановка,
возобновление и тд.) Во время выполнения выведите информацию о статусе
потока, имени, приоритете, числовой идентификатор и т.д.
4. Создайте два потока. Первый выводит четные числа, второй нечетные до n и
записывают их в общий файл и на консоль. Скорость расчета чисел у потоков –
разная.
a. Поменяйте приоритет одного из потоков.
b. Используя средства синхронизации организуйте работу потоков, таким
образом, чтобы
i. выводились сначала четные, потом нечетные числа
ii. последовательно выводились одно четное, другое нечетное.
5. Придумайте и реализуйте повторяющуюся задачу на основе класса Timer

Дополнительно (по желанию)
1. На складе имеются товары (файл с записями). Создайте три потока - машины,
каждая машина имеет свою скорость работы. Разгрузите склад. Обеспечьте
последовательный доступ складу (только одна машина может загружаться)
2. Создайте пул ресурсов видеоканалов (класс) которых изначально меньше чем
клиентов (класс), которые хотят ими воспользоваться. Каждый клиент получает
доступ к каналу, причем пользоваться можно только одним каналом. Если все
каналы заняты, то клиент ждет заданное время и по его истечении уходит не
получив услуги. (используйте средства синхронизации - семафор)*/
        static object locker = new object();
        static bool acquiredLock = false;
        //public string EvenOrOdd { get; set; }
        static void Main(string[] args)
        {
            Console.WriteLine("1. Вывод на консоль всех запущенных процессов:\n");
            try
            {
                foreach (Process process in Process.GetProcesses())
                {
                    Console.WriteLine($"\n" +
                    $"Id: {process.Id} " +
                    $"\nИмя: {process.ProcessName} " +
                    $"\nПриоритет: {process.BasePriority}");                 

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine($"PeakWorkingSet64: {PrettifyByte(process.PeakWorkingSet64)}");
                    sb.AppendLine($"VirtualMemorySize64: {PrettifyByte(process.VirtualMemorySize64)}");
                    sb.AppendLine($"PrivateMemorySize64: {PrettifyByte(process.PrivateMemorySize64)}");                  
                    sb.AppendLine();
                    Console.WriteLine(sb.ToString());
                    Count++;
                }
                Console.WriteLine($"Всего запущенно процессов: {Count}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("2. Исследование текущего домена моего приложения:\n");
            AppDomain domain = AppDomain.CurrentDomain;
            Console.WriteLine($"Имя домена приложения: {domain.FriendlyName}");
            Console.WriteLine($"Каталог приложения: {domain.BaseDirectory}");           
            Console.WriteLine();

            Console.WriteLine("Вывод всех загруженных в домен сборок .NET:");
            Assembly[] assemblies = domain.GetAssemblies();
            foreach (Assembly asm in assemblies)
                Console.WriteLine($" {asm.GetName().Name}");

            ///*Создание нового домена. Загрузка туда сборки. Выгрузка домена.*/
            //Console.WriteLine("Метод Main выполняется в домене : {0}", AppDomain.CurrentDomain.Id);

            //// Создание второго домена приложения
            //AppDomain appDomain = AppDomain.CreateDomain("Second Domain");

            //string assembleName = Assembly.GetExecutingAssembly().GetName().Name;
            //string typename = typeof(MyClass).FullName;

            //// Создание объекта во втором домене
            //ObjectHandle handle = domain.CreateInstance(assembleName, typename);

            //// Создание прозрачного прокси-переходника для взаимодействия с объектом во втором домене
            //MyClass instance = handle.Unwrap() as MyClass;

            //Console.WriteLine("instance {0}", instance.GetHashCode());

            ////// Проверка: действительно ли прозрачный переходник предствлен?
            ////Console.WriteLine("IsTransparentProxy(instance) : {0}", RemotingServices.IsTransparentProxy(instance));

            //// Вызов метода объекта, находящегося во втором домене
            //instance.Operation();

            Console.WriteLine("\nСоздание нового домена, загрузка туда сборки, выгрузка домена:");
            LoadAssembly(6);
            // очистка
            GC.Collect();
            GC.WaitForPendingFinalizers();

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                Console.WriteLine(asm.GetName().Name);

            // 3. Создание в отдельном потоке задачи расчёта           
            Console.WriteLine("\n3. Создание в отдельном потоке задачи расчета");
            Thread myThread = new Thread(new ThreadStart(SecondThread));
            myThread.Name = "SecondThread";
            myThread.Start();// запуск потока  
            Thread.Sleep(1);
            Console.WriteLine($"" +
            $"\nИмя: {myThread.Name} " +
            $"\nПриоритет: {myThread.Priority} " +
            $"\nЧисловой идентификатор: {myThread.ManagedThreadId} " +
            $"\nСостояние потока: {myThread.ThreadState} \n");
            Thread.Sleep(500);

            // 4. Создание двух потоков:                
            try
            {
                acquiredLock = false;
                Monitor.Enter(locker, ref acquiredLock);

                Console.WriteLine("\n4. Создание двух потоков:");
                var thread1 = new Thread(new ThreadStart(FirstStream));
                thread1.Name = "Поток 1";

                var thread2 = new Thread(new ThreadStart(SecondStream));
                thread2.Name = "Поток 2";

                // a. Изменение приоритета одного из потоков
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\na. Изменение приоритета одного из потоков: второй поток получает приоритет - низший");
                Console.ForegroundColor = ConsoleColor.Gray;
                thread2.Priority = ThreadPriority.Highest;

                // b. Использования средств синхронизации                    
                Console.WriteLine("\nb. Использования средств синхронизации");

                // i. вывод сначала чётных, потом нечётных чисел
                Console.WriteLine("\ni. вывод сначала чётных, потом нечётных чисел:");
                thread1.Start();
                thread2.Start();             
            }
            finally
            {
                if (acquiredLock) Monitor.Exit(locker);
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            // ii. последовательный вывод одного четного, другого нечетного числа. 
            lock (locker)
            {
                Console.WriteLine("\nii. последовательный вывод одного чётного, другого нечётного числа:");
                ParameterizedThreadStart parameterizedThreadStart = new ParameterizedThreadStart(OutputEvenOrOdd);
                Thread thread = new Thread(parameterizedThreadStart);//
                thread.Name = "Поток1";
                thread.Start("Even");

                OutputEvenOrOdd("Odd");
            }

            // 5. Реализация повторяющиеся задачи на основе класса Timer 
            int num = 0;
            // установка метода обратного вызова
            TimerCallback tm = new TimerCallback(TimerMethod);
            // создание таймера
            Timer timer = new Timer(tm, num, 0, 2000);

          
            //Console.ReadKey();         
        }
        public static int Count { get; set; } = 0;
        public static string path { get; set; }        
        private static object PrettifyByte(long allocatedMemory)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            while (allocatedMemory >= 1024 && order < sizes.Length - 1)
            {
                order++;
                allocatedMemory = allocatedMemory / 1024;
            }
            return $"{allocatedMemory:0.##} {sizes[order]}";
        }
        public static void SecondThread()
        {
            bool acquiredLock = false;
            try
            {
                Monitor.Enter(locker, ref acquiredLock);
                Count = 0;
                int number;
                var arrayList = new System.Collections.ArrayList();

                string FilePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                FilePath = Path.Combine(FilePath, "Работа с потоками выполнения");
                DirectoryInfo dirInf = new DirectoryInfo(FilePath);
                if (!dirInf.Exists)
                {
                    dirInf.Create();
                }
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Нажмите на пробел и введите количество вводимых чисел:");
                Count = Convert.ToInt32(Console.ReadLine());
                Console.WriteLine($"Вводите {Count} разных чисел:");               
                try
                {
                    do
                    {
                        number = Convert.ToInt32(Console.ReadLine());
                        arrayList.Add(number);

                        if (arrayList.Count == Count)
                        {
                            Console.WriteLine("Готово!");
                        }
                        Thread.Sleep(400);
                    } while (arrayList.Count <= Count);                   
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                path = FilePath + @"\SecondStream.txt";
                using (StreamWriter sw = new StreamWriter(path, false, System.Text.Encoding.Default))
                {
                    foreach (var item in arrayList)
                    {
                        sw.WriteLine(item);
                    }
                }
            }
            finally
            {
                if (acquiredLock) Monitor.Exit(locker);
                Console.ForegroundColor = ConsoleColor.Gray;
            }           
        }
        public static void FirstStream()
        {
            try
            {
                bool acquiredLock = false;
                Monitor.Enter(locker, ref acquiredLock);
                string FilePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                FilePath = Path.Combine(FilePath, "Работа с потоками выполнения");
                DirectoryInfo dirInf = new DirectoryInfo(FilePath);
                if (!dirInf.Exists)
                {
                    dirInf.Create();
                }
                var arrayList = new System.Collections.ArrayList();
                Console.ForegroundColor = ConsoleColor.Yellow;
                // первый поток выводит четные числа до n и записывает их в общий файл и на консоль // скорость расчета чисел - быстрая
                Console.WriteLine("\n первый поток выводит чётные числа до n и записывает их в общий файл и на консоль (скорость расчёта чисел - быстрая)");

                for (int i = -9; i < 10; i++)
                {
                    Thread.Sleep(250);
                    if (i % 2 == 0)
                    {
                        Console.WriteLine(i);
                        arrayList.Add(i);
                    }
                }
                path = FilePath + @"\SecondStream.txt";
                if (IsFree(path))
                {
                    using (StreamWriter sw = new StreamWriter(path, false, System.Text.Encoding.Default))
                    {
                        foreach (var item in arrayList)
                        {
                            sw.WriteLine(item);
                        }
                    }
                }

            }
            finally
            {
                if (acquiredLock) Monitor.Exit(locker);
                Console.ForegroundColor = ConsoleColor.Gray;
            }                    
        }
        public static void SecondStream()
        {
            try
            {
                bool acquiredLock = false;
                Monitor.Enter(locker, ref acquiredLock);
                string FilePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                FilePath = Path.Combine(FilePath, "Работа с потоками выполнения");
                DirectoryInfo dirInf = new DirectoryInfo(FilePath);
                if (!dirInf.Exists)
                {
                    dirInf.Create();
                }
                var arrayList = new System.Collections.ArrayList();
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                // второй поток выводит нечетные числа до n и записывает их в общий файл и на консоль // скорость расчета чисел - медленная
                Console.WriteLine("\nвторой поток выводит нечётные числа до n и записывает их в общий файл и на консоль (скорость расчёта чисел - медленная)");

                for (int i = -9; i < 10; i++)
                {
                    Thread.Sleep(500);
                    if (i % 2 != 0)
                    {
                        Console.WriteLine(i);
                        arrayList.Add(i);
                    }
                }
                path = FilePath + @"\SecondStream.txt";
                if (IsFree(path))
                {
                    using (StreamWriter sw = new StreamWriter(path, false, System.Text.Encoding.Default))
                    {
                        foreach (var item in arrayList)
                        {
                            sw.WriteLine(item);
                        }
                    }
                }
            }
            finally
            {
                if (acquiredLock) Monitor.Exit(locker);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
        public static bool IsFree(string path)
        {
            try
            {
                using (var fs = System.IO.File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return true;//файл свободен
                }
            }
            catch (IOException ioex)
            {
                return false;//файл занят
            }
        }
        public static void OutputEvenOrOdd(object EvenOrOdd)
        {      
            string FilePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            FilePath = Path.Combine(FilePath, "Работа с потоками выполнения");
            DirectoryInfo dirInf = new DirectoryInfo(FilePath);
            if (!dirInf.Exists)
            {
                dirInf.Create();
            }
            var arrayList = new System.Collections.ArrayList();
            Console.ForegroundColor = ConsoleColor.Cyan;           
            for (int i = -9; i < 10; i++)
            {
                if ((string)EvenOrOdd == "Even")
                {
                    if (i % 2 == 0)
                    {
                        Console.WriteLine(i);
                        arrayList.Add(i);
                    }
                }
                if ((string)EvenOrOdd == "Odd")
                {
                    if (i % 2 != 0)
                    {
                        Console.WriteLine(i);
                        arrayList.Add(i);
                    }
                }
            }
            path = FilePath + @"\SecondStream.txt";
            if (IsFree(path))
            {
                using (StreamWriter sw = new StreamWriter(path, false, System.Text.Encoding.Default))
                {
                    foreach (var item in arrayList)
                    {
                        sw.WriteLine(item);
                    }
                }
            }
            Console.ForegroundColor = ConsoleColor.Gray;
        }
        public static void TimerMethod(object obj)
        {
            bool acquiredLock = false;
            try
            {
                Monitor.Enter(locker, ref acquiredLock);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n5. Реализация повторяющиеся задачи на основе класса Timer:");
                int x = (int)obj;
                for (int i = 1; i < 9; i++, x++)
                {
                    Console.WriteLine($" TimerMethod - {x * i}");
                }
            }
            finally
            {
                if (acquiredLock) Monitor.Exit(locker);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
        public static int Factorial(int n)
        {           
            int result = 1;
            for (int i = 1; i <= n; i++)
            {
                result *= i;
            }           
            return result;           
        }
        private static void LoadAssembly(int number)
        {
            var context = new CustomAssemblyLoadContext();
            // установка обработчика выгрузки
            context.Unloading += Context_Unloading;
            // получаем путь к сборке MyApp
            var assemblyPath = Path.Combine(Directory.GetCurrentDirectory(), "WorkwithThreads.dll");
            // загружаем сборку
            Assembly assembly = context.LoadFromAssemblyPath(assemblyPath);
            // получаем тип Program из сборки MyApp.dll
            var type = assembly.GetType("WorkwithThreads.Program");
            // получаем его метод Factorial
            var greetMethod = type.GetMethod("Factorial");

            // вызываем метод
            var instance = Activator.CreateInstance(type);
            int result = (int)greetMethod.Invoke(instance, new object[] { number });
            // выводим результат метода на консоль           
            Console.Write("\nПолучаем результат: ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"факториал числа {number} равен {result}");
            Console.ForegroundColor = ConsoleColor.Gray;

            // смотим, какие сборки у нас загружены
            Console.WriteLine("\nСборки загружены:\n");
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                Console.WriteLine($" {asm.GetName().Name}");

            // выгружаем контекст
            Console.WriteLine("\nвыгрузка 'WorkwithThreads.dll'...");
            context.Unload();
        }
        // обработчик выгрузки контекста
        private static void Context_Unloading(AssemblyLoadContext obj)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Библиотека WorkwithThreads выгружена \n");
            Console.ForegroundColor = ConsoleColor.Gray;
        }       
    }
    public class CustomAssemblyLoadContext : AssemblyLoadContext
    {
        public CustomAssemblyLoadContext() : base(isCollectible: true) { }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            return null;
        }
    }   
    class MyClass : MarshalByRefObject
    {
        public void Operation()
        {
            Console.WriteLine("Метод Operation выполняется в домене : {0}", AppDomain.CurrentDomain.Id);
            Console.WriteLine("instance {0}",this.GetHashCode());
        }
    }
}
    

