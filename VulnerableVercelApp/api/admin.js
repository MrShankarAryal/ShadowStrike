import { getUsers, getArticles, updateUsers } from './db.js';

export default async function handler(req, res) {
    // VULNERABILITY: No authentication check
    // In a real app, this should require admin authentication
    // But we intentionally leave it open for SQLi bypass testing

    const { action, userId, newRole } = req.query;

    try {
        if (action === 'users') {
            const users = await getUsers();
            return res.status(200).json({
                success: true,
                users,
                warning: 'Admin panel accessible without authentication!'
            });
        }

        if (action === 'articles') {
            const articles = await getArticles();
            return res.status(200).json({
                success: true,
                articles
            });
        }

        if (action === 'promote' && userId && newRole) {
            // VULNERABILITY: No input validation
            const users = await getUsers();
            const user = users.find(u => u.id == userId);

            if (user) {
                user.role = newRole; // Direct assignment without validation
                await updateUsers(users);

                return res.status(200).json({
                    success: true,
                    message: `User ${user.username} promoted to ${newRole}`,
                    user
                });
            }
        }

        // Default: Show admin panel info
        return res.status(200).json({
            success: true,
            message: 'Admin Panel',
            endpoints: {
                users: '/api/admin?action=users',
                articles: '/api/admin?action=articles',
                promote: '/api/admin?action=promote&userId=2&newRole=admin'
            },
            vulnerability: 'This panel should require authentication but does not!'
        });

    } catch (error) {
        return res.status(500).json({
            error: 'Server error',
            message: error.message
        });
    }
}
