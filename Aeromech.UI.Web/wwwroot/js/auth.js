window.aeroMechAuth = window.aeroMechAuth || {};

window.aeroMechAuth.signIn = async function (userName, password) {
    try {
        const response = await fetch('/Account/SignIn', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ userName, password }),
            credentials: 'same-origin'
        });

        let data = null;
        try {
            const contentType = response.headers.get('content-type') || '';
            if (contentType.includes('application/json')) {
                data = await response.json();
            } else {
                data = { success: response.ok, message: await response.text() };
            }
        } catch {
            data = null;
        }

        if (!response.ok) {
            return {
                success: false,
                message: (data && data.message) ? data.message : 'Invalid username or password.',
                redirectUrl: null
            };
        }

        return data || { success: true, message: null, redirectUrl: '/' };
    } catch {
        return {
            success: false,
            message: 'Network error while signing in.',
            redirectUrl: null
        };
    }
};

// Same shape as signIn and for the same reason: the password change has to travel as an
// HTTP request so the server can reissue the authentication cookie in its response.
window.aeroMechAuth.changePassword = async function (currentPassword, newPassword) {
    try {
        const response = await fetch('/Account/ChangePassword', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ currentPassword, newPassword }),
            credentials: 'same-origin'
        });

        let data = null;
        try {
            const contentType = response.headers.get('content-type') || '';
            if (contentType.includes('application/json')) {
                data = await response.json();
            } else {
                data = { success: response.ok, message: await response.text() };
            }
        } catch {
            data = null;
        }

        if (!response.ok) {
            return {
                success: false,
                message: (data && data.message) ? data.message : 'The password could not be changed.'
            };
        }

        return data || { success: true, message: null };
    } catch {
        return {
            success: false,
            message: 'Network error while changing the password.'
        };
    }
};
