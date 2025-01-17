#include "pch.h"
#include "Shared/Video/SystemHud.h"
#include "Shared/Video/DebugHud.h"
#include "Shared/Movies/MovieManager.h"
#include "Shared/MessageManager.h"
#include "Shared/BaseControlManager.h"
#include "Shared/Video/DrawStringCommand.h"
#include "Shared/Interfaces/IMessageManager.h"

SystemHud::SystemHud(Emulator* emu, DebugHud* hud)
{
	_emu = emu;
	_hud = hud;
	MessageManager::RegisterMessageManager(this);
}

SystemHud::~SystemHud()
{
	MessageManager::UnregisterMessageManager(this);
}

void SystemHud::Draw(uint32_t width, uint32_t height)
{
	_screenWidth = width;
	_screenHeight = height;
	DrawCounters();
	DrawMessages();

	bool showMovieIcons = _emu->GetSettings()->GetPreferences().ShowMovieIcons;
	if(_emu->IsPaused()) {
		DrawPauseIcon();
	} else if(showMovieIcons && _emu->GetMovieManager()->Playing()) {
		DrawPlayIcon();
	} else if(showMovieIcons && _emu->GetMovieManager()->Recording()) {
		DrawRecordIcon();
	}
}

void SystemHud::DrawMessage(MessageInfo &msg, int& lastHeight)
{
	//Get opacity for fade in/out effect
	uint8_t opacity = (uint8_t)(msg.GetOpacity() * 255);
	int textLeftMargin = 4;

	string text = "[" + msg.GetTitle() + "] " + msg.GetMessage();

	int maxWidth = _screenWidth - textLeftMargin;
	TextSize size = DrawStringCommand::MeasureString(text, maxWidth);
	lastHeight += size.Y;
	DrawString(text, textLeftMargin, _screenHeight - lastHeight, opacity);
}

void SystemHud::DrawString(string text, int x, int y, uint8_t opacity)
{
	int maxWidth = _screenWidth - x;
	opacity = 255 - opacity;
	for(int i = -1; i <= 1; i++) {
		for(int j = -1; j <= 1; j++) {
			_hud->DrawString(x + i, y + j, text, 0 | (opacity << 24), 0xFF000000, 1, -1, maxWidth);
		}
	}
	_hud->DrawString(x, y, text, 0xFFFFFF | (opacity << 24), 0xFF000000, 1, -1, maxWidth);
}

void SystemHud::ShowFpsCounter(int lineNumber)
{
	int yPos = 10 + 10 * lineNumber;
	if(_fpsTimer.GetElapsedMS() > 1000) {
		//Update fps every sec
		uint32_t frameCount = _emu->GetFrameCount();
		if(_lastFrameCount > frameCount) {
			_currentFPS = 0;
		} else {
			_currentFPS = (int)(std::round((double)(frameCount - _lastFrameCount) / (_fpsTimer.GetElapsedMS() / 1000)));
			_currentRenderedFPS = (int)(std::round((double)(_renderedFrameCount - _lastRenderedFrameCount) / (_fpsTimer.GetElapsedMS() / 1000)));
		}
		_lastFrameCount = frameCount;
		_lastRenderedFrameCount = _renderedFrameCount;
		_fpsTimer.Reset();
	}

	if(_currentFPS > 5000) {
		_currentFPS = 0;
	}
	if(_currentRenderedFPS > 5000) {
		_currentRenderedFPS = 0;
	}

	string fpsString = string("FPS: ") + std::to_string(_currentFPS); // +" / " + std::to_string(_currentRenderedFPS);
	uint32_t length = DrawStringCommand::MeasureString(fpsString).X;
	DrawString(fpsString, _screenWidth - 8 - length, yPos);
}

void SystemHud::ShowGameTimer(int lineNumber)
{
	int yPos = 10 + 10 * lineNumber;
	uint32_t frameCount = _emu->GetFrameCount();
	double frameRate = _emu->GetFps();
	uint32_t seconds = (uint32_t)(frameCount / frameRate) % 60;
	uint32_t minutes = (uint32_t)(frameCount / frameRate / 60) % 60;
	uint32_t hours = (uint32_t)(frameCount / frameRate / 3600);

	std::stringstream ss;
	ss << std::setw(2) << std::setfill('0') << hours << ":";
	ss << std::setw(2) << std::setfill('0') << minutes << ":";
	ss << std::setw(2) << std::setfill('0') << seconds;

	string text = ss.str();
	uint32_t length = DrawStringCommand::MeasureString(text).X;
	DrawString(ss.str(), _screenWidth - 8 - length, yPos);
}

