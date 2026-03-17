import React, { useState, useEffect, useRef } from 'react';
import axios from 'axios';
import { useAuth } from '../context/AuthContext';
import { format } from 'date-fns';
import { Search, MessageSquare, Plus, Send, User } from 'lucide-react';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import DashboardLayout from '../components/DashboardLayout';
import Topbar from '../components/Topbar';

const Inbox = () => {
  const { tenant, token } = useAuth();
  const [sessions, setSessions] = useState([]);
  const [activeSession, setActiveSession] = useState(null);
  const [history, setHistory] = useState([]);
  const [replyMessage, setReplyMessage] = useState("");
  const [isSending, setIsSending] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const messagesEndRef = useRef(null);
  const activeSessionRef = useRef(activeSession);
  const connectionRef = useRef(null);

  useEffect(() => {
    activeSessionRef.current = activeSession;
  }, [activeSession]);

  useEffect(() => {
    fetchSessions();
    const connection = new HubConnectionBuilder()
        .withUrl("http://localhost:5083/hubs/dashboard", {
            accessTokenFactory: () => token
        })
        .configureLogging(LogLevel.Information)
        .withAutomaticReconnect()
        .build();

    connectionRef.current = connection;
    connection.on("ReceiveSessionUpdate", () => {
        fetchSessions();
        if (activeSessionRef.current) {
            fetchHistory(activeSessionRef.current.id);
        }
    });

    connection.start()
        .then(() => console.log('🟢 SignalR WebSockets Connected'))
        .catch(err => console.error('🔴 SignalR Connection Error: ', err));

    return () => {
      if (connectionRef.current) connectionRef.current.stop();
    };
  }, []);

  useEffect(() => {
    if (activeSession) {
      fetchHistory(activeSession.id);
    } else {
      setHistory([]);
    }
  }, [activeSession]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [history]);

  const fetchSessions = async () => {
    try {
      const res = await axios.get('/api/dashboard/sessions');
      const sorted = res.data.sort((a, b) => {
        if (a.needsHumanAttention === b.needsHumanAttention) {
          return new Date(b.updatedAt) - new Date(a.updatedAt);
        }
        return a.needsHumanAttention ? -1 : 1;
      });
      setSessions(sorted);
      if (activeSession) {
        const updatedActive = sorted.find(s => s.id === activeSession.id);
        if (updatedActive) setActiveSession(updatedActive);
      }
    } catch (err) {
      console.error('Failed to fetch sessions', err);
    }
  };

  const fetchHistory = async (sessionId) => {
    try {
      const res = await axios.get(`/api/dashboard/sessions/${sessionId}/history`);
      setHistory(res.data);
    } catch (err) {
      console.error('Failed to fetch history', err);
    }
  };

  const handleReply = async (e) => {
    if (e) e.preventDefault();
    if (!replyMessage.trim() || !activeSession) return;
    
    setIsSending(true);
    try {
      await axios.post(`/api/dashboard/sessions/${activeSession.id}/reply`, {
        message: replyMessage
      });
      setReplyMessage("");
      await fetchHistory(activeSession.id);
      await fetchSessions();
    } catch (err) {
      console.error('Failed to send reply', err);
      alert('Error al enviar el mensaje. Verifique la consola.');
    } finally {
      setIsSending(false);
    }
  };

  const filteredSessions = sessions.filter(s => {
    const search = (searchTerm || "").toLowerCase();
    const phone = (s.userPhone || "").toLowerCase();
    const id = (s.id || "").toLowerCase();
    return phone.includes(search) || id.includes(search);
  });

  return (
    <DashboardLayout>
      <Topbar title="Inbox" />
      <div className="flex-1 flex overflow-hidden">
        
        {/* Thread List */}
        <div className="w-[300px] shrink-0 border-r border-[var(--border)] overflow-y-auto bg-[var(--surface)] scrollbar-hide">
          <div className="p-3.5 border-b border-[var(--border)] sticky top-0 bg-[var(--surface)] z-10">
            <div className="relative">
              <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-[var(--text-muted)]" />
              <input 
                className="w-full pl-8 pr-3 py-2 bg-[var(--bg)] border border-[var(--border)] rounded-lg text-[13px] outline-none focus:border-[var(--accent)] transition-all"
                placeholder="Buscar conversaciones..."
                value={searchTerm}
                onChange={e => setSearchTerm(e.target.value)}
              />
            </div>
          </div>

          <div>
            {filteredSessions.map(session => (
              <div 
                key={session.id}
                onClick={() => setActiveSession(session)}
                className={`p-3.5 border-b border-[var(--border)] cursor-pointer transition-colors flex gap-2.5 items-start ${activeSession?.id === session.id ? 'bg-[var(--accent-light)]' : 'hover:bg-[var(--bg)]'}`}
              >
                <div className="w-9 h-9 rounded-full bg-gradient-to-br from-[var(--accent)] to-[#7C3AED] flex items-center justify-center text-white text-[13px] font-bold shrink-0">
                  {session.userPhone.slice(-2)}
                </div>
                <div className="flex-1 min-w-0">
                  <div className="flex items-center justify-between mb-0.5">
                    <div className="font-medium text-[13px] truncate">+{session.userPhone}</div>
                    <div className="text-[11px] text-[var(--text-muted)]">
                      {format(new Date(session.updatedAt), "HH:mm")}
                    </div>
                  </div>
                  <div className="text-[12px] text-[var(--text-muted)] truncate">ID: {session.id.slice(0, 8)}</div>
                </div>
                {session.needsHumanAttention && (
                  <div className="w-2 h-2 bg-[var(--accent)] rounded-full mt-1.5 shrink-0"></div>
                )}
              </div>
            ))}
          </div>
        </div>

        {/* Chat Area */}
        <div className="flex-1 flex flex-col overflow-hidden bg-[var(--bg)]">
          {activeSession ? (
            <>
              <div className="p-3.5 px-5 border-b border-[var(--border)] bg-[var(--surface)] flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <div className="w-9 h-9 rounded-full bg-slate-200 flex items-center justify-center text-[13px] font-bold">
                    {activeSession.userPhone.slice(-2)}
                  </div>
                  <div>
                    <div className="font-bold text-[14px]">+{activeSession.userPhone}</div>
                    <div className="flex items-center gap-1.5 text-[var(--green)] text-[12px]">
                      <span className="w-1.5 h-1.5 bg-[var(--green)] rounded-full"></span>
                      En línea
                    </div>
                  </div>
                </div>
                <div className="flex gap-2">
                  <button className="w-8 h-8 rounded-lg border border-[var(--border)] flex items-center justify-center text-[var(--text-secondary)] hover:bg-[var(--bg)] transition-colors">
                    <MessageSquare size={14} />
                  </button>
                  <button className="w-8 h-8 rounded-lg border border-[var(--border)] flex items-center justify-center text-[var(--text-secondary)] hover:bg-[var(--bg)] transition-colors">
                    <User size={14} />
                  </button>
                </div>
              </div>

              <div className="flex-1 overflow-y-auto p-5 pb-2 flex flex-col gap-1.5">
                {history.map((msg, i) => {
                  const role = (msg.role || msg.Role || "").toLowerCase();
                  if (role === 'system') return null;
                  const isUser = role === 'user';
                  
                  return (
                    <div key={i} className={`flex flex-col max-w-[68%] ${isUser ? 'self-start items-start' : 'self-end items-end'}`}>
                      <div className={`px-3.5 py-2.5 rounded-[16px] text-[13.5px] leading-relaxed ${!isUser ? 'bg-[var(--accent)] text-white rounded-br-[4px]' : 'bg-[var(--surface)] text-[var(--text-primary)] rounded-bl-[4px] shadow-[0_1px_2px_rgba(0,0,0,0.06)]'}`}>
                        {msg.content || msg.Content}
                      </div>
                      <div className="text-[10px] text-[var(--text-muted)] mt-1 px-0.5">
                        {format(new Date(msg.timestamp || Date.now()), "HH:mm")}
                      </div>
                    </div>
                  );
                })}
                <div ref={messagesEndRef} />
              </div>

              <div className="p-4 px-5 bg-[var(--surface)] border-t border-[var(--border)] flex items-center gap-2.5">
                <button className="w-[38px] h-[38px] rounded-lg border border-[var(--border)] flex items-center justify-center text-[var(--text-secondary)] hover:bg-[var(--bg)] transition-colors">
                  <Plus size={16} />
                </button>
                <textarea 
                  className="flex-1 bg-[var(--bg)] border-[1.5px] border-[var(--border)] rounded-[22px] px-4 py-2.5 text-[13.5px] outline-none focus:border-[var(--accent)] transition-all resize-none max-h-[100px] overflow-y-auto"
                  placeholder="Escribe un mensaje..."
                  rows="1"
                  value={replyMessage}
                  onChange={e => setReplyMessage(e.target.value)}
                  onKeyDown={e => {
                    if (e.key === 'Enter' && !e.shiftKey) {
                      e.preventDefault();
                      handleReply();
                    }
                  }}
                />
                <button 
                  onClick={() => handleReply()}
                  disabled={isSending || !replyMessage.trim()}
                  className="w-[38px] h-[38px] rounded-full bg-[var(--accent)] text-white flex items-center justify-center cursor-pointer transition-all hover:bg-[var(--accent-hover)] hover:scale-105 active:scale-100 disabled:opacity-50"
                >
                  <Send size={15} className="ml-0.5" />
                </button>
              </div>
            </>
          ) : (
            <div className="flex-1 flex flex-col items-center justify-center text-[var(--text-muted)] gap-2 border-l border-[var(--border)]">
              <MessageSquare size={48} className="opacity-20 mb-2" />
              <div className="font-display text-[15px] font-bold">Selecciona una conversación</div>
              <div className="text-[13px]">Elige un chat de la lista para comenzar</div>
            </div>
          )}
        </div>
      </div>
    </DashboardLayout>
  );
};

export default Inbox;
