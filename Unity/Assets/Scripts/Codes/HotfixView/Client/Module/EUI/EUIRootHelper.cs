using System;
using UnityEngine;

namespace ET.Client.EUI
{
    [FriendOf(typeof(GlobalComponent))]
    public static class EUIRootHelper
    {
        public static void Init()
        {
          
        }
        
        public static Transform GetTargetRoot(UIWindowType type)
        {
            if (type == UIWindowType.Normal)
            {
                return Root.Instance.Scene.GetComponent<GlobalComponent>().NormalRoot;
            }
            else if (type == UIWindowType.Fixed)
            {
                return Root.Instance.Scene.GetComponent<GlobalComponent>().FixedRoot;
            }
            else if (type == UIWindowType.PopUp)
            {
                return Root.Instance.Scene.GetComponent<GlobalComponent>().PopUpRoot;
            }
            else if (type == UIWindowType.Other)
            {
                return Root.Instance.Scene.GetComponent<GlobalComponent>().OtherRoot;
            }

            Log.Error("uiroot type is error: " + type.ToString());
            return null;
        }
    }
}