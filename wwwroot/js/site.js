// Notification system and utilities
(function () {
    const NOTIF_POLL_MS = 15000; // Poll every 15 seconds
    let lastNotificationIds = new Set();

    function showToast(message, type = 'success') {
        const toastEl = document.getElementById('globalToast');
        const bodyEl = document.getElementById('globalToastBody');
        if (!toastEl || !bodyEl) return;
        bodyEl.textContent = message;
        toastEl.classList.remove('text-bg-success', 'text-bg-danger', 'text-bg-info', 'text-bg-warning');
        toastEl.classList.add(`text-bg-${type}`);
        const toast = new bootstrap.Toast(toastEl, { delay: 3000 });
        toast.show();
    }

    function getTimeAgo(dateString) {
        const date = new Date(dateString);
        const now = new Date();
        const diffMs = now - date;
        const diffMins = Math.floor(diffMs / 60000);
        const diffHours = Math.floor(diffMs / 3600000);
        const diffDays = Math.floor(diffMs / 86400000);

        if (diffMins < 1) return 'Just now';
        if (diffMins < 60) return `${diffMins} min${diffMins > 1 ? 's' : ''} ago`;
        if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`;
        if (diffDays < 7) return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`;
        return date.toLocaleDateString();
    }

    function getNotificationIcon(type) {
        switch (type) {
            case 'Success': return 'fa-check-circle text-success';
            case 'Warning': return 'fa-exclamation-triangle text-warning';
            case 'Error': return 'fa-times-circle text-danger';
            default: return 'fa-info-circle text-info';
        }
    }

    function renderNotificationItem(notif) {
        const li = document.createElement('li');
        li.className = `notification-item ${notif.isRead ? '' : 'unread'}`;
        li.setAttribute('data-notif-id', notif.id);

        const iconClass = getNotificationIcon(notif.type);
        const timeAgo = getTimeAgo(notif.createdAt);
        const url = notif.url || '/User/History';

        li.innerHTML = `
            <a href="${url}" class="dropdown-item notification-link" data-notif-id="${notif.id}">
                <div class="d-flex align-items-start">
                    <div class="notification-icon me-2">
                        <i class="fa-solid ${iconClass}"></i>
                    </div>
                    <div class="flex-grow-1">
                        <div class="notification-title fw-semibold">${notif.title}</div>
                        <div class="notification-message text-muted small">${notif.message}</div>
                        <div class="notification-time text-muted" style="font-size: 0.7rem;">${timeAgo}</div>
                    </div>
                    ${!notif.isRead ? '<span class="badge bg-success rounded-pill" style="width: 8px; height: 8px; padding: 0;"></span>' : ''}
                </div>
            </a>
        `;

        // Mark as read on click
        li.querySelector('.notification-link').addEventListener('click', async function (e) {
            if (!notif.isRead) {
                try {
                    await fetch(`/api/user/notifications/${notif.id}/read`, {
                        method: 'POST',
                        credentials: 'include'
                    });
                } catch (err) {
                    console.error('Failed to mark notification as read', err);
                }
            }
        });

        return li;
    }

    async function loadNotifications() {
        try {
            const resp = await fetch('/api/user/notifications', { credentials: 'include' });
            if (!resp.ok) return;
            const data = await resp.json();

            const notifList = document.getElementById('notifList');
            const notifBadge = document.getElementById('notifBadge');
            const markAllReadBtn = document.getElementById('markAllReadBtn');

            if (!notifList || !notifBadge) return;

            const unreadCount = data.filter(n => !n.isRead).length;

            // Update badge
            if (unreadCount > 0) {
                notifBadge.style.display = '';
                notifBadge.textContent = unreadCount > 99 ? '99+' : unreadCount.toString();
            } else {
                notifBadge.style.display = 'none';
            }

            // Show/hide mark all read button
            if (markAllReadBtn) {
                markAllReadBtn.style.display = unreadCount > 0 ? '' : 'none';
            }

            // Clear existing notifications
            notifList.innerHTML = '';

            // Render notifications
            if (data.length === 0) {
                notifList.innerHTML = '<li class="px-3 py-4 text-center text-muted"><i class="fa-solid fa-bell-slash me-2"></i>No notifications</li>';
            } else {
                data.forEach(notif => {
                    notifList.appendChild(renderNotificationItem(notif));
                });

                // Check for new notifications
                const currentIds = new Set(data.map(n => n.id));
                const newNotifications = data.filter(n => !lastNotificationIds.has(n.id) && !n.isRead);

                if (newNotifications.length > 0 && lastNotificationIds.size > 0) {
                    newNotifications.forEach(notif => {
                        showToast(notif.title, notif.type.toLowerCase());
                    });
                }

                lastNotificationIds = currentIds;
            }
        } catch (err) {
            console.error('Failed to load notifications', err);
        }
    }

    // Mark all as read
    document.addEventListener('DOMContentLoaded', function () {
        const markAllReadBtn = document.getElementById('markAllReadBtn');
        if (markAllReadBtn) {
            markAllReadBtn.addEventListener('click', async function (e) {
                e.preventDefault();
                e.stopPropagation();
                try {
                    const resp = await fetch('/api/user/notifications/read-all', {
                        method: 'POST',
                        credentials: 'include'
                    });
                    if (resp.ok) {
                        await loadNotifications();
                    }
                } catch (err) {
                    console.error('Failed to mark all as read', err);
                }
            });
        }

        // Load notifications on page load
        loadNotifications();

        // Poll for new notifications
        setInterval(loadNotifications, NOTIF_POLL_MS);
    });

    // Mobile menu collapse on nav link click
    document.addEventListener('click', function (e) {
        const target = e.target.closest('.navbar .nav-link');
        if (target) {
            const collapse = document.getElementById('navbarContent');
            if (collapse && collapse.classList.contains('show')) {
                const bsCollapse = new bootstrap.Collapse(collapse, { toggle: true });
                bsCollapse.hide();
            }
        }
    });

    // Show TempData messages as toasts
    document.addEventListener('DOMContentLoaded', function () {
        // Check for success messages
        const successAlert = document.querySelector('.alert-success');
        if (successAlert) {
            const message = successAlert.textContent.trim();
            if (message) {
                showToast(message, 'success');
            }
        }

        // Check for error messages
        const errorAlert = document.querySelector('.alert-danger');
        if (errorAlert) {
            const message = errorAlert.textContent.trim();
            if (message) {
                showToast(message, 'danger');
            }
        }

        // Check for info messages
        const infoAlert = document.querySelector('.alert-info');
        if (infoAlert) {
            const message = infoAlert.textContent.trim();
            if (message) {
                showToast(message, 'info');
            }
        }

        // Check for warning messages
        const warningAlert = document.querySelector('.alert-warning');
        if (warningAlert) {
            const message = warningAlert.textContent.trim();
            if (message) {
                showToast(message, 'warning');
            }
        }
    });

    // Auto-refresh for dashboards (every 20 seconds)
    const DASHBOARD_REFRESH_MS = 20000;
    const isDashboard = window.location.pathname.includes('/Dashboard') ||
        window.location.pathname.includes('/Admin/Dashboard') ||
        window.location.pathname.includes('/Driver/Dashboard');

    if (isDashboard) {
        let refreshInterval;
        let lastRefreshTime = Date.now();

        function refreshDashboard() {
            // Only refresh if page is visible and user hasn't interacted recently
            if (document.hidden) return;

            // Don't refresh if user interacted in last 5 seconds
            const timeSinceLastInteraction = Date.now() - lastRefreshTime;
            if (timeSinceLastInteraction < 5000) return;

            // Reload the page to get fresh data
            window.location.reload();
        }

        document.addEventListener('DOMContentLoaded', function () {
            // Track user interactions
            ['click', 'keydown', 'scroll'].forEach(event => {
                document.addEventListener(event, function () {
                    lastRefreshTime = Date.now();
                }, { passive: true });
            });

            // Start auto-refresh after initial load delay
            setTimeout(function () {
                refreshInterval = setInterval(refreshDashboard, DASHBOARD_REFRESH_MS);
            }, DASHBOARD_REFRESH_MS);

            // Stop refresh when page becomes hidden
            document.addEventListener('visibilitychange', function () {
                if (document.hidden) {
                    if (refreshInterval) clearInterval(refreshInterval);
                } else {
                    if (!refreshInterval) {
                        refreshInterval = setInterval(refreshDashboard, DASHBOARD_REFRESH_MS);
                    }
                }
            });
        });
    }

    // Export showToast for use in other scripts
    window.showToast = showToast;
})();

