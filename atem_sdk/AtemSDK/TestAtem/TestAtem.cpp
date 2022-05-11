// TestAtem.cpp : このファイルには 'main' 関数が含まれています。プログラム実行の開始と終了がそこで行われます。
//


#include <iostream>
#include <chrono>
#include <thread>
#include <atlstr.h>

#include "BMDSwitcherAPI_h.h"

class StreamRTMPMonitor : public IBMDSwitcherStreamRTMPCallback
{
public:
  StreamRTMPMonitor() : mRefCount(1) { }

protected:
  virtual ~StreamRTMPMonitor() { }

public:
  HRESULT QueryInterface(const IID &, void **) {
    return S_OK;
  }
  ULONG AddRef(void) {
    return InterlockedIncrement(&mRefCount);
  }
  ULONG Release(void) {
    int newCount = InterlockedDecrement(&mRefCount);
    if (newCount == 0)
      delete this;
    return newCount;
  }

  HRESULT STDMETHODCALLTYPE	Notify(BMDSwitcherStreamRTMPEventType eventType) {
    std::cout << "Notify ----" << std::endl;

    if (eventType == bmdSwitcherStreamRTMPEventTypeKeyChanged )
    {
      std::cout << "Key is Changed!" << std::endl;
    }
    else {
      std::cout << eventType << std::endl;
    }

    return S_OK;
  }

  HRESULT STDMETHODCALLTYPE	NotifyStatus(BMDSwitcherStreamRTMPState stateType, BMDSwitcherStreamRTMPError error) {
    std::cout << "NotifyStatus ----" << std::endl;

    if (stateType == bmdSwitcherStreamRTMPStateIdle) {
      std::cout << "Switcher is Idle." << std::endl;
    } else {
    }

    std::cout << stateType << std::endl;
    std::cout << error << std::endl;

    return S_OK;
  }

private:
  LONG mRefCount;
};



class AtemControl {
public:
  AtemControl() : switcherDiscovery(NULL), switcher(NULL) {
    HRESULT hr;

    // Initialize COM and Switcher related members
    if (FAILED(CoInitialize(NULL))) {
      std::cout << "CoInitialize failed." << std::endl;
      return;
    }

    // COM作成
    hr = CoCreateInstance(CLSID_CBMDSwitcherDiscovery, NULL, CLSCTX_ALL, IID_IBMDSwitcherDiscovery, (void**)&(this->switcherDiscovery));
    if (FAILED(hr))
    {
      std::cout << "Could not create Switcher Discovery Instance.\nATEM Switcher Software may not be installed." << std::endl;
      return;
    }
  }

  virtual ~AtemControl() {}

  HRESULT connect(BSTR address) {
    HRESULT hr;

    BMDSwitcherConnectToFailure failReason;

    // 接続
    hr = switcherDiscovery->ConnectTo(address, &switcher, &failReason);

    if (SUCCEEDED(hr)) {
      // switcherConnected();
    } else {
      switch (failReason)
      {
      case bmdSwitcherConnectToFailureNoResponse:
        std::cout << "No response from Switcher" << std::endl;
        break;
      case bmdSwitcherConnectToFailureIncompatibleFirmware:
        std::cout << "Switcher has incompatible firmware" << std::endl;
        break;
      default:
        std::cout << "Connection failed for unknown reason" << std::endl;
        break;
      }
    }

    return hr;
  }

  HRESULT getStreamKey() { 
    HRESULT hr;
    /*
    // get url
    BSTR url;
    hr = switcherStreamRTMP->GetUrl(&url);
    if (FAILED(hr))
    {
      std::cout << "Could not get stream url." << std::endl;
      return;
    }
    wprintf_s(url);
    wprintf_s(_T("\n"));
    SysFreeString(url);

    // get key
    BSTR key;
    hr = switcherStreamRTMP->GetKey(&key);
    if (FAILED(hr))
    {
      std::cout << "Could not get stream key." << std::endl;
      return;
    }
    wprintf_s(key);
    wprintf_s(_T("\n"));
    SysFreeString(key);
    */

  }

  bool setStreamKey(BSTR newKey){
    HRESULT hr;

    IBMDSwitcherStreamRTMP* switcherStreamRTMP = this->getSwitcherStreamRTMP();

    // イベントコールバックの登録
    StreamRTMPMonitor *streamRTMPMonitor = new StreamRTMPMonitor();
    switcherStreamRTMP->AddCallback(streamRTMPMonitor);

    if (!this->isStreamIdle(switcherStreamRTMP)) {
      return false;
    }

    // set key
    hr = switcherStreamRTMP->SetKey(newKey);
    if (FAILED(hr))
    {
      std::cout << "Could not set stream key." << std::endl;
      return false;
    }

    std::cout << "Success to set stream key." << std::endl;
    return true;
  }

private:
  IBMDSwitcherStreamRTMP* getSwitcherStreamRTMP() {
    HRESULT hr;
    // Stream

    IBMDSwitcherStreamRTMP* switcherStreamRTMP = NULL;
    hr = switcher->QueryInterface(IID_IBMDSwitcherStreamRTMP, (void**)&switcherStreamRTMP);
    if (FAILED(hr))
    {
      std::cout << "Could not create Switcher Stream RTMP Instance." << std::endl;
      return NULL;
    }

    return switcherStreamRTMP;

  }

  bool isStreamIdle(IBMDSwitcherStreamRTMP* switcherStreamRTMP) {
    HRESULT hr;

    BMDSwitcherStreamRTMPState state;
    BMDSwitcherStreamRTMPError error;
    hr = switcherStreamRTMP->GetStatus(&state, &error);
    if (state == bmdSwitcherStreamRTMPStateIdle) {
      std::cout << "Switcher is Idle." << std::endl;
      return true;
    }
    else {
      std::cout << "Switcher is not Idle." << std::endl;
      return false;
    }
  }

  IBMDSwitcherDiscovery* switcherDiscovery = NULL;
  IBMDSwitcher* switcher = NULL;

};

int main(int argc, char* argv[])
{
  AtemControl atem;

  std::cout << "Connect to " << argv[1] << std::endl;

  CString address = argv[1];
  BSTR addressBstr = address.AllocSysString();
  HRESULT hr = atem.connect(addressBstr);
  SysFreeString(addressBstr);
  if (FAILED(hr)) {
    return 1;
  }

  std::cout << "Set key to " << argv[2] << std::endl;

  CString key = argv[2];
  BSTR new_key = SysAllocString(key);
  atem.setStreamKey(new_key);
  SysFreeString(new_key);

  std::this_thread::sleep_for(std::chrono::milliseconds(3000));

  return 0;
}

