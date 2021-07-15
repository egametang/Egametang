using System;

namespace ET
{
    public static class LoginHelper
    {
        #region 登陆
        /// <summary>
        /// 登陆
        /// </summary>
        public static void Login(Scene zoneScene, string loginData, string account)
        {
            Connect(zoneScene, loginData, account, ConnectSuccess, ConnectFailure).Coroutine();
        }
        /// <summary>
        /// 自动登陆
        /// </summary>
        public static void AutoLogin(Scene zoneScene)
        {
            Log.Debug("当前尝试自动连接服务器");
            //获取当前登陆数据
            LoginComponent loginState = zoneScene.GetComponent<LoginComponent>();
            if (loginState.CanReconnect)
                Connect(zoneScene, loginState.Address, loginState.LoginData, ConnectSuccess, ConnectFailure).Coroutine();
            else
                Log.Debug($"当前不能自动登陆 autoLogin:{loginState.IsAutoLogin} address:{loginState.Address} loginData:{loginState.LoginData}");
        }
        /// <summary>
        /// 返回登陆场景
        /// </summary>
        public static async ETVoid BackLogin(Scene zoneScene)
        {
            Log.Debug("当前用户选择返回登陆场景");
            LoginComponent loginState = zoneScene.GetComponent<LoginComponent>();
            //返回登陆设置为不自动登陆
            loginState.IsAutoLogin = false;
            //重置当前连接
            ResetConnect(zoneScene);
            //TODO:切换到登陆场景
            await Game.EventSystem.Publish(new EventType.ChangeScene() { Scene = zoneScene, SceneName = "Init" });
        }
        #endregion

        #region 连接验证服和网关服
        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <param name="zoneScene">scene</param>
        /// <param name="address">服务器地址</param>
        /// <param name="loginData">登陆数据</param>
        /// <param name="success">成功回调</param>
        /// <param name="failure">失败回调</param>
        /// <returns></returns>
        private static async ETVoid Connect(Scene zoneScene, string address, string loginData, Action<Scene, G2C_LoginGate> success, Action<Scene> failure)
        {
            try
            {
                LoginComponent loginState = zoneScene.GetComponent<LoginComponent>();
                if (!loginState.CanConnect)
                {
                    Log.Error("当前正在连接或已经连接不需要再次连接");
                    return;
                }
                //开始连接
                StartConnect(zoneScene);
                Log.Debug($"开始登陆 address:{address} loginData:{loginData}");
                //登陆验证服
                R2C_Login r2cLogin = await ConnectRealm(zoneScene, address, loginData);
                //登陆网关服
                G2C_LoginGate g2cLoginGate = await ConnectGate(zoneScene, r2cLogin);
                //登陆成功
                success.Invoke(zoneScene, g2cLoginGate);
                //保存登陆信息供重连使用
                loginState.Address = address;
                loginState.LoginData = loginData;
                
                //结束连接
                EndConnect(zoneScene, true);
            }
            catch (Exception e)
            {
                //登陆异常
                failure.Invoke(zoneScene);
                //失败重置
                ResetConnect(zoneScene);
                //结束连接
                EndConnect(zoneScene, false);
                Log.Error($"登陆异常 {e}");
            }
        }
        /// <summary>
        /// 重连服务器
        /// </summary>
        private static void Reconnect(Scene zoneScene)
        {
            Log.Debug("当前尝试重连服务器");
            //获取当前登陆数据
            LoginComponent loginState = zoneScene.GetComponent<LoginComponent>();
            if (loginState.CanReconnect)
                Connect(zoneScene, loginState.Address, loginState.LoginData, ReconnectSuccess, ReconnectFailure).Coroutine();
            else
                Log.Debug($"当前不能重连 autoLogin:{loginState.IsAutoLogin} address:{loginState.Address} loginData:{loginState.LoginData}");
        }
        /// <summary>
        /// 登陆验证服
        /// </summary>
        private static async ETTask<R2C_Login> ConnectRealm(Scene zoneScene, string address, string account)
        {
            try
            {
                Log.Debug($"连接验证服 address:{address}");
                // 创建一个ETModel层的Session
                R2C_Login response;
                using (Session relamSession = zoneScene.GetComponent<NetKcpComponent>().Create(NetworkHelper.ToIPEndPoint(address)))
                {
                    relamSession.AddComponent<SessionCallbackComponent>().DisposeCallback = RealmSessionDispose;
                    response = await relamSession.Call(new C2R_Login() { Account = account, Password = "111111" }) as R2C_Login;
                }
                if (response == null)
                    throw new Exception($"登陆验证服失败 Error:response is null");
                if (!string.IsNullOrEmpty(response.Message))
                    throw new Exception($"登陆验证服失败 Error:{response.Message}");
                Log.Debug($"连接验证服成功");
                return response;
            }
            catch (Exception e)
            {
                throw new Exception($"连接验证服异常 {e}");
            }
        }
        /// <summary>
        /// 登陆网关服
        /// </summary>
        private static async ETTask<G2C_LoginGate> ConnectGate(Scene zoneScene, R2C_Login r2CLogin)
        {
            try
            {
                Log.Debug($"连接网关服 address:{r2CLogin.Address}");
                // 创建一个gate Session,并且保存到SessionComponent中
                Session gateSession = zoneScene.GetComponent<NetKcpComponent>().Create(NetworkHelper.ToIPEndPoint(r2CLogin.Address));
                gateSession.AddComponent<SessionCallbackComponent>().DisposeCallback = GateSessionDispose;
           
                G2C_LoginGate response = await gateSession.Call(new C2G_LoginGate() { Key = r2CLogin.Key, GateId = r2CLogin.GateId}) as G2C_LoginGate;
     		
		        gateSession.AddComponent<PingComponent>();
                zoneScene.GetComponent<SessionComponent>().Session = gateSession;
				
                if (response == null)
                    throw new Exception($"登陆网关服失败 Error:response is null");
                if (!string.IsNullOrEmpty(response.Message))
                    throw new Exception($"登陆网关服失败 Error:{response.Message}");
                Log.Debug($"连接网关服成功");
                return response;
            }
            catch (Exception e)
            {
                throw new Exception($"连接网关服异常 {e}");
            }
        }
        #endregion

