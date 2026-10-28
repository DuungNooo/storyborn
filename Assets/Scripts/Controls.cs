using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace Storyborn
{
    // Prefer the installed Input System; otherwise use Unity's legacy backend.
    public static class Controls
    {
        public static bool MouseDown(int button)
        {
#if ENABLE_INPUT_SYSTEM
            if(Mouse.current==null) return false;
            return button==0?Mouse.current.leftButton.wasPressedThisFrame:Mouse.current.rightButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(button);
#endif
        }
        public static bool Down(KeyCode key) { return Read(key,true); }
        public static bool Held(KeyCode key) { return Read(key,false); }
        static bool Read(KeyCode key,bool pressed)
        {
#if ENABLE_INPUT_SYSTEM
            if(Keyboard.current==null) return false;
            Key mapped;
            switch(key)
            {
                case KeyCode.A:mapped=Key.A;break; case KeyCode.D:mapped=Key.D;break;
                case KeyCode.W:mapped=Key.W;break; case KeyCode.S:mapped=Key.S;break;
                case KeyCode.Q:mapped=Key.Q;break; case KeyCode.E:mapped=Key.E;break;
                case KeyCode.R:mapped=Key.R;break; case KeyCode.Space:mapped=Key.Space;break;
                case KeyCode.Escape:mapped=Key.Escape;break;case KeyCode.Return:mapped=Key.Enter;break;
                case KeyCode.LeftArrow:mapped=Key.LeftArrow;break; case KeyCode.RightArrow:mapped=Key.RightArrow;break;
                case KeyCode.UpArrow:mapped=Key.UpArrow;break;case KeyCode.DownArrow:mapped=Key.DownArrow;break;
                default:return false;
            }
            var control=Keyboard.current[mapped];
            return pressed?control.wasPressedThisFrame:control.isPressed;
#else
            return pressed?Input.GetKeyDown(key):Input.GetKey(key);
#endif
        }
    }
}
