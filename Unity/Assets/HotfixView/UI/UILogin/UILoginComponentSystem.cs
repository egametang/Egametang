using System;
using System.Net;

using UnityEngine;
using UnityEngine.UI;

namespace ET
{
	public class UILoginComponentAwakeSystem : AwakeSystem<UILoginComponent>
	{
		public override void Awake(UILoginComponent self)
		{
			ReferenceCollector rc = self.GetParent<UI>().GameObject.GetComponent<ReferenceCollector>();
			self.loginBtn = rc.Get<GameObject>("LoginBtn");
			self.loginBtn.GetComponent<Button>().onClick.AddListener(self.OnLogin);
			self.account = rc.Get<GameObject>("Account");
			//打开登陆界面尝试自动登陆
			LoginHelper.AutoLogin(self.DomainScene());
		}
	}
	
	public static class UILoginComponentSystem
	{
		public static void OnLogin(this UILoginComponent self)
		{
			LoginHelper.Login(self.DomainScene(), "127.0.0.1:10002", self.account.GetComponent<InputField>().text);
		}
	}
}
