

namespace ET
{
	[ActorMessageHandler]
	public class G2M_SessionDisconnectHandler : AMActorLocationHandler<Unit, G2M_SessionDisconnect>
	{
		protected override async ETTask Run(Unit unit, G2M_SessionDisconnect message)
		{
			unit.Domain.GetComponent<UnitComponent>().Remove(unit.Id);
			await ETTask.CompletedTask;
		}
	}
}