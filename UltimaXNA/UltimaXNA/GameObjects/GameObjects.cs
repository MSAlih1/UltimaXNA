﻿#region File Description & Usings
//-----------------------------------------------------------------------------
// GameObjects.cs
//
// Created by Poplicola
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using UltimaXNA.Network.Packets.Client;
#endregion

namespace UltimaXNA.GameObjects
{
    public interface IGameObjects
    {
        int MySerial { get; set; }
        T GetObject<T>(Serial serial, bool create) where T : BaseObject;
        BaseObject GetPlayerObject();
        void RemoveObject(Serial serial);
        void Reset();
    }

    class GameObjects : GameComponent, IGameObjects
    {
        private Dictionary<int, BaseObject> m_Objects = new Dictionary<int, BaseObject>();

        public int MySerial { get; set; }

        private TileEngine.IWorld _worldService;
        private IGameState _gameStateService;
        private Client.IUltimaClient _gameClientService;
        GUI.IGUI m_GUIService;

        public GameObjects(Game game)
            : base(game)
        {
            game.Services.AddService(typeof(IGameObjects), this);
        }

        public override void Initialize()
        {
            _worldService = (TileEngine.IWorld)Game.Services.GetService(typeof(TileEngine.IWorld));
            _gameStateService = (IGameState)Game.Services.GetService(typeof(IGameState));
            _gameClientService = (Client.IUltimaClient)Game.Services.GetService(typeof(Client.IUltimaClient));
            m_GUIService = (GUI.IGUI)Game.Services.GetService(typeof(GUI.IGUI));
            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            // We only need to update objects if we are in the world.
            if (_gameStateService.InWorld)
            {
                List<int> iRemoveObjects = new List<int>();
                foreach (KeyValuePair<int, BaseObject> iObjectPair in m_Objects)
                {
                    // First check if we need to remove any objects. Objects that are due to be disposed
                    // are not updated, but are added to a list to be removed after we enumerate m_Objects.
                    if (iObjectPair.Value.IsDisposed)
                    {
                        iRemoveObjects.Add(iObjectPair.Key);
                        continue;
                    }

                    // Some object types need to be updated. Others do not.
                    switch (iObjectPair.Value.ObjectType)
                    {
                        case ObjectType.GameObject:
                            iObjectPair.Value.Update(gameTime);
                            break;
                        case ObjectType.Unit:
                        case ObjectType.Player:
                            iObjectPair.Value.Update(gameTime);
                            break;
                        default:
                            // no need to update.
                            break;
                    }
                }

                // Run through the list of objects needing to be removed from the collection.
                foreach (int i in iRemoveObjects)
                {
                    m_Objects.Remove(i);
                }
            }
            base.Update(gameTime);
        }

        public T GetObject<T>(Serial serial, bool create) where T : BaseObject
        {
            T iObject;
            // Check for existence in the collection.
            if (m_Objects.ContainsKey(serial))
            {
                // This object is in the m_Objects collection. If it is being disposed, then we should complete disposal
                // of the object and then return a new object. If it is not being disposed, return the object in the collection.
                if (m_Objects[serial].IsDisposed)
                {
                    if (create)
                    {
                        m_Objects.Remove(serial);
                        iObject = addObject<T>(serial);
                        return (T)iObject;
                    }
                    else
                    {
                        return null;
                    }
                }
                return (T)m_Objects[serial];
            }

            // No object with this Serial is in the collection. So we create a new one and return that, and hope that the server
            // will fill us in on the details of this object soon.
            if (create)
            {
                iObject = addObject<T>(serial);
                return (T)iObject;
            }
            else
            {
                return null;
            }
        }

        private T addObject<T>(Serial serial) where T : BaseObject
        {
            T o = (T)Activator.CreateInstance(typeof(T), new object[] { serial });
            o.World = _worldService; // Add the world service (for movement).
            // If this object is the client, designate it to return events.
            if (o.Serial == MySerial)
                o.Movement.DesignateClientPlayer();
            m_Objects.Add(o.Serial, o); // Add the object to the objects collection.
            return (T)o;
        }

        public void RemoveObject(Serial serial)
        {
            // When Dispose() is called, the object will tidy up and then
            // set m_Dispose = true. Reference this with IsDisposed on the
            // next update cycle.
            if (m_Objects.ContainsKey(serial))
            {
                m_Objects[serial].Dispose();
            }
        }

        public BaseObject GetPlayerObject()
        {
            // This could be cached to save time.
            return m_Objects[MySerial];
        }

        public void Reset()
        {
            m_Objects.Clear();
        }
    }
}
