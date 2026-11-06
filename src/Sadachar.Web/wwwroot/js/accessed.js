/**
 * Sadachar / AccessEd UI/UX Core Engine
 * Implements accessible portal navigation, account-aware UI, speech synthesis, and API hooks
 */

// Global State
const AccessEdState = {
    currentRole: 'public',
    currentScreen: 'screen-landing',
    currentUser: null,
    accessibility: {
        fontSize: 100,
        fontFamily: 'default', // 'default' | 'dyslexia' | 'mono'
        lineSpacing: 1.6,
        theme: 'paper', // 'paper' | 'light' | 'dark' | 'high-contrast'
        reduceMotion: false,
        visibleFocus: true,
        underlineLinks: false,
        readAloud: true
    },
    speech: {
        synth: window.speechSynthesis || null,
        utterance: null,
        isPlaying: false,
        isPaused: false,
        currentSentenceIdx: 0,
        sentences: [],
        rate: 1.0,
        pitch: 1.0,
        voice: null
    }
};

// Initialize Application
document.addEventListener('DOMContentLoaded', () => {
    loadAccessibilitySettings();
    initScreenNavigation();
    initAccessibilityControls();
    initSpeechSynthesis();
    initKanbanBoard();
    initApiInteractions();
    loadCurrentAccount();
});

/* ==========================================================================
   Screen & Role Navigation
   ========================================================================== */

function navigateToScreen(screenId) {
    if (AccessEdState.currentScreen === 'screen-course-details' && screenId !== 'screen-course-details') {
        if (activeVideo && !activeVideo.paused) activeVideo.pause();
        stopLearningTimer();
    }
    const allScreens = document.querySelectorAll('.screen-view');
    allScreens.forEach(s => s.classList.remove('active'));

    const targetScreen = document.getElementById(screenId);
    if (targetScreen) {
        targetScreen.classList.add('active');
        AccessEdState.currentScreen = screenId;
        if (screenId === 'screen-course-details' && activeLesson && !activeLesson.isCompleted && !activeVideo) startLearningTimer();
        window.scrollTo({ top: 0, behavior: 'smooth' });

        // Update active class on nav items
        document.querySelectorAll('.sidebar-nav-item').forEach(item => {
            if (item.getAttribute('data-screen') === screenId) {
                item.classList.add('active');
            } else {
                item.classList.remove('active');
            }
        });

        // Announce for screen readers
        const announcement = document.getElementById('screen-announcer');
        if (announcement) {
            const screenTitle = targetScreen.querySelector('h1, h2')?.innerText || screenId;
            announcement.innerText = `Navigated to ${screenTitle}`;
        }
    }
}

async function loadCurrentAccount() {
    try {
        const response = await fetch('/api/account/me', { credentials: 'same-origin' });
        if (response.status === 401) {
            applyCurrentAccount(null);
            return;
        }
        if (!response.ok) {
            throw new Error(`Account lookup failed with HTTP ${response.status}`);
        }

        applyCurrentAccount(await response.json());
    } catch (error) {
        console.error('Could not load the signed-in account.', error);
        const accountLink = document.getElementById('account-nav-link');
        if (accountLink) {
            accountLink.textContent = 'Account unavailable';
            accountLink.removeAttribute('href');
        }
    }
}

function applyCurrentAccount(user) {
    AccessEdState.currentUser = user;

    const roleMap = {
        Student: 'student',
        Educator: 'teacher',
        Admin: 'admin',
        Auditor: 'auditor'
    };
    const role = user ? roleMap[user.role] : 'public';
    if (!role) {
        console.error('The signed-in account has an unsupported role.');
        return;
    }

    AccessEdState.currentRole = role;
    document.body.setAttribute('data-role', role);

    document.querySelectorAll('.sidebar-nav-item').forEach(item => {
        const roles = item.getAttribute('data-roles')?.split(',') || [];
        item.style.display = roles.includes(role) || (role !== 'public' && roles.includes('all')) ? 'block' : 'none';
    });

    const displayName = user?.fullName || 'Guest';
    const initials = displayName
        .split(/\s+/)
        .filter(Boolean)
        .slice(0, 2)
        .map(part => part[0].toLocaleUpperCase())
        .join('') || 'G';
    const roleLabel = user ? user.role : 'Not signed in';

    const nameElement = document.getElementById('sidebar-user-name');
    const roleElement = document.getElementById('sidebar-user-role');
    const avatarElement = document.getElementById('sidebar-user-avatar');
    if (nameElement) nameElement.textContent = displayName;
    if (roleElement) roleElement.textContent = roleLabel;
    if (avatarElement) avatarElement.textContent = initials;

    const profileName = document.getElementById('profile-display-name');
    const profileRole = document.getElementById('profile-display-role');
    const profileAvatar = document.getElementById('profile-avatar');
    const profileDisability = document.getElementById('profile-disability');
    const profileNameInput = document.getElementById('prof-name');
    const profileEmailInput = document.getElementById('prof-email');
    if (profileName) profileName.textContent = displayName;
    if (profileRole) profileRole.textContent = roleLabel;
    if (profileAvatar) profileAvatar.textContent = initials;
    if (profileDisability) profileDisability.textContent = user ? user.disability : 'Accessibility needs not set';
    if (profileNameInput) profileNameInput.value = user?.fullName || '';
    if (profileEmailInput) profileEmailInput.value = user?.email || '';

    const dashboardHeading = document.getElementById('screen3-heading');
    if (dashboardHeading && user && role === 'student') {
        dashboardHeading.textContent = `Welcome, ${displayName}`;
    }

    const accountLink = document.getElementById('account-nav-link');
    if (accountLink) {
        accountLink.textContent = initials;
        accountLink.setAttribute('aria-label', user ? `${displayName} · My account` : 'Sign in');
        accountLink.title = user ? `${displayName} · My account` : 'Sign in';
        accountLink.href = user ? '/Account/Profile' : '/Account/Login';
    }

    navigateToScreen(user
        ? ({ student: 'screen-student-dash', teacher: 'screen-teacher-dash', admin: 'screen-admin-dash', auditor: 'screen-compliance' })[role]
        : 'screen-landing');
}

function initScreenNavigation() {
    // Sidebar navigation clicks
    document.querySelectorAll('.sidebar-nav-item button, .sidebar-nav-item a').forEach(el => {
        el.addEventListener('click', (e) => {
            e.preventDefault();
            const parent = el.closest('.sidebar-nav-item');
            const targetScreen = parent.getAttribute('data-screen');
            if (targetScreen) navigateToScreen(targetScreen);
        });
    });

    // Mobile bottom bar clicks
    document.querySelectorAll('.mobile-tab-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            document.querySelectorAll('.mobile-tab-btn').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            const screen = btn.getAttribute('data-screen');
            if (screen) navigateToScreen(screen);
        });
    });
}

/* ==========================================================================
   Accessibility Engine
   ========================================================================== */

function applyAccessibilitySettings() {
    const a = AccessEdState.accessibility;

    // Font size
    document.documentElement.style.setProperty('--font-scale', `${a.fontSize}%`);

    // Line spacing
    document.documentElement.style.setProperty('--line-height-base', a.lineSpacing);

    // Font family class
    document.body.classList.remove('font-dyslexia', 'font-mono');
    if (a.fontFamily === 'dyslexia') document.body.classList.add('font-dyslexia');
    if (a.fontFamily === 'mono') document.body.classList.add('font-mono');

    // Theme class
    document.body.classList.remove('theme-light', 'theme-dark', 'theme-high-contrast');
    if (a.theme === 'light') document.body.classList.add('theme-light');
    if (a.theme === 'dark') document.body.classList.add('theme-dark');
    if (a.theme === 'high-contrast') document.body.classList.add('theme-high-contrast');
    const themeToggle = document.getElementById('quick-contrast-btn');
    if (themeToggle) {
        const isDark = a.theme === 'dark';
        themeToggle.setAttribute('aria-label', isDark ? 'Switch to light mode' : 'Switch to dark mode');
        themeToggle.title = isDark ? 'Switch to light mode' : 'Switch to dark mode';
        themeToggle.innerHTML = `<i class="bi ${isDark ? 'bi-sun' : 'bi-moon-stars'}" aria-hidden="true"></i>`;
    }
    document.querySelectorAll('[data-theme-name]').forEach(btn => {
        btn.classList.toggle('active', btn.getAttribute('data-theme-name') === a.theme);
    });

    // Toggles
    document.body.classList.toggle('reduced-motion', a.reduceMotion);
    document.body.classList.toggle('underline-links', a.underlineLinks);

    // Live preview element update
    const previewEl = document.getElementById('accessibility-live-preview');
    if (previewEl) {
        previewEl.style.fontSize = `${a.fontSize}%`;
        previewEl.style.lineHeight = a.lineSpacing;
        if (a.fontFamily === 'dyslexia') previewEl.style.fontFamily = 'var(--font-dyslexic)';
        else if (a.fontFamily === 'mono') previewEl.style.fontFamily = 'var(--font-mono)';
        else previewEl.style.fontFamily = 'var(--font-body)';
    }

    // Persist
    localStorage.setItem('accessed_settings', JSON.stringify(a));
}

