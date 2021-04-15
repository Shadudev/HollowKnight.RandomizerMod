﻿using System;
using System.Collections.Generic;
using MultiWorldProtocol.Binary;
using MultiWorldProtocol.Messaging;
using MultiWorldProtocol.Messaging.Definitions.Messages;
using System.Net.Sockets;
using System.Threading;
using Modding;
using static RandomizerMod.LogHelper;
using Newtonsoft.Json;

using RandomizerLib;
using RandomizerLib.MultiWorld;
using System.Net;

namespace RandomizerMod.MultiWorld
{
    public class ClientConnection
    {
        private const int PING_INTERVAL = 10000;

        private readonly MWMessagePacker Packer = new MWMessagePacker(new BinaryMWMessageEncoder());
        private TcpClient _client;
        private Timer PingTimer;
        private readonly ConnectionState State;
        private List<MWItemSendMessage> ItemSendQueue = new List<MWItemSendMessage>();
        private Thread ReadThread;

        // TODO use these to make this class nicer
        public delegate void DisconnectEvent();
        public delegate void ConnectEvent(ulong uid);
        public delegate void JoinEvent();
        public delegate void LeaveEvent();

        public Action<int, string> ReadyConfirmReceived;
        public event DisconnectEvent OnDisconnect;
        public event ConnectEvent OnConnect;
        public event JoinEvent OnJoin;
        public event LeaveEvent OnLeave;

        public RandoResult LastResult = null;

        private List<MWMessage> messageEventQueue = new List<MWMessage>();

        public ClientConnection()
        {
            State = new ConnectionState();

            ModHooks.Instance.HeroUpdateHook += SynchronizeEvents;
        }

        public void Connect()
        {
            if (_client != null && _client.Connected)
            {
                Disconnect();
            }

            Reconnect();
        }

        private void Reconnect()
        {
            if (_client != null && _client.Connected)
            {
                return;
            }

            Log($"Attempting to connect to server");

            State.Uid = 0;
            State.LastPing = DateTime.Now;

            _client = new TcpClient
            {
                ReceiveTimeout = 2000,
                SendTimeout = 2000
            };

            if(!TryConnect())
            {
                throw new Exception($"Could not connect to {RandomizerMod.Instance.MWSettings.URL}");
            }

            if (ReadThread != null && ReadThread.IsAlive)
            {
                ReadThread.Abort();
            }

            // Make sure we never have more than one ping timer
            PingTimer?.Dispose();
            PingTimer = new Timer(DoPing, State, 1000, PING_INTERVAL);

            ReadThread = new Thread(ReadWorker);
            ReadThread.Start();

            SendMessage(new MWConnectMessage());
        }

        private bool TryConnect()
        {
            List<string> ips = ResolveURL();
            foreach (string ip in ips)
            {
                try
                {
                    Log($"attemping connection to {ip}");
                    _client.Connect(ip, RandomizerMod.Instance.MWSettings.Port);
                }
                catch { } // Ignored exception as we may connect to another IP successfully
            }
            return _client.Connected;
        }

        private List<string> ResolveURL()
        {
            List<string> ips = new List<string>();
            string url = RandomizerMod.Instance.MWSettings.URL;

            IPAddress ip;
            if (IPAddress.TryParse(url, out ip))
            {
                ips.Add(url);
            } 
            else
            {
                IPHostEntry hostEntry = Dns.GetHostEntry(url);
                Array.ForEach<IPAddress>(hostEntry.AddressList, ipAddress => ips.Add(ipAddress.ToString()));
            }

            return ips;
        }

        public void JoinRando(int randoId, int playerId)
        {
            Log("Joining rando session");
            Log(RandomizerMod.Instance.MWSettings.UserName);
            Log(randoId);
            Log(playerId);
            
            State.SessionId = randoId;
            State.PlayerId = playerId;

            SendMessage(new MWJoinMessage
            {
                DisplayName = RandomizerMod.Instance.MWSettings.UserName,
                RandoId = randoId,
                PlayerId = playerId
            });
        }

        public void Rejoin()
        {
            if (State.SessionId == -1 || State.PlayerId == -1) return;
            JoinRando(State.SessionId, State.PlayerId);
        }

