using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

using DbContextLib;
using DbModelsLib;
using DbCRUDReposLib;

namespace DbAppConsole
{
    static class MyLinqExtensions
    {
        public static void Print<T>(this IEnumerable<T> collection)
        {
            collection.ToList().ForEach(item => Console.WriteLine(item));
        }
    }
    class Program
    {
        private static DbContextOptionsBuilder<SeidoDbContext> _optionsBuilder;
        static void Main(string[] args)
        {
            BuildOptions();

            //SeedDataBase();
            
            QueryDatabaseAsync().Wait();
            QueryDatabaseLinq();
            QueryDatabaseCRUDE().Wait();

            Console.WriteLine("\nPress any key to terminate");
            Console.ReadKey();
        }

        private static void BuildOptions()
        {
            _optionsBuilder = new DbContextOptionsBuilder<SeidoDbContext>();

            #region Ensuring appsettings.json is in the right location
            Console.WriteLine($"DbConnections Directory: {DBConnection.DbConnectionsDirectory}");

            var connectionString = DBConnection.ConfigurationRoot.GetConnectionString("SQLite_seidowebservice");
            if (!string.IsNullOrEmpty(connectionString))
                Console.WriteLine($"Connection string to Database: {connectionString}");
            else
            {
                Console.WriteLine($"Please copy the 'DbConnections.json' to this location");
                return;
            }
            #endregion

            _optionsBuilder.UseSqlite(connectionString);
        }

        private static void SeedDataBase()
        {
            using (var db = new SeidoDbContext(_optionsBuilder.Options))
            {
                //Create some customers
                var CustomerList = new List<Customer>();
                for (int i = 0; i < 100; i++)
                {
                    CustomerList.Add(new Customer());
                }
                //Create some orders randomly linked to customers
                var rnd = new Random();
                var OrderList = new List<Order>();
                for (int i = 0; i < 500; i++)
                {
                    OrderList.Add(new Order(CustomerList[rnd.Next(0, CustomerList.Count)].CustomerID));
                }

                //Add it to the Database
                CustomerList.ForEach(cust => db.Customers.Add(cust));
                OrderList.ForEach(order => db.Orders.Add(order));

                db.SaveChanges();
            }
        }
        private static async Task QueryDatabaseAsync()
        {
            using (var db = new SeidoDbContext(_optionsBuilder.Options))
            {
                Console.WriteLine("\n\nQuery Database");
                Console.WriteLine("--------------");

                var cusCount = await db.Customers.CountAsync();
                var ordCount = await db.Orders.CountAsync();

                Console.WriteLine($"Nr of Customers: {cusCount}");
                Console.WriteLine($"Nr of Orders: {ordCount}");

                var c = db.Customers.AsEnumerable();
            }
        }

        private static void QueryDatabaseLinq()
        {
            using (var db = new SeidoDbContext(_optionsBuilder.Options))
            {
                //Use .AsEnumerable() to make sure the Db request is fully translated to be managed by Linq.
                var customers = db.Customers.AsEnumerable();
                var orders = db.Orders.AsEnumerable(); ;

                Console.WriteLine("\n\nQuery Database with Linq");
                Console.WriteLine("------------------------");
                Console.WriteLine($"Nr of orders: {orders.Count()}");
                Console.WriteLine($"Total order value: {orders.Sum(order => order.Total):C2}");
                
                Console.WriteLine("\nFirst 5 orders:");
                orders.Take(5).OrderByDescending(order => order.Value).Print();

                Console.WriteLine("Join examples");
                var list1 = customers.GroupJoin(orders, cust => cust.CustomerID, order => order.CustomerID, (cust, order) => new { cust, order });
                Console.WriteLine($"\nOuterJoin: Customer - Order via GroupJoin by Customer, Count: {list1.Count()}");
                //list1.Print();

                var list2 = list1.Where(custorder => custorder.order.Count() == 0);
                Console.WriteLine($"\nGroupJoin with Order list Count == 0: {list2.Count()}");
                //list2.Print();

                var list3 = list1.Where(custorder => custorder.order.Count() != 0);
                Console.WriteLine($"\nGroupJoin with Order list Count != 0: {list3.Count()}");
                //list3.Print();

                var list4 = customers.Join(orders, cust => cust.CustomerID, order => order.CustomerID, (cust, order) => new { cust, order });
                Console.WriteLine($"\nInnerJoin Customer - Order via Join, Count: {list4.Count()}");
                //list4.Print();            
            }
        }

        private static async Task QueryDatabaseCRUDE()
        {
            using (var db = new SeidoDbContext(_optionsBuilder.Options))
            using (var _repo = new CustomerRepository(db))
            {
                Console.WriteLine("\n\nQuery Database CRUDE");
                Console.WriteLine("--------------------");

                Console.WriteLine("Testing ReadAllAsync()");
                var AllCustomers = await _repo.ReadAllAsync();
                Console.WriteLine($"Nr of Customers {AllCustomers.Count()}");
                Console.WriteLine($"\nFirst 5 Customers");
                AllCustomers.Take(5).Print();

                Console.WriteLine("\nTesting ReadAsync()");
                var LastCust1 = AllCustomers.Last();
                var LastCust2 = await _repo.ReadAsync(LastCust1.CustomerID);
                Console.WriteLine($"Last Customer with Orders.\n{LastCust1}");
                Console.WriteLine($"Read Customer with CustomerID == Last Customer\n{LastCust2}");
                if (LastCust1 == LastCust2)
                    Console.WriteLine("Customers Equal");
                else
                    Console.WriteLine("ERROR: Customers not equal");

                Console.WriteLine("\nTesting UpdateAsync()");
                LastCust2.FirstName += "_Updated";
                LastCust2.LastName += "_Updated";
                
                var LastCust3 = await _repo.UpdateAsync(LastCust2);
                Console.WriteLine($"Last Customer with updated names.\n{LastCust2}");

                LastCust3.FirstName = LastCust3.FirstName.Replace("_Updated", "");
                LastCust3.LastName = LastCust3.LastName.Replace("_Updated", "");

                LastCust3 = await _repo.UpdateAsync(LastCust3);
                Console.WriteLine($"Last Customer with restored names.\n{LastCust3}");

                Console.WriteLine("\nTesting CreateAsync()");
                var NewCust1 = new Customer();
                var NewCust2 = await _repo.CreateAsync(NewCust1);
                var NewCust3 = await _repo.ReadAsync(NewCust2.CustomerID);
                
                Console.WriteLine($"Customer created.\n{NewCust1}");
                Console.WriteLine($"Customer Inserted in Db.\n{NewCust2}");
                Console.WriteLine($"Customer ReadAsync from Db.\n{NewCust3}");

                if (NewCust1 == NewCust2 && NewCust1 == NewCust3)
                    Console.WriteLine("Customers Equal");
                else
                    Console.WriteLine("ERROR: Customers not equal");

                Console.WriteLine("\nTesting DeleteAsync()");
                var DelCust1 = await _repo.DeleteAsync(NewCust1.CustomerID);
                Console.WriteLine($"Customer to delete.\n{NewCust1}");
                Console.WriteLine($"Deleted Customer.\n{DelCust1}");

                if (DelCust1 != null && DelCust1 == NewCust1)
                    Console.WriteLine("Customer Equal");
                else
                    Console.WriteLine("ERROR: Customers not equal");

                var DelCust2 = await _repo.ReadAsync(DelCust1.CustomerID);
                if (DelCust2 != null)
                    Console.WriteLine("ERROR: Customer not removed");
                else
                    Console.WriteLine("Customer confirmed removed from Db");
            }
        }
    }
}