        #region Session Dispose回调
        /// <summary>
        /// 验证服session销毁
        /// </summary>
        private static void RealmSessionDispose(Session session)
        {
            Log.Debug("验证服Session销毁 ErrorCode:" + session?.Error);
            if (session == null) 
                return;
            string tips = GetError(session.Error);
            if (!string.IsNullOrEmpty(tips))
            {
                //TODO:打开Tips提示错误信息
                Game.EventSystem.Publish(new EventType.ShowFloatTips() { Scene = session.ZoneScene(), Tips = tips });
            }
        }
        /// <summary>
        /// 网关服session销毁
        /// </summary>
        private static void GateSessionDispose(Session session)
        {
            Log.Debug("网关服session销毁 ErrorCode:" + session?.Error);
            if (session == null) 
                return;
            string tips = GetError(session.Error);
            //网关Session正常销毁不需要重连
            if (session.Error != ErrorCode.ERR_Success)
            {
                Log.Debug("连接丢失断开");
                Scene zoneScene = session.ZoneScene();
                //重置连接
                ResetConnect(zoneScene);
            
                if (string.IsNullOrEmpty(tips))
                    Reconnect(zoneScene);//没有提示自动重连 尝试自动重连
                else
                    TryConnect(zoneScene, tips+"是否重连...");//有提示弹出提示用户选择是否重连
            }
        }
        /// <summary>
        /// 获取Session错误信息
        /// </summary>
        private static string GetError(int sessionError)
        {
            string tips = "";
            switch (sessionError)
            {
                case ErrorCode.ERR_Success:
                    Log.Debug("网关Session正常销毁");
                    break;
                case ErrorCode.ERR_PeerDisconnect:      //被服务器主动断开                     
                case ErrorCode.ERR_KcpRemoteDisconnect: //这是服务 没了
                    tips = "服务器断开连接！";
                    break; 
                case (int)System.Net.Sockets.SocketError.ConnectionRefused://服务器没开
                    tips = "服务器可能未启动！";
                    break;
                case (int)System.Net.Sockets.SocketError.NotConnected: //处理真实没有网络的情况 可以直接弹出tip不打开弹出一直重连相当于走default
                    tips = "网络断开,请检查网络连接！";
                    break;
                default:
                    //连接断开 1.收发消息超时 2.Socket错误 3.发消息 无法发现soket断开 4.网络断开 等
                    Log.Debug("网关Session销毁 自动重新链接");
                    break;
            }

            if (!string.IsNullOrEmpty(tips))
            {
                Log.Error($"Session Error:{sessionError} desc:{tips}");
            }
            return tips;
        }
        #endregion

