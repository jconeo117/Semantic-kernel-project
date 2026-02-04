using ClinicSimulator.Core.Models;

namespace ClinicSimulator.Core.Repositories;

public interface IPatients
{
    Task<Patient> CreateAsync(Patient patient);
    Task<Patient> GetByIdAsync(string id);
    Task<Patient> FindByEmailAsync(string email);
    Task<Patient> FindByPhoneAsync(string phone);
    Task<List<Patient>> GetAllAsync();
    Task<bool> UpdateAsync(Patient patient);
}