function loadAccessibilitySettings() {
    const saved = localStorage.getItem('accessed_settings');
    if (saved) {
        try {
            AccessEdState.accessibility = Object.assign(AccessEdState.accessibility, JSON.parse(saved));
        } catch (e) {
            console.error('Failed to parse saved settings', e);
        }
    }
    applyAccessibilitySettings();
}

function initAccessibilityControls() {
    // Font Size Slider
    const fontSlider = document.getElementById('setting-text-size');
    const fontLabel = document.getElementById('text-size-val');
    if (fontSlider) {
        fontSlider.value = AccessEdState.accessibility.fontSize;
        if (fontLabel) fontLabel.innerText = `${fontSlider.value}%`;

        fontSlider.addEventListener('input', (e) => {
            const val = parseInt(e.target.value);
            AccessEdState.accessibility.fontSize = val;
            if (fontLabel) fontLabel.innerText = `${val}%`;
            applyAccessibilitySettings();
        });
    }

    // Typeface Buttons
    document.querySelectorAll('[data-font-type]').forEach(btn => {
        btn.addEventListener('click', () => {
            document.querySelectorAll('[data-font-type]').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            AccessEdState.accessibility.fontFamily = btn.getAttribute('data-font-type');
            applyAccessibilitySettings();
        });
    });

    // Spacing Buttons
    document.querySelectorAll('[data-spacing-val]').forEach(btn => {
        btn.addEventListener('click', () => {
            document.querySelectorAll('[data-spacing-val]').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            AccessEdState.accessibility.lineSpacing = parseFloat(btn.getAttribute('data-spacing-val'));
            applyAccessibilitySettings();
        });
    });

    // Theme Buttons
    document.querySelectorAll('[data-theme-name]').forEach(btn => {
        btn.addEventListener('click', () => {
            document.querySelectorAll('[data-theme-name]').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            AccessEdState.accessibility.theme = btn.getAttribute('data-theme-name');
            applyAccessibilitySettings();
        });
    });

    // Toggle Checks
    document.getElementById('toggle-reduce-motion')?.addEventListener('change', (e) => {
        AccessEdState.accessibility.reduceMotion = e.target.checked;
        applyAccessibilitySettings();
    });

    document.getElementById('toggle-underline-links')?.addEventListener('change', (e) => {
        AccessEdState.accessibility.underlineLinks = e.target.checked;
        applyAccessibilitySettings();
    });

    // Quickbar dark theme toggle
    document.getElementById('quick-contrast-btn')?.addEventListener('click', () => {
        AccessEdState.accessibility.theme = (AccessEdState.accessibility.theme === 'dark') ? 'paper' : 'dark';
        applyAccessibilitySettings();
    });

    // Quick Font Size Buttons
    document.getElementById('quick-font-decrease')?.addEventListener('click', () => {
        AccessEdState.accessibility.fontSize = Math.max(80, AccessEdState.accessibility.fontSize - 10);
        applyAccessibilitySettings();
    });

    document.getElementById('quick-font-increase')?.addEventListener('click', () => {
        AccessEdState.accessibility.fontSize = Math.min(150, AccessEdState.accessibility.fontSize + 10);
        applyAccessibilitySettings();
    });

    document.getElementById('quick-font-reset')?.addEventListener('click', () => {
        AccessEdState.accessibility.fontSize = 100;
        applyAccessibilitySettings();
    });
}

/* ==========================================================================
   Text-to-Speech Karaoke Engine
   ========================================================================== */

function initSpeechSynthesis() {
    const readerTextEl = document.getElementById('karaoke-reader-text');
    if (!readerTextEl) return;

    // Parse sentences
    const rawText = readerTextEl.getAttribute('data-reader-raw') || readerTextEl.innerText;
    AccessEdState.speech.sentences = rawText.match(/[^.!?]+[.!?]+/g) || [rawText];

    // Wrap sentences in spans for highlighting
    readerTextEl.innerHTML = AccessEdState.speech.sentences
        .map((sentence, idx) => `<span class="reader-sentence" data-idx="${idx}">${sentence.trim()} </span>`)
        .join('');

    // Load voices
    if (window.speechSynthesis) {
        window.speechSynthesis.onvoiceschanged = () => {
            const voiceSelect = document.getElementById('tts-voice-select');
            if (voiceSelect) {
                const voices = window.speechSynthesis.getVoices();
                voiceSelect.innerHTML = voices
                    .filter(v => v.lang.startsWith('en') || v.lang.startsWith('hi'))
                    .map(v => `<option value="${v.name}">${v.name} (${v.lang})</option>`)
                    .join('');
            }
        };
    }

    // Play/Pause Button
    const playBtn = document.getElementById('tts-play-btn');
    if (playBtn) {
        playBtn.addEventListener('click', toggleSpeech);
    }

    // Skip Buttons
    document.getElementById('tts-prev-btn')?.addEventListener('click', () => {
        stopSpeech();
        AccessEdState.speech.currentSentenceIdx = Math.max(0, AccessEdState.speech.currentSentenceIdx - 1);
        speakCurrentSentence();
    });

    document.getElementById('tts-next-btn')?.addEventListener('click', () => {
        stopSpeech();
        AccessEdState.speech.currentSentenceIdx = Math.min(AccessEdState.speech.sentences.length - 1, AccessEdState.speech.currentSentenceIdx + 1);
        speakCurrentSentence();
    });

    // Speed Slider
    document.getElementById('tts-speed-slider')?.addEventListener('input', (e) => {
        AccessEdState.speech.rate = parseFloat(e.target.value);
        document.getElementById('tts-speed-label').innerText = `${e.target.value}×`;
    });

    // Pitch Slider
    document.getElementById('tts-pitch-slider')?.addEventListener('input', (e) => {
        AccessEdState.speech.pitch = parseFloat(e.target.value);
    });

    // Keyboard Shortcuts (Space to play/pause, Left/Right for sentence)
    window.addEventListener('keydown', (e) => {
        if (AccessEdState.currentScreen === 'screen-tts-reader' || AccessEdState.currentScreen === 'screen-course-details') {
            if (e.code === 'Space' && e.target.tagName !== 'INPUT' && e.target.tagName !== 'TEXTAREA') {
                e.preventDefault();
                toggleSpeech();
            } else if (e.code === 'ArrowRight') {
                document.getElementById('tts-next-btn')?.click();
            } else if (e.code === 'ArrowLeft') {
                document.getElementById('tts-prev-btn')?.click();
            }
        }
    });
}

function toggleSpeech() {
    if (!AccessEdState.accessibility.readAloud) { alert('Read aloud is turned off in your accessibility settings.'); return; }
    if (AccessEdState.speech.isPlaying) {
        pauseSpeech();
    } else if (AccessEdState.speech.isPaused && window.speechSynthesis) {
        window.speechSynthesis.resume();
        AccessEdState.speech.isPaused = false;
        AccessEdState.speech.isPlaying = true;
        const playBtn = document.getElementById('tts-play-btn');
        if (playBtn) playBtn.innerHTML = '<i class="bi bi-pause-fill"></i> Pause';
    } else {
        speakCurrentSentence();
    }
}

function speakCurrentSentence() {
    if (!window.speechSynthesis) {
        alert('Web Speech API is not supported in this browser.');
        return;
    }

    const sentences = AccessEdState.speech.sentences;
    if (AccessEdState.speech.currentSentenceIdx >= sentences.length) {
        AccessEdState.speech.currentSentenceIdx = 0;
    }

    const idx = AccessEdState.speech.currentSentenceIdx;
    const textToSpeak = sentences[idx];

    highlightSentence(idx);

    window.speechSynthesis.cancel();
    const utterance = new SpeechSynthesisUtterance(textToSpeak);
    utterance.rate = AccessEdState.speech.rate;
    utterance.pitch = AccessEdState.speech.pitch;

    const voiceSelect = document.getElementById('tts-voice-select');
    if (voiceSelect && voiceSelect.value) {
        const voices = window.speechSynthesis.getVoices();
        utterance.voice = voices.find(v => v.name === voiceSelect.value) || null;
    }

    utterance.onend = () => {
        if (AccessEdState.speech.currentSentenceIdx < sentences.length - 1) {
            AccessEdState.speech.currentSentenceIdx++;
            speakCurrentSentence();
        } else {
            stopSpeech();
        }
    };

    AccessEdState.speech.utterance = utterance;
    AccessEdState.speech.isPlaying = true;
    window.speechSynthesis.speak(utterance);

    // Update UI button icon
    const playBtn = document.getElementById('tts-play-btn');
    if (playBtn) playBtn.innerHTML = '<i class="bi bi-pause-fill"></i>';

    // Update status counter
    const counterEl = document.getElementById('tts-sentence-counter');
    if (counterEl) counterEl.innerText = `Sentence ${idx + 1} of ${sentences.length}`;
}

