using System;
using System.IO;

namespace ET
{
    public class SessionCallbackComponent: Entity
    {
        public Action<Session, ushort, MemoryStream> MessageCallback;
        public Action<Session> DisposeCallback;

        public override void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }
            this.DisposeCallback?.Invoke(this.GetParent<Session>());
            base.Dispose();
        }
    }
}