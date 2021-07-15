using UnityEngine;

namespace ET
{
    public enum E_NetworkState
    {
        /// <summary>
        /// 开始连接
        /// </summary>
        BebeingConnect,
        /// <summary>
        /// 已经连接
        /// </summary>
        Connect,
        /// <summary>
        /// 断开连接
        /// </summary>
        Disconnect,
    }
    
    public class LoginComponent : Entity
    {
        /// <summary>
        /// 当前连接状态
        /// </summary>
        public E_NetworkState NetworkState = E_NetworkState.Disconnect;
        /// <summary>
        /// 尝试重连次数
        /// </summary>
        public int ReconnectionCount = 0;
        /// <summary>
        /// 是否可以进行连接
        /// </summary>
        public bool CanConnect => this.NetworkState == E_NetworkState.Disconnect;
        /// <summary>
        /// 是否可以自动重连
        /// </summary>
        public bool CanReconnect => this.IsAutoLogin && !string.IsNullOrEmpty(this.Address) && !string.IsNullOrEmpty(this.LoginData);
        /// <summary>
        /// 是否可以自动登陆
        /// </summary>
        public bool IsAutoLogin
        {
            set
            {
                PlayerPrefs.SetInt("IsAutoLogin", value? 1 : 0);
            }
            get
            {
                return PlayerPrefs.GetInt("IsAutoLogin", 0) == 1;
            }
        }
        /// <summary>
        /// 永久化验证服地址
        /// </summary>
        public string Address
        {
            set
            {
                PlayerPrefs.SetString("Address", value);
            }
            get
            {
                return PlayerPrefs.GetString("Address");
            }
        }
        /// <summary>
        /// 永久化登陆信息
        /// </summary>
        public string LoginData
        {
            set
            {
                PlayerPrefs.SetString("LoginData", value);
            }
            get
            {
                return PlayerPrefs.GetString("LoginData");
            }
        }
    }
}