function pauseSpeech() {
    if (window.speechSynthesis) {
        window.speechSynthesis.pause();
        AccessEdState.speech.isPlaying = false;
        AccessEdState.speech.isPaused = true;
        const playBtn = document.getElementById('tts-play-btn');
        if (playBtn) playBtn.innerHTML = '<i class="bi bi-play-fill"></i>';
    }
}

function stopSpeech() {
    if (window.speechSynthesis) {
        window.speechSynthesis.cancel();
        AccessEdState.speech.isPlaying = false;
        AccessEdState.speech.isPaused = false;
        AccessEdState.speech.currentSentenceIdx = 0;
        const playBtn = document.getElementById('tts-play-btn');
        if (playBtn) playBtn.innerHTML = '<i class="bi bi-play-fill"></i>';
    }
}

function highlightSentence(idx) {
    document.querySelectorAll('.reader-sentence').forEach(span => {
        if (parseInt(span.getAttribute('data-idx')) === idx) {
            span.classList.add('active');
            span.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        } else {
            span.classList.remove('active');
        }
    });
}

/* ==========================================================================
   Kanban Board Interactive Statuses
   ========================================================================== */

function initKanbanBoard() {
    document.querySelectorAll('.kanban-card').forEach(card => {
        card.addEventListener('click', () => {
            const currentCol = card.closest('.kanban-column').getAttribute('data-status');
            let nextCol = 'in-review';
            if (currentCol === 'in-review') nextCol = 'resolved';
            else if (currentCol === 'resolved') nextCol = 'open';

            const targetColumn = document.querySelector(`.kanban-column[data-status="${nextCol}"] .kanban-cards-container`);
            if (targetColumn) {
                targetColumn.appendChild(card);
                updateKanbanCounters();
            }
        });
    });
}

function updateKanbanCounters() {
    document.querySelectorAll('.kanban-column').forEach(col => {
        const count = col.querySelectorAll('.kanban-card').length;
        const badge = col.querySelector('.column-count');
        if (badge) badge.innerText = count;
    });
}

/* ==========================================================================
   Backend API Integrations (Compliance, Grievances & Conduct)
   ========================================================================== */

function initApiInteractions() {
    // 1. Grievance Form
    const grievanceForm = document.getElementById('portal-grievance-form');
    if (grievanceForm) {
        grievanceForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const desc = document.getElementById('portal-grievance-desc').value;
            const category = document.getElementById('portal-grievance-cat').value;
            const fullDesc = `[Category: ${category}] ${desc}`;

            try {
                const res = await lmsFetch('/api/governance/grievance', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        issueDescription: fullDesc
                    })
                });

                if (res.ok) {
                alert('Support ticket created. Its 72-hour SLA is now active.');
                    document.getElementById('portal-grievance-desc').value = '';
                    loadGrievanceTable();
                    if (typeof lmsLoadDashboard === 'function') lmsLoadDashboard();
                } else {
                alert('Could not submit the support ticket. Please sign in and try again.');
                }
            } catch (err) {
            alert(err.message || 'Could not reach Sadachar. Your support ticket was not submitted.');
            }
        });
    }

    // 2. Trigger SLA Check Button
    document.getElementById('portal-trigger-sla')?.addEventListener('click', async () => {
        try {
            const res = await lmsFetch('/api/governance/grievance/trigger-sla-check', { method: 'POST' });
            const data = await res.json();
            alert(`SLA Scan Completed: ${data.ticketsEscalated} overdue tickets escalated to Ombudsman.`);
            loadGrievanceTable();
        } catch (err) {
            alert('Could not run the SLA review. Please verify your administrator access and try again.');
        }
    });

    // 3. Compliance Evaluator in Screen 11
    const evalForm = document.getElementById('portal-compliance-form');
    if (evalForm) {
        evalForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const payload = {
                materialId: document.getElementById('eval-material-id').value,
                materialTitle: document.getElementById('eval-material-title').value,
                colorContrastScore: parseFloat(document.getElementById('eval-contrast').value),
                altTextScore: parseFloat(document.getElementById('eval-alt').value),
                keyboardNavigabilityScore: parseFloat(document.getElementById('eval-nav').value),
                screenReaderAriaScore: parseFloat(document.getElementById('eval-aria').value),
                remarks: "Submitted via Sadachar Portal"
            };

            try {
                const res = await lmsFetch('/api/compliance/evaluate', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload)
                });
                const data = await res.json();
                alert(`Audit complete! WCAG 2.2 Score: ${data.wcagScore}% (${data.complianceStatusSummary})`);
            } catch (err) {
                alert('Could not submit the compliance audit. Please try again.');
            }
        });
    }

    // Export the saved audit history from the compliance API.
    document.getElementById('export-report-csv')?.addEventListener('click', async () => {
        const response = await lmsFetch('/api/compliance/all');
        if (!response.ok) { alert('Could not export the saved audit history.'); return; }
        const logs = await response.json();
        const cell = value => `"${String(value ?? '').replaceAll('"', '""')}"`;
        const rows = [['Material', 'WCAG Score', 'Status', 'Audited At'], ...logs.map(log => [log.materialTitle, log.wcagScore, log.complianceStatusSummary, log.auditedAt])];
        const blob = new Blob([rows.map(row => row.map(cell).join(',')).join('\r\n')], { type: 'text/csv;charset=utf-8' });
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = 'Sadachar_Compliance_Audit_Report.csv';
        document.body.appendChild(link);
        link.click();
        link.remove();
        URL.revokeObjectURL(link.href);
    });

    // Initial load
    loadGrievanceTable();
}

async function loadGrievanceTable() {
    const tableBody = document.getElementById('support-requests-tbody');
    const adminBody = document.getElementById('admin-ticket-list');
    if (!tableBody && !adminBody) return;

    try {
        const res = await fetch('/api/governance/grievances');
        if (!res.ok) { if (tableBody) tableBody.innerHTML = '<tr><td colspan="4">Support requests are unavailable.</td></tr>'; if (adminBody) adminBody.innerHTML = '<tr><td colspan="6">Support tickets could not be loaded.</td></tr>'; return; }
        const tickets = await res.json();
        if (tickets.length === 0) { if (tableBody) tableBody.innerHTML = '<tr><td colspan="4">No support tickets yet.</td></tr>'; if (adminBody) adminBody.innerHTML = '<tr><td colspan="6">No support tickets yet.</td></tr>'; return; }

        const statusLabels = { 1: 'Open', 2: 'In progress', 3: 'Escalated', 4: 'Resolved' };
        if (tableBody) tableBody.innerHTML = tickets.map(t => `<tr><td><strong>#${t.ticketId.substring(0, 8)}</strong></td><td>${lmsEscape(t.issueDescription)}</td><td>${statusLabels[t.status] || 'Unknown'}</td><td><small>${new Date(t.slaExpiryDateTime).toLocaleString()}</small></td></tr>`).join('');
        if (adminBody) adminBody.innerHTML = tickets.map(t => `<tr><td>#${t.ticketId.substring(0, 8)}</td><td>${lmsEscape(t.studentId)}</td><td>${lmsEscape(t.issueDescription)}</td><td>${statusLabels[t.status] || 'Unknown'}</td><td>${new Date(t.slaExpiryDateTime).toLocaleString()}</td><td>${t.status === 4 ? 'Closed' : `<button class="course-secondary" data-ticket-id="${t.ticketId}">Resolve</button>`}</td></tr>`).join('');
        adminBody?.querySelectorAll('[data-ticket-id]').forEach(button => button.addEventListener('click', async () => {
            const resolutionNotes = prompt('Resolution note (optional)') || '';
            try { await lmsFetch(`/api/governance/grievance/${button.dataset.ticketId}/status`, { method: 'PUT', body: JSON.stringify({ status: 4, resolutionNotes }) }); await loadGrievanceTable(); await lmsLoadDashboard(); }
            catch (error) { document.getElementById('admin-ticket-feedback').textContent = error.message; }
        }));
    } catch (e) {
        if (tableBody) tableBody.innerHTML = '<tr><td colspan="4">Support tickets could not be loaded.</td></tr>';
        if (adminBody) adminBody.innerHTML = '<tr><td colspan="6">Support tickets could not be loaded.</td></tr>';
    }
}

/* Authenticated LMS screens: all learner values are read from the database APIs. */
let lmsCsrfToken = null;
let lmsPage = 1;
let lmsPageCount = 1;
let activeLesson = null;
let activeVideo = null;
let learningTimer = null;
let preferenceSaveTimer = null;
let lmsControlsReady = false;

