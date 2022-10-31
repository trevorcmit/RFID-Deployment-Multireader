﻿// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;
using CSLibrary.Constants;
// using CSLibrary.Structures;


namespace CSLibrary {
    public partial class RFIDReader {
        private RegionCode m_save_region_code = RegionCode.UNKNOWN;
        private bool m_save_fixed_channel = false;
        private bool m_save_agile_channel = false;
        private uint m_save_freq_channel = 0;
        private double m_save_selected_freq = 0;

        public uint SelectedChannel {
            get {return m_save_freq_channel;}
        }

        public double SelectedFrequencyBand {
            get {return m_save_selected_freq;}
        }

        /// <param name="prof">Region Code</param>
        /// <param name="channel">Channel number start from zero, you can get the available channels 
        /// from CSLibrary.HighLevelInterface.AvailableFrequencyTable(CSLibrary.Constants.RegionCode)</param>
        public Result SetFixedChannel(RegionCode prof = RegionCode.CURRENT, uint channel = 0) {

            if (m_save_fixed_channel == true && m_save_region_code == prof && m_save_freq_channel == channel) {
                if (currentInventoryFreqRevIndex == null) currentInventoryFreqRevIndex = new uint[1] { channel };
                return Result.OK;
            }

            uint Reg0x700 = 0;

            if (IsHoppingChannelOnly) return Result.INVALID_PARAMETER;

            if (!GetActiveRegionCode().Contains(prof)) return Result.INVALID_PARAMETER;

            // disable agile mode
            MacReadRegister(MACREGISTER.HST_ANT_CYCLES /*0x700*/, ref Reg0x700);
            Reg0x700 &= ~0x01000000U;
            MacWriteRegister(MACREGISTER.HST_ANT_CYCLES /*0x700*/, Reg0x700);

            {
                //Result status = Result.OK;
                uint TotalCnt = FreqChnCnt(prof);
                uint[] freqTable = FreqTable(prof);
                uint i = 0;

                // Check Parameters
                if (!FreqChnWithinRange(channel, prof) || freqTable == null) return Result.INVALID_PARAMETER;

                int Index = FreqSortedIdxTbls(prof, channel);
                if (Index < 0) return Result.INVALID_PARAMETER;

                //Enable channel
                SetFrequencyBand(0, BandState.ENABLE, freqTable[Index], GetPllcc(prof));
                i = 1;

                //Disable channels
                for (uint j = i; j < MAXFRECHANNEL; j++) {
                    SetFrequencyBand(j, BandState.DISABLE, 0, 0);
                }

                SetRadioLBT(LBT.OFF);

                m_save_region_code = prof;
                m_save_freq_channel = channel;
                m_save_fixed_channel = true;
                m_save_agile_channel = false;
                m_save_selected_freq = GetAvailableFrequencyTable(prof)[channel];
            }
#if nouse
            catch (ReaderException ex) {}
            catch {
                m_Result = Result.SYSTEM_CATCH_EXCEPTION;
            }
#endif
            currentInventoryFreqRevIndex = new uint[1] { channel };
            return Result.OK;
        }

        /// <param name="prof">Country Profile</param>
        /// <returns>Result</returns>
        public Result SetHoppingChannels(RegionCode prof) {
            if (!(m_save_fixed_channel == true || m_save_agile_channel == true) && m_save_region_code == prof)
            {
                if (currentInventoryFreqRevIndex == null)
                    currentInventoryFreqRevIndex = FreqIndex(m_save_region_code);
                return Result.OK;
            }

            if (IsFixedChannelOnly || !GetActiveRegionCode().Contains(prof))
                return Result.INVALID_PARAMETER;

            uint TotalCnt = FreqChnCnt(prof);
            uint[] freqTable = FreqTable(prof);

            //Enable channels
            for (uint i = 0; i < TotalCnt; i++)
            {
                SetFrequencyBand(i, BandState.ENABLE, freqTable[i], GetPllcc(prof));
            }

            //Disable channels
            for (uint i = TotalCnt; i < 50; i++)
            {
                SetFrequencyBand(i, BandState.DISABLE, 0, 0);
            }

            SetRadioLBT(LBT.OFF);

            m_save_region_code = prof;
            m_save_fixed_channel = false;
            m_save_agile_channel = false;
            m_Result = Result.OK;

            currentInventoryFreqRevIndex = FreqIndex(m_save_region_code);

            return Result.OK;
        }

        /// <returns></returns>
        public Result SetHoppingChannels()
        {
            return SetHoppingChannels(m_save_region_code);
        }

