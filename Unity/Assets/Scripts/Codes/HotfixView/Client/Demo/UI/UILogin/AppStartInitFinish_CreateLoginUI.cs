using ET.Client.EUI;
namespace ET.Client
{
	[Event(SceneType.Client)]
	public class AppStartInitFinish_CreateLoginUI: AEvent<EventType.AppStartInitFinish>
	{
		protected override async ETTask Run(Scene scene, EventType.AppStartInitFinish args)
		{
			await scene.GetComponent<EUI.UIComponent>().ShowWindowAsync(WindowID.WindowID_Login);

			//await UIHelper.Create(scene, UIType.UILogin, UILayer.Mid);
		}
	}
}
