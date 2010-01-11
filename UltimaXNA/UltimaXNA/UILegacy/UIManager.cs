﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using UltimaXNA.Entities;
using UltimaXNA.Extensions;
using UltimaXNA.Input;
using UltimaXNA.TileEngine;

namespace UltimaXNA.UILegacy
{
    public class UIManager : DrawableGameComponent, IUIManager
    {
        List<Control> _controls = null;
        Cursor _cursor = null;
        IInputService _input = null;

        // ExtendedSpriteBatchUI _spriteBatch;
        // SpriteBatch3D _spriteBatch3D;
        ExtendedSpriteBatch _spriteBatch;

        List<string> _DEBUG_TEXT_LINES = new List<string>();
        List<GameTime> _DEBUG_TEXT_TIMES = new List<GameTime>();
        GameTime _lastGameTime;

        Control[] _mouseOverControls = null; // the controls that the mouse is over, 0 index is the frontmost control, last index is the backmost control (always the owner gump).
        Control[] _mouseDownControl = new Control[5]; // the control that the mouse was over when the button was clicked. 5 buttons
        public Control MouseOverControl
        {
            get
            {
                if (_mouseOverControls == null)
                    return null;
                else
                    return _mouseOverControls[0];
            }
        }

        // Keyboard-handling control 'announce' themselves when they are created. But only the first one per update
        // cycle is recognized.
        bool _keyboardHandlingControlAnnouncedThisRound = false;
        Control _keyboardFocusControl = null;
        public Control KeyboardFocusControl
        {
            get
            {
                if (_keyboardFocusControl == null)
                    return null;
                else
                    return _keyboardFocusControl;
            }
            set
            {
                _keyboardFocusControl = value;
            }
        }

        public bool IsMouseOverUI
        {
            get
            {
                if (MouseOverControl == null)
                    return false;
                else
                    return true;
            }
        }
        public Cursor Cursor
        {
            get
            {
                return _cursor;
            }
        }

        public UIManager(Game game)
            : base(game)
        {
            _spriteBatch = new ExtendedSpriteBatch(game);

            _controls = new List<Control>();
            _cursor = new Cursor(this);

            // Retrieve the needed services.
            _input = game.Services.GetService<IInputService>(true);
        }

        public void MsgBox(string msg)
        {
            // pop up an error message, modal.
        }

        public Gump AddGump_Server(Serial serial, Serial gumpID, string[] gumplings, string[] lines, int x, int y)
        {
            Gump g = new Gump(serial, gumpID, gumplings, lines);
            g.Position = new Vector2(x, y);
            g.IsServerGump = true;
            g.IsMovable = true;
            _controls.Add(g);
            return g;
        }

        public Gump AddGump_Local(Gump gump, int x, int y)
        {
            gump.Position = new Vector2(x, y);
            _controls.Add(gump);
            return gump;
        }

        public Gump GetGump(Serial serial)
        {
            foreach (Gump g in _controls)
            {
                if (g.Serial == serial)
                    return g;
            }
            return null;
        }

        public T GetGump<T>(Serial serial) where T : Gump
        {
            foreach (Gump g in _controls)
            {
                if (g.Serial == serial)
                    if (g.GetType() == typeof(T))
                        return (T)g;
            }
            return null;
        }

        public Gump AddContainerGump(Entity containerItem, int gumpID)
        {
            Gump g = new Clientside.ContainerGump(containerItem, gumpID);
            g.Position = new Vector2(64, 64);
            _controls.Add(g);
            return g;
        }

