namespace ET.Client
{
	 [ComponentOf(typeof(EUI.UIBaseWindow))]
	public  class DlgLogin :Entity,IAwake,EUI.IUILogic
	{

		public DlgLoginViewComponent View { get => this.GetParentComponent<DlgLoginViewComponent>();} 

		 

	}
}