        public void Leave()
        {
            State.SessionId = -1;
            State.PlayerId = -1;

            State.Joined = false;
            SendMessage(new MWLeaveMessage());
            OnLeave?.Invoke();
        }
        public void Disconnect()
        {
            Log("Disconnecting from server");
            PingTimer?.Dispose();
            PingTimer = null;

            try
            {
                ReadThread?.Abort();
                ReadThread = null;
                Log($"Disconnecting (UID = {State.Uid})");
                byte[] buf = Packer.Pack(new MWDisconnectMessage {SenderUid = State.Uid}).Buffer;
                _client?.GetStream().Write(buf, 0, buf.Length);
                _client?.Close();
            }
            catch (Exception e)
            {
                Log("Error disconnection:\n" + e);
            }
            finally
            {
                State.Connected = false;
                State.Joined = false;
                _client = null;
                messageEventQueue.Clear();

                OnDisconnect?.Invoke();
            }
        }

        private void SynchronizeEvents()
        {
            MWMessage message = null;

            lock (messageEventQueue)
            {
                if (messageEventQueue.Count > 0)
                {
                    message = messageEventQueue[0];
                    messageEventQueue.RemoveAt(0);
                }
            }

            if (message == null)
            {
                return;
            }

            switch (message)
            {
                case MWItemReceiveMessage item:
                    GiveItemActions.GiveItemMW(item.Item, item.Location, item.From);
                    break;
                default:
                    Log("Unknown type in message queue: " + message.MessageType);
                    break;
            }
        }

        private void DoPing(object state)
        {
            if (_client == null || !_client.Connected)
            {
                if (State.Connected)
                {
                    State.Connected = false;
                    State.Joined = false;

                    Log("Disconnected from server");
                }

                Disconnect();
                Reconnect();
                Rejoin();
            }

            if (State.Connected)
            {
                if (DateTime.Now - State.LastPing > TimeSpan.FromMilliseconds(PING_INTERVAL * 3.5))
                {
                    Log("Connection timed out");

                    Disconnect();
                    Reconnect();
                    Rejoin();
                }
                else
                {
                    SendMessage(new MWPingMessage());
                    //If there are items in the queue that the server hasn't confirmed yet
                    if (ItemSendQueue.Count > 0 && State.Joined)
                    {
                        ResendItemQueue();
                    }
                }
            }
        }

        private void SendMessage(MWMessage msg)
        {
            try
            {
                //Always set Uid in here, if uninitialized will be 0 as required.
                //Otherwise less work resuming session etc.
                msg.SenderUid = State.Uid;
                byte[] bytes = Packer.Pack(msg).Buffer;
                NetworkStream stream = _client.GetStream();
                stream.BeginWrite(bytes, 0, bytes.Length, WriteToServer, stream);
            }
            catch (Exception e)
            {
                Log($"Failed to send message '{msg}' to server:\n{e}");
            }
        }

        private void ReadWorker()
        {
            NetworkStream stream = _client.GetStream();
            while(true)
            {
                var message = new MWPackedMessage(stream);
                ReadFromServer(message);
            }
        }

        private void WriteToServer(IAsyncResult res)
        {
            NetworkStream stream = (NetworkStream)res.AsyncState;
            stream.EndWrite(res);
        }

        private void ReadFromServer(MWPackedMessage packed)
        {
            MWMessage message;
            try
            {
                message = Packer.Unpack(packed);
            }
            catch (Exception e)
            {
                Log(e);
                return;
            }

            switch (message.MessageType)
            {
                case MWMessageType.SharedCore:
                    break;
                case MWMessageType.ConnectMessage:
                    HandleConnect((MWConnectMessage)message);
                    break;
                case MWMessageType.ReconnectMessage:
                    break;
                case MWMessageType.DisconnectMessage:
                    HandleDisconnectMessage((MWDisconnectMessage)message);
                    break;
                case MWMessageType.JoinMessage:
                    break;
                case MWMessageType.JoinConfirmMessage:
                    HandleJoinConfirm((MWJoinConfirmMessage)message);
                    break;
                case MWMessageType.LeaveMessage:
                    HandleLeaveMessage((MWLeaveMessage)message);
                    break;
                case MWMessageType.ItemReceiveMessage:
                    HandleItemReceive((MWItemReceiveMessage)message);
                    break;
                case MWMessageType.ItemReceiveConfirmMessage:
                    break;
                case MWMessageType.ItemSendMessage:
                    break;
                case MWMessageType.ItemSendConfirmMessage:
                    HandleItemSendConfirm((MWItemSendConfirmMessage)message);
                    break;
                case MWMessageType.NotifyMessage:
                    HandleNotify((MWNotifyMessage)message);
                    break;
                case MWMessageType.PingMessage:
                    State.LastPing = DateTime.Now;
                    break;
                case MWMessageType.ReadyConfirmMessage:
                    HandleReadyConfirm((MWReadyConfirmMessage)message);
                    break;
                case MWMessageType.ResultMessage:
                    HandleResult((MWResultMessage)message);
                    break;
                case MWMessageType.InvalidMessage:
                default:
                    throw new InvalidOperationException("Received Invalid Message Type");
            }
        }

