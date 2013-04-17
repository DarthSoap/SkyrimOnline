﻿using Skyrim.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Skyrim.Game.IO
{
    public class InputManager
    {
        public InputManager()
        {
            UIEnabled = false;
        }

        public void Update()
        {
            Event e = null;
            while ((e = Skyrim.Script.Input.Poll()) != null)
            {
                switch (e.Type)
                {
                    case EventType.kKeyboard:
                        {
                            KeyboardEvent ev = (KeyboardEvent)e;
                            OnEvent(ev);
                            break;
                        }

                    case EventType.kMouse:
                        {
                            MouseEvent ev = (MouseEvent)e;
                            OnEvent(ev);
                            break;
                        }

                    case EventType.kPosition:
                        {
                            MousePositionEvent ev = (MousePositionEvent)e;
                            OnEvent(ev);
                            break;
                        }
                }
            }
        }

        public void OnEvent(KeyboardEvent ev)
        {
            // F3 - http://community.bistudio.com/wiki/DIK_KeyCodes
            if (ev.Key == 0x3D && ev.Pressed == true)
            {
                UIEnabled = !UIEnabled;
            }
            if(UIEnabled)
                Skyrim.Script.Overlay.System.InjectKeyboardKey(ev.Key, ev.Pressed);
        }

        public void OnEvent(MouseEvent ev)
        {
            if (UIEnabled)
                Skyrim.Script.Overlay.System.InjectMouseKey(ev.Key, ev.Pressed);
        }

        public void OnEvent(MousePositionEvent ev)
        {
            if (UIEnabled)
                Skyrim.Script.Overlay.System.InjectMousePosition(ev.X, ev.Y, ev.Z);
        }

        public bool UIEnabled
        {
            get;
            set;
        }
    }
}
