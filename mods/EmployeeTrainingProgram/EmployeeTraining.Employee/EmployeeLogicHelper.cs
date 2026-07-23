using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace EmployeeTraining.Employee;

internal class EmployeeLogicHelper
{
	public static IEnumerator MoveTo(MonoBehaviour employee, Vector3 target, NavMeshAgent Agent, float boost, float turningSpeed, float maxDist)
	{
		bool linearMotion = Agent.speed >= 10f;
		if (NavMesh.SamplePosition(target, out NavMeshHit navMeshHit, maxDist, -1))
		{
			Agent.SetDestination(navMeshHit.position);
		}
		else
		{
			Agent.SetDestination(target);
		}
		while (Vector3.Distance(((Component)employee).transform.position, Agent.destination) > Agent.stoppingDistance)
		{
			Vector3 val;
			if (linearMotion)
			{
				val = Agent.steeringTarget - ((Component)employee).transform.position;
				Agent.velocity = val.normalized * Agent.speed;
				((Component)employee).transform.forward = Agent.steeringTarget - ((Component)employee).transform.position;
			}
			else
			{
				val = Agent.velocity;
				if (val.magnitude > 0f)
				{
					((Component)employee).transform.rotation = Quaternion.Slerp(((Component)employee).transform.rotation, Quaternion.LookRotation(Agent.velocity), turningSpeed * boost * Time.deltaTime);
				}
			}
			yield return null;
		}
	}
}
