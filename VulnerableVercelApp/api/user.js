import { getUsers } from './db.js';
import jwt from 'jsonwebtoken';

const JWT_SECRET = 'vulnerable-secret-key-12345';

export default async function handler(req, res) {
    const { id } = req.query;

    // ❌ VULNERABILITY: No authentication required!
    // ❌ VULNERABILITY: IDOR - can access any user's profile by changing ID

    if (req.method === 'GET') {
        try {
            const users = await getUsers();

            // If ID is provided, return specific user
            if (id) {
                const user = users.find(u => u.id == id);

                if (!user) {
                    return res.status(404).json({ error: 'User not found' });
                }

                // ❌ VULNERABILITY: Returns sensitive data without authorization check
                return res.status(200).json({
                    success: true,
                    user: {
                        id: user.id,
                        username: user.username,
                        email: user.email,
                        role: user.role,
                        resetToken: user.resetToken, // ❌ Exposes reset token!
                        createdAt: user.createdAt || new Date().toISOString()
                    },
                    vulnerability: 'IDOR: No authorization check - try different IDs!'
                });
            }

            // If no ID, return all users (even worse!)
            return res.status(200).json({
                success: true,
                users: users.map(u => ({
                    id: u.id,
                    username: u.username,
                    email: u.email,
                    role: u.role
                })),
                count: users.length,
                vulnerability: 'Information disclosure: All users exposed!'
            });

        } catch (error) {
            return res.status(500).json({ error: error.message });
        }
    }

    // UPDATE user profile (with weak authorization)
    if (req.method === 'PUT') {
        const { token, newRole } = req.body;

        if (!id) {
            return res.status(400).json({ error: 'User ID required' });
        }

        try {
            // ✅ Requires token (looks secure)
            if (!token) {
                return res.status(401).json({ error: 'Authentication required' });
            }

            // ❌ VULNERABILITY: Accepts algorithm "none"
            const decoded = jwt.verify(token, JWT_SECRET, {
                algorithms: ['HS256', 'none']
            });

            const users = await getUsers();
            const user = users.find(u => u.id == id);

            if (!user) {
                return res.status(404).json({ error: 'User not found' });
            }

            // ❌ VULNERABILITY: No check if decoded.id === id
            // Anyone can update anyone's profile!

            if (newRole) {
                user.role = newRole; // ❌ Direct role assignment without validation
            }

            await updateUsers(users);

            return res.status(200).json({
                success: true,
                message: 'User updated',
                user: {
                    id: user.id,
                    username: user.username,
                    role: user.role
                },
                vulnerability: 'Privilege escalation: No ownership check!'
            });

        } catch (error) {
            return res.status(401).json({ error: 'Invalid token' });
        }
    }

    return res.status(405).json({ error: 'Method not allowed' });
}
