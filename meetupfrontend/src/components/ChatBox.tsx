import { useEffect, useState, useRef, useLayoutEffect } from 'react';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { getChatHistory } from '../api/chatService';
import type { ChatMessage } from '../models/Chat';
import './ChatBox.css';

interface ChatProps {
  eventId: string;
  currentUser: { id: string; name: string };
}

const ChatBox = ({ eventId, currentUser }: ChatProps) => {
  const [connection, setConnection] = useState<HubConnection | null>(null);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [newMessage, setNewMessage] = useState('');
  
  const [page, setPage] = useState(0);
  const [hasMore, setHasMore] = useState(true);
  const [isLoading, setIsLoading] = useState(false);

  const scrollContainerRef = useRef<HTMLDivElement>(null);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  
  const prevScrollHeightRef = useRef<number>(0);
  const isHistoryLoadRef = useRef(false);

  useEffect(() => { loadMessages(0); }, [eventId]);

  useLayoutEffect(() => {
    const container = scrollContainerRef.current;
    if (!container) return;
    if (isHistoryLoadRef.current) {
      const newScrollHeight = container.scrollHeight;
      const heightDifference = newScrollHeight - prevScrollHeightRef.current;
      container.scrollTop = heightDifference;
      isHistoryLoadRef.current = false;
    } else {
      if (page === 0) {
        messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
      }
    }
  }, [messages, page]);

  const loadMessages = async (pageNum: number) => {
    if (isLoading || !hasMore) return;
    try {
      setIsLoading(true);
      if (pageNum > 0 && scrollContainerRef.current) {
        prevScrollHeightRef.current = scrollContainerRef.current.scrollHeight;
        isHistoryLoadRef.current = true; 
      }
      const newBatch = await getChatHistory(eventId, pageNum);
      if (newBatch.length < 50) setHasMore(false);
      setMessages(prev => (pageNum === 0 ? newBatch : [...newBatch, ...prev]));
      setPage(pageNum);
    } catch (err) { console.error("Failed to load history", err); } finally { setIsLoading(false); }
  };

  const handleScroll = (e: React.UIEvent<HTMLDivElement>) => {
    const { scrollTop } = e.currentTarget;
    if (scrollTop === 0 && hasMore && !isLoading) {
      loadMessages(page + 1);
    }
  };

  useEffect(() => {
    const token = sessionStorage.getItem('meetup_token'); 
    const newConnection = new HubConnectionBuilder()
      .withUrl('https://localhost:7000/chatHub', { accessTokenFactory: () => token || '' })
      .withAutomaticReconnect()
      .build();
    setConnection(newConnection);
  }, []);

  useEffect(() => {
    if (connection) {
      connection.start()
        .then(() => {
            connection.invoke('JoinEventGroup', eventId);
            connection.on('ReceiveMessage', (message: ChatMessage) => {
                isHistoryLoadRef.current = false;
                setMessages(prev => [...prev, message]);
            });
        })
        .catch(e => console.error('Connection failed: ', e));

      return () => {
        if (connection.state === HubConnectionState.Connected) {
            connection.stop().catch(err => console.error(err));
        }
      };
    }
  }, [connection, eventId]);

  const sendMessage = async () => {
    if (connection && connection.state === HubConnectionState.Connected && newMessage.trim()) {
      try {
        await connection.invoke('SendMessageToEvent', eventId, newMessage);
        setNewMessage('');
      } catch (e) { console.error('Send failed', e); }
    }
  };

  const formatTime = (dateStr: string) => {
    return new Date(dateStr).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  };

  const getInitials = (name: string) => {
      if(!name) return '?';
      return name.split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase();
  };

  return (
    <div className="chat-container">

      <div 
        className="chat-messages" 
        ref={scrollContainerRef} 
        onScroll={handleScroll}
      >
        {isLoading && page > 0 && <div className="loading-indicator">Loading history...</div>}
        
        {messages.length === 0 && !isLoading && (
            <div className="empty-chat-state">
                <p>No messages yet. Start the conversation!</p>
            </div>
        )}

        {messages.map((msg, index) => {
          const isMe = msg.senderId === currentUser.id;
          return (
            <div key={index} className={`message-row ${isMe ? 'me' : 'other'}`}>
              
              {!isMe && (
                  <div className="message-avatar" title={msg.senderName}>
                      {getInitials(msg.senderName)}
                  </div>
              )}

              <div className="chat-bubble-wrapper">
                  {!isMe && <span className="sender-name-label">{msg.senderName}</span>}
                  <div className="chat-bubble">
                      {msg.content}
                      <span className="timestamp">{formatTime(msg.timestamp)}</span>
                  </div>
              </div>
            </div>
          );
        })}
        <div ref={messagesEndRef} />
      </div>

      <div className="chat-input-area">
        <input 
          value={newMessage}
          onChange={e => setNewMessage(e.target.value)}
          onKeyDown={e => e.key === 'Enter' && sendMessage()}
          placeholder="Type a message..."
          className="chat-input"
        />
        <button onClick={sendMessage} className="send-btn">➤</button>
      </div>
    </div>
  );
};

export default ChatBox;