using DbModelsLib;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using DbContextLib;
namespace DbCRUDReposLib
{
    public class CustomerRepository : ICustomerRepository
    {
        SeidoDbContext _db = null;

        public async Task<Customer> CreateAsync(Customer cust)
        {
            var added = await _db.Customers.AddAsync(cust);

            int affected = await _db.SaveChangesAsync();
            if (affected == 1)
                return cust;
            else
                return null;
        }

        public async Task<Customer> DeleteAsync(Guid custId)
        {
            var cusDel = await _db.Customers.FindAsync(custId);
            _db.Customers.Remove(cusDel);

            int affected = await _db.SaveChangesAsync();
            if (affected == 1)
                return cusDel;
            else
                return null;
        }

        public async Task<Customer> ReadAsync(Guid custId)
        {
            return await _db.Customers.FindAsync(custId);
        }

        public async Task<IEnumerable<Customer>> ReadAllAsync()
        {
            return await Task.Run(() => _db.Customers);
        }

        public async Task<Customer> UpdateAsync(Customer cust)
        {
            _db.Customers.Update(cust); //No db interaction until SaveChangesAsync
            int affected = await _db.SaveChangesAsync();
            if (affected == 1)
                return cust;
            else
                return null;
        }

        public CustomerRepository(SeidoDbContext db)
        {
            _db = db; 
        }

        public void Dispose() { }
    }
}
