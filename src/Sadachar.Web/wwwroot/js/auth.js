(() => {
    'use strict';

    document.querySelectorAll('[data-password-toggle]').forEach((button) => {
        button.addEventListener('click', () => {
            const input = document.getElementById(button.dataset.passwordToggle);
            if (!input) return;
            const showing = input.type === 'password';
            input.type = showing ? 'text' : 'password';
            button.setAttribute('aria-pressed', String(showing));
            button.setAttribute('aria-label', `${showing ? 'Hide' : 'Show'} ${input.id === 'ConfirmPassword' ? 'confirm password' : 'password'}`);
            const label = button.querySelector('.toggle-label');
            if (label) label.textContent = showing ? 'Hide' : 'Show';
        });
    });

    document.querySelectorAll('form#auth-form').forEach((form) => {
        const fieldError = (name) => document.getElementById(`${name}-error`);
        const setError = (input, message) => {
            if (!input) return;
            input.setAttribute('aria-invalid', message ? 'true' : 'false');
            const error = fieldError(input.name);
            if (error) error.textContent = message;
        };

        form.addEventListener('input', (event) => {
            const input = event.target;
            if (!(input instanceof HTMLInputElement)) return;
            if (input.type === 'radio') {
                const error = form.querySelector('[data-valmsg-for="Role"]');
                if (error) error.textContent = '';
                return;
            }
            setError(input, '');
            if (input.name === 'Password') {
                const confirm = form.querySelector('[data-confirm-password]');
                if (confirm && confirm.value) setError(confirm, confirm.value === input.value ? '' : 'Passwords do not match.');
            }
            if (input.matches('[data-confirm-password]')) {
                const password = form.querySelector('[name="Password"]');
                setError(input, input.value === (password?.value ?? '') ? '' : 'Passwords do not match.');
            }
        });

        form.addEventListener('submit', (event) => {
            let firstInvalid = null;
            const invalidate = (input, message) => {
                setError(input, message);
                if (message && !firstInvalid) firstInvalid = input;
            };

            form.querySelectorAll('input[required]:not([type="radio"]):not([type="checkbox"])').forEach((input) => {
                const value = input.value.trim();
                if (!value) invalidate(input, 'This field is required.');
                else if (input.type === 'email' && !input.validity.valid) invalidate(input, 'Enter a valid email address.');
                else if (input.maxLength > 0 && value.length > input.maxLength) invalidate(input, `Use ${input.maxLength} characters or fewer.`);
                else if (input.name === 'Password' && form.matches('[data-register-form]') && value.length < 8) invalidate(input, 'Use at least 8 characters.');
                else if (input.name === 'Password' && form.matches('[data-register-form]') && !/[a-z]/.test(value)) invalidate(input, 'Include at least one lowercase letter.');
                else if (input.name === 'Password' && form.matches('[data-register-form]') && !/\d/.test(value)) invalidate(input, 'Include at least one number.');
            });

            const password = form.querySelector('[name="Password"]');
            const confirm = form.querySelector('[data-confirm-password]');
            if (confirm && password && confirm.value !== password.value) invalidate(confirm, 'Passwords do not match.');

            const terms = form.querySelector('[name="AcceptTerms"]');
            const termsError = form.querySelector('[data-valmsg-for="AcceptTerms"]');
            if (terms && !terms.checked) {
                if (termsError) termsError.textContent = 'Please accept the terms to create an account.';
                if (!firstInvalid) firstInvalid = terms;
            } else if (termsError) termsError.textContent = '';

            if (firstInvalid) {
                event.preventDefault();
                firstInvalid.focus();
                return;
            }

            form.querySelectorAll('input[aria-invalid="true"]').forEach((input) => input.removeAttribute('aria-invalid'));
        });
    });
})();
