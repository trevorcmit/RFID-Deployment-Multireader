using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CSLibrary.Events {
    using CSLibrary.Structures;
    using CSLibrary.Constants;

    public class OnReaderStateChangedEventArgs : EventArgs {
        public readonly object info;
        public readonly ReaderCallbackType type = ReaderCallbackType.UNKNOWN;

        /// <param name="info">Tag Information</param>
        /// <param name="type">Callback Type</param>
        public OnReaderStateChangedEventArgs(object info, ReaderCallbackType type) {
            this.info = info;
            this.type = type;
        }
    }

    public class OnAsyncCallbackEventArgs : EventArgs {
        public readonly TagCallbackInfo info = new TagCallbackInfo();
        public readonly CallbackType type = CallbackType.UNKNOWN;

        /// <param name="info">Tag Information</param>
        /// <param name="type">Callback Type</param>
        public OnAsyncCallbackEventArgs(TagCallbackInfo info, CallbackType type) {
            this.info = info;
            this.type = type;
        }
    }

    public class OnAccessCompletedEventArgs : EventArgs {
        public readonly bool success = false;
        public readonly Bank bank = Bank.UNKNOWN;
        public readonly TagAccess access = TagAccess.UNKNOWN;
        public readonly IBANK data;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="success">Access Result</param>
        /// <param name="bank">Access bank</param>
        /// <param name="access">Access type</param>
        /// <param name="data">Access Data only use for Tag Read operation</param>
        //public OnAccessCompletedEventArgs(bool success, Bank bank, TagAccess access, IBANK data)
        public OnAccessCompletedEventArgs(bool success, Bank bank, TagAccess access, IBANK data) {
            this.access = access;
            this.success = success;
            this.bank = bank;
            this.data = data;
        }
    }

    public class OnFM13DTAccessCompletedEventArgs : EventArgs {   
        public readonly bool success = false;
        public readonly FM13DTAccess access = FM13DTAccess.UNKNOWN;

        /// <param name="success">Access Result</param>
        /// <param name="access">Access type</param>
        public OnFM13DTAccessCompletedEventArgs(FM13DTAccess access, bool success) {
            this.access = access;
            this.success = success;
        }
    }

    public class OnStateChangedEventArgs : EventArgs {
        public readonly RFState state = RFState.IDLE;

        /// <param name="state"></param>
        public OnStateChangedEventArgs(RFState state) {
            this.state = state;
        }
    }
}
