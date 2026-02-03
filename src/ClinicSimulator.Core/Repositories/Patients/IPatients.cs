
namespace ClinicSimulator.Core.Repositories;

public interface IPatients
{
    Task<Patient> CreateAsync(Patient patient);
    Task<Patient> GetByIdAsync(string id);
    Task<Patient> findByEmailAsync(string email);
    Task<Patient> findByPhoneAsync(string phone);
    Task<List<Patient>> GetAllAsync();
    Task<bool> UpdateAsync(Patient patient);
}