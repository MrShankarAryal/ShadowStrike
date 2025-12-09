import { getUsers, updateUsers } from './db.js';
import bcrypt from 'bcryptjs';
import jwt from 'jsonwebtoken';

const JWT_SECRET = 'vulnerable-secret-key-12345'; // Intentionally weak secret

export default async function handler(req, res) {
    const { action } = req.query;

    // REGISTER - Create new user with hashed password
    if (action === 'register' && req.method === 'POST') {
        const { username, password, email } = req.body;

        if (!username || !password) {
            return res.status(400).json({ error: 'Username and password required' });
        }

        try {
            const users = await getUsers();

            // Check if user exists
            if (users.find(u => u.username === username)) {
                return res.status(409).json({ error: 'Username already exists' });
            }

            // ✅ SECURE: Hash password with bcrypt
            const hashedPassword = await bcrypt.hash(password, 10);

            const newUser = {
                id: users.length + 1,
                username: username, // ❌ VULNERABILITY: Stores username as-is (enables second-order SQLi)
                password: hashedPassword,
                role: 'user',
                email: email || `${username}@example.com`,
                resetToken: null,
                resetTokenExpiry: null
            };

            users.push(newUser);
            await updateUsers(users);

            return res.status(201).json({
                success: true,
                message: 'User registered successfully',
                user: {
                    id: newUser.id,
                    username: newUser.username,
                    role: newUser.role
                }
            });
        } catch (error) {
            return res.status(500).json({ error: error.message });
        }
    }

    // LOGIN - Authenticate with bcrypt
    if (action === 'login' && req.method === 'POST') {
        const { username, password } = req.body;

        if (!username || !password) {
            return res.status(400).json({ error: 'Username and password required' });
        }

        try {
            const users = await getUsers();
            const user = users.find(u => u.username === username);

            if (!user) {
                return res.status(401).json({ error: 'Invalid credentials' });
            }

            // ✅ SECURE: Use bcrypt to compare passwords
            const isValid = await bcrypt.compare(password, user.password);

            if (!isValid) {
                return res.status(401).json({ error: 'Invalid credentials' });
            }

            // ✅ SECURE: Generate JWT token
            // ❌ VULNERABILITY: Accepts algorithm "none" (will be exploited)
            const token = jwt.sign(
                { id: user.id, username: user.username, role: user.role },
                JWT_SECRET,
                { algorithm: 'HS256', expiresIn: '24h' }
            );

            return res.status(200).json({
                success: true,
                message: 'Login successful',
                token: token,
                user: {
                    id: user.id,
                    username: user.username,
                    role: user.role,
                    email: user.email
                }
            });
        } catch (error) {
            return res.status(500).json({ error: error.message });
        }
    }

    // VERIFY TOKEN - Check JWT validity
    if (action === 'verify' && req.method === 'POST') {
        const { token } = req.body;

        if (!token) {
            return res.status(400).json({ error: 'Token required' });
        }

        try {
            // ❌ VULNERABILITY: Accepts algorithm "none"
            const decoded = jwt.verify(token, JWT_SECRET, {
                algorithms: ['HS256', 'none'] // This allows algorithm confusion attacks!
            });

            return res.status(200).json({
                success: true,
                user: decoded,
                vulnerability: 'Algorithm confusion enabled - try algorithm: none'
            });
        } catch (error) {
            return res.status(401).json({
                error: 'Invalid token',
                message: error.message
            });
        }
    }

    // REQUEST PASSWORD RESET - Generate reset token
    if (action === 'reset-request' && req.method === 'POST') {
        const { username } = req.body;

        if (!username) {
            return res.status(400).json({ error: 'Username required' });
        }

        try {
            const users = await getUsers();

            // ❌ VULNERABILITY: Second-order SQL injection
            // Username was stored as-is during registration (e.g., "admin'--")
            // Now when we query it, the malicious payload executes!

            // Simulate vulnerable query:
            // SELECT * FROM users WHERE username = 'admin'--'
            const query = `SELECT * FROM users WHERE username = '${username}'`;
            console.log('[VULNERABLE QUERY]', query);

            // Find user (simulating SQL result)
            let user = users.find(u => u.username === username);

            // If username contains SQL injection, bypass the search
            if (username.includes("'") && (username.includes('--') || username.includes('#'))) {
                const cleanUsername = username.split("'")[0];
                user = users.find(u => u.username === cleanUsername);
            }

            if (!user) {
                // Don't reveal if user exists (security best practice)
                return res.status(200).json({
                    success: true,
                    message: 'If user exists, reset link has been sent'
                });
            }

            // ❌ VULNERABILITY: Predictable token generation
            const resetToken = Buffer.from(`${user.id}-${Date.now()}`).toString('base64');
            const resetTokenExpiry = Date.now() + 3600000; // 1 hour

            user.resetToken = resetToken;
            user.resetTokenExpiry = resetTokenExpiry;

            await updateUsers(users);

            return res.status(200).json({
                success: true,
                message: 'Password reset link sent',
                debug: {
                    token: resetToken, // ❌ Exposed for testing
                    expiry: new Date(resetTokenExpiry).toISOString(),
                    vulnerability: 'Token is predictable: base64(userId-timestamp)'
                }
            });
        } catch (error) {
            return res.status(500).json({ error: error.message });
        }
    }

    // RESET PASSWORD - Use reset token
    if (action === 'reset-password' && req.method === 'POST') {
        const { token, newPassword } = req.body;

        if (!token || !newPassword) {
            return res.status(400).json({ error: 'Token and new password required' });
        }

        try {
            const users = await getUsers();
            const user = users.find(u =>
                u.resetToken === token &&
                u.resetTokenExpiry > Date.now()
            );

            if (!user) {
                return res.status(401).json({ error: 'Invalid or expired token' });
            }

            // Hash new password
            user.password = await bcrypt.hash(newPassword, 10);
            user.resetToken = null;
            user.resetTokenExpiry = null;

            await updateUsers(users);

            return res.status(200).json({
                success: true,
                message: 'Password reset successful'
            });
        } catch (error) {
            return res.status(500).json({ error: error.message });
        }
    }

    return res.status(400).json({ error: 'Invalid action' });
}
