/// <summary>
/// Resolve o PseudoStatus de um processo Camunda com base em seu estado e atividades.
/// Extraído do projeto CentralDeProcessos sem alterações de contrato.
/// </summary>
public interface IStatusResolver
{
	PseudoStatus ResolveProcessStatus(
		string processState,
		IEnumerable<CamundaHistoryActivity> activities);
}