void SystemHud::ShowFrameCounter(int lineNumber)
{
	int yPos = 10 + 10 * lineNumber;
	uint32_t frameCount = _emu->GetFrameCount();

	string frameCounter = MessageManager::Localize("Frame") + ": " + std::to_string(frameCount);
	uint32_t length = DrawStringCommand::MeasureString(frameCounter).X;
	DrawString(frameCounter, _screenWidth - 8 - length, yPos);
}

void SystemHud::ShowLagCounter(int lineNumber)
{
	int yPos = 10 + 10 * lineNumber;
	uint32_t count = _emu->GetLagCounter();

	string lagCounter = MessageManager::Localize("Lag") + ": " + std::to_string(count);
	uint32_t length = DrawStringCommand::MeasureString(lagCounter).X;
	DrawString(lagCounter, _screenWidth - 8 - length, yPos);
}

void SystemHud::DrawCounters()
{
	int lineNumber = 0;
	PreferencesConfig cfg = _emu->GetSettings()->GetPreferences();

	if(_emu->IsRunning()) {
		if(cfg.ShowFps) {
			ShowFpsCounter(lineNumber++);
		}
		if(cfg.ShowGameTimer) {
			ShowGameTimer(lineNumber++);
		}
		if(cfg.ShowFrameCounter) {
			ShowFrameCounter(lineNumber++);
		}
		if(cfg.ShowLagCounter) {
			ShowLagCounter(lineNumber++);
		}
		_renderedFrameCount++;
	}
}

void SystemHud::DisplayMessage(string title, string message)
{
	auto lock = _msgLock.AcquireSafe();
	_messages.push_front(std::make_unique<MessageInfo>(title, message, 3000));
}

void SystemHud::DrawMessages()
{
	auto lock = _msgLock.AcquireSafe();

	_messages.remove_if([](unique_ptr<MessageInfo>& msg) { return msg->IsExpired(); });

	int counter = 0;
	int lastHeight = 3;
	for(unique_ptr<MessageInfo>& msg : _messages) {
		if(counter < 4) {
			DrawMessage(*msg.get(), lastHeight);
		} else {
			break;
		}
		counter++;
	}
}

void SystemHud::DrawBar(int x, int y, int width, int height)
{
	_hud->DrawRectangle(x, y, width, height, 0xFFFFFF, true, 1);
	_hud->DrawLine(x, y + 1, x + width, y + 1, 0x4FBECE, 1);
	_hud->DrawLine(x+1, y, x+1, y + height, 0x4FBECE, 1);

	_hud->DrawLine(x + width - 1, y, x + width - 1, y + height, 0xCC9E22, 1);
	_hud->DrawLine(x, y + height - 1, x + width, y + height - 1, 0xCC9E22, 1);

	_hud->DrawLine(x, y, x + width, y, 0x303030, 1);
	_hud->DrawLine(x, y, x, y + height, 0x303030, 1);

	_hud->DrawLine(x + width, y, x + width, y + height, 0x303030, 1);
	_hud->DrawLine(x, y + height, x + width, y + height, 0x303030, 1);
}

void SystemHud::DrawPauseIcon()
{
	DrawBar(10, 7, 5, 12);
	DrawBar(17, 7, 5, 12);
}

void SystemHud::DrawPlayIcon()
{
	int x = 12;
	int y = 9;
	int width = 5;
	int height = 8;
	int borderColor = 0x00000;
	int color = 0xFFFFFF;

	for(int i = 0; i < width; i++) {
		int left = x + i * 2;
		int top = y + i;
		_hud->DrawLine(left, top - 1, left, y + height - i + 1, borderColor, 1);
		_hud->DrawLine(left + 1, top - 1, left + 1, y + height - i + 1, borderColor, 1);

		if(i > 0) {
			_hud->DrawLine(left, top, left, y + height - i, color, 1);
		}

		if(i < width - 1) {
			_hud->DrawLine(left + 1, top, left + 1, y + height - i, color, 1);
		}
	}
}

void SystemHud::DrawRecordIcon()
{
	int x = 12;
	int y = 8;
	int borderColor = 0x00000;
	int color = 0xFF0000;

	_hud->DrawRectangle(x + 3, y, 4, 10, borderColor, true, 1);
	_hud->DrawRectangle(x, y + 3, 10, 4, borderColor, true, 1);
	_hud->DrawRectangle(x + 2, y + 1, 6, 8, borderColor, true, 1);
	_hud->DrawRectangle(x + 1, y + 2, 8, 6, borderColor, true, 1);

	_hud->DrawRectangle(x + 3, y + 1, 4, 8, color, true, 1);
	_hud->DrawRectangle(x + 2, y + 2, 6, 6, color, true, 1);
	_hud->DrawRectangle(x + 1, y + 3, 8, 4, color, true, 1);
}