        #region 开始结束重置连接
        /// <summary>
        /// 开始链接
        /// </summary>
        private static void StartConnect(Scene zoneScene)
        {
            Log.Debug("开始登陆");
            zoneScene.GetComponent<LoginComponent>().NetworkState = E_NetworkState.BebeingConnect;
            //打开菊花
            //UIHelper.ShowWaiting();
        }
        /// <summary>
        /// 结束链接
        /// </summary>
        private static void EndConnect(Scene zoneScene, bool isConnect)
        {
            Log.Debug("结束登陆");
            LoginComponent loginState =  zoneScene.GetComponent<LoginComponent>();
            if (isConnect)
            {
                loginState.NetworkState = E_NetworkState.Connect;
                loginState.ReconnectionCount = 0;
                loginState.IsAutoLogin = true;//登陆成功后设置为可以自动登陆
            }
            //关闭菊花
            //UIHelper.HideWaiting();
        }
        /// <summary>
        /// 断开链接并重置状态
        /// </summary>
        private static void ResetConnect(Scene zoneScene)
        {
            Log.Debug("断开链接并重置状态");
            //状态改为断开连接
            zoneScene.GetComponent<LoginComponent>().NetworkState = E_NetworkState.Disconnect;
            zoneScene.GetComponent<SessionComponent>().Session?.Dispose();
        }
        #endregion

        #region 连接成功或失败
        /// <summary>
        /// 链接成功
        /// </summary>
        private static async void ConnectSuccess(Scene zoneScene, G2C_LoginGate g2CLoginGate)
        {
            try
            {
                Log.Debug($"登陆成功");
                // ETModel.SDK.SdkManager.JpushSetAlias(g2CLoginGate.User.UserId.ToString());
                // #region Test
                // //保存用户信息 创建Player
                // // Player player = ComponentFactory.CreateWithId<Player>(g2CLoginGate.User.UserId);
                // // PlayerComponent playerComponent = Game.Scene.GetComponent<PlayerComponent>();
                // // playerComponent.MyPlayer = player;
                // #endregion
                await Game.EventSystem.Publish(new EventType.LoginFinish() {ZoneScene = zoneScene});
            }
            catch (Exception e)
            {
                throw new Exception($"登录成功切换场景异常 重置到登录场景 {e}");
            }
        }
        /// <summary>
        /// 链接失败
        /// </summary>
        private static void ConnectFailure(Scene zoneScene)
        {
            Log.Debug($"登陆失败");
            // UIHelper.ShowTips("连接失败");
        }
        #endregion

        #region 重接成功或失败
        /// <summary>
        /// 重连成功
        /// </summary>
        private static void ReconnectSuccess(Scene zoneScene, G2C_LoginGate g2CLoginGate)
        {
            Log.Debug($"重连成功");
            // BaseModel.SetSelf(g2CLoginGate.User);
        }
        /// <summary>
        /// 重连失败
        /// </summary>
        private static async void ReconnectFailure(Scene zoneScene)
        {
            LoginComponent loginState = zoneScene.GetComponent<LoginComponent>();
            Log.Debug($"重连失败 Count:{loginState.ReconnectionCount}");
            //断线自动重连15次，15次后提示是否重连
            if (loginState.ReconnectionCount < 15)
            {
                await TimerComponent.Instance.WaitAsync(1000);
                if (loginState.CanConnect)
                {
                    loginState.ReconnectionCount++;
                    Log.Debug($"第{loginState.ReconnectionCount}次自动重连");
                    Reconnect(zoneScene);
                }
            }
            else
            {
                loginState.ReconnectionCount = 0;
                //弹窗提示是否重连
                TryConnect(zoneScene, "多次重连失败, 是否继续重连!");
            }
        }
        #endregion

        #region 重连界面操作
        /// <summary>
        /// 弹窗用户选择是否重连
        /// </summary>
        private static void TryConnect(Scene zoneScene, string tips)
        {
            Game.EventSystem.Publish(new EventType.ShowMessageBox() 
            {
                Scene = zoneScene, 
                Tips = tips, 
                CallBack = isReconnect =>
                {
                    if (isReconnect)
                        Reconnect(zoneScene);//尝试重连
                    else
                        BackLogin(zoneScene).Coroutine();//返回登陆
                }
            });
        }
        #endregion
 
    }
}