/* ============================================
   Modern Chatbot Widget Logic
   ============================================ */
(function () {
    // Wait for DOM to be ready
    function initChatbot() {
        const chatButton = document.getElementById('chatButton');
        const chatWindow = document.getElementById('chatWindow');
        const closeChatBtn = document.getElementById('closeChatBtn');
        const chatInput = document.getElementById('chatInput');
        const sendMessageBtn = document.getElementById('sendMessageBtn');
        const chatMessages = document.getElementById('chatMessages');
        const typingIndicator = document.getElementById('typingIndicator');

        if (!chatButton || !chatWindow) {
            console.warn('Chatbot elements not found, retrying...');
            setTimeout(initChatbot, 100);
            return;
        }

        console.log('Chatbot initialized successfully');

        // Message history for persistence
        let messageHistory = [];
        const MAX_HISTORY = 50;

        // Load message history from localStorage
        function loadHistory() {
            try {
                const saved = localStorage.getItem('chatbot_history');
                if (saved) {
                    messageHistory = JSON.parse(saved);
                    // Clear existing messages except the initial bot message
                    const initialMessage = chatMessages.querySelector('.message.bot');
                    chatMessages.innerHTML = '';
                    if (initialMessage) {
                        chatMessages.appendChild(initialMessage);
                    }
                    // Restore messages
                    messageHistory.forEach(msg => {
                        appendMessage(msg.text, msg.type, msg.timestamp, false);
                    });
                    scrollToBottom();
                }
            } catch (e) {
                console.error('Failed to load chat history:', e);
            }
        }

        // Save message history to localStorage
        function saveHistory() {
            try {
                localStorage.setItem('chatbot_history', JSON.stringify(messageHistory.slice(-MAX_HISTORY)));
            } catch (e) {
                console.error('Failed to save chat history:', e);
            }
        }

        // Toggle Chat Window
        function toggleChat() {
            const isOpening = !chatWindow.classList.contains('open');
            chatWindow.classList.toggle('open');

            if (isOpening) {
                setTimeout(() => {
                    chatInput.focus();
                    scrollToBottom();
                }, 300);
            }
        }

        chatButton.addEventListener('click', toggleChat);
        if (closeChatBtn) {
            closeChatBtn.addEventListener('click', toggleChat);
        }

        // Format timestamp
        function formatTime(date) {
            const hours = date.getHours();
            const minutes = date.getMinutes();
            const ampm = hours >= 12 ? 'PM' : 'AM';
            const displayHours = hours % 12 || 12;
            const displayMinutes = minutes.toString().padStart(2, '0');
            return `${displayHours}:${displayMinutes} ${ampm}`;
        }

        // Append Message with modern styling
        function appendMessage(text, type, timestamp = null, saveToHistory = true) {
            if (!text || !text.trim()) return;

            const messageDiv = document.createElement('div');
            messageDiv.className = `message ${type}`;

            const contentDiv = document.createElement('div');
            contentDiv.className = 'message-content';
            contentDiv.textContent = text;

            const timeDiv = document.createElement('div');
            timeDiv.className = 'message-time';
            timeDiv.textContent = timestamp ? formatTime(new Date(timestamp)) : formatTime(new Date());

            messageDiv.appendChild(contentDiv);
            messageDiv.appendChild(timeDiv);

            // Append message to the end of the container
            chatMessages.appendChild(messageDiv);

            // Save to history
            if (saveToHistory) {
                messageHistory.push({
                    text: text,
                    type: type,
                    timestamp: timestamp || Date.now()
                });
                saveHistory();
            }

            // Animate message appearance
            requestAnimationFrame(() => {
                messageDiv.style.opacity = '0';
                messageDiv.style.transform = 'translateY(10px)';
                requestAnimationFrame(() => {
                    messageDiv.style.transition = 'all 0.3s ease-out';
                    messageDiv.style.opacity = '1';
                    messageDiv.style.transform = 'translateY(0)';
                });
            });

            scrollToBottom();
        }

        // Show Typing Indicator
        function showTyping() {
            if (typingIndicator) {
                typingIndicator.classList.add('active');
                typingIndicator.style.display = 'flex';
                scrollToBottom();
            }
        }

        // Hide Typing Indicator
        function hideTyping() {
            if (typingIndicator) {
                typingIndicator.classList.remove('active');
                typingIndicator.style.display = 'none';
            }
        }

        // Smooth scroll to bottom
        function scrollToBottom() {
            requestAnimationFrame(() => {
                chatMessages.scrollTo({
                    top: chatMessages.scrollHeight,
                    behavior: 'smooth'
                });
            });
        }

        // Send Message with improved error handling
        async function sendMessage(text) {
            if (!text || !text.trim()) return;

            const userMessage = text.trim();

            // Disable input while sending
            chatInput.disabled = true;
            sendMessageBtn.disabled = true;

            // Append User Message
            appendMessage(userMessage, 'user');
            chatInput.value = '';

            // Show Typing Indicator
            showTyping();
            scrollToBottom();

            try {
                // Simulate realistic typing delay
                const typingDelay = 500 + Math.random() * 500;
                await new Promise(resolve => setTimeout(resolve, typingDelay));

                const apiUrl = `/api/chatbot/ask?query=${encodeURIComponent(userMessage)}`;
                console.log('Sending request to:', apiUrl);

                const response = await fetch(apiUrl, {
                    method: 'GET',
                    headers: {
                        'Accept': 'text/plain',
                    }
                });

                console.log('Response status:', response.status, response.statusText);

                if (response.ok) {
                    const data = await response.text();
                    console.log('Response data:', data);
                    // Add slight delay before showing response for natural feel
                    await new Promise(resolve => setTimeout(resolve, 300));
                    hideTyping();
                    appendMessage(data || "I'm sorry, I didn't get a response. Please try again.", 'bot');
                } else {
                    const errorData = await response.text().catch(() => '');
                    console.error('Error response:', response.status, errorData);
                    hideTyping();
                    const errorText = response.status === 400
                        ? "Please provide a valid question."
                        : `I'm having trouble connecting to the server (Status: ${response.status}). Please try again later.`;
                    appendMessage(errorText, 'bot');
                }
            } catch (error) {
                console.error('Chat error:', error);
                hideTyping();
                appendMessage("Something went wrong. Please check your connection and try again. Error: " + error.message, 'bot');
            } finally {
                // Re-enable input
                chatInput.disabled = false;
                sendMessageBtn.disabled = false;
                chatInput.focus();
                scrollToBottom();
            }
        }

        // Event Listeners
        if (sendMessageBtn) {
            sendMessageBtn.addEventListener('click', () => {
                const text = chatInput.value.trim();
                if (text) {
                    sendMessage(text);
                }
            });
        }

        if (chatInput) {
            chatInput.addEventListener('keypress', (e) => {
                if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    const text = chatInput.value.trim();
                    if (text) {
                        sendMessage(text);
                    }
                }
            });

            // Auto-resize input (if needed for multi-line in future)
            chatInput.addEventListener('input', function () {
                this.style.height = 'auto';
                this.style.height = this.scrollHeight + 'px';
            });
        }

        // Make sendQuickAction global so buttons can access it
        window.sendQuickAction = function (text) {
            if (text) {
                // Ensure chat is open
                if (!chatWindow.classList.contains('open')) {
                    toggleChat();
                }
                // Small delay to ensure chat is open
                setTimeout(() => {
                    sendMessage(text);
                }, 350);
            }
        };

        // Clear chat history function (optional, can be called from console)
        window.clearChatHistory = function () {
            if (confirm('Are you sure you want to clear chat history?')) {
                messageHistory = [];
                localStorage.removeItem('chatbot_history');
                const initialMessage = chatMessages.querySelector('.message.bot');
                chatMessages.innerHTML = '';
                if (initialMessage) {
                    chatMessages.appendChild(initialMessage);
                }
            }
        };

        // Load history on page load
        document.addEventListener('DOMContentLoaded', function () {
            loadHistory();
        });

        // Auto-scroll when window is resized
        let resizeTimer;
        window.addEventListener('resize', function () {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(() => {
                scrollToBottom();
            }, 250);
        });
    }

    // Initialize chatbot when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initChatbot);
    } else {
        initChatbot();
    }
})();

/* ============================================
   Google Translate Logic
   ============================================ */
window.triggerTranslation = function (lang) {
    if (lang === 'en') {
        // Clear cookies to reset to original
        document.cookie = "googtrans=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
        document.cookie = "googtrans=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/; domain=" + window.location.hostname;
    } else {
        // Set cookie for translation /en/lang
        document.cookie = "googtrans=/en/" + lang + "; path=/";
    }
    window.location.reload();
};

document.addEventListener('DOMContentLoaded', function () {
    const cookies = document.cookie.split(';');
    const googtrans = cookies.find(c => c.trim().startsWith('googtrans='));
    const langLabel = document.getElementById('currentLanguageLabel');

    if (langLabel) {
        if (googtrans) {
            const val = googtrans.split('=')[1];
            if (val.endsWith('/fr')) {
                langLabel.textContent = 'Français';
            } else if (val.endsWith('/rw')) {
                langLabel.textContent = 'Ikinyarwanda';
            } else {
                langLabel.textContent = 'English';
            }
        } else {
            langLabel.textContent = 'English';
        }
    }
});