        private void ResendItemQueue()
        {
            foreach(MWItemSendMessage message in ItemSendQueue)
            {
                SendMessage(message);
            }
        }

        private void ClearFromSendQueue(int playerId, string item)
        {
            for(int i=ItemSendQueue.Count-1; i>=0; i--)
            {
                var queueItem = ItemSendQueue[i];
                if (playerId == queueItem.To && item == queueItem.Item)
                    ItemSendQueue.RemoveAt(i);
            }
        }

        private void HandleConnect(MWConnectMessage message)
        {
            State.Uid = message.SenderUid;
            State.Connected = true;
            Log($"Connected! (UID = {State.Uid})");
            OnConnect?.Invoke(State.Uid);
        }

        private void HandleJoinConfirm(MWJoinConfirmMessage message)
        {
            State.Joined = true;
            OnJoin?.Invoke();

            foreach (string item in RandomizerMod.Instance.Settings.UnconfirmedItems)
            {
                (int playerId, string itemName) = LogicManager.ExtractPlayerID(item);
                if (playerId < 0) continue;
                SendItem(RandomizerMod.Instance.Settings.GetItemLocation(item), itemName, playerId);
            }
        }

        private void HandleLeaveMessage(MWLeaveMessage message)
        {
            State.Joined = false;
            OnLeave?.Invoke();
        }

        private void HandleDisconnectMessage(MWDisconnectMessage message)
        {
            State.Connected = false;
            State.Joined = false;
        }

        private void HandleNotify(MWNotifyMessage message)
        {
            lock (messageEventQueue)
            {
                messageEventQueue.Add(message);
            }
        }

        private void HandleReadyConfirm(MWReadyConfirmMessage message)
        {
            ReadyConfirmReceived?.Invoke(message.Ready, message.Names);
            RandomizerMod.Instance.MWSettings.LastReadyID = message.ReadyID;
        }

        private void HandleItemReceive(MWItemReceiveMessage message)
        {
            lock (messageEventQueue)
            {
                messageEventQueue.Add(message);
            }

            //Do whatever we want to do when we get an item here, then confirm
            SendMessage(new MWItemReceiveConfirmMessage { Item = message.Item, From = message.From });
        }

        private void HandleItemSendConfirm(MWItemSendConfirmMessage message)
        {
            // Mark the item confirmed here, so if we send an item but disconnect we can be sure it will be resent when we open again
            Log($"Confirming item: {message.Item} to {message.To}");
            RandomizerMod.Instance.Settings.MarkItemConfirmed(new MWItem(message.To, message.Item).ToString());
            ClearFromSendQueue(message.To, message.Item);
        }

        private void HandleResult(MWResultMessage message)
        {
            RandomizerMod.Instance.StartNewGame(true, message.Result);
        }

        public void ReadyUp(string room)
        {
            SendMessage(new MWReadyMessage { Room = room, Nickname = RandomizerMod.Instance.MWSettings.UserName, Settings = RandomizerMod.Instance.Settings.RandomizerSettings });
        }

        public void Unready()
        {
            SendMessage(new MWUnreadyMessage());
        }

        public void Start()
        {
            LastResult = null;
            SendMessage(new MWStartMessage());
        }

        public void RejoinGame()
        {
            SendMessage(new MWRejoinMessage { ReadyID = RandomizerMod.Instance.MWSettings.LastReadyID });
        }

        public void SendItem(string loc, string item, int playerId)
        {
            Log($"Sending item {item} to {playerId}");
            MWItemSendMessage msg = new MWItemSendMessage { Location = loc, Item = item, To = playerId };
            ItemSendQueue.Add(msg);
            SendMessage(msg);
        }

        public void NotifySave()
        {
            SendMessage(new MWSaveMessage { ReadyID = RandomizerMod.Instance.MWSettings.LastReadyID });
            RandomizerMod.Instance.MWSettings.LastReadyID = -1;
        }

        public bool IsConnected()
        {
            return State.Connected;
        }

        public ConnectionStatus GetStatus()
        {
            if (!State.Connected)
            {
                return ConnectionStatus.NotConnected;
            }

            if (!State.Joined)
            {
                return ConnectionStatus.Connected;
            }

            return ConnectionStatus.Joined;
        }

        public enum ConnectionStatus
        {
            NotConnected,
            Connected,
            Joined
        }
    }
}