const lmsEscape = value => String(value ?? '').replace(/[&<>"']/g, char => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[char]));
function stopLearningTimer() { if (learningTimer) clearInterval(learningTimer); learningTimer = null; }
function startLearningTimer() {
    if (learningTimer || !activeLesson || activeLesson.isCompleted || document.hidden || (activeVideo && activeVideo.paused)) return;
    learningTimer = setInterval(() => lmsJson('/api/learning/activities', { method: 'POST', body: JSON.stringify({
        lessonId: activeLesson.id, type: activeVideo ? 'video' : 'lesson', progressSeconds: 30,
        mediaPositionSeconds: activeVideo ? Math.floor(activeVideo.currentTime) : null, isCompleted: false
    }) }).catch(console.error), 30000);
}
const lmsFetch = async (url, options = {}) => {
    const method = (options.method || 'GET').toUpperCase();
    const headers = new Headers(options.headers || {});
    if (!lmsCsrfToken && !['GET', 'HEAD', 'OPTIONS'].includes(method)) {
        const tokenResponse = await fetch('/api/account/csrf', { credentials: 'same-origin' });
        if (tokenResponse.ok) lmsCsrfToken = (await tokenResponse.json()).requestToken;
    }
    if (lmsCsrfToken && !['GET', 'HEAD', 'OPTIONS'].includes(method)) headers.set('RequestVerificationToken', lmsCsrfToken);
    if (options.body && !(options.body instanceof FormData) && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json');
    return fetch(url, { ...options, method, headers, credentials: 'same-origin' });
};

async function lmsJson(url, options) {
    const response = await lmsFetch(url, options);
    if (!response.ok) throw new Error(response.status === 401 ? 'Please sign in to use this feature.' : `Request failed (${response.status}).`);
    return response.status === 204 ? null : response.json();
}

async function lmsBoot() {
    try {
        const tokenResponse = await fetch('/api/account/csrf', { credentials: 'same-origin' });
        if (tokenResponse.ok) lmsCsrfToken = (await tokenResponse.json()).requestToken;
        const accountResponse = await fetch('/api/account/me', { credentials: 'same-origin' });
        if (!accountResponse.ok) { applyCurrentAccount(null); return; }
        const account = await accountResponse.json();
        applyCurrentAccount(account);
        const requestForm = document.getElementById('portal-grievance-form');
        if (requestForm) requestForm.closest('.col-lg-6').hidden = account.role === 'Admin' || account.role === 'Auditor';
        await lmsLoadPreferences();
        await Promise.all([lmsLoadDashboard(), lmsLoadCourses(), lmsLoadResources(), loadGrievanceTable(), lmsLoadAdmin(), lmsLoadCompliance(), lmsLoadProfile()]);
        if (!lmsControlsReady) { lmsSetupControls(); lmsControlsReady = true; }
    } catch (error) {
        console.error('Sadachar could not load the account workspace.', error);
        const live = document.getElementById('screen-announcer');
        if (live) live.textContent = error.message;
    }
}

async function lmsLoadDashboard() {
    if (!AccessEdState.currentUser) return;
    const data = await lmsJson('/api/learning/dashboard');
    const stats = data.stats;
    for (const [id, value] of Object.entries({ 'stat-enrolled': stats.enrolledCourses, 'stat-resources': stats.accessibleResources,
        'stat-streak': `${stats.readingStreak} days`, 'stat-tickets': stats.openSupportTickets,
        'stat-completed': stats.completedCourses, 'stat-in-progress': stats.inProgressCourses,
        'stat-hours': stats.learningHours, 'stat-progress': `${stats.overallProgress}%` })) {
        const element = document.getElementById(id); if (element) element.textContent = value;
    }
    const pending = document.getElementById('stat-pending'); if (pending) pending.textContent = stats.pendingAssignments;
    const assessmentCount = document.getElementById('stat-assessments'); if (assessmentCount) assessmentCount.textContent = stats.upcomingAssessments;
    const weekly = document.getElementById('weekly-activity');
    if (weekly) {
        const activityByDate = new Map(data.weeklyActivity.map(item => [item.date.slice(0, 10), item.seconds]));
        const days = Array.from({ length: 7 }, (_, index) => { const day = new Date(); day.setHours(0, 0, 0, 0); day.setDate(day.getDate() - 6 + index); return day; });
        const values = days.map(day => activityByDate.get(day.toISOString().slice(0, 10)) || 0);
        const maxSeconds = Math.max(1, ...values);
        weekly.innerHTML = days.map((day, index) => `<div class="weekly-activity-row"><span>${day.toLocaleDateString([], { weekday: 'short' })}</span><progress max="${maxSeconds}" value="${values[index]}" aria-label="${day.toLocaleDateString()}: ${Math.round(values[index] / 60)} minutes"></progress><span>${Math.round(values[index] / 60)} min</span></div>`).join('');
        document.getElementById('weekly-total').textContent = `${Math.round(values.reduce((a, b) => a + b, 0) / 60)} minutes this week`;
    }
    const recommendations = document.getElementById('learning-recommendations');
    if (recommendations) {
        const weak = data.recommendations.map(item => `<article class="mb-3"><strong>Review ${lmsEscape(item.topic)}</strong><p class="small">Average score: ${Math.round(item.averageScore)}% over ${item.attempts} attempt${item.attempts === 1 ? '' : 's'}.</p>${item.resources.length ? item.resources.map(resource => `<button class="course-secondary d-block mb-1" data-resource-topic="${lmsEscape(item.topic)}">${lmsEscape(resource.title)} · ${lmsEscape(resource.type)}</button>`).join('') : '<p class="small">No related resource is available yet.</p>'}</article>`).join('');
        const strong = data.topicPerformance.filter(topic => topic.averageScore >= 80).map(topic => `<p><strong>Strength:</strong> ${lmsEscape(topic.topic)} · ${Math.round(topic.averageScore)}% across ${topic.attempts} attempt${topic.attempts === 1 ? '' : 's'}</p>`).join('');
        recommendations.innerHTML = weak + strong || '<p class="text-muted">Take a quiz to get recommendations based on your results.</p>';
        recommendations.querySelectorAll('[data-resource-topic]').forEach(button => button.addEventListener('click', () => { navigateToScreen('screen-resources'); const search = document.getElementById('resource-search'); search.value = button.dataset.resourceTopic; lmsPage = 1; lmsLoadResources(); }));
    }
    const profileHistory = document.getElementById('profile-learning-history');
    if (profileHistory) profileHistory.innerHTML = `<p>${stats.enrolledCourses} enrolled courses · ${stats.completedCourses} completed · ${stats.learningHours} learning hours</p><p>Overall lesson completion: ${stats.overallProgress}% · Current learning streak: ${stats.readingStreak} days</p><p>Last activity: ${data.lastActivityUtc ? new Date(data.lastActivityUtc).toLocaleString() : 'No activity recorded'}</p>`;
    const scoreTrend = document.getElementById('quiz-score-trend');
    if (scoreTrend) scoreTrend.innerHTML = data.scoreTrend.length ? data.scoreTrend.map(item => `<div class="d-flex justify-content-between py-1 border-bottom"><span>${lmsEscape(item.topic || 'Assessment')} · ${new Date(item.attemptedAtUtc).toLocaleDateString()}</span><strong>${item.score}%</strong></div>`).join('') : '<p class="small text-muted">No quiz scores recorded yet.</p>';
    const list = document.getElementById('continue-learning-list');
    if (list) {
        list.innerHTML = data.courses.length ? data.courses.map(course => {
            const lessonId = (data.recentLesson?.courseId === course.id && data.recentLesson?.lessonId) || course.resumeLessonId || course.lessons[0]?.id;
            const lessonTitle = course.lessons.find(l => l.id === lessonId)?.title || 'No lessons published yet';
            return `<article class="featured-course"><div class="course-progress" role="img" aria-label="${course.progress} percent complete"><span>${course.progress}%</span></div><div class="featured-course-copy"><h3>${lmsEscape(course.title)}</h3><p>${lmsEscape(lessonTitle)} · ${lmsEscape(course.subject)}</p><div class="course-actions">${lessonId ? `<button class="hero-cta" data-resume-lesson="${lessonId}"><i class="bi bi-play-fill"></i> Resume</button>` : ''}<span>${course.completed} of ${course.lessons.length} lessons complete</span></div></div></article>`;
        }).join('') : '<p class="text-muted">You are not enrolled in a course yet. Browse the course catalog to get started.</p>';
        list.querySelectorAll('[data-resume-lesson]').forEach(button => button.addEventListener('click', () => lmsOpenLesson(button.dataset.resumeLesson)));
    }
    const schedule = document.getElementById('schedule-list');
    if (schedule) schedule.innerHTML = data.schedule.length ? data.schedule.map(event => {
        const start = new Date(event.startsAtUtc);
        return `<div class="schedule-row"><span class="schedule-date">${start.toLocaleDateString([], { weekday: 'short' })}<strong>${start.getDate()}</strong></span><div><strong>${lmsEscape(event.title)}</strong><small>${start.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} · ${lmsEscape(event.type)}${event.accessNotes ? ` · ${lmsEscape(event.accessNotes)}` : ''}</small></div></div>`;
    }).join('') : '<p class="text-muted">No upcoming classes or deadlines.</p>';
    const assessmentList = document.getElementById('assessment-list');
    if (assessmentList) assessmentList.innerHTML = data.assessments.length ? data.assessments.map(item => `<article class="schedule-row"><div><strong>${lmsEscape(item.title)}</strong><small>Due ${new Date(item.dueAtUtc).toLocaleString()}</small></div>${item.isSubmitted ? '<span class="course-status status-ready">Submitted</span>' : `<button class="course-secondary" data-submit-assignment="${item.id}">Submit</button>`}</article>`).join('') : '<p class="text-muted">No upcoming assignments.</p>';
    assessmentList?.querySelectorAll('[data-submit-assignment]').forEach(button => button.addEventListener('click', async () => {
        const submissionText = prompt('Enter your assignment response'); if (!submissionText?.trim()) return;
        try { await lmsJson(`/api/learning/assignments/${button.dataset.submitAssignment}/submit`, { method: 'POST', body: JSON.stringify(submissionText.trim()) }); await lmsLoadDashboard(); }
        catch (error) { alert(error.message); }
    }));
    const announcements = document.getElementById('announcement-list');
    if (announcements) announcements.innerHTML = data.announcements.length ? data.announcements.map(item => `<article class="announcement-row"><span class="announcement-dot" aria-hidden="true"></span><div><strong>${lmsEscape(item.title)}${item.isRead ? '' : ' (new)'}</strong><p class="small mb-1">${lmsEscape(item.body)}</p><small>${new Date(item.publishedAtUtc).toLocaleString()}</small>${!item.isRead ? `<button class="course-secondary" data-read-announcement="${item.id}">Mark as read</button>` : ''}${AccessEdState.currentUser?.role === 'Admin' ? `<div><button class="course-secondary" data-edit-announcement="${item.id}">Edit</button><button class="course-secondary ms-2" data-delete-announcement="${item.id}">Delete</button></div>` : ''}</div></article>`).join('') : '<p class="text-muted">No announcements yet.</p>';
    announcements?.querySelectorAll('[data-read-announcement]').forEach(button => button.addEventListener('click', async () => {
        await lmsJson(`/api/learning/announcements/${button.dataset.readAnnouncement}/read`, { method: 'POST' }); await lmsLoadDashboard();
    }));
    announcements?.querySelectorAll('[data-edit-announcement]').forEach(button => button.addEventListener('click', async () => {
        const item = data.announcements.find(a => a.id === button.dataset.editAnnouncement); if (!item) return;
        const title = prompt('Announcement title', item.title); if (!title?.trim()) return;
        const body = prompt('Announcement text', item.body); if (body === null) return;
        try { await lmsJson(`/api/learning/admin/announcements/${item.id}`, { method: 'PUT', body: JSON.stringify({ title, body, isPublished: true }) }); await lmsLoadDashboard(); }
        catch (error) { alert(error.message); }
    }));
    announcements?.querySelectorAll('[data-delete-announcement]').forEach(button => button.addEventListener('click', async () => {
        if (!confirm('Delete this announcement?')) return;
        try { await lmsJson(`/api/learning/admin/announcements/${button.dataset.deleteAnnouncement}`, { method: 'DELETE' }); await lmsLoadDashboard(); }
        catch (error) { alert(error.message); }
    }));
    const notificationDot = document.querySelector('.notification-dot');
    if (notificationDot) notificationDot.hidden = !data.announcements.some(item => !item.isRead);
}

async function lmsLoadProfile() {
    if (!AccessEdState.currentUser) return;
    const saved = await lmsJson('/api/learning/bookmarks');
    const host = document.getElementById('saved-resources'); if (!host) return;
    const items = [...saved.resources, ...saved.lessons];
    host.innerHTML = items.length ? items.map(item => `<p>${lmsEscape(item.title)} · ${lmsEscape(item.type)}${item.fileUrl ? ` · <a href="${lmsEscape(item.fileUrl)}">Open</a>` : ''}</p>`).join('') : '<p class="text-muted">No saved learning items yet.</p>';
}

async function lmsLoadCourses() {
    if (!AccessEdState.currentUser) return;
    const courses = await lmsJson('/api/learning/courses');
    const host = document.getElementById('available-courses-list');
    if (!host) return;
    host.innerHTML = courses.length ? courses.map(course => `<article class="col-md-6" data-course-id="${course.id}"><div class="accessed-card h-100"><h3 class="h6 fw-bold">${lmsEscape(course.title)}</h3><p>${lmsEscape(course.subject)}</p><p class="small">${lmsEscape(course.description)}</p><p class="small">${course.lessons.length} lessons · ${course.isPublished ? 'Published' : 'Draft'}</p>${course.isEnrolled ? (course.lessons.map(lesson => `<button class="course-secondary me-1 mb-1" data-open-lesson="${lesson.id}">${lmsEscape(lesson.title)}</button>`).join('') || '<span>Lessons will appear here when published.</span>') : (course.isPublished && AccessEdState.currentUser.role === 'Student' ? `<button class="hero-cta" data-enroll-course="${course.id}">Enroll</button>` : '')}${['Admin', 'Educator'].includes(AccessEdState.currentUser.role) ? `<div class="mt-2"><button class="course-secondary" data-edit-course="${course.id}">Edit</button>${AccessEdState.currentUser.role === 'Admin' ? `<button class="course-secondary ms-2" data-delete-course="${course.id}">Delete</button>` : ''}</div>` : ''}</div></article>`).join('') : '<p class="text-muted">No courses are available yet.</p>';
    host.querySelectorAll('[data-open-lesson]').forEach(button => button.addEventListener('click', () => lmsOpenLesson(button.dataset.openLesson)));
    host.querySelectorAll('[data-enroll-course]').forEach(button => button.addEventListener('click', async () => {
        try { await lmsJson(`/api/learning/courses/${button.dataset.enrollCourse}/enroll`, { method: 'POST' }); await Promise.all([lmsLoadCourses(), lmsLoadDashboard()]); }
        catch (error) { alert(error.message); }
    }));
    host.querySelectorAll('[data-edit-course]').forEach(button => button.addEventListener('click', async () => {
        const course = courses.find(c => c.id === button.dataset.editCourse); if (!course) return;
        const title = prompt('Course title', course.title); if (!title?.trim()) return;
        const subject = prompt('Course subject', course.subject); if (!subject?.trim()) return;
        const description = prompt('Course description', course.description) ?? course.description;
        try { await lmsJson(`/api/learning/admin/courses/${course.id}`, { method: 'PUT', body: JSON.stringify({ title, subject, description, isPublished: course.isPublished }) }); await lmsLoadCourses(); }
        catch (error) { alert(error.message); }
    }));
    host.querySelectorAll('[data-delete-course]').forEach(button => button.addEventListener('click', async () => {
        if (!confirm('Delete this course and its lessons and enrollments?')) return;
        try { await lmsJson(`/api/learning/admin/courses/${button.dataset.deleteCourse}`, { method: 'DELETE' }); await lmsLoadCourses(); await lmsLoadDashboard(); }
        catch (error) { alert(error.message); }
    }));
    for (const course of courses.filter(c => c.isEnrolled)) {
        const quizzes = await lmsJson(`/api/learning/courses/${course.id}/quizzes`);
        const courseCard = host.querySelector('article[data-course-id="' + course.id + '"] .accessed-card');
        if (courseCard && quizzes.length) {
            const quizList = document.createElement('div'); quizList.className = 'mt-3';
            for (const quiz of quizzes) {
                const button = document.createElement('button'); button.className = 'course-secondary me-2'; button.textContent = `${quiz.title} (${quiz.attempts} attempts)`;
                button.addEventListener('click', () => lmsStartQuiz(quiz.id)); quizList.append(button);
            }
            courseCard.append(quizList);
        }
    }
    if (['Admin', 'Educator'].includes(AccessEdState.currentUser.role)) {
        for (const course of courses) {
            const card = host.querySelector('article[data-course-id="' + course.id + '"] .accessed-card');
            if (!card || !course.lessons.length) continue;
            const editor = document.createElement('div'); editor.className = 'mt-3';
            for (const lesson of course.lessons) {
                const open = document.createElement('button'); open.className = 'course-secondary me-1'; open.textContent = `Open: ${lesson.title}`; open.addEventListener('click', () => lmsOpenLesson(lesson.id)); editor.append(open);
                const edit = document.createElement('button'); edit.className = 'course-secondary me-1'; edit.textContent = 'Edit lesson'; edit.addEventListener('click', async () => {
                    try {
                        const current = await lmsJson(`/api/learning/lessons/${lesson.id}`);
                        const title = prompt('Lesson title', current.title); if (!title?.trim()) return;
                        const content = prompt('Lesson text', current.content); if (content === null) return;
                        await lmsJson(`/api/learning/admin/lessons/${lesson.id}`, { method: 'PUT', body: JSON.stringify({ title, content, order: current.order, durationMinutes: current.durationMinutes, videoUrl: current.videoUrl, transcriptUrl: current.transcriptUrl }) }); await lmsLoadCourses();
                    } catch (error) { alert(error.message); }
                }); editor.append(edit);
                if (AccessEdState.currentUser.role === 'Admin') {
                    const remove = document.createElement('button'); remove.className = 'course-secondary'; remove.textContent = 'Delete lesson'; remove.addEventListener('click', async () => {
                        if (!confirm(`Delete ${lesson.title}?`)) return;
                        try { await lmsJson(`/api/learning/admin/lessons/${lesson.id}`, { method: 'DELETE' }); await lmsLoadCourses(); }
                        catch (error) { alert(error.message); }
                    }); editor.append(remove);
                }
            }
            card.append(editor);
        }
    }
    const adminPanel = document.getElementById('admin-content-panel');
    if (adminPanel) adminPanel.hidden = !['Admin', 'Educator'].includes(AccessEdState.currentUser.role);
    ['lesson-course', 'quiz-course', 'assignment-course'].forEach(id => { const select = document.getElementById(id); if (select) select.innerHTML = courses.map(course => `<option value="${course.id}">${lmsEscape(course.title)}</option>`).join(''); });
    const announcementForm = document.getElementById('admin-announcement-form'); if (announcementForm) announcementForm.hidden = AccessEdState.currentUser.role !== 'Admin';
    const scheduleForm = document.getElementById('admin-schedule-form'); if (scheduleForm) scheduleForm.hidden = !['Admin', 'Educator'].includes(AccessEdState.currentUser.role);
    const assignmentForm = document.getElementById('admin-assignment-form'); if (assignmentForm) assignmentForm.hidden = !['Admin', 'Educator'].includes(AccessEdState.currentUser.role);
    const lessonForm = document.getElementById('admin-lesson-form'); if (lessonForm) lessonForm.hidden = !['Admin', 'Educator'].includes(AccessEdState.currentUser.role);
    const quizForm = document.getElementById('admin-quiz-form'); if (quizForm) quizForm.hidden = !['Admin', 'Educator'].includes(AccessEdState.currentUser.role);
    const educatorList = document.getElementById('educator-course-list'); if (educatorList) educatorList.innerHTML = courses.map(c => `<p>${lmsEscape(c.title)} · ${c.lessons.length} lessons</p>`).join('') || '<p>No published courses yet.</p>';
    const resourceCourse = document.getElementById('resource-course');
    if (resourceCourse) { const selected = resourceCourse.value; resourceCourse.innerHTML = '<option value="">All courses</option>' + courses.map(c => `<option value="${c.id}">${lmsEscape(c.title)}</option>`).join(''); resourceCourse.value = selected; }
}

async function lmsLoadAdmin() {
    if (!AccessEdState.currentUser || AccessEdState.currentUser.role !== 'Admin') return;
    try {
        const overview = await lmsJson('/api/learning/admin/overview');
        for (const [id, key] of Object.entries({ 'admin-users-count': 'users', 'admin-students-count': 'students', 'admin-courses-count': 'courses', 'admin-resources-count': 'publishedResources', 'admin-enrollments-count': 'enrollments', 'admin-assignments-count': 'pendingAssignments', 'admin-tickets-count': 'openTickets' })) document.getElementById(id).textContent = overview[key];
        const users = await lmsJson('/api/learning/admin/users'); const host = document.getElementById('admin-users-list');
        host.innerHTML = users.map(user => `<tr><td>${lmsEscape(user.fullName)}</td><td>${lmsEscape(user.email)}</td><td>${lmsEscape(user.role)}</td><td>${user.enrolledCourses}</td><td>${user.isActive ? 'Active' : 'Inactive'} <button class="course-secondary" data-user-id="${user.id}" data-active="${!user.isActive}">${user.isActive ? 'Deactivate' : 'Activate'}</button></td></tr>`).join('') || '<tr><td colspan="5">No accounts yet.</td></tr>';
        host.querySelectorAll('[data-user-id]').forEach(button => button.addEventListener('click', async () => { await lmsJson(`/api/learning/admin/users/${encodeURIComponent(button.dataset.userId)}/active`, { method: 'PUT', body: JSON.stringify(button.dataset.active === 'true') }); await lmsLoadAdmin(); }));
        const analytics = await lmsJson('/api/learning/admin/analytics');
        const scoreList = document.getElementById('admin-score-history'); scoreList.innerHTML = analytics.scores.length ? analytics.scores.map(s => `<p>${lmsEscape(s.student)} · ${lmsEscape(s.title)} (${lmsEscape(s.topic)}): ${s.score}% · ${new Date(s.attemptedAtUtc).toLocaleString()}</p>`).join('') : '<p>No quiz attempts have been recorded.</p>';
        document.getElementById('admin-course-enrollments').innerHTML = analytics.enrollments.length ? analytics.enrollments.map(c => `<p>${lmsEscape(c.title)} · ${c.total} enrollment${c.total === 1 ? '' : 's'}</p>`).join('') : '<p>No courses have been created.</p>';
    } catch (error) { const node = document.getElementById('admin-overview-error'); if (node) node.textContent = error.message; }
}

async function lmsLoadCompliance() {
    if (!AccessEdState.currentUser || !['Admin', 'Educator', 'Auditor'].includes(AccessEdState.currentUser.role)) return;
    const response = await lmsFetch('/api/compliance/all'); const body = document.getElementById('compliance-history'); if (!body) return;
    if (!response.ok) { body.innerHTML = '<tr><td colspan="4">Audit history is unavailable for this account.</td></tr>'; return; }
    const logs = await response.json();
    body.innerHTML = logs.length ? logs.map(log => `<tr><td>${lmsEscape(log.materialTitle)}</td><td>${log.wcagScore}%</td><td>${lmsEscape(log.complianceStatusSummary)}</td><td>${new Date(log.auditedAt).toLocaleString()}</td></tr>`).join('') : '<tr><td colspan="4">No audits have been recorded.</td></tr>';
}

async function lmsStartQuiz(id) {
    const quiz = await lmsJson(`/api/learning/quizzes/${id}`);
    const panel = document.getElementById('quiz-panel'); const form = document.getElementById('quiz-form');
    panel.hidden = false; document.getElementById('quiz-heading').textContent = quiz.title;
    form.innerHTML = quiz.questions.map((question, questionIndex) => `<fieldset class="mb-3"><legend>${questionIndex + 1}. ${lmsEscape(question.question)}</legend>${question.options.map((option, optionIndex) => `<label class="d-block"><input type="radio" name="answer-${questionIndex}" value="${optionIndex}" required> ${lmsEscape(option)}</label>`).join('')}</fieldset>`).join('') + '<button class="btn-accessed-primary">Submit quiz</button>';
    form.onsubmit = async event => {
        event.preventDefault();
        const answers = quiz.questions.map((_, index) => Number(new FormData(form).get(`answer-${index}`)));
        try { const result = await lmsJson(`/api/learning/quizzes/${id}/attempts`, { method: 'POST', body: JSON.stringify({ answers }) }); document.getElementById('quiz-feedback').textContent = `Score: ${result.score}%`; await lmsLoadDashboard(); }
        catch (error) { document.getElementById('quiz-feedback').textContent = error.message; }
    };
    panel.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

async function lmsLoadResources() {
    if (!AccessEdState.currentUser) return;
    const q = new URLSearchParams({ page: String(lmsPage), pageSize: '12', sort: document.getElementById('resource-sort')?.value || 'recent' });
    const search = document.getElementById('resource-search')?.value.trim();
    const type = document.getElementById('resource-type')?.value;
    const subject = document.getElementById('resource-subject')?.value.trim();
    const courseId = document.getElementById('resource-course')?.value;
    const category = document.getElementById('resource-category')?.value.trim();
    if (search) q.set('q', search); if (type) q.set('type', type); if (subject) q.set('subject', subject); if (courseId) q.set('courseId', courseId); if (category) q.set('category', category);
    const page = await lmsJson(`/api/learning/resources?${q}`);
    lmsPageCount = Math.max(1, Math.ceil(page.total / page.pageSize));
    const host = document.getElementById('resource-list');
    host.innerHTML = page.items.length ? page.items.map(resource => `<article class="col-md-6 col-lg-4"><div class="accessed-card h-100"><span class="status-chip audio-badge">${lmsEscape(resource.type)}${resource.isPublished ? '' : ' · Draft'}</span><h2 class="h6 fw-bold mt-3">${lmsEscape(resource.title)}</h2><p class="small">${lmsEscape(resource.subject)} · ${lmsEscape(resource.category)}</p><p>${lmsEscape(resource.description)}</p>${resource.fileUrl ? `<a class="btn-accessed-primary" href="${lmsEscape(resource.fileUrl)}" target="_blank" rel="noopener" data-resource-open="${resource.id}">Open resource</a>` : '<span class="text-muted">No file attached</span>'}${resource.transcriptUrl ? `<a class="d-block mt-2" href="${lmsEscape(resource.transcriptUrl)}">Transcript</a>` : ''}${resource.isPublished ? `<button class="course-secondary ms-2" data-bookmark-resource="${resource.id}">${resource.isBookmarked ? 'Remove saved' : 'Save resource'}</button>` : ''}${['Admin', 'Educator'].includes(AccessEdState.currentUser.role) ? `<div class="mt-2"><button class="course-secondary" data-edit-resource="${resource.id}">Edit</button>${AccessEdState.currentUser.role === 'Admin' ? `<button class="course-secondary ms-2" data-delete-resource="${resource.id}">Delete</button>` : ''}</div>` : ''}</div></article>`).join('') : '<p class="text-muted">No matching resources have been published.</p>';
    host.querySelectorAll('[data-bookmark-resource]').forEach(button => button.addEventListener('click', async () => { await lmsJson(`/api/learning/resources/${button.dataset.bookmarkResource}/bookmark`, { method: 'POST' }); await lmsLoadResources(); }));
    host.querySelectorAll('[data-resource-open]').forEach(link => link.addEventListener('click', () => lmsJson('/api/learning/activities', { method: 'POST', body: JSON.stringify({ resourceId: link.dataset.resourceOpen, type: 'resource-opened', progressSeconds: 0, isCompleted: false }) }).catch(console.error)));
    host.querySelectorAll('[data-edit-resource]').forEach(button => button.addEventListener('click', async () => {
        const resource = page.items.find(item => item.id === button.dataset.editResource); if (!resource) return;
        const title = prompt('Resource title', resource.title); if (!title?.trim()) return;
        const isPublished = confirm('Publish this resource? Choose Cancel to keep it as a draft.');
        try { await lmsJson(`/api/learning/admin/resources/${resource.id}`, { method: 'PUT', body: JSON.stringify({ title, description: resource.description, type: resource.type, category: resource.category, subject: resource.subject, fileUrl: resource.fileUrl, fileName: resource.fileName, transcriptUrl: resource.transcriptUrl, courseId: resource.courseId, isPublished }) }); await lmsLoadResources(); }
        catch (error) { alert(error.message); }
    }));
    host.querySelectorAll('[data-delete-resource]').forEach(button => button.addEventListener('click', async () => {
        if (!confirm('Delete this resource?')) return;
        try { await lmsJson(`/api/learning/admin/resources/${button.dataset.deleteResource}`, { method: 'DELETE' }); await lmsLoadResources(); }
        catch (error) { alert(error.message); }
    }));
    const label = document.getElementById('resource-page-label'); if (label) label.textContent = `Page ${lmsPage} of ${lmsPageCount} · ${page.total} resources`;
    document.getElementById('resources-previous').disabled = lmsPage <= 1;
    document.getElementById('resources-next').disabled = lmsPage >= lmsPageCount;
}

async function lmsOpenLesson(id) {
    try {
        const lesson = await lmsJson(`/api/learning/lessons/${id}`);
        activeLesson = lesson;
        document.getElementById('lesson-breadcrumb').textContent = lesson.courseTitle;
        document.getElementById('screen5-heading').textContent = lesson.title;
        document.getElementById('lesson-content').textContent = lesson.content || 'This lesson has no text content.';
        const media = document.getElementById('lesson-media');
        media.replaceChildren();
        activeVideo = null;
        const isDirectVideo = lesson.videoUrl && /\.(mp4|webm|ogg)(?:$|\?)/i.test(lesson.videoUrl);
        if (isDirectVideo) {
            const video = document.createElement('video'); video.controls = true; video.preload = 'metadata'; video.className = 'w-100'; video.src = lesson.videoUrl;
            activeVideo = video;
            video.addEventListener('loadedmetadata', () => { if (lesson.mediaPositionSeconds > 0 && lesson.mediaPositionSeconds < video.duration) video.currentTime = lesson.mediaPositionSeconds; });
            let lastSave = 0;
            video.addEventListener('timeupdate', () => { if (video.currentTime - lastSave >= 5) { lastSave = video.currentTime; lmsJson('/api/learning/activities', { method: 'POST', body: JSON.stringify({ lessonId: lesson.id, type: 'video', progressSeconds: 0, mediaPositionSeconds: Math.floor(video.currentTime), progressPercent: video.duration ? Math.round(video.currentTime / video.duration * 100) : 0, isCompleted: false }) }).catch(console.error); } });
            video.addEventListener('play', startLearningTimer);
            video.addEventListener('pause', stopLearningTimer);
            video.addEventListener('ended', () => lmsJson('/api/learning/activities', { method: 'POST', body: JSON.stringify({ lessonId: lesson.id, type: 'video', mediaPositionSeconds: Math.floor(video.currentTime), progressPercent: 100, progressSeconds: 0, isCompleted: true }) }).then(lmsLoadDashboard).catch(console.error));
            media.append(video);
        } else if (lesson.videoUrl) { const link = document.createElement('a'); link.href = lesson.videoUrl; link.target = '_blank'; link.rel = 'noopener'; link.textContent = 'Open lesson video'; media.append(link); }
        if (lesson.transcriptUrl) { const link = document.createElement('a'); link.href = lesson.transcriptUrl; link.className = 'ms-3'; link.textContent = 'Open transcript'; media.append(link); }
        const done = document.getElementById('lesson-complete'); done.disabled = false; done.textContent = lesson.isCompleted ? 'Completed · mark complete again' : 'Mark lesson complete';
        const bookmarkButton = document.getElementById('lesson-bookmark'); bookmarkButton.disabled = false; bookmarkButton.textContent = lesson.isBookmarked ? 'Remove bookmark' : 'Bookmark lesson';
        document.getElementById('lesson-breadcrumb').textContent = `${lesson.courseTitle} · ${isDirectVideo ? `Resume video at ${lesson.mediaPositionSeconds}s` : `Reading progress ${lesson.progressPercent}%`}`;
        if (!isDirectVideo) requestAnimationFrame(() => { const content = document.getElementById('lesson-content'); content.scrollTop = Math.round((content.scrollHeight - content.clientHeight) * lesson.progressPercent / 100); });
        const reader = document.getElementById('karaoke-reader-text');
        if (reader) {
            const raw = lesson.content || lesson.title;
            AccessEdState.speech.sentences = raw.match(/[^.!?]+[.!?]+/g) || [raw];
            AccessEdState.speech.currentSentenceIdx = 0;
            reader.innerHTML = AccessEdState.speech.sentences.map((sentence, index) => `<span class="reader-sentence" data-idx="${index}">${lmsEscape(sentence.trim())} </span>`).join('');
        }
        stopLearningTimer();
        if (!isDirectVideo) startLearningTimer();
        document.getElementById('lesson-content').onscroll = null;
        let lastScrollSave = 0;
        document.getElementById('lesson-content').onscroll = event => {
            if (Date.now() - lastScrollSave < 5000) return; lastScrollSave = Date.now();
            const node = event.currentTarget; const percent = Math.round(100 * node.scrollTop / Math.max(1, node.scrollHeight - node.clientHeight));
            lmsJson('/api/learning/activities', { method: 'POST', body: JSON.stringify({ lessonId: lesson.id, type: 'reading', progressSeconds: 0, progressPercent: percent, isCompleted: false }) }).catch(console.error);
        };
        await lmsJson('/api/learning/activities', { method: 'POST', body: JSON.stringify({ lessonId: lesson.id, type: 'lesson-opened', progressSeconds: 0, isCompleted: false }) });
        navigateToScreen('screen-course-details');
    } catch (error) { alert(error.message); }
}

function lmsSetupControls() {
    document.getElementById('notification-button')?.addEventListener('click', () => {
        if (!AccessEdState.currentUser) { navigateToScreen('screen-login'); return; }
        navigateToScreen('screen-student-dash'); document.getElementById('announcement-list')?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    });
    const reloadResources = () => { lmsPage = 1; lmsLoadResources().catch(console.error); };
    ['resource-type', 'resource-subject', 'resource-sort', 'resource-course', 'resource-category'].forEach(id => document.getElementById(id)?.addEventListener('change', reloadResources));
    document.getElementById('resource-search')?.addEventListener('input', (() => { let timer; return () => { clearTimeout(timer); timer = setTimeout(reloadResources, 250); }; })());
    document.getElementById('resources-previous')?.addEventListener('click', () => { if (lmsPage > 1) { lmsPage--; lmsLoadResources(); } });
    document.getElementById('resources-next')?.addEventListener('click', () => { if (lmsPage < lmsPageCount) { lmsPage++; lmsLoadResources(); } });
    document.addEventListener('visibilitychange', () => {
        if (!activeLesson) return;
        if (document.hidden) stopLearningTimer(); else if (!activeLesson.isCompleted) startLearningTimer();
    });
    document.getElementById('lesson-complete')?.addEventListener('click', async () => { if (!activeLesson) return; await lmsJson('/api/learning/activities', { method: 'POST', body: JSON.stringify({ lessonId: activeLesson.id, type: 'lesson', progressSeconds: 0, progressPercent: 100, isCompleted: true }) }); activeLesson.isCompleted = true; stopLearningTimer(); await lmsLoadDashboard(); document.getElementById('lesson-complete').textContent = 'Lesson completed'; });
    document.getElementById('lesson-read-aloud')?.addEventListener('click', () => { navigateToScreen('screen-tts-reader'); toggleSpeech(); });
    document.getElementById('tts-stop-btn')?.addEventListener('click', stopSpeech);
    document.getElementById('lesson-bookmark')?.addEventListener('click', async () => { if (!activeLesson) return; const result = await lmsJson(`/api/learning/lessons/${activeLesson.id}/bookmark`, { method: 'POST' }); activeLesson.isBookmarked = result.isBookmarked; document.getElementById('lesson-bookmark').textContent = result.isBookmarked ? 'Remove bookmark' : 'Bookmark lesson'; });
    document.getElementById('save-profile')?.addEventListener('click', async () => {
        const feedback = document.getElementById('profile-feedback');
        try { await lmsJson('/api/account/me', { method: 'PUT', body: JSON.stringify({ fullName: document.getElementById('prof-name').value }) }); feedback.textContent = 'Profile saved.'; const updated = await lmsJson('/api/account/me'); AccessEdState.currentUser = updated; document.getElementById('sidebar-user-name').textContent = updated.fullName; document.getElementById('profile-display-name').textContent = updated.fullName; document.getElementById('prof-name').value = updated.fullName; }
        catch (error) { feedback.textContent = error.message; }
    });
    document.getElementById('admin-course-form')?.addEventListener('submit', async event => {
        event.preventDefault();
        try { await lmsJson('/api/learning/admin/courses', { method: 'POST', body: JSON.stringify({ title: document.getElementById('new-course-title').value, subject: document.getElementById('new-course-subject').value, description: document.getElementById('new-course-description').value, isPublished: true }) }); event.target.reset(); await lmsLoadCourses(); document.getElementById('admin-form-feedback').textContent = 'Course published.'; }
        catch (error) { document.getElementById('admin-form-feedback').textContent = error.message; }
    });
    document.getElementById('admin-resource-form')?.addEventListener('submit', async event => {
        event.preventDefault(); const feedback = document.getElementById('admin-form-feedback');
        try {
            let uploaded = {};
            const file = document.getElementById('new-resource-file').files[0];
            if (file) { const form = new FormData(); form.append('file', file); uploaded = await lmsJson('/api/learning/admin/upload', { method: 'POST', body: form }); }
            await lmsJson('/api/learning/admin/resources', { method: 'POST', body: JSON.stringify({ title: document.getElementById('new-resource-title').value, description: '', type: document.getElementById('new-resource-type').value, category: '', subject: document.getElementById('new-resource-subject').value, fileUrl: uploaded.fileUrl || null, fileName: uploaded.fileName || null, transcriptUrl: null, courseId: null, isPublished: true }) });
            event.target.reset(); feedback.textContent = 'Resource published.'; await lmsLoadResources();
        } catch (error) { feedback.textContent = error.message; }
    });
    document.getElementById('admin-lesson-form')?.addEventListener('submit', async event => {
        event.preventDefault(); const feedback = document.getElementById('admin-form-feedback');
        try { await lmsJson(`/api/learning/admin/courses/${document.getElementById('lesson-course').value}/lessons`, { method: 'POST', body: JSON.stringify({ title: document.getElementById('lesson-title').value, content: document.getElementById('lesson-text').value, order: Number(document.getElementById('lesson-order').value), durationMinutes: 0 }) }); event.target.reset(); feedback.textContent = 'Lesson added.'; await lmsLoadCourses(); }
        catch (error) { feedback.textContent = error.message; }
    });
    document.getElementById('admin-quiz-form')?.addEventListener('submit', async event => {
        event.preventDefault(); const options = document.getElementById('quiz-options').value.split('|').map(value => value.trim()).filter(Boolean);
        const correctOption = Number(document.getElementById('quiz-answer').value) - 1;
        const feedback = document.getElementById('admin-form-feedback');
        if (options.length < 2 || correctOption < 0 || correctOption >= options.length) { feedback.textContent = 'Add at least two options and a valid correct option number.'; return; }
        try { await lmsJson('/api/learning/admin/quizzes', { method: 'POST', body: JSON.stringify({ courseId: document.getElementById('quiz-course').value, title: document.getElementById('quiz-topic').value, topic: document.getElementById('quiz-topic').value, isPublished: true, questions: [{ question: document.getElementById('quiz-question').value, options, correctOption }] }) }); event.target.reset(); feedback.textContent = 'Quiz published.'; await lmsLoadCourses(); }
        catch (error) { feedback.textContent = error.message; }
    });
    document.getElementById('admin-announcement-form')?.addEventListener('submit', async event => {
        event.preventDefault(); const feedback = document.getElementById('admin-form-feedback');
        try { await lmsJson('/api/learning/admin/announcements', { method: 'POST', body: JSON.stringify({ title: document.getElementById('announcement-title').value, body: document.getElementById('announcement-body').value, isPublished: true }) }); event.target.reset(); feedback.textContent = 'Announcement published.'; await lmsLoadDashboard(); }
        catch (error) { feedback.textContent = error.message; }
    });
    document.getElementById('admin-assignment-form')?.addEventListener('submit', async event => {
        event.preventDefault(); const feedback = document.getElementById('admin-form-feedback');
        try { await lmsJson('/api/learning/admin/assignments', { method: 'POST', body: JSON.stringify({ courseId: document.getElementById('assignment-course').value, title: document.getElementById('assignment-title').value, dueAtUtc: new Date(document.getElementById('assignment-due').value).toISOString(), description: document.getElementById('assignment-description').value, isPublished: true }) }); event.target.reset(); feedback.textContent = 'Assignment published.'; await lmsLoadDashboard(); }
        catch (error) { feedback.textContent = error.message; }
    });
    document.getElementById('admin-schedule-form')?.addEventListener('submit', async event => {
        event.preventDefault(); const feedback = document.getElementById('admin-form-feedback');
        try { await lmsJson('/api/learning/admin/schedule', { method: 'POST', body: JSON.stringify({ title: document.getElementById('schedule-title').value, type: document.getElementById('schedule-type').value, startsAtUtc: new Date(document.getElementById('schedule-start').value).toISOString(), accessNotes: document.getElementById('schedule-notes').value, userId: null, courseId: null }) }); event.target.reset(); feedback.textContent = 'Schedule event published.'; await lmsLoadDashboard(); }
        catch (error) { feedback.textContent = error.message; }
    });
}

async function lmsLoadPreferences() {
    if (!AccessEdState.currentUser) return;
    const saved = await lmsJson('/api/learning/preferences');
    AccessEdState.accessibility = { ...AccessEdState.accessibility, fontSize: saved.fontSize, fontFamily: saved.fontFamily,
        theme: saved.theme, lineSpacing: Number(saved.lineSpacing), reduceMotion: saved.reduceMotion,
        underlineLinks: saved.underlineLinks, readAloud: saved.readAloud };
    applyAccessibilitySettings();
    const slider = document.getElementById('setting-text-size'); if (slider) slider.value = saved.fontSize;
    document.querySelectorAll('.profile-setting-row input[type="checkbox"]').forEach(input => {
        if (input.getAttribute('aria-label') === 'Dyslexia-friendly font') input.checked = saved.fontFamily === 'dyslexia';
        if (input.getAttribute('aria-label') === 'High contrast mode') input.checked = saved.theme === 'high-contrast';
        if (input.getAttribute('aria-label') === 'Read aloud tool') input.checked = saved.readAloud;
        if (input.getAttribute('aria-label') === 'Reduce motion') input.checked = saved.reduceMotion;
    });
}

const lmsOriginalApplyAccessibility = applyAccessibilitySettings;
applyAccessibilitySettings = function () {
    lmsOriginalApplyAccessibility();
    if (!AccessEdState.currentUser || !lmsCsrfToken) return;
    clearTimeout(preferenceSaveTimer);
    preferenceSaveTimer = setTimeout(() => lmsFetch('/api/learning/preferences', { method: 'PUT', body: JSON.stringify({
        userId: AccessEdState.currentUser.id, fontSize: AccessEdState.accessibility.fontSize,
        fontFamily: AccessEdState.accessibility.fontFamily, theme: AccessEdState.accessibility.theme,
        lineSpacing: AccessEdState.accessibility.lineSpacing, reduceMotion: AccessEdState.accessibility.reduceMotion,
        underlineLinks: AccessEdState.accessibility.underlineLinks, readAloud: AccessEdState.accessibility.readAloud
    }) }).catch(error => console.error('Accessibility preferences were not saved.', error)), 300);
};

loadCurrentAccount = lmsBoot;
