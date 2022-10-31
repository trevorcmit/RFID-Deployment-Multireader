using System;
using CSLibrary.Constants;


namespace CSLibrary {
	public partial class RFIDReader {

		public Result TurnCarrierWaveOn(bool isDataMode) {
			if (isDataMode) return Result.NOT_SUPPORTED;
			_deviceHandler.SendAsync(0, 0, DOWNLINKCMD.RFIDCMD, PacketData(0xf000, (UInt32)HST_CMD.CWON), HighLevelInterface.BTWAITCOMMANDRESPONSETYPE.WAIT_BTAPIRESPONSE_COMMANDENDRESPONSE);

			return Result.OK;
		}

		public Result TurnCarrierWaveOff() {
			_deviceHandler.SendAsync(0, 0, DOWNLINKCMD.RFIDCMD, PacketData(0xf000, (UInt32)HST_CMD.CWOFF), HighLevelInterface.BTWAITCOMMANDRESPONSETYPE.WAIT_BTAPIRESPONSE_COMMANDENDRESPONSE);
			return Result.OK;
		}

	}
}
