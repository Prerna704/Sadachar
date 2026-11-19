/**
 * AccessEd — Retro Fuzzy UI Engine
 * Enhanced accessibility controls, TTS reader shortcuts, avatar interactions, and responsive layouts
 */

(function () {
    'use strict';

    // Initialize Retro Fuzzy Enhancements
    document.addEventListener('DOMContentLoaded', () => {
        initThemeAndAccessibility();
        initAudioPlayerWidgets();
        initAvatarAnimations();
        initMobileTabBar();
        initKeyboardShortcuts();
    });

    /**
     * Synchronize Accessibility Settings with Body Classes
     */
    function initThemeAndAccessibility() {
        const storedTheme = localStorage.getItem('accessed_theme') || 'paper';
        const storedFont = localStorage.getItem('accessed_font') || 'default';
        const storedSize = localStorage.getItem('accessed_font_size') || '100';
        const reduceMotion = localStorage.getItem('accessed_reduce_motion') === 'true';

        applyAccessibility(storedTheme, storedFont, storedSize, reduceMotion);

        // Listen for system preference changes
        if (window.matchMedia) {
            window.matchMedia('(prefers-reduced-motion: reduce)').addEventListener('change', (e) => {
                if (!localStorage.getItem('accessed_reduce_motion')) {
                    document.body.classList.toggle('reduce-motion', e.matches);
                }
            });
        }
    }

    window.applyAccessibility = function (theme, font, size, reduceMotion) {
        document.body.classList.remove('theme-high-contrast', 'theme-paper', 'theme-dark');
        if (theme === 'high-contrast') {
            document.body.classList.add('theme-high-contrast');
        } else {
            document.body.classList.add('theme-paper');
        }

        document.body.classList.remove('font-dyslexia', 'font-mono');
        if (font === 'dyslexia') {
            document.body.classList.add('font-dyslexia');
        } else if (font === 'mono') {
            document.body.classList.add('font-mono');
        }

        document.documentElement.style.fontSize = (size ? (parseInt(size, 10) / 100 * 16) : 16) + 'px';

        if (reduceMotion) {
            document.body.classList.add('reduce-motion');
        } else {
            document.body.classList.remove('reduce-motion');
        }
    };

    /**
     * Accessible Audio Narration Widgets
     */
    function initAudioPlayerWidgets() {
        document.querySelectorAll('.retro-audio-player').forEach(player => {
            const playBtn = player.querySelector('.audio-play-btn');
            const playLabel = player.querySelector('.play-label');
            const icon = playBtn?.querySelector('i');
            const seek = player.querySelector('input[type="range"]');

            if (!playBtn) return;

            let isPlaying = false;
            let timer = null;

            playBtn.addEventListener('click', () => {
                isPlaying = !isPlaying;
                playBtn.setAttribute('data-playing', isPlaying ? 'true' : 'false');
                playBtn.setAttribute('aria-label', isPlaying ? 'Pause narration' : 'Play narration');

                if (isPlaying) {
                    if (icon) {
                        icon.classList.remove('bi-play-fill');
                        icon.classList.add('bi-pause-fill');
                    }
                    if (playLabel) playLabel.textContent = 'Pause';

                    // Simulate progress if no audio element is attached
                    timer = setInterval(() => {
                        if (seek && parseInt(seek.value, 10) < 100) {
                            seek.value = parseInt(seek.value, 10) + 1;
                        } else if (seek) {
                            clearInterval(timer);
                            isPlaying = false;
                            if (icon) {
                                icon.classList.remove('bi-pause-fill');
                                icon.classList.add('bi-play-fill');
                            }
                            if (playLabel) playLabel.textContent = 'Play';
                        }
                    }, 800);
                } else {
                    if (timer) clearInterval(timer);
                    if (icon) {
                        icon.classList.remove('bi-pause-fill');
                        icon.classList.add('bi-play-fill');
                    }
                    if (playLabel) playLabel.textContent = 'Play';
                }
            });
        });
    }

    /**
     * Subtle bounce for Retro Avatars on focus / click
     */
    function initAvatarAnimations() {
        document.querySelectorAll('.retro-avatar').forEach(avatar => {
            avatar.style.transition = 'transform 0.2s cubic-bezier(0.34, 1.56, 0.64, 1)';
            avatar.addEventListener('mouseenter', () => {
                avatar.style.transform = 'scale(1.08) rotate(-2deg)';
            });
            avatar.addEventListener('mouseleave', () => {
                avatar.style.transform = 'scale(1) rotate(0deg)';
            });
        });
    }

    /**
     * Mobile Tab Bar active state highlight
     */
    function initMobileTabBar() {
        const currentPath = window.location.pathname.toLowerCase();
        document.querySelectorAll('.mobile-tab-btn').forEach(btn => {
            const href = btn.getAttribute('href')?.toLowerCase();
            if (href && (currentPath === href || (href !== '/' && currentPath.startsWith(href)))) {
                btn.classList.add('active');
            }
        });
    }

    /**
     * Global WCAG Keyboard Shortcuts:
     * Alt + 1: Skip to content
     * Alt + A: Open Accessibility Settings
     * Alt + R: Open TTS Reader
     */
    function initKeyboardShortcuts() {
        document.addEventListener('keydown', (e) => {
            if (e.altKey && (e.key === 'a' || e.key === 'A')) {
                window.location.href = '/Accessibility';
            }
            if (e.altKey && (e.key === 'r' || e.key === 'R')) {
                window.location.href = '/Resources/Reader';
            }
        });
    }
})();