        /// <param name="prof">Country Profile</param>
        /// <returns>Result</returns>
        public Result SetAgileChannels(RegionCode prof) {
            if (!(m_save_fixed_channel == true || m_save_agile_channel == false) && m_save_region_code == prof) {
                if (currentInventoryFreqRevIndex == null) currentInventoryFreqRevIndex = FreqIndex(m_save_region_code);
                return Result.OK;
            }
            uint Reg0x700 = 0;

            if (!GetActiveRegionCode().Contains(prof) || (prof != RegionCode.ETSI && prof != RegionCode.JP))
                return Result.INVALID_PARAMETER;

            uint TotalCnt = FreqChnCnt(prof);
            uint[] freqTable = FreqTable(prof);

            //Enable channels
            for (uint i = 0; i < TotalCnt; i++) {
                SetFrequencyBand(i, BandState.ENABLE, freqTable[i], GetPllcc(prof));
            }
            //Disable channels
            for (uint i = TotalCnt; i < 50; i++) {
                SetFrequencyBand(i, BandState.DISABLE, 0, 0);
            }

            SetRadioLBT(LBT.OFF);

            m_save_region_code = prof;
            m_save_fixed_channel = false;
            m_save_agile_channel = true;

            MacReadRegister(MACREGISTER.HST_ANT_CYCLES /*0x700*/, ref Reg0x700);
            Reg0x700 |= 0x01000000U;
            MacWriteRegister(MACREGISTER.HST_ANT_CYCLES /*0x700*/, Reg0x700);

            currentInventoryFreqRevIndex = FreqIndex(m_save_region_code);
            return Result.OK;
        }

        internal Result InitDefaultChannel()
        {
            switch (m_save_country_code)
            {
                case 1:     // ETSI
                    m_save_region_code = RegionCode.ETSI;
                    m_save_fixed_channel = true;
                    m_save_agile_channel = false;
                    m_save_freq_channel = 0;
                    break;

                case 2:     // FCC
                    if (m_oem_freq_modification_flag == 0x00)
                        m_save_region_code = RegionCode.FCC;
                    else
                    {
                        switch (m_oem_special_country_version)
                        {
                            default: // and case 0x2a555341
                                m_save_region_code = RegionCode.FCC;
                                break;
                            case 0x4f464341:
                                m_save_region_code = RegionCode.HK;
                                break;
                            case 0x2a2a4153:
                                m_save_region_code = RegionCode.AU;
                                break;
                            case 0x2a2a4e5a:
                                m_save_region_code = RegionCode.NZ;
                                break;
                            case 0x20937846:
                                m_save_region_code = RegionCode.ZA;
                                break;
                        }
                    }
                    m_save_fixed_channel = false;
                    m_save_agile_channel = false;
                    break;

                case 4:     // 
                    m_save_region_code = RegionCode.TW;
                    m_save_fixed_channel = false;
                    m_save_agile_channel = false;
                    break;

                case 6:     // 
                    m_save_region_code = RegionCode.KR;
                    m_save_fixed_channel = false;
                    m_save_agile_channel = false;
                    break;

                case 7:     // 
                    m_save_region_code = RegionCode.CN;
                    m_save_fixed_channel = false;
                    m_save_agile_channel = false;
                    break;

                case 8:     // 
                    m_save_region_code = RegionCode.JP;
                    m_save_fixed_channel = true;
                    m_save_agile_channel = false;
                    m_save_freq_channel = 0;
                    break;

                case 9:     // 
                    m_save_region_code = RegionCode.ETSIUPPERBAND;
                    m_save_fixed_channel = true;
                    m_save_agile_channel = false;
                    m_save_freq_channel = 0;
                    break;

                default:
                    break;
            }

            return Result.OK;
        }

        public Result SetDefaultChannel() {
            switch (m_save_country_code) {
                case 1:     // ETSI
                    SetFixedChannel(RegionCode.ETSI, 0);
                    break;

                case 2:     // FCC
                    if (m_oem_freq_modification_flag == 0x00) SetHoppingChannels(RegionCode.FCC);
                    else {
                        switch (m_oem_special_country_version)
                        {
                            default: // and case 0x2a555341
                                SetHoppingChannels(RegionCode.FCC);
                                break;
                            case 0x4f464341:
                                SetHoppingChannels(RegionCode.HK);
                                break;
                            case 0x2a2a4153:
                                SetHoppingChannels(RegionCode.AU);
                                break;
                            case 0x2a2a4e5a:
                                SetHoppingChannels(RegionCode.NZ);
                                break;
                            case 0x20937846:
                                SetHoppingChannels(RegionCode.ZA);
                                break;
                        }
                    }
                    break;

                case 4:     // 
                    SetHoppingChannels(RegionCode.TW);
                    break;

                case 6:     // 
                    SetHoppingChannels(RegionCode.KR);
                    break;

                case 7:     // 
                    SetHoppingChannels(RegionCode.CN);
                    break;
                case 8:     // 
                    SetFixedChannel(RegionCode.JP, 0);
                    break;
                case 9:     // 
                    SetFixedChannel(RegionCode.ETSIUPPERBAND, 0);
                    break;

                default:
                    break;
            }

            return Result.OK;
        }
    }
}