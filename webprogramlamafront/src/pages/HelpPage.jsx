import { useState } from "react";

const HelpPage = () => {
  const [messages, setMessages] = useState([]);
  const [input, setInput] = useState("");

  const sendMessage = async () => {
    if (!input.trim()) return;

    // Kullanıcının mesajını ekle
    const userMsg = { sender: "user", text: input };
    setMessages((prev) => [...prev, userMsg]);

    try {
      const res = await fetch(
        "https://localhost:7062/api/DeepSeek/simple-chat",
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ message: input }),
        }
      );

      const data = await res.json();
      const botMsg = {
        sender: "bot",
        text: data.Response || data.response || "Yanıt alınamadı.",
      };
      setMessages((prev) => [...prev, botMsg]);
    } catch (err) {
      setMessages((prev) => [
        ...prev,
        { sender: "bot", text: "Sunucu hatası oluştu." },
      ]);
    }

    setInput("");
  };

  return (
    <div className="container py-5">
      <h2 className="mb-4 text-center">Canlı Yardım</h2>
      <div
        className="border rounded p-3 mb-3"
        style={{ height: "300px", overflowY: "auto", background: "#f9f9f9" }}
      >
        {messages.map((msg, index) => (
          <div
            key={index}
            className={`mb-2 ${
              msg.sender === "user" ? "text-end" : "text-start"
            }`}
          >
            <span
              className={`badge bg-${
                msg.sender === "user" ? "primary" : "secondary"
              }`}
            >
              {msg.text}
            </span>
          </div>
        ))}
      </div>
      <div className="input-group">
        <input
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={(e) => e.key === "Enter" && sendMessage()}
          className="form-control"
          placeholder="Yardım için mesajınızı yazın..."
        />
        <button className="btn btn-primary" onClick={sendMessage}>
          Gönder
        </button>
      </div>
    </div>
  );
};

export default HelpPage;
