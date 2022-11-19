namespace ET.Client
{
    [Event(SceneType.Client)]
    public class AfterCreateClientScene_AddComponent: AEvent<EventType.AfterCreateClientScene>
    {
        protected override async ETTask Run(Scene scene, EventType.AfterCreateClientScene args)
        {
            //scene.AddComponent<UIEventComponent>();
            //scene.AddComponent<UIComponent>();
            scene.AddComponent<EUI.UIEventComponent>();
            scene.AddComponent<EUI.UIComponent>();
            scene.AddComponent<EUI.UIPathComponent>();

            scene.AddComponent<ResourcesLoaderComponent>();
            await ETTask.CompletedTask;
        }
    }
}