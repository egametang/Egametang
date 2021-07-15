using System;

namespace ET
{
    namespace EventType
    {
        public struct AppStart
        {
        }

        public struct ChangePosition
        {
            public Unit Unit;
        }

        public struct ChangeRotation
        {
            public Unit Unit;
        }

        public struct PingChange
        {
            public Scene ZoneScene;
            public long Ping;
        }
        
        public struct AfterCreateZoneScene
        {
            public Scene ZoneScene;
        }
        
        public struct AfterCreateLoginScene
        {
            public Scene LoginScene;
        }

        public struct AppStartInitFinish
        {
            public Scene ZoneScene;
        }

        public struct LoginFinish
        {
            public Scene ZoneScene;
        }

        public struct LoadingBegin
        {
            public Scene Scene;
        }

        public struct LoadingFinish
        {
            public Scene Scene;
        }

        public struct EnterMapFinish
        {
            public Scene ZoneScene;
        }

        public struct AfterUnitCreate
        {
            public Unit Unit;
        }
        
        public struct MoveStart
        {
            public Unit Unit;
        }

        public struct MoveStop
        {
            public Unit Unit;
        }

        /// <summary>
        /// 切换场景
        /// </summary>
        public struct ChangeScene
        {
            public Scene Scene;
            public string SceneName;//场景名字
        }

        /// <summary>
        /// 打开选择提示框
        /// </summary>
        public struct ShowMessageBox
        {
            public Scene Scene;
            public string Tips;//提示内容
            public Action<bool> CallBack;//选择回调
        }

        /// <summary>
        /// 飘字提示
        /// </summary>
        public struct ShowFloatTips
        {
            public Scene Scene;
            public string Tips;//飘字内容
        }
    }
}