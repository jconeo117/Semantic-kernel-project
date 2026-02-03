namespace ClinicSimulator.Core.Repositories;
public class InMemoryPatients : IPatients
{
    private List<Patient> _patients = new List<Patient>();

    public Task<Patient> CreateAsync(Patient patient)
    {
        patient.Id = new Guid();
        patient.CreatedAt = DateTime.Now;
        patient.Type = PatientType.Real;
        _patients.Add(patient);
        return Task.FromResult(patient);
    }

    public Task<Patient> findByEmailAsync(string email)
    {
        var patient = _patients.FirstOrDefault(p => p.Email == email);
        return Task.FromResult(patient);
    }

    public Task<Patient> findByPhoneAsync(string phone)
    {
        var patient = _patients.FirstOrDefault(p => p.Phone == phone);
        return Task.FromResult(patient);
    }

    public Task<List<Patient>> GetAllAsync()
    {
        return Task.FromResult(_patients);
    }

    public Task<Patient> GetByIdAsync(string id)
    {
        var patient = _patients.FirstOrDefault(p => p.Id == Guid.Parse(id));
        return Task.FromResult(patient);
    }

    public Task<bool> UpdateAsync(Patient patient)
    {
        var existing = _patients.FirstOrDefault(p => p.Id == patient.Id);
        if (existing == null) return Task.FromResult(false);

        var index = _patients.IndexOf(existing);
        _patients[index] = patient;
        return Task.FromResult(true);
    }
}