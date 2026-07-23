namespace EmployeeTraining.Employee;

public abstract class Employee<T>
{
	public T Instance;

	public abstract int ID { get; }

	public Employee()
	{
	}
}