        public override void Update(GameTime gameTime)
        {
            _lastGameTime = gameTime;

            foreach (Control c in _controls)
            {
                if (!c.IsInitialized)
                    c.Initialize(this);
                c.Update(gameTime);
            }

            updateInput();

            List<Control> disposedControls = new List<Control>();
            foreach (Control c in _controls)
            {
                if (c.IsDisposed)
                    disposedControls.Add(c);
            }
            foreach (Control c in disposedControls)
            {
                _controls.Remove(c);
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            _spriteBatch.ResetZ();

            foreach (Control c in _controls)
            {
                if (c.IsInitialized)
                    c.Draw(_spriteBatch);
            }

            // Draw debug message
            if (GameState.DebugMessage != null)
                DEBUG_DrawText(new Vector2(5, 5), GameState.DebugMessage + Environment.NewLine + _DEBUG_TEXT(gameTime));

            // Draw the cursor
            _cursor.Draw(_spriteBatch, _input.CurrentMousePosition);

            _spriteBatch.Flush();

            base.Draw(gameTime);
        }

        public void Reset()
        {
            foreach (Control c in _controls)
            {
                c.Dispose();
            }
            _controls.Clear();
        }

        internal void DEBUG_DrawText(Vector2 position, string text)
        {
            Texture2D t = Data.UniText.GetTexture(text);
            _spriteBatch.Draw(t, position, 0, false);
        }

        internal string _DEBUG_TEXT(GameTime gameTime)
        {
            List<int> indexesToRemove = new List<int>();
            string s = string.Empty;

            for (int i = 0; i < _DEBUG_TEXT_LINES.Count; i++)
            {
                if (_DEBUG_TEXT_TIMES[i].TotalRealTime.Ticks == 0)
                {
                    _DEBUG_TEXT_TIMES[i] = new GameTime(gameTime.TotalRealTime, gameTime.ElapsedRealTime, gameTime.TotalGameTime, gameTime.ElapsedGameTime);
                }

                if (gameTime.TotalRealTime.TotalSeconds - _DEBUG_TEXT_TIMES[i].TotalRealTime.TotalSeconds >= 10.0f)
                {
                    indexesToRemove.Add(i);
                }
                else
                {
                    s += _DEBUG_TEXT_LINES[i] + Environment.NewLine;
                }
            }

            for (int i = 0; i < indexesToRemove.Count; i++)
            {
                _DEBUG_TEXT_LINES.RemoveAt(indexesToRemove[i] - i);
                _DEBUG_TEXT_TIMES.RemoveAt(indexesToRemove[i] - i);
            }

            return s;
        }

        public void DebugMessage_AddLine(string line)
        {
            _DEBUG_TEXT_LINES.Add(line);
            _DEBUG_TEXT_TIMES.Add(new GameTime());
        }
        public void DebugMessage_Clear()
        {
            _DEBUG_TEXT_LINES.Clear();
            _DEBUG_TEXT_TIMES.Clear();
        }

        public void AnnounceNewKeyboardHandler(Control c)
        {
            // Pass null to CLEAR the keyboardhandlingcontrol.
            if (c == null)
            {
                _keyboardHandlingControlAnnouncedThisRound = false;
                _keyboardFocusControl = null;
            }
            else
            {
                if (c.HandlesKeyboardFocus)
                {
                    if (_keyboardHandlingControlAnnouncedThisRound == false)
                    {
                        _keyboardHandlingControlAnnouncedThisRound = true;
                        _keyboardFocusControl = c;
                    }
                }
            }
        }

        void updateInput()
        {
            _keyboardHandlingControlAnnouncedThisRound = false;

            Control[] focusedControls = null;

            foreach (Control c in _controls)
            {
                Control[] mouseOverControls = c.HitTest(_input.CurrentMousePosition);
                if (mouseOverControls != null)
                {
                    focusedControls = mouseOverControls;
                }
            }

            for (int iButton = 0; iButton < 5; iButton++)
            {
                // MouseOver event.
                Control controlGivenMouseOver = null;
                if (focusedControls != null)
                {
                    for (int iControl = 0; iControl < focusedControls.Length; iControl++)
                    {
                        if (focusedControls[iControl].HandlesMouseInput)
                        {
                            // mouse over for the moused over control
                            focusedControls[iControl].MouseOver(_input.CurrentMousePosition);
                            controlGivenMouseOver = focusedControls[iControl];
                            if (MouseOverControl != null && controlGivenMouseOver != MouseOverControl)
                                MouseOverControl.MouseOut(_input.CurrentMousePosition);
                            break;
                        }
                    }
                }

                // mouse over for any controls moused down on (have focus, in more common parlance)
                if (_mouseDownControl[iButton] != null && _mouseDownControl[iButton] != controlGivenMouseOver)
                    _mouseDownControl[iButton].MouseOver(_input.CurrentMousePosition);

                if (focusedControls != null)
                {
                    // MouseDown event.
                    if (_input.IsMouseButtonPress((MouseButtons)iButton))
                    {
                        for (int iControl = 0; iControl < focusedControls.Length; iControl++)
                        {
                            if (focusedControls[iControl].HandlesMouseInput)
                            {
                                focusedControls[iControl].MouseDown(_input.CurrentMousePosition, (MouseButtons)iButton);
                                _mouseDownControl[iButton] = focusedControls[iControl];
                                break;
                            }
                        }
                    }
                }


                // MouseUp and MouseClick events
                if (_input.IsMouseButtonRelease((MouseButtons)iButton))
                {
                    if (_mouseDownControl[iButton] != null)
                    {
                        if (focusedControls != null)
                        {
                            if (focusedControls[0] == _mouseDownControl[iButton])
                            {
                                _mouseDownControl[iButton].MouseClick(_input.CurrentMousePosition, (MouseButtons)iButton);
                            }
                        }
                        _mouseDownControl[iButton].MouseUp(_input.CurrentMousePosition, (MouseButtons)iButton);
                    }
                }

                if (_input.IsMouseButtonUp((MouseButtons)iButton))
                {
                    _mouseDownControl[iButton] = null;
                }
            }

            _mouseOverControls = focusedControls;

            // keyboard events: if we're over a keyboard-handling control and press lmb, then give focus to the control.
            if (_mouseOverControls != null)
            {
                if (_mouseOverControls[0].HandlesKeyboardFocus)
                {
                    if (_input.IsMouseButtonPress(MouseButtons.LeftButton))
                    {
                        _keyboardFocusControl = _mouseOverControls[0];
                    }
                }
            }
            if (_keyboardFocusControl != null)
            {
                if (_keyboardFocusControl.IsDisposed)
                {
                    _keyboardFocusControl = null;
                }
                else
                {
                    string keys;
                    List<Keys> specials;
                    _input.GetKeyboardInput(out keys, out specials);
                    if (keys != string.Empty || specials.Count > 0)
                    {
                        if (_input.IsKeyDown(Keys.LeftAlt) || _input.IsKeyDown(Keys.LeftControl))
                        {
                            // do not pass on these keypresse
                        }
                        else
                        {
                            _keyboardFocusControl.KeyboardInput(keys, specials);
                        }
                    }
                }
            }
        }

        string[] splitGumpPieces(string gumpData)
        {
            List<string> i = new List<string>(); ;
            bool isData = true;
            int dataIndex = 0;
            while (isData)
            {
                if (gumpData.Substring(dataIndex) == "\0")
                {
                    isData = false;
                }
                else
                {
                    int begin = gumpData.IndexOf("{ ", dataIndex);
                    int end = gumpData.IndexOf(" }", dataIndex + 1);
                    if ((begin != -1) && (end != -1))
                    {
                        string sub = gumpData.Substring(begin + 2, end - begin - 2);
                        // iConstruct = iConstruct.Substring(0, iBeginReplace) + iArgs[i] + iConstruct.Substring(iEndReplace + 1, iConstruct.Length - iEndReplace - 1);
                        i.Add(sub);
                        dataIndex += (end - begin) + 2;
                    }
                    else
                    {
                        isData = false;
                    }
                }
            }

            return i.ToArray();
        }
    }
}