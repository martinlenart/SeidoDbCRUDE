using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DbModelsLib;

namespace DbCRUDReposLib
{
    public interface ICustomerRepository
    {
        //Using a fluent syntax when possible
        Task<Customer> CreateAsync(Customer cust);
        Task<IEnumerable<Customer>> ReadAllAsync();
        Task<Customer> ReadAsync(Guid custId);
        Task<Customer> UpdateAsync(Customer cust);
        Task<Customer> DeleteAsync(Guid custId);
    }
}
